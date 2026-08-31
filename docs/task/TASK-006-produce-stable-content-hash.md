# GH-006 — Produce stable content hash

- **Status:** done
- **PR bucket:** PR-1
- **Phase:** F1
- **Source:** PRD 10.6, ADR-001 section 2.6, ADR-002 section 2.9

## Objective

Gerar hash canônico estável para rastreabilidade e replay de execução.

## Scope

- Implementar `PromptHasher` com SHA-256.
- Incluir conteúdo relevante de spec+args+refs.
- Garantir determinismo por serialização canônica.

## Deliverables

- Hash em `RenderedPrompt.ContentSha256`.
- Testes de estabilidade e mudança de hash quando aplicável.

## Dependencies

- GH-005.

## Acceptance criteria

- [x] Mesmo input gera mesmo hash.
- [x] Mudança relevante altera hash.
- [x] Resultado reproduzível em CI.
