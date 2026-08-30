# Prompt Catalog Conventions

This document defines the canonical repository conventions for prompt artifacts.

## Canonical trees

Required top-level trees:

- `schemas/`
- `prompts/`
- `evals/`
- `src/`
- `tests/`
- `samples/`
- `tools/`

## Prompt identity convention

Canonical path format:

- `prompts/{domain}/{slug}/{semver}.yaml`

Canonical ID format:

- `{domain}.{slug}`

Version format:

- semantic version: `MAJOR.MINOR.PATCH`

Example:

- `prompts/image-analysis/analyze-document/1.0.0.yaml`
- `id: image-analysis.analyze-document`
- `version: 1.0.0`

## Catalog consistency rules (`prompts/catalog.yaml`)

- Every prompt entry in `prompts[].id` + `versions[]` must map to an existing file path.
- Every prompt file under `prompts/{domain}/{slug}/{version}.yaml` must be listed in `catalog.yaml`.
- Aliases must point to an existing version listed for the same `id`.
- `_shared` fragments are not listed in `prompts[]`.

## Shared fragments

- Shared fragments live under `prompts/_shared/`.
- Shared fragments are referenced via `includes`.
- Shared fragments must not be used as production alias selectors.

## Scope boundaries

- `prompts/` is source of truth.
- `src/DevComputaria.PromptKit` does not contain domain YAML.
- `src/DevComputaria.Prompts` packages prompt assets for runtime consumption.
