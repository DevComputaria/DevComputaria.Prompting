# GH-012 — Implement CI publish gates

- **Status:** todo
- **PR bucket:** PR-2
- **Phase:** F3
- **Source:** PRD 10.12, ADR-002 section 2.11

## Objective

Automatizar bloqueios de merge/publish quando contrato de prompt falhar.

## Scope

- Configurar workflow `validate-prompts.yml`.
- Executar suites: schema, consistência, imutabilidade e fixtures.
- Garantir gate obrigatório para publicação.

## Deliverables

- Workflow CI funcional e documentado.

## Dependencies

- GH-010, GH-011.

## Acceptance criteria

- [ ] Pipeline quebra em erro de contrato.
- [ ] Publicação depende de sucesso do gate.
- [ ] Reexecução reproduzível em PR.
