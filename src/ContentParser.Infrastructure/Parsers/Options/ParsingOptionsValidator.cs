using Microsoft.Extensions.Options;

namespace ContentParser.Parser.Parsers.Options;

/// <summary>
/// Zla konfiguracja to blad operatora, nie uzytkownika — aplikacja nie powinna wstac.
/// Dlatego walidacja przy starcie (ValidateOnStart) i wyjatek, a nie Result.
/// </summary>
public sealed class ParsingOptionsValidator : IValidateOptions<ParsingOptions>
{
    public ValidateOptionsResult Validate(string? name, ParsingOptions options)
    {
        List<string> failures = [];

        if (options.MaxDecodedContentBytes <= 0)
        {
            failures.Add($"{nameof(options.MaxDecodedContentBytes)} must be greater than zero.");
        }

        if (options.MaxRecords <= 0)
        {
            failures.Add($"{nameof(options.MaxRecords)} must be greater than zero.");
        }

        // Base64 rosnie 4/3; bez zapasu Kestrel odrzucalby zadania, zanim zadziala limit tresci.
        var minimumBody = (long)options.MaxDecodedContentBytes * 4 / 3;
        if (options.MaxRequestBodyBytes <= minimumBody)
        {
            failures.Add(
                $"{nameof(options.MaxRequestBodyBytes)} ({options.MaxRequestBodyBytes}) must exceed "
                + $"{nameof(options.MaxDecodedContentBytes)} inflated by Base64 ({minimumBody}).");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
