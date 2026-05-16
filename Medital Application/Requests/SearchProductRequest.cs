namespace Medital_Application.Requests;

public class SearchProductRequest
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public int? ManufacturerId { get; set; }
    public bool OnlyInStock { get; set; }
    public int PageNo { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
