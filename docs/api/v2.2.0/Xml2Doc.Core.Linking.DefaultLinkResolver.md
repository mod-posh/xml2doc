# DefaultLinkResolver

Centralizes all type/member and single-file/per-type decisions for cref links. Uses delegates supplied by the renderer to preserve existing behavior.

<a id="xml2doc.core.linking.defaultlinkresolver.containingtypeid(string)"></a>

## Method: ContainingTypeId(string)

Extracts "T:Ns.Type" from a member cref like "M:Ns.Type.Method(...)"
