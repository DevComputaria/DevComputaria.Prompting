# GH-002 — Implement PromptKit core abstractions

- **Status:** done
- **PR bucket:** PR-1
- **Phase:** F1
- **Source:** PRD 10.2, ADR-001 sections 2.6, 2.7, ADR-003 section 4

## Objective

Implementar as abstrações e interfaces públicas do `DevComputaria.PromptKit` mantendo boundary agnóstico de domínio/provider.

## Scope

- Implementar `PromptId`, `PromptSpec`, `PromptArgs`, `RenderedPrompt`, `RenderedMessage`.
- Implementar interfaces: `IPromptCatalog`, `IPromptRenderer`, `IPromptComposer`, `IPromptSanitizer`.
- Assegurar imutabilidade e contrato estável.

## Deliverables

- Tipos centrais em `src/DevComputaria.PromptKit/Abstractions`.
- Testes básicos de contrato público.

## Dependencies

- GH-001.

## Acceptance criteria

- [x] Tipos imutáveis implementados.
- [x] Interfaces públicas compilam e são testáveis.
- [x] Sem dependência de provider/HTTP.
