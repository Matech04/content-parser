# ContentParser

Generyczny parser danych przesyłanych przez API — ASP.NET Core Minimal API (.NET 10).

Endpoint `POST /api/v1/parse-content` przyjmuje ładunek JSON z treścią zakodowaną Base64,
dekoduje ją i parsuje jako **CSV** albo **INTERNAL_JSON**, zwracając wynik w jednej,
wspólnej dla obu formatów strukturze.

---

## Uruchomienie

Wymagany **.NET SDK 10.0** (`dotnet --version`).

```bash
git clone <adres-repozytorium>
cd content-parser
dotnet run --project src/ContentParser.Api
```

Aplikacja startuje na `http://localhost:5076`.
W trybie Development dostępna jest dokumentacja OpenAPI: <http://localhost:5076/scalar/v1>.

Testy:

```bash
dotnet test
```

Gotowe przykłady żądań (również błędnych) znajdują się w `src/ContentParser.Api/ContentParser.Api.http`.

---

## Kontrakt API

### Żądanie

```http
POST /api/v1/parse-content
Content-Type: application/json

{
  "type": "CSV" | "INTERNAL_JSON",
  "content": "<dane zakodowane Base64>"
}
```

Nagłówek `Content-Type: application/json` jest wymagany — inny daje `415 Unsupported Media Type`.

### Odpowiedź `200 OK`

```json
{
  "status": "Success",
  "processedCount": 2,
  "data": [
    { "id": "1", "name": "Anna" },
    { "id": "2", "name": "Piotr" }
  ]
}
```

`data` to zawsze tablica płaskich obiektów `nazwa pola → wartość`. **Ten sam zestaw danych
przesłany jako CSV i jako INTERNAL_JSON daje bajt w bajt identyczną odpowiedź** — pilnuje
tego test `BothTypes_ProduceIdenticalResponseBody`.

### Odpowiedź błędna — RFC 9457 Problem Details

```json
{
  "type": "urn:content-parser:error:csv-row-field-count",
  "title": "Validation Error",
  "status": 422,
  "detail": "Row 2 has 1 field(s) but the header declares 2.",
  "errors": [
    { "code": "csv-row-field-count", "message": "Row 2 has 1 field(s) but the header declares 2." },
    { "code": "csv-row-field-count", "message": "Row 3 has 1 field(s) but the header declares 2." }
  ]
}
```

Status wyznacza pierwszy błąd, ale **pole `errors` zawiera komplet** — walidacja nie zatrzymuje
się na pierwszym problemie, żeby klient nie musiał odbijać się od API po jednym błędzie.

### Statusy

| Status | Kiedy | Przykładowe kody |
|---|---|---|
| `200 OK` | sparsowano poprawnie | — |
| `400 Bad Request` | żądanie źle sformułowane | `type-missing`, `unsupported-parser` |
| `413 Content Too Large` | treść przekracza limity | `content-too-large`, `too-many-records` |
| `415 Unsupported Media Type` | zły nagłówek `Content-Type` | — |
| `422 Unprocessable Entity` | treść nie spełnia kontraktu formatu | `incorrect-base64`, `json-not-array`, `csv-duplicate-column`, … |
| `429 Too Many Requests` | przekroczony limit zapytań | — |

Podział 400 / 422 jest świadomy: **400 = zła koperta żądania** (nie wiem, co mam zrobić),
**422 = koperta w porządku, ale zawartość nie spełnia kontraktu** (wiem, co mam zrobić,
ale się nie da). Odpowiada to trzem klasom błędów w kodzie: `RequestError`, `ValidationError`,
`TooLargeError`.

---

## Formaty wejściowe

### CSV

