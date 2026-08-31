# GH-003 — Resolve prompt by id/version

- **Status:** done
- **PR bucket:** PR-1
- **Phase:** F1
- **Source:** PRD 10.3, ADR-001 section 2.5

## Objective

Disponibilizar resolução determinística de prompt por `id + version` com tratamento de erro explícito.

## Scope

- Implementar busca em `IPromptCatalog`.
- Implementar exceções de not found/version mismatch.
- Cobrir cenários positivos e negativos em testes.

## Deliverables

- Catálogo funcional para lookup versionado.
- Exceções com mensagem diagnóstica.

## Dependencies

- GH-002.

## Acceptance criteria

- [x] Lookup existente funciona.
- [x] Prompt inexistente gera exceção específica.
- [x] Versão incompatível gera erro claro.
