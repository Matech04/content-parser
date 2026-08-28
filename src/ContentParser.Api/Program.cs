using ContentParser.Api.Endpoints;

using ContentParser.Parser.Parsers;
using ContentParser.Parser.Parsers.Options;
using ContentParser.Parser.Parsers.Services;

using Microsoft.Extensions.Options;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services
    .AddOptions<ParsingOptions>()
    .Bind(builder.Configuration.GetSection(ParsingOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<ParsingOptions>, ParsingOptionsValidator>();

builder.Services.AddSingleton<IContentParser, InternalJsonParser>();
builder.Services.AddSingleton<IContentParser, CsvParser>();
builder.Services.AddSingleton<Base64Decoder>();
builder.Services.AddSingleton<ContentParsingService>();

builder.Services.AddApiRateLimiter();

builder.WebHost.ConfigureKestrel((context, options) =>
{
    // Jedno zrodlo prawdy: ten sam obiekt opcji, ktory dostaje reszta aplikacji.
    var parsing = context.Configuration
        .GetSection(ParsingOptions.SectionName)
        .Get<ParsingOptions>() ?? new ParsingOptions();

    options.Limits.MaxRequestBodySize = parsing.MaxRequestBodyBytes;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    // Bledy bindowania ciala rzucaja BadHttpRequestException (m.in. 400 i 413).
    // Domyslny handler zwrocilby 500 — mapujemy status z wyjatku, zeby Dev i Prod byly spojne.
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});

app.UseStatusCodePages();
app.UseRateLimiter();
app.MapApiEndpoints();

app.Run();

/// <summary>Widoczne dla testow integracyjnych opartych o WebApplicationFactory.</summary>
public partial class Program;
