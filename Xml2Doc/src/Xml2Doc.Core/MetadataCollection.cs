using System.Collections;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Xml2Doc.Core;

/// <summary>
/// Provides an immutable, ordinally keyed snapshot of deterministic metadata values.
/// </summary>
/// <remarks>
/// Values may be <see langword="null"/>, strings, booleans, numeric values, dates, enums, or
/// recursively nested lists of those scalar values. Object-valued metadata is not supported.
/// </remarks>
public sealed class MetadataCollection :
    IReadOnlyDictionary<string, object?>,
    IEquatable<MetadataCollection>
{
    private readonly IReadOnlyDictionary<string, object?> _values;

    /// <summary>Gets an empty metadata collection.</summary>
    public static MetadataCollection Empty { get; } = new(
        Array.Empty<KeyValuePair<string, object?>>());

    /// <summary>Creates an immutable snapshot of caller-supplied metadata.</summary>
    /// <param name="values">Metadata values keyed using ordinal semantics.</param>
    public MetadataCollection(
        IEnumerable<KeyValuePair<string, object?>> values)
    {
        if (values is null)
            throw new ArgumentNullException(nameof(values));

        var snapshot = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach (var pair in values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
                throw new ArgumentException("Metadata keys cannot be empty.", nameof(values));
            if (snapshot.ContainsKey(pair.Key))
            {
                throw new ArgumentException(
                    $"Metadata key '{pair.Key}' is duplicated.",
                    nameof(values));
            }

            snapshot.Add(pair.Key, FreezeValue(pair.Value));
        }

        _values = new ReadOnlyDictionary<string, object?>(snapshot);
    }

    /// <summary>Parses a JSON object into an immutable metadata collection.</summary>
    /// <param name="json">JSON object containing scalar or list metadata values.</param>
    /// <returns>The parsed immutable metadata collection.</returns>
    public static MetadataCollection ParseJson(string json)
    {
        if (json is null)
            throw new ArgumentNullException(nameof(json));

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Metadata JSON must contain an object.", nameof(json));

        return new MetadataCollection(
            document.RootElement
                .EnumerateObject()
                .Select(property => new KeyValuePair<string, object?>(
                    property.Name,
                    ConvertJsonValue(property.Value))));
    }

    /// <inheritdoc />
    public int Count => _values.Count;

    /// <inheritdoc />
    public IEnumerable<string> Keys => _values.Keys;

    /// <inheritdoc />
    public IEnumerable<object?> Values => _values.Values;

    /// <inheritdoc />
    public object? this[string key] => _values[key];

    /// <inheritdoc />
    public bool ContainsKey(string key) => _values.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(string key, out object? value) =>
        _values.TryGetValue(key, out value);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() =>
        _values.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public bool Equals(MetadataCollection? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other is null || Count != other.Count)
            return false;

        return this.Zip(other, (left, right) =>
                StringComparer.Ordinal.Equals(left.Key, right.Key) &&
                ValuesEqual(left.Value, right.Value))
            .All(equal => equal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is MetadataCollection other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var pair in this)
            {
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(pair.Key);
                hash = (hash * 31) + ValueHashCode(pair.Value);
            }
            return hash;
        }
    }

    private static object? FreezeValue(object? value)
    {
        if (value is JsonElement jsonElement)
            return FreezeValue(ConvertJsonValue(jsonElement));
        if (value is null ||
            value is string ||
            value is bool ||
            value is byte ||
            value is sbyte ||
            value is short ||
            value is ushort ||
            value is int ||
            value is uint ||
            value is long ||
            value is ulong ||
            value is decimal ||
            value is DateTime ||
            value is DateTimeOffset ||
            value is Enum)
        {
            return value;
        }

        if (value is float single)
        {
            if (float.IsNaN(single) || float.IsInfinity(single))
                throw UnsupportedValue(value);
            return single;
        }

        if (value is double @double)
        {
            if (double.IsNaN(@double) || double.IsInfinity(@double))
                throw UnsupportedValue(value);
            return @double;
        }

        if (value is IEnumerable sequence)
        {
            var items = new List<object?>();
            foreach (var item in sequence)
                items.Add(FreezeValue(item));
            return new ReadOnlyCollection<object?>(items);
        }

        throw UnsupportedValue(value);
    }

    private static object? ConvertJsonValue(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.String:
                return value.GetString();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Number:
                if (value.TryGetInt64(out var integer))
                    return integer;
                if (value.TryGetDecimal(out var decimalValue))
                    return decimalValue;
                return value.GetDouble();
            case JsonValueKind.Array:
                return new ReadOnlyCollection<object?>(
                    value.EnumerateArray().Select(ConvertJsonValue).ToList());
            default:
                throw new ArgumentException(
                    $"Unsupported metadata JSON value kind '{value.ValueKind}'.");
        }
    }

    private static bool ValuesEqual(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is IEnumerable<object?> leftItems &&
            right is IEnumerable<object?> rightItems)
        {
            return leftItems.Zip(rightItems, ValuesEqual).All(equal => equal) &&
                leftItems.Count() == rightItems.Count();
        }

        return object.Equals(left, right);
    }

    private static int ValueHashCode(object? value)
    {
        if (value is null)
            return 0;
        if (value is IEnumerable<object?> items)
        {
            unchecked
            {
                var hash = 17;
                foreach (var item in items)
                    hash = (hash * 31) + ValueHashCode(item);
                return hash;
            }
        }

        return value.GetHashCode();
    }

    private static ArgumentException UnsupportedValue(object value) =>
        new(
            $"Unsupported metadata value type '{value.GetType().FullName}'. " +
            "Only deterministic scalar and list values are supported.");
}
