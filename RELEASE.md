# Version 2.0.2 — MSBuild Packaging Fix

## Goal

Restore the published `Xml2Doc.MSBuild` package so it can execute from a clean NuGet installation without requiring consumers to install or manually copy task dependencies.

## Scope

* Package `Xml2Doc.Core.dll` beside `Xml2Doc.MSBuild.dll`.
* Include dependencies required by both the `net472` and `net8.0` task assemblies.
* Use NuGet’s supported target-framework-specific pack extension point.
* Preserve the self-contained MSBuild task package design.
* Add package-layout regression tests.
* Add a clean-consumer integration test.
* Validate the same build-and-pack sequence used by the release workflow.

## Issues

* #75 — MSBuild package omits `Xml2Doc.Core` task dependency

## Acceptance checks

* `lib/net472/Xml2Doc.Core.dll` is present in the package.
* `lib/net8.0/Xml2Doc.Core.dll` is present in the package.
* Required non-MSBuild runtime dependencies are packaged beside the task.
* A clean .NET 9 consumer referencing only `Xml2Doc.MSBuild` builds successfully.
* The MSBuild task executes and generates Markdown.
* CI inspects the generated package before publication.
* Existing Core and CLI packages remain unaffected.
* All test and integration workflows pass.

## Release notes

Version 2.0.1 published an incomplete `Xml2Doc.MSBuild` package containing the task assembly but not its required `Xml2Doc.Core` dependency. Version 2.0.2 corrects the package layout and adds clean-consumer validation to prevent recurrence.

## BUG, AREA:MSBUILD

* issue-75: MSBuild package omits Xml2Doc.Core task dependency

