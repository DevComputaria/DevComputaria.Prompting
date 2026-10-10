# GH-013 — Deliver end-to-end sample

- **Status:** done
- **PR bucket:** PR-2
- **Phase:** F4
- **Source:** PRD 10.13, ADR-003 section 9

## Objective

Entregar sample que comprove integração ponta-a-ponta sem provider real.

## Scope

- Criar `samples/Consumer.ImageAnalysis`.
- Demonstrar render + hash + metadados no console.
- Configurar `DirectoryOverride` apenas para development.

## Deliverables

- Projeto sample compilável.
- Documentação de execução local.

## Dependencies

- GH-007, GH-009.

## Acceptance criteria

- [x] Sample compila e executa.
- [x] Prompt versionado é renderizado corretamente.
- [x] Override local não afeta production.
