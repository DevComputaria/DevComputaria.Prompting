# GH-014 — Implement extension set v1.1

- **Status:** done
- **PR bucket:** PR-2
- **Phase:** F5
- **Source:** PRD 10.14, ADR-002 section 2.4/2.5/2.9

## Objective

Evoluir contrato para `parts[].from`, schema injection e skills anexadas.

## Scope

- Implementar resolução `parts[].from`.
- Implementar `SchemaInjector` (`inject_as`, `inject_format`).
- Implementar `SkillAttacher` + `PromptPins` (source generation).

## Deliverables

- Funcionalidades v1.1 com cobertura de testes.

## Dependencies

- GH-010, GH-013.

## Acceptance criteria

- [x] `parts[].from` resolvido e entra no hash.
- [x] Schema injetado corretamente no prompt renderizado.
- [x] Skills e pins funcionam com versionamento explícito.

## Implementation notes

- A versão `image-analysis.analyze-document@1.1.0` mantém a `1.0.0` imutável e usa um Markdown irmão em `parts[].from`.
- `SchemaInjector` produz `compact_example` a partir do schema embarcado e o renderer combina a injeção com os argumentos do consumidor.
- A skill `image-analysis.document-extractor@1.0.0` anexa instruções de sistema e uma tool ao `RenderedPrompt`.
- `DevComputaria.Prompts.SourceGen` lê `prompts/catalog.yaml` e gera `PromptPins` por versão publicada.
