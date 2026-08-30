# GH-008 — Implement packed catalog loader

- **Status:** todo
- **PR bucket:** PR-1
- **Phase:** F2
- **Source:** PRD 10.8, ADR-003 section 5

## Objective

Implementar carregamento de YAML embarcado e hidratação de `PromptSpec` para consumo em runtime.

## Scope

- Implementar `YamlPromptLoader`, `PromptManifest`, `PackedPromptCatalog`.
- Definir convenção de `LogicalName` para `EmbeddedResource`.
- Integrar resolução por `id + version`.

## Deliverables

- Loader funcional (`YAML -> PromptSpec`).
- Catálogo packed consumível pelo `PromptKit`.

## Dependencies

- GH-001, GH-002.

## Acceptance criteria

- [ ] Prompt YAML válido é carregado corretamente.
- [ ] Lookup por id/version funciona no catálogo packed.
- [ ] Resource names estáveis e previsíveis.
