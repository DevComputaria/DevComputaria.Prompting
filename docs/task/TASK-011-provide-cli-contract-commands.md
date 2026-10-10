# GH-011 — Provide CLI contract commands

- **Status:** done
- **PR bucket:** PR-2
- **Phase:** F3
- **Source:** PRD 10.11, ADR-003 section 9

## Objective

Disponibilizar comandos locais para validação de contrato antes de PR.

## Scope

- Implementar `validate` e `render`.
- Opcional: `list` e `diff`.
- Padronizar exit code para sucesso/falha.

## Deliverables

- CLI funcional em `tools/DevComputaria.Prompts.Cli`.
- Documentação mínima de uso.

## Dependencies

- GH-010.

## Acceptance criteria

- [x] `validate` retorna 0/1 corretamente.
- [x] `render` cobre fixture base.
- [x] Mensagens de erro são diagnósticas.
