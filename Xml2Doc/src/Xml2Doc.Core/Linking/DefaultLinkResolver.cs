using System;

namespace Xml2Doc.Core.Linking
{
    /// <summary>
    /// Centralizes all type/member and single-file/per-type decisions for cref links.
    /// Uses delegates supplied by the renderer to preserve existing behavior.
    /// </summary>
    internal sealed class DefaultLinkResolver : ILinkResolver
    {
        private readonly Func<string, string> _labelFromCref;
        private readonly Func<string, string> _idToAnchor;
        private readonly Func<string, string> _typeFileName;
        private readonly Func<string, string> _headingSlug;

        internal DefaultLinkResolver(
            Func<string, string> labelFromCref,
            Func<string, string> idToAnchor,
            Func<string, string> typeFileName,
            Func<string, string> headingSlug)
        {
            _labelFromCref = labelFromCref ?? throw new ArgumentNullException(nameof(labelFromCref));
            _idToAnchor = idToAnchor ?? throw new ArgumentNullException(nameof(idToAnchor));
            _typeFileName = typeFileName ?? throw new ArgumentNullException(nameof(typeFileName));
            _headingSlug = headingSlug ?? throw new ArgumentNullException(nameof(headingSlug));
        }

        public MarkdownLink Resolve(string cref, LinkContext ctx)
        {
            // Kind is 'T','M','P','F','E', etc. (or '?' if not a cref).
            var isCref = !string.IsNullOrWhiteSpace(cref) && cref.Length > 1 && cref[1] == ':';
            var kind = isCref ? cref[0] : '?';
            var label = _labelFromCref(cref);

            // Derive the id portion (strip the leading "X:" if present) so anchors do not contain the kind prefix.
            // Many callers (e.g., renderer emission) call IdToAnchor with the plain id (no "M:"/"T:"), so the resolver
            // must pass an id compatible with that logic.
            string idPortion = cref;
            if (isCref)
                idPortion = cref.Substring(2); // drop "X:"

            string href;
            if (ctx.SingleFile)
            {
                // Single-file: types → heading slug; members → explicit member anchor.
                href = (kind == 'T')
                    ? "#" + _headingSlug(label)
                    : "#" + _idToAnchor(idPortion);
            }
            else
            {
                if (kind == 'T')
                {
                    href = PrefixBase(ctx.BasePath, _typeFileName(cref));
                }
                else
                {
                    var typeId = ContainingTypeId(cref);
                    href = $"{PrefixBase(ctx.BasePath, _typeFileName(typeId))}#{_idToAnchor(idPortion)}";
                }
            }

            return new MarkdownLink(href, label);
        }

        private static string PrefixBase(string? basePath, string file) =>
            string.IsNullOrEmpty(basePath) ? file : basePath!.TrimEnd('/') + "/" + file;

        /// <summary>Extracts "T:Ns.Type" from a member cref like "M:Ns.Type.Method(...)"</summary>
        private static string ContainingTypeId(string cref)
        {
            // Strip "X:" prefix if present
            var span = cref.AsSpan();
            if (span.Length >= 2 && span[1] == ':')
                span = span.Slice(2);

            // Methods: cut at '('; Others: whole head
            var paren = span.IndexOf('(');
            var head = paren >= 0 ? span.Slice(0, paren) : span;

            // Last '.' separates the type from the member
            var lastDot = head.LastIndexOf('.');
            var typeName = lastDot >= 0 ? head.Slice(0, lastDot) : head;

            return "T:" + typeName.ToString();
        }
    }
}
