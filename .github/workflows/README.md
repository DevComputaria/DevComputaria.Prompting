# CI and package gates

## Prompt contract validation

`validate-prompts.yml` runs for every pull request, on manual dispatch, and as a
reusable workflow. Its `prompt-contract` job performs the same deterministic
sequence on every run:

1. restore and build the solution in Release mode;
2. run the CLI `validate` command;
3. test manifest and embedded-resource consistency;
4. test schemas, published-artifact immutability, and render fixtures;
5. test CLI exit codes and diagnostics;
6. run the end-to-end consumer sample in Production with directory override
   suppression enabled.

Any failed command stops the job and leaves the check unsuccessful. Configure
the repository's `main` branch protection to require the
`validate-prompts / prompt-contract` check before merge.

The equivalent local commands are:

```bash
dotnet restore DevComputaria.Prompting.sln
dotnet build DevComputaria.Prompting.sln --configuration Release --no-restore
dotnet run --project tools/DevComputaria.Prompts.Cli --configuration Release --no-build -- validate
dotnet test tests/DevComputaria.Prompts.Tests/DevComputaria.Prompts.Tests.csproj --configuration Release --no-build --no-restore
dotnet test tests/DevComputaria.Prompts.Contract.Tests/DevComputaria.Prompts.Contract.Tests.csproj --configuration Release --no-build --no-restore
dotnet test tests/DevComputaria.Prompts.Cli.Tests/DevComputaria.Prompts.Cli.Tests.csproj --configuration Release --no-build --no-restore
DOTNET_ENVIRONMENT=Production PROMPTS_DIRECTORY_OVERRIDE=./prompts dotnet run --project samples/Consumer.ImageAnalysis --configuration Release --no-build
```

## Prompts package

`pack-prompts.yml` can only execute its `pack prompts` job after the reusable
prompt contract gate succeeds. A successful run uploads the generated `.nupkg`
as a workflow artifact tied to the commit SHA. A failed gate therefore produces
no releasable package artifact.

The workflow intentionally does not push directly to NuGet. Publishing that
artifact to a package registry requires the release credentials and governance
defined by GH-015.
