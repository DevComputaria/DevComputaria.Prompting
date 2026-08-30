# GH-010 — Validate schema and immutability gates

- **Status:** todo
- **PR bucket:** PR-2
- **Phase:** F3
- **Source:** PRD 10.10, ADR-002 sections 2.2, 2.11

## Objective

Criar gates formais de schema e imutabilidade para impedir regressões contratuais.

## Scope

- `SchemaValidationTests` para prompts/catalog.
- `ImmutabilityGuardTests` para versões publicadas.
- Fixtures de render para regressão textual.

## Deliverables

- Projeto `DevComputaria.Prompts.Contract.Tests` com suite mínima obrigatória.

## Dependencies

- GH-009.

## Acceptance criteria

- [ ] YAML inválido falha contra schema.
- [ ] Edição retroativa de versão publicada falha.
- [ ] Snapshot de render detecta regressão.
