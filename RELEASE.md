# BUGFIX 2.0.1

## Issue description:

- The Xml2Doc.MSBuild NuGet package was shipping the MSBuild task assembly, but not reliably shipping its runtime dependency Xml2Doc.Core.dll in the package layout that MSBuild loads at build time.
- When the task executes during dotnet build, the task assembly loads successfully, but as soon as it tries to instantiate or use the core engine it throws:
System.IO.FileNotFoundException: Could not load file or assembly 'Xml2Doc.Core'
- This shows up as a build failure even though the package was restored successfully and the project otherwise compiles.

## What the fix should accomplish:

- Ensure the NuGet package contains the required task dependency binaries beside the task assembly under the correct lib/<tfm>/ folder.
- Make the MSBuild targets resolve the task from the packaged location without missing dependencies.
- Allow the GenerateMarkdownFromXmlDoc task to load and execute correctly in both:
  - dotnet build (net8.0 task host)
  - Visual Studio/MSBuild (net472 task host)
- Prevent the build from failing when XML documentation generation runs.
- Preserve the task as a build-time-only dependency while keeping the runtime assembly graph complete for the task to operate.

### In short: the fix is a packaging/runtime integrity fix for the MSBuild task, not a problem with the consumer project itself.

## BUG, AREA:MSBUILD

* issue-73: System.IO.FileNotFoundException: Could not load file or assembly 'Xml2Doc.Core, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified

