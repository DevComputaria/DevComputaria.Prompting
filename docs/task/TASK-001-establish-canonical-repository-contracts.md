# GH-001 — Establish canonical repository contracts

- **Status:** todo
- **PR bucket:** PR-1
- **Phase:** F0
- **Source:** PRD 10.1, ADR-003 sections 3, 7

## Objective

Definir e materializar a estrutura canônica do repositório com separação clara entre source of truth (`prompts/`) e código (`src/`).

## Scope

- Criar/validar árvores: `schemas/`, `prompts/`, `evals/`, `src/`, `tests/`, `samples/`, `tools/`.
- Garantir convenção path => id para prompts.
- Garantir que `catalog.yaml` liste apenas artefatos existentes.

## Deliverables

- Estrutura de pastas conforme ADR-003.
- `prompts/catalog.yaml` consistente.
- Documentação atualizada de convenções.

## Dependencies

- GH-000.

## Acceptance criteria

- [ ] Estrutura mínima do repo existe e está documentada.
- [ ] Convenção `prompts/{domain}/{slug}/{semver}.yaml` aplicada.
- [ ] `catalog.yaml` sem órfãos e sem entradas quebradas.
