namespace UKHO.Search.Ingestion;

public static class IngestionRequestExtensions
{
    public static bool TryGetString(this IngestionRequest request, string name, out string? value)
    {
        return TryGet(request, name, IngestionPropertyType.String, out value);
    }

    public static bool TryGetId(this IngestionRequest request, string name, out string? value)
    {
        return TryGet(request, name, IngestionPropertyType.Id, out value);
    }

    public static bool TryGetInt64(this IngestionRequest request, string name, out long value)
    {
        if (TryGet(request, name, IngestionPropertyType.Integer, out var v) && v is long l)
        {
            value = l;
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryGetDouble(this IngestionRequest request, string name, out double value)
    {
        if (TryGet(request, name, IngestionPropertyType.Double, out var v) && v is double d)
        {
            value = d;
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryGetDecimal(this IngestionRequest request, string name, out decimal value)
    {
        if (TryGet(request, name, IngestionPropertyType.Decimal, out var v) && v is decimal d)
        {
            value = d;
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryGetBoolean(this IngestionRequest request, string name, out bool value)
    {
        if (TryGet(request, name, IngestionPropertyType.Boolean, out var v) && v is bool b)
        {
            value = b;
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryGetDateTimeOffset(this IngestionRequest request, string name, out DateTimeOffset value)
    {
        if (TryGet(request, name, IngestionPropertyType.DateTime, out var v) && v is DateTimeOffset dto)
        {
            value = dto;
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryGetTimeSpan(this IngestionRequest request, string name, out TimeSpan value)
    {
        if (TryGet(request, name, IngestionPropertyType.TimeSpan, out var v) && v is TimeSpan ts)
        {
            value = ts;
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryGetGuid(this IngestionRequest request, string name, out Guid value)
    {
        if (TryGet(request, name, IngestionPropertyType.Guid, out var v) && v is Guid g)
        {
            value = g;
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryGetUri(this IngestionRequest request, string name, out Uri? value)
    {
        if (TryGet(request, name, IngestionPropertyType.Uri, out var v) && v is Uri uri)
        {
            value = uri;
            return true;
        }

        value = null;
        return false;
    }

    public static bool TryGetStringArray(this IngestionRequest request, string name, out string[]? value)
    {
        if (TryGet(request, name, IngestionPropertyType.StringArray, out var v) && v is string[] arr)
        {
            value = arr;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryGet<T>(IngestionRequest request, string name, IngestionPropertyType type, out T? value)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(name);

        var properties = request.Properties ?? throw new InvalidOperationException("IngestionRequest.Properties cannot be null.");

        var match = properties.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (match is null || match.Type != type)
        {
            value = default;
            return false;
        }

        if (match.Value is T v)
        {
            value = v;
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryGet(IngestionRequest request, string name, IngestionPropertyType type, out object? value)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(name);

        var properties = request.Properties ?? throw new InvalidOperationException("IngestionRequest.Properties cannot be null.");

        var match = properties.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (match is null || match.Type != type)
        {
            value = null;
            return false;
        }

        value = match.Value;
        return true;
    }
}
