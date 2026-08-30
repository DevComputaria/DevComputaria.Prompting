# GH-014 — Implement extension set v1.1

- **Status:** todo
- **PR bucket:** PR-2
- **Phase:** F5
- **Source:** PRD 10.14, ADR-002 section 2.4/2.5/2.9

## Objective

Evoluir contrato para `parts[].from`, schema injection e skills anexadas.

## Scope

- Implementar resolução `parts[].from`.
- Implementar `SchemaInjector` (`inject_as`, `inject_format`).
- Implementar `SkillAttacher` + `PromptPins` (source generation).

## Deliverables

- Funcionalidades v1.1 com cobertura de testes.

## Dependencies

- GH-010, GH-013.

## Acceptance criteria

- [ ] `parts[].from` resolvido e entra no hash.
- [ ] Schema injetado corretamente no prompt renderizado.
- [ ] Skills e pins funcionam com versionamento explícito.
