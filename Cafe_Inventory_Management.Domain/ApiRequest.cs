namespace Cafe_Inventory_Management.Domain;
public record ApiRequest(HttpMethod method, string url, object? requestBody = default!, string? token = default!);
