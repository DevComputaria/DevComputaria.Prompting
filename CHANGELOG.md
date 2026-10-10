# Changelog

All notable changes to this repository are documented in this file.

The format is inspired by Keep a Changelog and adapted to the execution model of this repository.

## [Unreleased]

### Added

- Functional `DevComputaria.Prompts.Cli` project with `validate` and `render` contract commands.
- Repository validation for prompt/catalog schemas, manifest resources, aliases, path identities, includes, and output schema references.
- JSON render output with pinned prompt identity, rendered messages, content hash, package metadata, and renderer hints.
- Dedicated CLI tests covering success/failure exit codes, diagnostic errors, and the base render fixture.
- Reusable prompt contract workflow covering CLI validation, catalog consistency, schemas, immutability, and render fixtures.
- Gated `DevComputaria.Prompts` package workflow that uploads a `.nupkg` artifact only after contract validation succeeds.
- End-to-end `Consumer.ImageAnalysis` sample with pinned rendering, metadata output, and a deterministic fake chat client.
- Prompt contract v1.1 with sibling Markdown resolution through `parts[].from` and hash participation.
- Compact output-schema injection through `inject_as` and `inject_format: compact_example` without caller-supplied schema arguments.
- Explicitly versioned skills that attach system instructions and provider-neutral tools to `RenderedPrompt`.
- Incremental `PromptPins` source generator backed by the versions published in `prompts/catalog.yaml`.
- `image-analysis.analyze-document@1.1.0` and `image-analysis.document-extractor@1.0.0` packed artifacts.
- Public `PromptLogRedactor` with conservative sensitive-variable masking and DI registration.
- Security and release governance policy covering roles, access, logging, compliance, and rollback.
- CLI `release-check` command for diff-based SemVer classification and artifact immutability enforcement.
- Pull-request release-governance workflow and automatic tag-based package version planning.
- Security reporting policy and pull-request security/SemVer checklist.
- Unit-test and contract validation on pull requests and pushes to `main`, reused as a mandatory gate before package creation.
- Governed publication of validated PromptKit and Prompts packages to the GitHub Packages NuGet registry.
- Input-free `publish-packages` workflow that runs automatically on `main`, derives SemVer from the latest reviewed commit and release tag, and serializes publications.

## [2026-08-30]

### Added

- Initial monorepo structure for `DevComputaria.Prompting`.
- Solution files: `DevComputaria.Prompting.sln` and `DevComputaria.Prompting.slnx`.
- Root engineering configuration:
  - `Directory.Build.props`
  - `Directory.Packages.props`
  - `global.json`
  - `nuget.config`
  - `version.json`
- Library and test project scaffolding:
  - `src/DevComputaria.PromptKit`
  - `src/DevComputaria.Prompts`
  - `tests/DevComputaria.PromptKit.Tests`
  - `tests/DevComputaria.Prompts.Tests`
  - `tests/DevComputaria.Prompts.Contract.Tests`
- Canonical repository artifacts:
  - `schemas/prompt.schema.json`
  - `schemas/catalog.schema.json`
  - `schemas/output/image-analysis-document-v1.json`
  - `prompts/catalog.yaml`
  - `prompts/image-analysis/analyze-document/1.0.0.yaml`
  - `prompts/_shared/json-only.yaml`
  - `evals/image-analysis.analyze-document/1.0.0.cases.json`
- CI workflow scaffolding:
  - `.github/workflows/validate-prompts.yml`
  - `.github/workflows/pack-promptkit.yml`
  - `.github/workflows/pack-prompts.yml`
- Documentation set for architecture, planning, and delivery governance:
  - `docs/PRD/PRD-DevComputaria.Prompting.md`
  - `docs/ADR/ADR-001-devcomputaria-prompt-catalog.md`
  - `docs/ADR/ADR-002-catalog-versioning-rules.md`
  - `docs/ADR/ADR-003-repository-layout-and-packaging-boundaries.md`
  - `docs/plan/DESIGN-prompt-catalog.md`
  - `docs/plan/PLAN-src-tests.md`
  - `docs/INDEX.md`
  - `docs/CONVENTIONS-prompt-catalog.md`
- Execution board and one-file-per-task tracking under `docs/task/`.
- PromptKit public core abstractions in `src/DevComputaria.PromptKit/Abstractions/`:
  - `PromptId`
  - `PromptSpec`
  - `PromptArgs`
  - `RenderedPrompt`
  - `RenderedMessage`
  - `PromptVariableSpec`
