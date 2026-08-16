using System.Text;
using System.Text.RegularExpressions;

namespace Xml2Doc.Core.AutoLinking;

/// <summary>
/// Links complete identifier tokens while preserving fences, inline code, and existing links.
/// </summary>
public sealed class SimpleAutoLinker : IAutoLinker
{
    private static readonly Regex ProtectedMarkdown = new(
        @"(?:`+[^\r\n]*?`+|!?\[[^\]\r\n]*\]\([^\r\n)]*\))",
        RegexOptions.Compiled);

    /// <summary>The shared stateless instance.</summary>
    public static SimpleAutoLinker Instance { get; } = new();

    private SimpleAutoLinker()
    {
    }

    /// <inheritdoc />
    public string Apply(string markdown, AutoLinkContext context)
    {
        if (markdown is null)
            throw new ArgumentNullException(nameof(markdown));
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        if (markdown.Length == 0 || context.Targets.Count == 0)
            return markdown;

        var targets = context.Targets
            .Where(target =>
                !string.IsNullOrWhiteSpace(target.Label) &&
                !string.IsNullOrWhiteSpace(target.Href))
            .GroupBy(target => target.Label, StringComparer.Ordinal)
            .Where(group => group.Select(target => target.Href)
                .Distinct(StringComparer.Ordinal)
                .Count() == 1)
            .Select(group => group.First())
            .OrderByDescending(target => target.Label.Length)
            .ThenBy(target => target.Label, StringComparer.Ordinal)
            .ToArray();

        if (targets.Length == 0)
            return markdown;

        var byLabel = targets.ToDictionary(
            target => target.Label,
            StringComparer.Ordinal);
        var identifierPattern = new Regex(
            @"(?<![A-Za-z0-9_])(?:" +
            string.Join("|", targets.Select(target => Regex.Escape(target.Label))) +
            @")(?![A-Za-z0-9_])");
        var lines = markdown.Split('\n');
        var output = new StringBuilder(markdown.Length);
        char? fenceMarker = null;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var trimmed = line.TrimStart();
            var marker = GetFenceMarker(trimmed);

            if (fenceMarker is null)
            {
                if (marker is not null)
                    fenceMarker = marker;
                else
                    line = LinkUnprotected(line, identifierPattern, byLabel);
            }
            else if (marker == fenceMarker)
            {
                fenceMarker = null;
            }

            if (index > 0)
                output.Append('\n');
            output.Append(line);
        }

        return output.ToString();
    }

    private static string LinkUnprotected(
        string line,
        Regex identifierPattern,
        IReadOnlyDictionary<string, AutoLinkTarget> byLabel)
    {
        var output = new StringBuilder(line.Length);
        var position = 0;

        foreach (Match protectedMatch in ProtectedMarkdown.Matches(line))
        {
            output.Append(LinkText(
                line.Substring(position, protectedMatch.Index - position),
                identifierPattern,
                byLabel));
            output.Append(protectedMatch.Value);
            position = protectedMatch.Index + protectedMatch.Length;
        }

        output.Append(LinkText(
            line.Substring(position),
            identifierPattern,
            byLabel));
        return output.ToString();
    }

    private static string LinkText(
        string text,
        Regex identifierPattern,
        IReadOnlyDictionary<string, AutoLinkTarget> byLabel) =>
        identifierPattern.Replace(
            text,
            match =>
            {
                var target = byLabel[match.Value];
                return $"[{target.Label}]({target.Href})";
            });

    private static char? GetFenceMarker(string line)
    {
        if (line.StartsWith("```", StringComparison.Ordinal))
            return '`';
        if (line.StartsWith("~~~", StringComparison.Ordinal))
            return '~';
        return null;
    }
}
