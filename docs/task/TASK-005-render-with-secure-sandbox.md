# GH-005 — Render with secure sandbox

- **Status:** todo
- **PR bucket:** PR-1
- **Phase:** F1
- **Source:** PRD 10.5, ADR-001 section 2.7

## Objective

Implementar renderização de template com sandbox para bloquear helpers inseguros.

## Scope

- Implementar `HandlebarsPromptRenderer` + `TemplateSandbox`.
- Permitir interpolação e condicionais suportadas.
- Bloquear I/O, rede e execução arbitrária.

## Deliverables

- Renderizador funcional e seguro.
- Testes de bloqueio de helper inseguro.

## Dependencies

- GH-002, GH-004.

## Acceptance criteria

- [ ] Interpolação básica funciona.
- [ ] Helpers proibidos são bloqueados.
- [ ] Logs não vazam informação sensível.