- PromptKit public interfaces:
  - `IPromptCatalog`
  - `IPromptRenderer`
  - `IPromptComposer`
  - `IPromptSanitizer`
- PromptKit in-memory versioned catalog support in `src/DevComputaria.PromptKit/Catalogs/InMemoryPromptCatalog.cs`.
- Explicit catalog exception types:
  - `PromptCatalogException`
  - `PromptNotFoundException`
  - `PromptVersionMismatchException`
- Prompt variable validation support in `src/DevComputaria.PromptKit/Validation/VariableValidator.cs`.
- Explicit validation exception type:
  - `MissingRequiredVariableException`
- Sandboxed rendering support:
  - `src/DevComputaria.PromptKit/Rendering/HandlebarsPromptRenderer.cs`
  - `src/DevComputaria.PromptKit/Rendering/TemplateSandbox.cs`
- Explicit unsafe helper exception type:
  - `UnsafeTemplateHelperException`
- Canonical content hashing support:
  - `src/DevComputaria.PromptKit/Hashing/PromptHasher.cs`
- DI bootstrap support:
  - `src/DevComputaria.PromptKit/Hosting/PromptKitOptions.cs`
  - `src/DevComputaria.PromptKit/Hosting/PromptKitServiceCollectionExtensions.cs`
  - `src/DevComputaria.PromptKit/Composition/PassthroughPromptComposer.cs`
  - `src/DevComputaria.Prompts/Hosting/PackedPromptsOptions.cs`
  - `src/DevComputaria.Prompts/Hosting/PackedPromptsServiceCollectionExtensions.cs`
  - `src/DevComputaria.Prompts/Catalogs/PackedPromptCatalog.cs`
- Explicit packed catalog loading support:
  - `src/DevComputaria.Prompts/Catalogs/YamlPromptLoader.cs`
  - `src/DevComputaria.Prompts/Catalogs/PromptManifest.cs`
  - `src/DevComputaria.Prompts/Catalogs/PromptResourceNames.cs`
  - `src/DevComputaria.Prompts/Catalogs/PromptManifestConsistencyValidator.cs`
  - `src/DevComputaria.Prompts/Catalogs/PromptManifestConsistencyException.cs`
- Public contract tests for PromptKit core abstractions in `tests/DevComputaria.PromptKit.Tests/CoreAbstractionsContractTests.cs`.
- Dedicated renderer safety tests in `tests/DevComputaria.PromptKit.Tests/HandlebarsPromptRendererTests.cs`.
- Dedicated hash stability tests in `tests/DevComputaria.PromptKit.Tests/PromptHasherTests.cs`.
- Dedicated DI registration tests in `tests/DevComputaria.Prompts.Tests/ServiceRegistrationTests.cs`.
- Dedicated packed loader tests in `tests/DevComputaria.Prompts.Tests/PackedCatalogLoaderTests.cs`.
- Dedicated manifest/resource consistency tests in `tests/DevComputaria.Prompts.Tests/PromptManifestConsistencyValidatorTests.cs`.
- Contract test infrastructure and baselines:
  - `tests/DevComputaria.Prompts.Contract.Tests/SchemaValidationTests.cs`
  - `tests/DevComputaria.Prompts.Contract.Tests/ImmutabilityGuardTests.cs`
  - `tests/DevComputaria.Prompts.Contract.Tests/RenderSnapshotTests.cs`
  - `tests/DevComputaria.Prompts.Contract.Tests/SchemaSubsetValidator.cs`
  - `tests/DevComputaria.Prompts.Contract.Tests/Baselines/published-artifacts.lock.json`
  - `tests/DevComputaria.Prompts.Contract.Tests/Baselines/render-image-analysis-analyze-document-1.0.0.json`

### Changed

