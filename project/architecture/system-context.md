# System Context

Xml2Doc sits between compiler-generated XML documentation and Markdown consumers such as repositories, wikis, documentation sites, and release artifacts.

```text
C# projects / compiler XML
          │
          ├──────────────┐
          ▼              ▼
        CLI           MSBuild
          │              │
          └──────┬───────┘
                 ▼
           Xml2Doc.Core
                 │
                 ▼
     deterministic Markdown
                 │
        ┌────────┼────────┐
        ▼        ▼        ▼
      GitHub   Wikis    Docs sites
```

## External actors and systems

- **C# compiler / .NET SDK** produces XML documentation inputs.
- **Developers and CI systems** invoke the CLI or build projects using the MSBuild package.
- **NuGet** distributes `Xml2Doc.Core`, `Xml2Doc.Cli`, and `Xml2Doc.MSBuild`.
- **GitHub Actions** validates cross-platform behavior and drives milestone releases.
- **Documentation consumers** read the generated Markdown but are not coupled to the runtime implementation.

## Multi-project context

A repository may have many C# projects but should have one owner for a combined documentation set. CLI callers may supply several `--xml` inputs directly; MSBuild repositories declare one aggregation owner that collects participating compiler XML and renders once.

The repository version is `2.3.0`; the latest published release is `2.2.0` until the current milestone is released.
