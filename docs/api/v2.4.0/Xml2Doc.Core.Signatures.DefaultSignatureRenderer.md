# DefaultSignatureRenderer

Default depth-aware C#-style signature renderer.

<a id="xml2doc.core.signatures.defaultsignaturerenderer.#ctor(xml2doc.core.aliasing.ialiasprovider,string)"></a>

## Method: #ctor(IAliasProvider, string)

Creates a signature renderer with optional aliases and root trimming.

**Parameters**

- `aliasProvider` — Alias provider used for signature types.
- `rootNamespaceToTrim` — Optional root namespace removed from type headings.

<a id="xml2doc.core.signatures.defaultsignaturerenderer.rendercreflabel(string)"></a>

## Method: RenderCrefLabel(string)

Formats the visible label for an XML documentation cref.

<a id="xml2doc.core.signatures.defaultsignaturerenderer.rendermemberheader(xml2doc.core.models.xmember,xml2doc.core.signatures.signaturestyle)"></a>

## Method: RenderMemberHeader(XMember, SignatureStyle)

Formats a member heading, including its readable member kind.

<a id="xml2doc.core.signatures.defaultsignaturerenderer.rendertypename(string)"></a>

## Method: RenderTypeName(string)

Formats a type documentation identifier for display.
