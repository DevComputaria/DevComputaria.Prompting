# Task Index — DevComputaria.Prompting

Este índice organiza as atividades do PRD (`GH-001`..`GH-015`) em tarefas executáveis, com rastreabilidade para ADR e divisão em dois PRs.

## Regras de gestão

- Cada task representa uma atividade única do PRD.
- Status permitido: `todo`, `in-progress`, `blocked`, `done`.
- Toda alteração de escopo deve atualizar:
  - o arquivo da task afetada;
  - este índice (colunas `Status`, `Updated`, `Notes`).
- Dependências entre tasks devem ser respeitadas antes de mover para `done`.

## Mapa de execução por PR

- **PR-1**: Foundation + Runtime + Packed Catalog (`F0`, `F1`, `F2`)
- **PR-2**: Contract Gates + Tooling + Sample + Hardening (`F3`, `F4`, `F5`)

## Task board

| ID | Task | PR | Fase | Depends on | Status | Updated | File | Notes |
|---|---|---|---|---|---|---|---|---|
| GH-000 | Bootstrap solution and library project structure | PR-1 | F0 | - | done | 2026-08-30 | [TASK-000](./TASK-000-bootstrap-solution-and-library-project-structure.md) | Esqueleto inicial ADR |
| GH-001 | Establish canonical repository contracts | PR-1 | F0 | GH-000 | done | 2026-08-30 | [TASK-001](./TASK-001-establish-canonical-repository-contracts.md) | Base estrutural |
| GH-002 | Implement PromptKit core abstractions | PR-1 | F1 | GH-001 | done | 2026-08-30 | [TASK-002](./TASK-002-implement-promptkit-core-abstractions.md) | Contrato runtime |
| GH-003 | Resolve prompt by id/version | PR-1 | F1 | GH-002 | done | 2026-08-30 | [TASK-003](./TASK-003-resolve-prompt-by-id-version.md) | Lookup determinístico |
| GH-004 | Enforce variable validation | PR-1 | F1 | GH-002 | done | 2026-08-30 | [TASK-004](./TASK-004-enforce-variable-validation.md) | Required args |
| GH-005 | Render with secure sandbox | PR-1 | F1 | GH-002, GH-004 | done | 2026-08-30 | [TASK-005](./TASK-005-render-with-secure-sandbox.md) | Segurança template |
| GH-006 | Produce stable content hash | PR-1 | F1 | GH-005 | done | 2026-08-30 | [TASK-006](./TASK-006-produce-stable-content-hash.md) | Rastreabilidade |
| GH-007 | Register services via DI | PR-1 | F1/F2 | GH-002, GH-003 | todo | 2026-08-30 | [TASK-007](./TASK-007-register-services-via-di.md) | Bootstrap |
| GH-008 | Implement packed catalog loader | PR-1 | F2 | GH-001, GH-002 | todo | 2026-08-30 | [TASK-008](./TASK-008-implement-packed-catalog-loader.md) | YAML -> PromptSpec |
| GH-009 | Enforce manifest/resource consistency | PR-1 | F2 | GH-008 | todo | 2026-08-30 | [TASK-009](./TASK-009-enforce-manifest-resource-consistency.md) | Integridade do pacote |
| GH-010 | Validate schema and immutability gates | PR-2 | F3 | GH-009 | todo | 2026-08-30 | [TASK-010](./TASK-010-validate-schema-and-immutability-gates.md) | Gate de contrato |
| GH-011 | Provide CLI contract commands | PR-2 | F3 | GH-010 | todo | 2026-08-30 | [TASK-011](./TASK-011-provide-cli-contract-commands.md) | Ferramentas locais |
| GH-012 | Implement CI publish gates | PR-2 | F3 | GH-010, GH-011 | todo | 2026-08-30 | [TASK-012](./TASK-012-implement-ci-publish-gates.md) | Bloqueio de publish |
| GH-013 | Deliver end-to-end sample | PR-2 | F4 | GH-007, GH-009 | todo | 2026-08-30 | [TASK-013](./TASK-013-deliver-end-to-end-sample.md) | Prova de integração |
| GH-014 | Implement extension set v1.1 | PR-2 | F5 | GH-010, GH-013 | todo | 2026-08-30 | [TASK-014](./TASK-014-implement-extension-set-v1-1.md) | Evolução de contrato |
| GH-015 | Security, authorization and release governance | PR-2 | F3-F5 | GH-012 | todo | 2026-08-30 | [TASK-015](./TASK-015-security-authorization-release-governance.md) | Governança final |

## Change log

- 2026-08-30: Criação inicial do board de tasks a partir de PRD v1.1 e ADR-001/002/003.
- 2026-08-30: Inclusão da task explícita de bootstrap da solution e projetos (`GH-000`).
- 2026-08-30: Execução da `GH-000` concluída com build Release verde da solution.
- 2026-08-30: Execução da `GH-001` concluída (convenções documentadas + validação de consistência do `catalog.yaml`).
- 2026-08-30: Execução da `GH-002` concluída (abstrações core + interfaces públicas + testes de contrato no `PromptKit`).
- 2026-08-30: Execução da `GH-003` concluída (lookup determinístico por `id + version` + exceções diagnósticas de catálogo).
- 2026-08-30: Execução da `GH-004` concluída (validação antecipada de variáveis obrigatórias + exceção específica de contrato).
- 2026-08-30: Execução da `GH-005` concluída (renderização com sandbox seguro + bloqueio de helpers inseguros).
- 2026-08-30: Execução da `GH-006` concluída (hash canônico SHA-256 com serialização determinística de spec + args + render).