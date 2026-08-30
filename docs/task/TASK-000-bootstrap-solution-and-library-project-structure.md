# GH-000 — Bootstrap solution and library project structure

- **Status:** done
- **PR bucket:** PR-1
- **Phase:** F0
- **Source:** ADR-003 sections 3 and 10, ADR-001 section 3, PLAN-src-tests section 6 (critério de esqueleto pronto)

## Objective

Criar o esqueleto técnico inicial do repositório para suportar os dois contratos (`PromptKit` e `Prompts`) com solution única e projetos organizados conforme ADR.

## Scope

- Criar/validar solution `DevComputaria.Prompting.slnx`.
- Criar projetos:
  - `src/DevComputaria.PromptKit/DevComputaria.PromptKit.csproj`
  - `src/DevComputaria.Prompts/DevComputaria.Prompts.csproj`
  - `tests/DevComputaria.PromptKit.Tests/DevComputaria.PromptKit.Tests.csproj`
  - `tests/DevComputaria.Prompts.Tests/DevComputaria.Prompts.Tests.csproj`
  - `tests/DevComputaria.Prompts.Contract.Tests/DevComputaria.Prompts.Contract.Tests.csproj`
- Garantir referências na solution para os 5 projetos.
- Criar arquivos raiz de build/versionamento:
  - `Directory.Build.props`
  - `Directory.Packages.props`
  - `global.json`
  - `nuget.config`
  - `version.json`
- Estruturar diretórios canônicos `schemas/`, `prompts/`, `evals/`, `samples/`, `tools/`.

## Deliverables

- Solution com 2 projetos de src + 3 projetos de testes referenciados.
- Build inicial verde (mesmo com stubs).
- Estrutura de pastas aderente aos ADRs.

## Dependencies

- Nenhuma.

## Acceptance criteria

- [x] `DevComputaria.Prompting.slnx` existe e referencia os 5 projetos previstos.
- [x] `PromptKit` e `Prompts` compilam (stubs permitidos nesta etapa).
- [x] Pastas canônicas do ADR-003 existem.
- [x] Arquivos base de build/versionamento estão presentes.
- [x] Estrutura atende ao critério de “esqueleto pronto” do plano.
