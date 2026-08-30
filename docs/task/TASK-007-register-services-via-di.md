# GH-007 — Register services via DI

- **Status:** todo
- **PR bucket:** PR-1
- **Phase:** F1/F2
- **Source:** PRD 10.7, ADR-003 sections 4, 5

## Objective

Estabelecer bootstrap consistente por DI para runtime e catálogo packed.

## Scope

- Implementar `AddPromptKit()` e `AddPackedPrompts()`.
- Configurar options (`StrictPins`, `AllowDirectoryOverride`).
- Garantir override apenas em development.

## Deliverables

- Extensions de DI em `Hosting/` de ambos os projetos.
- Testes de registro e resolução de serviços.

## Dependencies

- GH-002, GH-003.

## Acceptance criteria

- [ ] Serviços essenciais resolvem via DI.
- [ ] Production ignora directory override.
- [ ] Configuração testada em cenário padrão.
