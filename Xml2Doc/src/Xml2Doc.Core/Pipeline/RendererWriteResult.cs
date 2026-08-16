using System.Collections.Generic;

namespace Xml2Doc.Core.Pipeline;

internal sealed record RendererWriteResult(
    IReadOnlyList<string> WrittenFiles,
    IReadOnlyList<string> SkippedFiles);
