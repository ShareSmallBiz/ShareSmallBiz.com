using Microsoft.EntityFrameworkCore;

namespace ShareSmallBiz.Api.Common;

/// <summary>The one paged-list shape every list endpoint returns.</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public static class Paging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    /// <summary>Counts and fetches one page of an already-ordered query.</summary>
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> orderedQuery, int page, int pageSize, CancellationToken cancellationToken)
    {
        var total = await orderedQuery.CountAsync(cancellationToken);
        var items = await orderedQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResponse<T>(items, page, pageSize, total, totalPages);
    }
}