- Repository terminology was normalized across Markdown documentation to use English terms consistently.
- Product planning was expanded into a deeper technical PRD with explicit functional requirements, runtime boundaries, and a two-PR delivery strategy.
- Task governance was formalized with dependency-aware tracking and execution status updates.
- Central package version management was updated to include test infrastructure dependencies for xUnit-based validation.
- `IPromptCatalog` was tightened from nullable lookup semantics to deterministic resolution with explicit catalog failures.
- `README.md` was expanded from a brief repository note into an architectural overview, then refined to keep the canonical folder design while removing delivery-status noise.
- Render-time sanitization now enforces required-variable validation before prompt execution continues.
- Prompt rendering now supports basic interpolation and conditional blocks while rejecting unsafe helper invocation patterns.
- `RenderedPrompt.ContentSha256` is now produced by a dedicated canonical hasher that includes relevant prompt spec, arguments, and rendered content.
- Runtime and packed catalog bootstrapping now support DI-first registration with environment-aware directory override normalization.
- Packed prompt loading is now split into manifest parsing, YAML hydration, and stable logical resource naming for deterministic embedded resolution.
- Packed catalog construction now validates alias, manifest, and embedded resource consistency before runtime use.
- Contract gates now enforce schema validity, immutable published artifacts, and approved render snapshots in a dedicated test suite.
- The task index and GH-011 task file now record the CLI contract commands as completed.
- The prompts packaging workflow now requires the reusable contract gate before producing a release artifact.
- The prompt validation workflow now runs the end-to-end sample with Production directory-override suppression.
- The end-to-end sample now consumes the generated v1.1 prompt pin and prints attached tools.
- Prompt contract validation now runs the PromptKit security/runtime test suite.
- PromptKit packaging now requires the reusable contract gate and emits a versioned workflow artifact.
- Both package workflows verify the declared impact and concrete package version transition before packing.

### Verified

- GH-000 completed: bootstrap structure and solution build validated.
- GH-001 completed: canonical repository contracts documented and catalog consistency verified.
- GH-002 completed: PromptKit runtime abstractions implemented with immutable public contracts and tests.
- GH-003 completed: deterministic prompt lookup by `id + version` implemented with explicit not-found and version-mismatch exceptions.
- GH-004 completed: required variable validation implemented with a dedicated validator and explicit missing-variable exception.
- GH-005 completed: secure sandbox rendering implemented with unsafe helper blocking and no sensitive argument leakage in failure messages.
- GH-006 completed: stable SHA-256 content hashing implemented with deterministic serialization and regression coverage.
- GH-007 completed: AddPromptKit/AddPackedPrompts registration flow implemented and validated through service-resolution tests.
- GH-008 completed: embedded YAML prompt loading and packed catalog hydration implemented with explicit manifest and logical-name conventions.
- GH-009 completed: manifest/resource consistency enforcement implemented with blocking tests for broken aliases, missing files, and orphan resources.
- GH-010 completed: schema validation, immutability guards, and render snapshots implemented as mandatory contract gates.
- GH-011 completed: CLI `validate`/`render` commands implemented with consistent exit codes and diagnostic output.
- GH-012 completed: pull-request validation and package artifact creation are blocked by the prompt contract gate.
- GH-013 completed: the sample renders the pinned Image Analysis prompt and invokes a fake client without HTTP.
- GH-014 completed: external Markdown parts, schema injection, versioned skill attachment, tools, and generated prompt pins are covered end to end.
- GH-015 completed: authorization roles, log redaction, release checklist, and SemVer governance are documented and enforced by tests/workflows.
- Full solution build succeeded in Release configuration with 0 errors and 0 warnings.
- PromptKit test suite passed for core abstraction contracts.
- PromptKit test suite passed expanded lookup/error coverage for the versioned catalog contract.
- PromptKit test suite passed positive and negative coverage for required and optional variable validation.
- PromptKit test suite passed interpolation, conditional rendering, and unsafe-helper blocking scenarios.
- PromptKit test suite passed stable-hash, ordering-insensitive args, and relevant-change hash coverage.
- Prompts test suite passed service registration, default packed catalog resolution, and production override suppression scenarios.
- Prompts test suite passed YAML hydration, packed lookup by id/version, manifest expansion, and predictable embedded resource naming.
- Prompts test suite passed consistency checks for broken aliases, missing manifest resources, orphans, and current assembly validation.
- Contract tests passed repository schema validation, published artifact hash locks, and render snapshot regression checks.
- CLI tests passed validation success/failure, missing-include diagnostics, base fixture rendering, and missing-argument diagnostics.

### Notes

- Production contract remains domain-agnostic and provider-agnostic in `DevComputaria.PromptKit`.
- Current implementation intentionally avoids provider SDKs and HTTP dependencies.
- GH-000 through GH-015 are complete in the task board.
