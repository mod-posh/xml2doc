using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Xml2Doc.Core.Templates;

internal static class YamlFrontMatter
{
    internal static string Serialize(
        IReadOnlyDictionary<string, object?> values)
    {
        var builder = new StringBuilder();
        builder.AppendLine("---");

        foreach (var pair in values.OrderBy(
                     pair => pair.Key,
                     StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
                throw new ArgumentException("Front-matter keys cannot be empty.");

            builder
                .Append(FormatKey(pair.Key))
                .Append(": ")
                .AppendLine(FormatValue(pair.Value));
        }

        builder.Append("---");
        return builder.ToString();
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
            return "null";
        if (value is string text)
            return FormatString(text);
        if (value is bool boolean)
            return boolean ? "true" : "false";
        if (value is DateTime dateTime)
            return FormatString(dateTime.ToString("O", CultureInfo.InvariantCulture));
        if (value is DateTimeOffset dateTimeOffset)
            return FormatString(dateTimeOffset.ToString("O", CultureInfo.InvariantCulture));
        if (value is Enum)
            return FormatString(value.ToString()!);
        if (value is IEnumerable sequence)
        {
            var items = sequence.Cast<object?>().Select(FormatValue);
            return "[" + string.Join(", ", items) + "]";
        }
        if (value is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture) ?? "null";

        throw new ArgumentException(
            $"Unsupported front-matter value type '{value.GetType().FullName}'.");
    }

    private static string FormatString(string value) =>
        JsonSerializer.Serialize(value);

    private static string FormatKey(string value) =>
        Regex.IsMatch(value, @"^[A-Za-z_][A-Za-z0-9_-]*$")
            ? value
            : FormatString(value);
}
