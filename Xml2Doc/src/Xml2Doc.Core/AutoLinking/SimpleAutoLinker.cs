using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

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

    private readonly ConditionalWeakTable<AutoLinkContext, PreparedTargets>
        _preparedContexts = new();

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

        var prepared = _preparedContexts.GetValue(context, PrepareTargets);
        if (prepared.IdentifierPattern is null)
            return markdown;

        return ApplyPrepared(markdown, prepared);
    }

    private static PreparedTargets PrepareTargets(AutoLinkContext context)
    {
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
            return PreparedTargets.Empty;

        var byLabel = targets.ToDictionary(
            target => target.Label,
            StringComparer.Ordinal);
        var identifierPattern = new Regex(
            @"(?<![A-Za-z0-9_])(?:" +
            string.Join("|", targets.Select(target => Regex.Escape(target.Label))) +
            @")(?![A-Za-z0-9_])");

        return new PreparedTargets(identifierPattern, byLabel);
    }

    private static string ApplyPrepared(string markdown, PreparedTargets prepared)
    {
        var lines = markdown.Split('\n');
        var output = new StringBuilder(markdown.Length);
        Fence? fence = null;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var trimmed = line.TrimStart();
            var candidate = GetFence(trimmed);

            if (fence is null)
            {
                if (candidate is not null)
                    fence = candidate;
                else
                    line = LinkUnprotected(
                        line,
                        prepared.IdentifierPattern!,
                        prepared.ByLabel);
            }
            else if (IsClosingFence(trimmed, fence.Value))
            {
                fence = null;
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

    private static Fence? GetFence(string line)
    {
        if (line.Length < 3 || (line[0] != '`' && line[0] != '~'))
            return null;

        var marker = line[0];
        var length = 1;
        while (length < line.Length && line[length] == marker)
            length++;

        return length >= 3 ? new Fence(marker, length) : null;
    }

    private static bool IsClosingFence(string line, Fence opening)
    {
        var candidate = GetFence(line);
        return candidate is not null &&
               candidate.Value.Marker == opening.Marker &&
               candidate.Value.Length >= opening.Length &&
               line.Substring(candidate.Value.Length).Trim().Length == 0;
    }

    private readonly struct Fence
    {
        public Fence(char marker, int length)
        {
            Marker = marker;
            Length = length;
        }

        public char Marker { get; }
        public int Length { get; }
    }

    private sealed class PreparedTargets
    {
        public static PreparedTargets Empty { get; } = new(null,
            new Dictionary<string, AutoLinkTarget>(StringComparer.Ordinal));

        public PreparedTargets(
            Regex? identifierPattern,
            IReadOnlyDictionary<string, AutoLinkTarget> byLabel)
        {
            IdentifierPattern = identifierPattern;
            ByLabel = byLabel;
        }

        public Regex? IdentifierPattern { get; }
        public IReadOnlyDictionary<string, AutoLinkTarget> ByLabel { get; }
    }
}
