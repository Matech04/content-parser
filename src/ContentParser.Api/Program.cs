using ContentParser.Api.Endpoints;

using ContentParser.Core.Parsers;
using ContentParser.Core.Parsers.Options;
using ContentParser.Core.Parsers.Services;

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
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});

app.UseStatusCodePages();
app.UseRateLimiter();
app.MapApiEndpoints();

app.Run();

public partial class Program;
