# GH-009 — Enforce manifest/resource consistency

- **Status:** done
- **PR bucket:** PR-1
- **Phase:** F2
- **Source:** PRD 10.9, ADR-002 section 2.11

## Objective

Garantir consistência entre `catalog.yaml`, manifesto e recursos embutidos.

## Scope

- Criar testes para órfãos e referências inexistentes.
- Validar aliases para versões publicadas no lote.
- Validar correspondência catálogo ↔ resources.

## Deliverables

- Bateria de testes de consistência no projeto de pacote.

## Dependencies

- GH-008.

## Acceptance criteria

- [x] Alias quebrado falha em teste.
- [x] Entrada sem arquivo falha em teste.
- [x] Publicação bloqueada em inconsistência.
