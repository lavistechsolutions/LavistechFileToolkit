# Security Policy

## Supported versions

Security fixes are currently considered for the latest published version of Lavistech File Toolkit and the current default development branch.

| Version | Security updates |
|---|---|
| Latest release | Best effort |
| Older releases | Not guaranteed |
| Development branch | Best effort |

This policy may change as the project evolves.

## Reporting a vulnerability

**Please do not report security vulnerabilities in public GitHub issues.**

To report a suspected vulnerability, use GitHub's **Report a vulnerability** option on the repository's Security tab, if private vulnerability reporting is enabled:

https://github.com/lavistechsolutions/LavistechFileToolkit/security

If private vulnerability reporting is unavailable, contact the project maintainer at:

info@lavistech.com

Do not publish exploit code, sensitive logs, personal data, file contents, or details that would put users at risk before a fix is available.

## Include in your report

Please provide, where possible:

- A clear description of the suspected vulnerability.
- The affected application version and Windows version.
- Steps to reproduce the issue.
- Expected and actual behaviour.
- The potential impact.
- Relevant logs or screenshots, after removing personal or confidential information.
- Any suggested mitigation or fix.

## What to expect

The maintainer will make reasonable efforts to:

1. Acknowledge receipt of a report.
2. Assess and reproduce the issue.
3. Coordinate a fix or mitigation where appropriate.
4. Prepare an updated release or security advisory when warranted.
5. Credit the reporter if they wish to be credited and disclosure is appropriate.

Response and remediation times are not guaranteed. Please allow reasonable time for investigation and coordinated disclosure.

## Scope

Reports may include issues involving the application, installer, update mechanism, dependency integration, unsafe file handling, or unintended exposure of data.

A report that bulk rename changes file names after Apply, or that duplicate cleanup and folder sync send confirmed extra files to the Recycle Bin, is outside this policy. A report that the app permanently deletes a file, removes the last copy in a duplicate group, or deletes anything without the confirmation those tools ask for, is in scope.

Third-party libraries and services may have their own security reporting processes. Where appropriate, report vulnerabilities to their respective maintainers as well.
