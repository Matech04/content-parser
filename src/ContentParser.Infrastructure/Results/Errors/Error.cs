namespace ContentParser.Parser.Results.Errors;

/// <summary>
/// Bazowy blad domenowy. Kategoria (typ pochodny) wyznacza status HTTP w warstwie API —
/// warstwa domenowa nie wie nic o HTTP.
/// </summary>
public abstract record Error(string Code, string Message);
