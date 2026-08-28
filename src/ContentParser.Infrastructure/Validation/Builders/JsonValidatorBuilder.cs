using System.Text.Json.Nodes;

using ContentParser.Parser.Validation.Specifications;

namespace ContentParser.Parser.Validation.Builders;

/// <summary>
/// Sklada zestaw specyfikacji w jedna zlozona regule. Powtorzone wywolanie tej samej
/// metody nadpisuje wczesniejsza specyfikacje, wiec nie da sie zdublowac bledow.
/// </summary>
public sealed class JsonValidatorBuilder
{
    // Dictionary zachowuje kolejnosc wstawiania przy braku usuwania — bledy wychodza
    // w kolejnosci deklaracji regul, co jest wazne dla czytelnosci odpowiedzi.
    private readonly Dictionary<Type, Specification<JsonNode>> _specifications = [];

    public JsonValidatorBuilder EnsureIsArray() =>
        Add(new IsJsonArraySpecification());

    public JsonValidatorBuilder EnsureAllElementsAreObjects() =>
        Add(new AllElementsAreObjectsSpecification());

    public JsonValidatorBuilder EnsureAllValuesAreFlat() =>
        Add(new AllValuesAreFlatSpecification());

    public JsonValidatorBuilder EnsurePropertyNamesAreNotEmpty() =>
        Add(new PropertyNamesAreNotEmptySpecification());

    public JsonValidatorBuilder EnsureKeysAreUniform() =>
        Add(new UniformKeysSpecification());

    public JsonValidatorBuilder EnsureAtMostRecords(int maxRecords) =>
        Add(new MaxRecordCountSpecification(maxRecords));

    private JsonValidatorBuilder Add(Specification<JsonNode> specification)
    {
        _specifications[specification.GetType()] = specification;
        return this;
    }

    /// <summary>
    /// Zbudowanie walidatora bez regul jest bledem programisty, nie uzytkownika —
    /// zaden request tego nie wywola, wiec wyjatek jest wlasciwa reakcja.
    /// </summary>
    public Specification<JsonNode> Build()
    {
        if (_specifications.Count == 0)
        {
            throw new InvalidOperationException("No specification was configured.");
        }

        return _specifications.Values.Aggregate(static (left, right) => left.And(right));
    }
}
