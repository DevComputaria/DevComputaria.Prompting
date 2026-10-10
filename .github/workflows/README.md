# CI and package gates

## Prompt contract validation

`validate-prompts.yml` runs for every pull request, for every push to `main`, on
manual dispatch, and as a reusable workflow. Its `prompt-contract` job performs
the same deterministic sequence on every run:

1. restore and build the solution in Release mode;
2. run the CLI `validate` command;
3. test PromptKit runtime and redaction security contracts;
4. test manifest and embedded-resource consistency;
5. test schemas, published-artifact immutability, and render fixtures;
6. test CLI exit codes and diagnostics;
7. run the end-to-end consumer sample in Production with directory override
   suppression enabled.

Any failed command stops the job and leaves the check unsuccessful. Configure
the repository's `main` branch protection to require the
`validate-prompts / prompt-contract` check before merge.

The equivalent local commands are:

```bash
dotnet restore DevComputaria.Prompting.sln
dotnet build DevComputaria.Prompting.sln --configuration Release --no-restore
dotnet run --project tools/DevComputaria.Prompts.Cli --configuration Release --no-build -- validate
dotnet test tests/DevComputaria.PromptKit.Tests/DevComputaria.PromptKit.Tests.csproj --configuration Release --no-build --no-restore
dotnet test tests/DevComputaria.Prompts.Tests/DevComputaria.Prompts.Tests.csproj --configuration Release --no-build --no-restore
dotnet test tests/DevComputaria.Prompts.Contract.Tests/DevComputaria.Prompts.Contract.Tests.csproj --configuration Release --no-build --no-restore
dotnet test tests/DevComputaria.Prompts.Cli.Tests/DevComputaria.Prompts.Cli.Tests.csproj --configuration Release --no-build --no-restore
DOTNET_ENVIRONMENT=Production PROMPTS_DIRECTORY_OVERRIDE=./prompts dotnet run --project samples/Consumer.ImageAnalysis --configuration Release --no-build
```

## Release governance

`release-governance.yml` requires exactly one `semver:patch`, `semver:minor`, or
`semver:major` label on every pull request. It runs the CLI `release-check`
against the base branch to calculate the minimum impact and reject in-place
changes to published artifacts. Configure branch protection to require
`release-governance / semver-and-immutability`.

Manual release runs provide a previous Git ref, previous package version, next
package version, and declared impact. The pack workflows reject a version
transition that does not exactly match that declaration.

## Package artifacts

`pack-promptkit.yml` and `pack-prompts.yml` can only execute their packaging jobs
after the reusable prompt contract gate succeeds, including all four test
projects. A successful run uploads the generated `.nupkg` as a workflow artifact
tied to the commit SHA. A failed test or contract check therefore produces no
releasable package artifact.

The workflows intentionally do not push directly to NuGet. Promotion uses the
reviewed workflow artifact and a protected production environment as defined in
`docs/SECURITY-AND-RELEASE-GOVERNANCE.md`.
