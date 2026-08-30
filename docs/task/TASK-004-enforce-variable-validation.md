# GH-004 — Enforce variable validation

- **Status:** todo
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

- [ ] Required ausente falha com exceção específica.
- [ ] Opcionais não quebram render.
- [ ] Cobertura de validação documentada.
