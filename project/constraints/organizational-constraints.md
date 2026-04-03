[LLAMARC42-METADATA]
Type: Constraint

Concepts: [
  "GPL-3.0",
  "governance",
  "ADR",
  "Constitution",
  "mod-posh"
]

Scope: System

Confidence: Mixed

Source: [
  "docs",
  "code"
]
[/LLAMARC42-METADATA]

# Organizational Constraints

## License

xml2doc is licensed under **GPL-3.0-only**.

This is a deliberate choice. Nearly all repositories under the `mod-posh` organization use GPL-3.0. Consuming projects must be compatible with GPL-3.0 terms.

> **Developer confirmation:** GPL-3.0 is an intentional, standing choice for this project and the wider `mod-posh` organization.

The license expression is centralized in `Directory.Build.props`:

```xml
<PackageLicenseExpression>GPL-3.0-only</PackageLicenseExpression>
```

## Author and Organization

| Property | Value |
|----------|-------|
| Author | Jeffrey Patton |
| Organization | mod-posh |
| Repository | https://github.com/mod-posh/Xml2Doc |

## Governance Model

The project uses a **Constitution + ADR** governance model:

1. **CONSTITUTION.md** defines the project's purpose, principles, and a governance rule.
2. **Architecture Decision Records (ADRs)** document the *why* behind architectural choices.
3. **Governance rule:** If code and an ADR disagree, the ADR is the source of truth. Either the code must be updated, or a new ADR must supersede the old one.

This means:
- Developers must check ADRs before changing architecture
- ADRs must be updated before (or alongside) implementation changes
- There is no committee or approval process documented — governance is the author's own standard

## Contribution Model

Contributions are accepted via pull request. The process (from `CONTRIBUTING.md`):

1. Fork the repository
2. Create a branch
3. Implement changes with tests
4. Update documentation
5. Submit a pull request for review

No formal review SLA or merge policy is documented beyond this. The project is currently a single-author project with community contributions welcome.

## Versioning

The project uses [Semantic Versioning](https://semver.org). The current version prefix is `1.4.0` (centralized in `Directory.Build.props`). CI builds produce preview versions in the format `{VersionPrefix}-preview.{RunNumber}-g{SHA7}`.

## Dependency Management

- Dependabot is configured for automated dependency scanning and update PRs.

## Publishing

NuGet packages are published via an automated `release.yml` GitHub Actions workflow. Symbol packages (`.snupkg`) are also published.

> **Cross-reference:** [technical-constraints.md](technical-constraints.md) · [decisions/architecture-decisions.md](../decisions/architecture-decisions.md)
