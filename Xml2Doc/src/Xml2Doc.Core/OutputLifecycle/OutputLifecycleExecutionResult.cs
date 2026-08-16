using System.Collections.Generic;

namespace Xml2Doc.Core.OutputLifecycle;

internal sealed record OutputLifecycleExecutionResult(
    OutputLifecyclePlan Plan,
    IReadOnlyList<string> DeletedFiles);
