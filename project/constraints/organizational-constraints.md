# Organizational Constraints

## License and ownership

Xml2Doc is licensed under `GPL-3.0-only`. Package metadata centralizes that expression in `Directory.Build.props`.

The repository is maintained under the `mod-posh` organization and uses semantic versioning for published packages.

## Governance

The project uses a Constitution + ADR model:

1. `docs/CONSTITUTION.md` defines project principles.
2. `docs/adr/` records durable architecture decisions.
3. Code and architecture documentation should be updated together when an accepted decision changes.

Contributions are made through GitHub pull requests and should include relevant tests and documentation updates.

## Versioning and releases

`Directory.Build.props` is the repository version source. The repository/release target is `2.4.0`.

Release automation builds/tests/packages the solution, validates clean consumers, publishes NuGet packages and symbols, creates the GitHub release/tag, and refreshes generated API documentation.

## Dependency management

Dependabot is configured for dependency updates. Build-time dependencies and MSBuild-host assemblies must preserve the package-hosting constraints documented in [`technical-constraints.md`](technical-constraints.md).
