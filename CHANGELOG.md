# Changelog

All notable changes to this repository are documented in this file.

The format is inspired by Keep a Changelog and adapted to the execution model of this repository.

## [Unreleased]

### Planned

- GH-011 — Provide CLI contract commands.
- GH-012 — Implement CI publish gates.
- GH-013 — Deliver end-to-end sample.
- GH-014 — Implement extension set v1.1.
- GH-015 — Security, authorization and release governance.

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

### Notes

- Production contract remains domain-agnostic and provider-agnostic in `DevComputaria.PromptKit`.
- Current implementation intentionally avoids provider SDKs and HTTP dependencies.
- The next execution target in sequence is `GH-011`.
