# Contributing to Lavistech File Toolkit

Thank you for your interest in contributing to **Lavistech File Toolkit**.

Lavistech File Toolkit is an open-source Windows desktop app for renaming, comparing, syncing, hashing, compressing, and archiving files locally. Contributions such as bug fixes, documentation, accessibility improvements, and carefully tested file tools are welcome.

## Before you start

1. Read the project's [`LICENSE`](LICENSE), [`EULA.txt`](EULA.txt), and [`THIRD-PARTY-NOTICES.txt`](THIRD-PARTY-NOTICES.txt).
2. Search existing [issues](https://github.com/lavistechsolutions/LavistechFileToolkit/issues) and pull requests to avoid duplicate work.
3. For a significant feature or architectural change, open an issue first to discuss the proposal.
4. Do not submit credentials, private user data, or other people's files.

## Development setup

- Windows 10 or Windows 11, 64-bit.
- Visual Studio 2026 with the workload required for WPF desktop development, or the .NET 10 SDK.
- Open `LavistechFileToolkit.sln`.

### Build

1. Fork this repository.
2. Clone your fork.
3. Open `LavistechFileToolkit.sln`.
4. Restore and build the **Release** configuration:

```powershell
dotnet build -c Release
```

5. Run the application and test the changes on Windows.

## Coding guidelines

- Keep changes focused and explain why they are needed.
- Follow the existing C# and WPF conventions in the codebase.
- Prefer clear names and maintainable methods over unnecessary complexity.
- Avoid unrelated formatting changes.
- File tools must not delete or overwrite a file unless that behavior is explicit and tested.
- Do not log file contents, paths that are unnecessary, or personal information.
- Do not add dependencies without explaining their purpose, license, and maintenance status.
- Preserve required third-party copyright and license notices.
- Do not commit generated build output, local settings, secrets, or user-specific files.

## Testing expectations

Before opening a pull request:

- Confirm the project restores and builds successfully.
- Run the application on a supported Windows system.
- Test the affected tool on copies of files, including a failure case.
- Include the steps you used to test the change.
- If you cannot test a particular scenario, state that clearly.

## Pull request process

1. Create a branch with a descriptive name, such as `fix/rename-collision`.
2. Make small, logically grouped commits.
3. Update documentation and `CHANGELOG.md` when appropriate.
4. Open a pull request against the default branch.
5. Describe the problem, the solution, related issue numbers, and tests performed.
6. Include a screenshot for user-interface changes when useful. Do not include private files.

The maintainers may request changes, ask questions, or decline a contribution that is outside the project's scope.

## Licensing contributions

By submitting a contribution, you confirm that:

- You created it or have the right to submit it.
- You have the right to make it available under the project's applicable license.
- You understand that the project is distributed under the MIT License unless a file or component states otherwise.
- You have identified any third-party code or assets and supplied their applicable license and attribution information.

Unless you clearly state otherwise and the maintainers agree, contributions submitted for inclusion in the project are offered under the MIT License, consistent with the project's license.

## Community standards

Be respectful, constructive, and considerate. Harassment, discrimination, personal attacks, and disruptive behaviour are not acceptable.

## Questions

For bugs and feature proposals, use the GitHub issue tracker:

https://github.com/lavistechsolutions/LavistechFileToolkit/issues
