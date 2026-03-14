# Engineering Guide

Development follows a **structured workflow**.

## Reading order

1. Constitution
2. ADR index
3. Relevant ADR
4. Lowest numbered open issue
5. Playbook for the work

## Implementation flow

Typical issue flow:

1. Identify the active issue
2. Determine whether it changes architecture or behavior
3. Update ADRs if architecture changes
4. Implement in the correct layer
5. Add tests
6. Update documentation

## Layer responsibilities

### Xml2Doc.Core

Owns:

• XML loading
• rendering
• link resolution
• anchors
• signature formatting
• renderer options

### Xml2Doc.Cli

Owns:

• argument parsing
• configuration files
• CLI UX

### Xml2Doc.MSBuild

Owns:

• MSBuild task wiring
• build properties
• incremental execution
