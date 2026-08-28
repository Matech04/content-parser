using System.Runtime.CompilerServices;

// Endpointy sa `internal` - testy jednostkowe musza widziec ich handlery.
[assembly: InternalsVisibleTo("ContentParser.Api.Tests")]
