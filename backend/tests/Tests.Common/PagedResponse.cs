namespace Tests.Common;

/// <summary>
/// Mirrors the shape of <c>PagedResult&lt;T&gt;</c> returned by paginated API endpoints.
/// Used only for deserializing integration-test HTTP responses.
/// </summary>
public sealed class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}
