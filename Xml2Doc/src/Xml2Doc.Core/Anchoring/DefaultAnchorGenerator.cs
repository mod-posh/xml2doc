using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xml2Doc.Core.Aliasing;

namespace Xml2Doc.Core.Anchoring;

/// <summary>
/// Implements Xml2Doc's built-in heading algorithms and member-anchor behavior.
/// </summary>
public sealed class DefaultAnchorGenerator : IAnchorGenerator
{
    private readonly AnchorAlgorithm _algorithm;
    private readonly IAliasProvider _aliasProvider;

    /// <summary>Creates a built-in anchor generator.</summary>
    public DefaultAnchorGenerator(
        AnchorAlgorithm algorithm,
        IAliasProvider? aliasProvider = null)
    {
        if (!Enum.IsDefined(typeof(AnchorAlgorithm), algorithm))
            throw new ArgumentOutOfRangeException(nameof(algorithm));

        _algorithm = algorithm;
        _aliasProvider = aliasProvider ?? DefaultAliasProvider.Instance;
    }

    /// <inheritdoc />
    public string GenerateHeadingAnchor(string heading)
    {
        if (heading is null)
            throw new ArgumentNullException(nameof(heading));

        return _algorithm switch
        {
            AnchorAlgorithm.Github => GithubSlug(heading),
            AnchorAlgorithm.Kramdown => KramdownSlug(heading),
            AnchorAlgorithm.Gfm => GfmSlug(heading),
            _ => DefaultSlug(heading)
        };
    }

    /// <inheritdoc />
    public string GenerateMemberAnchor(string memberId)
    {
        if (memberId is null)
            throw new ArgumentNullException(nameof(memberId));

        return _aliasProvider.ApplyAliases(memberId)
            .Replace('{', '[')
            .Replace('}', ']')
            .ToLowerInvariant();
    }

    private static string DefaultSlug(string heading)
    {
        var value = heading.Trim().ToLowerInvariant();
        value = Regex.Replace(value, @"\s+", "-");
        value = Regex.Replace(value, @"[^a-z0-9\-]", "");
        return Regex.Replace(value, @"\-{2,}", "-").Trim('-');
    }

    private static string GithubSlug(string heading)
    {
        var value = RemoveDiacritics(heading).ToLowerInvariant();
        value = Regex.Replace(value, @"[^a-z0-9\s\-]", " ");
        value = Regex.Replace(value, @"\s+", "-");
        return Regex.Replace(value, @"\-{2,}", "-").Trim('-');
    }

    private static string KramdownSlug(string heading)
    {
        var value = RemoveDiacritics(heading).ToLowerInvariant();
        value = Regex.Replace(value, @"[^\w\s\-]", " ");
        value = Regex.Replace(value, @"\s+", "-");
        return Regex.Replace(value, @"\-{2,}", "-").Trim('-');
    }

    private static string GfmSlug(string heading)
    {
        var value = heading.Trim().ToLowerInvariant();
        value = Regex.Replace(value, @"[^a-z0-9\-_.\s]", "");
        value = Regex.Replace(value, @"\s+", "-");
        return Regex.Replace(value, @"\-{2,}", "-").Trim('-');
    }

    private static string RemoveDiacritics(string value)
    {
        var formD = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);

        foreach (var character in formD.Where(
                     character =>
                         CharUnicodeInfo.GetUnicodeCategory(character) !=
                         UnicodeCategory.NonSpacingMark))
        {
            builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
