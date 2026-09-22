namespace Wizard.Core;

/// <summary>
/// The month-window arithmetic the SponsorCheck verifier applies to every <c>...Until</c> value
/// (copied from SponsorCheck.Web's MonthBound): a claim is valid through the end of the month it names,
/// and a capped claim may name at most <c>maxTermMonths</c> past the build month. Calendar-field
/// arithmetic rather than AddMonths, as the verifier does it.
/// </summary>
public static class MonthBound
{
    public static bool TryParse(string? value, out int year, out int month)
    {
        year = 0;
        month = 0;
        var trimmed = value?.Trim() ?? "";
        if (!DateTime.TryParseExact(trimmed, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return false;
        }

        year = parsed.Year;
        month = parsed.Month;
        return true;
    }

    /// <summary>True once <paramref name="today"/> is past the named month.</summary>
    public static bool IsExpired(string? value, Date today) =>
        TryParse(value, out var year, out var month) &&
        (today.Year > year || (today.Year == year && today.Month > month));

    /// <summary>True when the named month is past the ceiling a claim capped at <paramref name="maxTermMonths"/> may reach.</summary>
    public static bool IsBeyondCeiling(string? value, Date today, int maxTermMonths)
    {
        if (!TryParse(value, out var year, out var month))
        {
            return false;
        }

        var (ceilingYear, ceilingMonth) = AddMonths(today, maxTermMonths);
        return year > ceilingYear || (year == ceilingYear && month > ceilingMonth);
    }

    /// <summary>The month <paramref name="months"/> after <paramref name="today"/>, as yyyy-MM.</summary>
    public static string Ceiling(Date today, int months)
    {
        var (year, month) = AddMonths(today, months);
        return $"{year:0000}-{month:00}";
    }

    static (int Year, int Month) AddMonths(Date today, int months)
    {
        var total = today.Year * 12 + (today.Month - 1) + months;
        return (total / 12, total % 12 + 1);
    }
}
