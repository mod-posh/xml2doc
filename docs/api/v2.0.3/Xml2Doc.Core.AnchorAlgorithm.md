# AnchorAlgorithm

Controls how heading anchors (slugs) are generated for types and members.

<a id="xml2doc.core.anchoralgorithm.default"></a>

## Field: Default

Default: lowercase, collapse whitespace → single dash, strip non `[a-z0-9-]`, collapse multi‑dash runs, trim leading/trailing dashes.

<a id="xml2doc.core.anchoralgorithm.gfm"></a>

## Field: Gfm

Alias of GitHub style (kept for explicit `gfm` selection in CLI/config).

<a id="xml2doc.core.anchoralgorithm.github"></a>

## Field: Github

GitHub (GFM) style: Unicode normalize, remove diacritics, lowercase, drop punctuation (except space / dash), whitespace → dash, collapse multi‑dash runs, trim dashes.

<a id="xml2doc.core.anchoralgorithm.kramdown"></a>

## Field: Kramdown

Kramdown/Jekyll style: similar to GitHub but preserves underscores (`_`) in the slug.
