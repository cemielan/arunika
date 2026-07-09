namespace Arunika.Api.Contracts;

/// <summary>
/// Standard success envelope for every <c>/v1</c> endpoint (design doc §7):
/// <c>{ "data": ..., "meta": {...} }</c>.
/// </summary>
public sealed record ApiResponse<TData>(TData Data, object? Meta = null);

/// <summary>
/// Standard error envelope (design doc §7): <c>{ "error": { "code": "...", "message": "..." } }</c>.
/// </summary>
public sealed record ApiErrorEnvelope(ApiErrorDetail Error);

public sealed record ApiErrorDetail(string Code, string Message);

/// <summary>
/// Pagination metadata attached to list responses.
/// </summary>
public sealed record PageMeta(int Page, int PageSize, int TotalItems, int TotalPages);
