namespace ContentParser.Api.Contracts.V1;

/// <summary>
/// Wlasciwosci sa nullowalne swiadomie: NRT nie sa egzekwowane w runtime, wiec brakujace
/// pole w JSON i tak trafi tu jako null. Lepiej to obsluzyc niz udawac, ze nie moze wystapic.
/// </summary>
public sealed record ParseContentRequestDto(string? Type, string? Content);
