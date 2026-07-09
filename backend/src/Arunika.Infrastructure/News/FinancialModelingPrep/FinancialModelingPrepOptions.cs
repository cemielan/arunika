namespace Arunika.Infrastructure.News.FinancialModelingPrep;

public class FinancialModelingPrepOptions
{
    public const string SectionName = "FinancialModelingPrep";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://financialmodelingprep.com";
}