Tokenizację wykonuje biblioteka [`CsvHelper`](https://joshclose.github.io/CsvHelper/) w trybie
RFC 4180; walidacja i raportowanie błędów są własne — patrz uzasadnienie niżej.

* pierwszy wiersz to **nagłówek**; jego nazwy stają się nazwami pól,
* pola mogą być cytowane: `"Kowalski, Jan"` — wewnątrz cudzysłowów dozwolone są przecinki
  i znaki nowej linii, a `""` oznacza pojedynczy cudzysłów,
* obsługiwane są zakończenia linii `LF` i `CRLF` (również mieszane),
* nazwy kolumn są przycinane ze spacji, **wartości nie**,
* wiersze całkowicie puste są pomijane (w tym końcowe znaki nowej linii),
* BOM UTF-8 jest usuwany.

Odrzucane jest: brak nagłówka, pusta nazwa kolumny, zduplikowana kolumna, wiersz o innej
liczbie pól niż nagłówek, niedomknięty cudzysłów. Błędy raportowane są z **numerem wiersza
liczonym tak, jak widzi go użytkownik w pliku** (nagłówek = wiersz 1) — numer pochodzi wprost
z parsera, więc puste linie w środku pliku go nie przesuwają.

Konfiguracja `CsvHelper` jest celowo minimalna i każdy jej element odpowiada regule powyżej:

| Ustawienie | Po co |
|---|---|
| `HasHeaderRecord = false` | nagłówek walidujemy sami — biblioteka odrzuciłaby duplikat wyjątkiem, bez kodu błędu i indeksu kolumny |
| `IgnoreBlankLines = true` | puste linie (w tym końcowe) są pomijane |
| `DetectColumnCountChanges = false` | rozjazd liczby pól ma dać `Result` z **kompletem** wierszy, a nie wyjątek na pierwszym |
| `BadDataFound` | biblioteka nie rzuca na złe cytowanie, tylko zgłasza pole — patrz niżej |

Ostatni punkt jest jedynym miejscem, gdzie trzeba było dołożyć logikę: `CsvHelper` zgłasza jako
„bad data" **każde** podejrzane cytowanie, również nieszkodliwy cudzysłów w środku niecytowanego
pola (`ab"cd`), który jest tu poprawną wartością. Błędem jest wyłącznie cudzysłów, który nigdy się
nie zamyka: pole zaczyna się cudzysłowem i zawiera ich nieparzystą liczbę (`""` to escape, więc
liczy się parzyście). Taki błąd może z definicji wystąpić tylko w ostatnim wierszu — niedomknięty
cudzysłów pochłania treść aż do końca pliku — i stamtąd bierze się numer wiersza w komunikacie.

### INTERNAL_JSON

Zadanie nie definiuje tego formatu, więc przyjęto następujący kontrakt:

> **INTERNAL_JSON to tablica płaskich, jednorodnych obiektów.**

```json
[{"id": 1, "name": "Anna"}, {"id": 2, "name": "Piotr"}]
```

Ten kształt jest wybrany celowo: **dokładnie taki sam zbiór danych opisuje CSV**. Bez tego
ograniczenia „ujednolicona struktura wyjściowa" z treści zadania byłaby niemożliwa — nie da
się odwzorować zagnieżdżonego JSON-a na tabelę.

Walidacja (każda reguła to osobna specyfikacja):

| Reguła | Błąd |
|---|---|
| korzeń jest tablicą | `json-not-array` |
| każdy element jest obiektem | `json-element-not-object` |
| nazwy pól nie są puste | `json-property-name-empty` |
| wartości są skalarne (bez zagnieżdżeń) | `json-value-nested` |
| wszystkie rekordy mają ten sam zestaw pól, w tej samej kolejności | `json-keys-not-uniform` |
| liczba rekordów mieści się w limicie | `too-many-records` |

Wartości sprowadzane są do tekstu, bo CSV nie zna typów. `null` w JSON pozostaje `null`.

---

## Konfiguracja

Sekcja `Parsing` w `appsettings.json`:

| Klucz | Domyślnie | Znaczenie |
|---|---|---|
| `MaxDecodedContentBytes` | `5242880` (5 MiB) | limit treści **po** zdekodowaniu Base64 |
| `MaxRequestBodyBytes` | `8388608` (8 MiB) | limit całego ciała HTTP (Kestrel) |
| `MaxRecords` | `100000` | maksymalna liczba rekordów |

Dwa osobne limity są konieczne: Base64 zwiększa rozmiar o ~33%, a do tego dochodzi koperta
JSON. Gdyby oba były równe, Kestrel odrzucałby żądanie **zanim** zadziała limit treści, a kod
sprawdzający rozmiar byłby martwy. Pilnuje tego `ParsingOptionsValidator` z `ValidateOnStart()` —
błędna konfiguracja przerywa start aplikacji, zamiast ujawnić się przy pierwszym żądaniu.

---

## Architektura

```
src/
  ContentParser.Api/               warstwa HTTP — kontrakty, endpointy, mapowanie na Problem Details
  ContentParser.Core/              logika parsowania — bez żadnej zależności od ASP.NET
tests/
  ContentParser.Api.Tests/         testy jednostkowe + integracyjne (WebApplicationFactory)
  ContentParser.Core.Tests/        testy parserów, specyfikacji i typu Result
```

Nazwa `Core`, a nie `Infrastructure`, jest celowa: w słowniku warstw *infrastructure* to adaptery
do świata zewnętrznego (baza, kolejka, klient HTTP). Tu mieszka logika domenowa, więc nazwanie jej
„infrastrukturą" odwracałoby kierunek zależności. Katalog, plik `.csproj` i `RootNamespace` noszą
tę samą nazwę.

Projekt `Core` **nie zna HTTP**. Dzięki temu logikę parsowania da się hostować
w konsoli, workerze albo teście bez frameworka webowego.

**W kodzie nie ma komentarzy.** Nazwy typów, metod i testów mają nieść znaczenie same z siebie,
a każde „dlaczego", które nie mieści się w nazwie, opisane jest w tym README — poniższe sekcje są
jedynym miejscem, w którym trzymane jest uzasadnienie decyzji projektowych.

### Zastosowane wzorce

**Strategy + Registry** — `IContentParser` z `Type` i słownikiem `FrozenDictionary`
w `ContentParsingService`. Dodanie formatu to jedna linia w `Program.cs` i nowa klasa;
żaden istniejący plik się nie zmienia.

> Świadome odstępstwo od treści zadania: `type` jest stringiem, nie enumem. Enum zamyka zbiór
> typów w kodzie — dodanie formatu wymagałoby zmiany w dwóch miejscach. Cena tej decyzji jest
> realna: OpenAPI nie wylistuje dozwolonych wartości i trzeba samodzielnie obsłużyć brak pola
> (`type-missing`). Uznano, że rozszerzalność jest tu ważniejsza.

**Specification** — reguły walidacji JSON jako osobne klasy z pełną algebrą Boole'a:
`And`, `Or`, `Not`. Każda specyfikacja zwraca `Result`, więc niesie jednocześnie **werdykt**
(spełniona / niespełniona) i **uzasadnienie** (lista błędów). Te dwie warstwy rządzą się różnymi
prawami i warto je rozdzielić w głowie:

| Operator | Werdykt | Błędy |
|---|---|---|
| `a.And(b)` | spełnione, gdy oba | suma błędów obu stron |
| `a.Or(b)` | spełnione, gdy którekolwiek | suma błędów — **tylko** gdy oba zawiodą |
| `a.Not(error)` | spełnione, gdy `a` **nie** jest spełnione | podany `error`; błędy `a` są odrzucane |

Na poziomie werdyktu jest to zwykła algebra Boole'a i **jest to sprawdzone testami**:
prawa De Morgana, podwójna negacja, przemienność, łączność, wyłączony środek i niesprzeczność
(`SpecificationLawsTests`). Na poziomie błędów obowiązują dwie asymetryczne decyzje, obie wynikające
z jednej zasady — *licz tylko te obliczenia, których wynik może trafić do odpowiedzi*:

* **`And` nie skraca obliczeń.** Gdy lewa strona zawiedzie, prawa i tak jest liczona, bo jej błędy
  trafią do odpowiedzi. Dzięki temu klient dostaje komplet naruszeń, a nie pierwsze z brzegu.
* **`Or` skraca obliczenia.** Gdy lewa strona przejdzie, całość przechodzi i błędy prawej nigdy
  nie zostałyby pokazane — liczenie ich byłoby czystym kosztem. Werdykt jest identyczny w obie strony.

**`Not` przyjmuje `Error` i to jest świadomy koszt.** Negacja reguły „wszystkie wartości są płaskie"
to „któraś wartość jest zagnieżdżona" — takiego komunikatu nie da się wyprowadzić automatycznie
z reguły pierwotnej. Alternatywą byłoby zmuszenie każdej specyfikacji do deklarowania błędu
negacji z góry, czyli wymyślanie sześciu komunikatów, z których w tej domenie nie użyto by ani
jednego. Powód negacji zna miejsce jej użycia, więc to ono go podaje.

> Kontrakt INTERNAL_JSON jest czystą koniunkcją, więc produkcyjny walidator składa się wyłącznie
> z `And`. `Or` i `Not` nie są w nim używane i **nie zostały tam wciśnięte na siłę** — są częścią
> abstrakcji i mają własne testy. To odwrotność sytuacji, w której wzorzec deklaruje komponowalność,
> a dostarcza jednego operatora.

**Prekondycja tablicy jest w typie, nie w `if`-ie.** Reguły elementów to `Specification<JsonArray>`,
a nie `Specification<JsonNode>` — nie mogą więc dostać czegoś, co tablicą nie jest, i nie powtarzają
strażnika `if (węzeł nie jest tablicą) return Ok()`. Podnosi je do poziomu korzenia jeden adapter,
`WhenArraySpecification`: dla węzła niebędącego tablicą jest spełniony w sposób pusty, bo brak
tablicy zgłasza już `IsJsonArraySpecification` i dokładanie do tego „element 3 nie jest obiektem"
byłoby szumem. Logicznie to implikacja `jest tablicą ⇒ reguła`; typowo — zwykłe zawężenie.

**Builder** — `JsonValidatorBuilder` składa zestaw specyfikacji i owija reguły elementów
w `WhenArraySpecification`. Powtórzone wywołanie tej samej metody nadpisuje regułę w miejscu,
więc nie da się zdublować błędów ani przestawić ich kolejności. Reguły trzyma `List`, bo kolejność
jest tu częścią kontraktu — błędy wracają w kolejności deklaracji reguł, a `Dictionary` nie daje
na to żadnej gwarancji. Walidator budowany jest **raz**, w konstruktorze parsera, a nie przy
każdym żądaniu.

**Result** — `Result<T>` zamiast wyjątków dla przewidywalnych porażek.

* `Bind` / `Map` — pozostają w świecie `Result`, przenosząc błąd bez `if`-ów w każdej warstwie:
  `_base64Decoder.TryDecode(content).Bind(parser.TryParse)`,
* `Match` — jedyne wyjście ze świata `Result`, wywoływane raz na żądanie, na granicy HTTP,
* **nie ma publicznego `Value`.** Właściwość, która rzuca wyjątkiem, przemyca wyjątek z powrotem
  do typu stworzonego po to, żeby go uniknąć. Dostęp idzie przez `Match` (kompilator wymusza
  obsługę obu gałęzi) albo `TryGetValue` (idiom `Dictionary.TryGetValue`),
* `Result` jest klasą, nie rekordem — automatyczne `ToString()` rekordu sięgałoby po wartość
  nieistniejącą na ścieżce błędu, a automatyczna równość i tak nie działałaby poprawnie dla listy,
* „porażka bez błędu" jest **niewyrażalna w typie**: `Fail(Error error, params Error[] additional)`
  wymaga co najmniej jednego błędu już na poziomie sygnatury, więc nie trzeba tego pilnować w runtime.

### Wyjątki kontra Result

Podział jest konsekwentny: **`Result` dla porażek spowodowanych przez dane, wyjątek dla porażek
spowodowanych przez kod.** Zły Base64 to normalny element pracy API — ktoś go obsłuży i zwróci 422.
Zbudowanie walidatora bez reguł albo przekazanie `null` do konstruktora `Result` oznacza błąd
programisty: nikt nie ma na to sensownej reakcji, więc właściwa jest głośna awaria ze stack trace'em.

### Decyzje warte odnotowania

* **`CsvHelper` do tokenizacji, własna warstwa walidacji.** Cytowanie, pola wielolinijkowe
  i zakończenia linii to rozwiązany problem — nie ma powodu utrzymywać własnej maszyny stanów.
  Biblioteka zwraca jednak surowe wiersze i wyjątki, więc nagłówek, zgodność liczby pól i limity
  sprawdzane są tutaj, żeby błąd wracał jako `Result` z kodem (`csv-duplicate-column`,
  `csv-row-field-count`, …) i numerem wiersza, a nie jako wyjątek biblioteki.
* **Rate limiting partycjonowany po adresie klienta**, `429` (nie domyślne `503`) i `QueueLimit = 0`.
  Wspólny kubełek dla wszystkich pozwoliłby jednemu klientowi zagłodzić pozostałych, a kolejka
  trzymałaby połączenie otwarte zamiast szybko odmówić.
* **Ścisłe dekodowanie UTF-8.** Domyślnie nieprawidłowe bajty zamieniają się w `U+FFFD`, przez co
  użytkownik dostawał komunikat o znaku, którego nigdy nie wysłał. Teraz to jawny błąd `invalid-utf8`.
* **`ArrayPool` przy dekodowaniu Base64** — bufory do 5 MiB nie trafiają na stertę dużych obiektów.
  Rozmiar sprawdzany jest dwa razy: najpierw po górnym oszacowaniu z długości Base64 (tani filtr,
  jeszcze przed wynajęciem bufora), potem dokładnie — dopiero po dekodowaniu znany jest realny rozmiar.
* **Błędy bindowania ciała żądania mapowane na status z wyjątku.** Kestrel i model binder zgłaszają
  je jako `BadHttpRequestException` (m.in. `400` i `413`); domyślny handler zwróciłby na to `500`,
  i to inaczej w Development niż w Production.

---

## Testy

243 testy: `dotnet test`.

| Projekt | Zakres |
|---|---|
| `ContentParser.Core.Tests` | parsery CSV/JSON, dekoder Base64, każda specyfikacja, prawa algebry `And`/`Or`/`Not`, builder, `Result` |
| `ContentParser.Api.Tests` | mapowanie błędów na Problem Details, endpoint, rejestracja tras, testy integracyjne przez `WebApplicationFactory` |

Handlery endpointów są `internal` i widoczne dla testów przez `InternalsVisibleTo`
(`src/ContentParser.Api/Properties/AssemblyInfo.cs`) — nie muszą być publiczne, żeby dało się
je przetestować jednostkowo.

Testy integracyjne przechodzą przez pełny potok HTTP — bindowanie, negocjację `Content-Type`,
obsługę wyjątków i serializację — więc weryfikują kontrakt, którego testy jednostkowe nie dotykają
(m.in. `415` dla złego nagłówka i `400` zamiast `500` przy braku pola `type`).
