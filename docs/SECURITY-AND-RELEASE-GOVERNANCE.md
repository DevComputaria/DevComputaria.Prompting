# Security, authorization, and release governance

This policy is normative for `DevComputaria.PromptKit` and `DevComputaria.Prompts`.
It translates ADR-001/ADR-002 into review, access, logging, and release controls.

## Roles and authorization

| Role | Responsibilities | Allowed actions | Restrictions |
|---|---|---|---|
| Contributor | Propose implementation, prompt, skill, schema, test, and documentation changes | Push feature branches and open pull requests | Cannot bypass required checks, approve their own protected change, create production tags, or access package credentials |
| Maintainer | Preserve architecture and catalog integrity | Review and merge approved pull requests; maintain schemas, runtime, catalog, and CI | No direct push to `main`; cannot bypass contract or governance gates |
| Security reviewer | Review sandbox, redaction, logging, secrets, permissions, and supply-chain changes | Approve security-sensitive pull requests and incident remediations | Does not receive package publishing credentials solely by holding this role |
| Release manager | Verify the release checklist and approve promotion of validated artifacts | Run the governed publish workflow through the protected release environment | Cannot supply or override a calculated version, publish an unreviewed commit, or bypass an unsuccessful gate |

Organization administrators map these roles to GitHub teams. Membership is reviewed
quarterly and removed immediately when no longer required. Repository administration,
branch-protection bypass, and package publishing remain separate privileges.

## Repository controls

Configure `main` with all of the following controls:

- disallow force pushes, deletion, and direct pushes;
- require pull requests with stale approvals dismissed after new commits;
- require at least one maintainer approval;
- require a security-reviewer approval when changes touch sandboxing, redaction,
  authentication, workflow permissions, secrets, or release policy;
- require `validate-prompts / prompt-contract` and
  `release-governance / semver-and-immutability` checks;
- require exactly one `semver:patch`, `semver:minor`, or `semver:major` label;
- restrict the production package environment to release managers and tag creation
  matching `v*` to the governed publish workflow identity;
- use least-privilege workflow permissions and environment-scoped package credentials.

The role names are stable policy identifiers. Team names and membership are managed at
organization level so personnel are not hard-coded into this repository.

## Logging and redaction

Allowed production telemetry is limited to contract metadata such as `prompt.id`,
`prompt.version`, `prompt.sha256`, package version, duration, status, and provider-neutral
diagnostics.

Raw `PromptArgs`, rendered messages, OCR text, documents, schema payloads, tool arguments,
and model responses must not be logged by default. Code that needs argument-level diagnostic
fields must call `PromptLogRedactor.RedactArguments` and log only its returned dictionary.

The redactor applies these rules:

- a variable with `redacted_in_logs: true` becomes `[REDACTED]` without invoking
  `ToString()` or otherwise materializing its value;
- declared non-sensitive variables may retain their value;
- undeclared arguments and missing variables are omitted;
- callers must not append the original argument or rendered content to the same log event.

Security tests verify masking, omission of undeclared values, non-materialization of secret
objects, and DI registration. Any change to this policy requires security-reviewer approval.

## SemVer policy enforced by tooling

Every pull request declares one impact label. The `release-check` command compares the pull
request with its base ref and computes the minimum impact:

| Change | Minimum impact |
|---|---|
| Documentation, tests, or CI-only change | PATCH |
| New prompt/skill PATCH version | PATCH |
| New prompt/skill ID or MINOR version; compatible runtime addition | MINOR |
| Prompt/skill MAJOR version; output schema revision; public schema change | MAJOR |
| Removal of a pin-able artifact or runtime source file | MAJOR |
| Modification of an existing versioned prompt, skill, Markdown part, or output schema | Forbidden; create a new version |

A higher declared impact is allowed during pull-request review. During package creation,
`publish-packages.yml` calculates the minimum impact since the latest reachable `vX.Y.Z`
tag, derives the exact next version, and asks the command to validate that transition. The
first release uses `version.json`; subsequent releases cannot accept manual version inputs.
The workflow starts for every reviewed commit pushed to `main`; manual dispatch is retained
only for idempotent recovery of an interrupted run. Release concurrency is serialized.

Local example:

```bash
dotnet run --project tools/DevComputaria.Prompts.Cli -- \
  release-check --base origin/main --impact minor
```

Release example:

```bash
dotnet run --project tools/DevComputaria.Prompts.Cli -- \
  release-check --base v1.2.3 --impact minor \
  --previous-version 1.2.3 --next-version 1.3.0
```

## Release checklist

### Pull request and compliance

- [ ] Change is reviewed through a pull request; no direct `main` update.
- [ ] Exactly one `semver:*` label matches the expected compatibility impact.
- [ ] Published prompt, skill, Markdown part, and output-schema bytes are unchanged.
- [ ] New artifact paths and manifest pins are explicit and immutable.
- [ ] Sensitive variables declare `redacted_in_logs: true`.
- [ ] No raw prompt arguments, messages, documents, or responses are added to logs.
- [ ] Security-sensitive changes have security-reviewer approval.
- [ ] Contract and release-governance checks pass.

### Package creation and promotion

- [ ] The release runs from the latest reviewed commit on `main`.
- [ ] The automatic `push` run, or a manual recovery of that same commit, is used.
- [ ] The workflow identifies the previous protected tag and calculates the exact
  next `X.Y.Z` version from the required impact.
- [ ] Changelog documents additions, breaking changes, and removed pins.
- [ ] Governed pack workflow succeeds for the exact reviewed commit SHA.
- [ ] The governed workflow uploads the `.nupkg` as an audit artifact and publishes
  that same file to GitHub Packages; it is not rebuilt locally.
- [ ] Package metadata and SHA-256 are recorded in the release notes.
- [ ] Publishing occurs only through the protected `production` environment.
- [ ] Publication uses the workflow-scoped `GITHUB_TOKEN` with `packages: write`;
  credentials are short-lived and absent from logs/artifacts.
- [ ] The workflow creates the protected `vX.Y.Z` tag only after both registry
  publications succeed.

### After release

- [ ] Restore/install smoke test succeeds from the package registry.
- [ ] Previous package and prompt pins remain available for rollback unless a documented
  MAJOR removal was approved.
- [ ] Release notes identify rollback package and prompt pins.
- [ ] Any anomaly or suspected leak follows `SECURITY.md` reporting instructions.

## Verifiable evidence

- Redaction: `PromptLogRedactorTests` and DI coverage in `ServiceRegistrationTests`.
- SemVer and immutability: `ReleaseGovernanceValidatorTests`.
- Pull-request enforcement: `.github/workflows/release-governance.yml`.
- Contract enforcement: `.github/workflows/validate-prompts.yml`.
- Automatic version planning and release orchestration: `publish-packages.yml`.
- GitHub Packages publication: `pack-promptkit.yml` and `pack-prompts.yml`.

