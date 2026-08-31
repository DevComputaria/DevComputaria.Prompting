# GH-004 — Enforce variable validation

- **Status:** done
- **PR bucket:** PR-1
- **Phase:** F1
- **Source:** PRD 10.4, ADR-001 section 2.6

## Objective

Garantir validação antecipada de variáveis obrigatórias antes da renderização.

## Scope

- Implementar `VariableValidator`.
- Lançar `MissingRequiredVariableException` quando necessário.
- Suportar opcionais sem falha.

## Deliverables

- Validador de variáveis integrado ao fluxo de render.
- Testes positivos/negativos.

## Dependencies

- GH-002.

## Acceptance criteria

- [x] Required ausente falha com exceção específica.
- [x] Opcionais não quebram render.
- [x] Cobertura de validação documentada.
