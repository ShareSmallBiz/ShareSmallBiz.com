using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ShareSmallBiz.Infrastructure.Data;

/// <summary>Stores <see cref="DateTimeOffset"/> as UTC ticks so SQLite can index, compare and sort it.</summary>
internal sealed class DateTimeOffsetTicksConverter() : ValueConverter<DateTimeOffset, long>(
    value => value.UtcTicks,
    ticks => new DateTimeOffset(ticks, TimeSpan.Zero))
{
    public static readonly DateTimeOffsetTicksConverter Instance = new();
}

internal sealed class NullableDateTimeOffsetTicksConverter() : ValueConverter<DateTimeOffset?, long?>(
    value => value.HasValue ? value.Value.UtcTicks : null,
    ticks => ticks.HasValue ? new DateTimeOffset(ticks.Value, TimeSpan.Zero) : null)
{
    public static readonly NullableDateTimeOffsetTicksConverter Instance = new();
}
