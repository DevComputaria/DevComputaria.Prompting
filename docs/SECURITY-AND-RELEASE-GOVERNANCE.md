# Security, authorization, and release governance

This policy is normative for `DevComputaria.PromptKit` and `DevComputaria.Prompts`.
It translates ADR-001/ADR-002 into review, access, logging, and release controls.

## Roles and authorization

| Role | Responsibilities | Allowed actions | Restrictions |
|---|---|---|---|
| Contributor | Propose implementation, prompt, skill, schema, test, and documentation changes | Push feature branches and open pull requests | Cannot bypass required checks, approve their own protected change, create production tags, or access package credentials |
| Maintainer | Preserve architecture and catalog integrity | Review and merge approved pull requests; maintain schemas, runtime, catalog, and CI | No direct push to `main`; cannot bypass contract or governance gates |
| Security reviewer | Review sandbox, redaction, logging, secrets, permissions, and supply-chain changes | Approve security-sensitive pull requests and incident remediations | Does not receive package publishing credentials solely by holding this role |
| Release manager | Verify the release checklist, choose the SemVer impact, create protected tags, and promote validated artifacts | Run governed pack/publish workflows through the protected release environment | Cannot publish an unreviewed commit or bypass an unsuccessful gate |

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
- restrict tag creation matching `v*` and the production package environment to
  release managers;
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

A higher declared impact is allowed. During package creation, the command additionally checks
that `previous-version -> package-version` exactly matches the declared impact. Versions use
the `X.Y.Z` form.

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

- [ ] Release manager identifies the previous protected tag and package version.
- [ ] Proposed `X.Y.Z` package version exactly matches the declared impact.
- [ ] Changelog documents additions, breaking changes, and removed pins.
- [ ] Governed pack workflow succeeds for the exact reviewed commit SHA.
- [ ] `.nupkg` is downloaded from the workflow artifact; it is not rebuilt locally.
- [ ] Package metadata and SHA-256 are recorded in the release notes.
- [ ] Publishing occurs only through the protected production environment.
- [ ] Credentials are environment-scoped, rotated, and absent from logs/artifacts.
- [ ] Protected `vX.Y.Z` tag points to the packaged commit.

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
- Package version enforcement: `pack-promptkit.yml` and `pack-prompts.yml`.

