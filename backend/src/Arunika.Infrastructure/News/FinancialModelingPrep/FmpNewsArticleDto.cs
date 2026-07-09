using System.Text.Json.Serialization;

namespace Arunika.Infrastructure.News.FinancialModelingPrep;

/// <summary>
/// Matches the JSON shape returned by FMP's General News API
/// (GET /stable/news/general-latest).
/// </summary>
internal record FmpNewsArticleDto(
    string? Symbol,
    [property: JsonPropertyName("publishedDate")] string? PublishedDate,
    string? Publisher,
    string? Title,
    string? Image,
    string? Site,
    string? Text,
    string? Url
);
