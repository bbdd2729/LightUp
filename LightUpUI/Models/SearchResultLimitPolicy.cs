namespace LightUpUI.Models;

public static class SearchResultLimitPolicy
{
    public const int MinimumLimit = 1;
    public const int DefaultLimit = 30;
    public const int MaximumLimit = 100;
    public const int DefaultQueryLimit = 10;

    public static int Normalize(int configuredLimit)
        => configuredLimit < MinimumLimit
            ? MinimumLimit
            : configuredLimit > MaximumLimit
                ? MaximumLimit
                : configuredLimit;

    public static int GetVisibleResultLimit(int configuredLimit, bool isEmptyQuery)
    {
        var normalizedLimit = Normalize(configuredLimit);
        return isEmptyQuery
            ? System.Math.Min(DefaultQueryLimit, normalizedLimit)
            : normalizedLimit;
    }
}
