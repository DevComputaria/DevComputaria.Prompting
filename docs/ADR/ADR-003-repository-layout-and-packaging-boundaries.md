# ADR-003 — Repository layout and packaging boundaries (PromptKit + Prompts)

- **Status:** Aceito
- **Data:** 2026-08-30
- **Decisores:** Engenharia de plataforma de prompts
- **Relacionado:** [ADR-001](./ADR-001-devcomputaria-prompt-catalog.md), [ADR-002](./ADR-002-catalog-versioning-rules.md)
- **Escopo:** Estrutura física do repositório, limites de responsabilidade entre `DevComputaria.PromptKit` e `DevComputaria.Prompts`, contratos de teste e empacotamento
- **Fora de escopo:** Implementação de provider LLM (`Dev.AI`), lógica de domínio de negócio, runtime remoto de registry

## 1. Contexto

ADR-001 definiu o modelo de catálogo versionado em NuGet e ADR-002 congelou regras de versionamento.
Faltava explicitar, como decisão arquitetural formal, **onde cada responsabilidade mora no repositório** e como evitar acoplamento indevido entre runtime, catálogo e ferramentas.

O objetivo é manter um único solution no início, com possibilidade de separação futura em dois repositórios sem reescrever estrutura interna.

## 2. Decisão

Adotar um layout com **duas libs empacotáveis** e **dois contratos claros**:

1. `DevComputaria.PromptKit` = runtime agnóstico de domínio
2. `DevComputaria.Prompts` = catálogo pinado e empacotado

Com isso:

- `prompts/` continua sendo source of truth em Git
- runtime de produção consome catálogo embedded, não arquivo solto
- validação de qualidade/contrato ocorre em testes e pipeline

## 3. Estrutura canônica do repositório

```text
DevComputaria.Prompting/
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── nuget.config
├── version.json
├── DevComputaria.Prompting.slnx
├── README.md
├── LICENSE
│
├── schemas/                              # contrato dos artefatos (não runtime por padrão)
│   ├── prompt.schema.json
│   ├── catalog.schema.json
│   └── output/
│       └── image-analysis-intent-v1.json
│
├── prompts/                              # SOURCE OF TRUTH (Git)
│   ├── catalog.yaml
│   ├── _shared/
│   │   ├── safety-ptbr.yaml
│   │   ├── json-only.yaml
│   │   └── no-invention.yaml
│   ├── image-analysis/
│   │   ├── classify-intent/
│   │   │   ├── 1.0.0.yaml
│   │   │   └── 1.4.0.yaml
│   │   └── extract-qr/
│   │       └── 1.0.0.yaml
│   └── prompt-management/
│       └── explain-alert/
│           └── 2.1.0.yaml
│
├── evals/
│   ├── image-analysis.classify-intent/
│   │   ├── 1.4.0.cases.json
│   │   └── 1.4.0.snapshots/
│   └── prompt-management.explain-alert/
│       └── 2.1.0.cases.json
│
├── src/
│   ├── DevComputaria.PromptKit/
│   └── DevComputaria.Prompts/
│
├── tests/
│   ├── DevComputaria.PromptKit.Tests/
│   ├── DevComputaria.Prompts.Tests/
│   └── DevComputaria.Prompts.Contract.Tests/
│
├── samples/
│   └── Consumer.ImageAnalysisIntent/
│
├── tools/
│   ├── DevComputaria.Prompts.Cli/
│   └── DevComputaria.Prompts.SourceGen/
│
└── .github/
    └── workflows/
        ├── validate-prompts.yml
        ├── pack-promptkit.yml
        └── pack-prompts.yml
```

## 4. Contract A: `DevComputaria.PromptKit` (runtime)

`PromptKit` não carrega domínio (`image-analysis`, `prompt-management`) e não possui dependência de SDK de provider.

Responsabilidades:

- abstrações (`PromptId`, `PromptSpec`, `PromptArgs`, `RenderedPrompt`)
- composição (`_shared`), validação de variáveis, renderização e hash
- integração DI (`AddPromptKit`)
- observabilidade (`prompt.id`, `prompt.version`, `prompt.sha256`)

Restrições explícitas:

- sem OpenAI/Semantic Kernel/MEAI
- sem HTTP client de provider
- sem lógica de negócio
- sem acoplamento direto à árvore física `prompts/`

Dependências base permitidas no v1:

- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Options`
- `Handlebars.Net`
- `System.Diagnostics.DiagnosticSource`

## 5. Contract B: `DevComputaria.Prompts` (packed catalog)

`Prompts` embute YAML do repositório e expõe catálogo pronto para runtime.

Responsabilidades:

- mapear `prompts/**/*.yaml` para `EmbeddedResource`
- carregar `catalog.yaml` e resolver aliases/versions
- hidratar `PromptSpec` para consumo do `PromptKit`
- expor extensões de DI (`AddPackedPrompts`)

Restrições explícitas:

- sem regra de negócio
- sem chamadas a provider
- sem runtime de avaliação

## 6. Regras de empacotamento

- `prompts/` entra no nupkg de runtime via `EmbeddedResource`
- `schemas/` e `evals/` não entram por padrão
- `schemas/output` só entra se houver validação de JSON em produção
- pasta `Embedded/` (quando existir) é artefato gerado, não fonte manual

## 7. Regras de identidade e catálogo

- path canônico: `prompts/{domain}/{slug}/{semver}.yaml`
- id canônico: `{domain}.{slug}`
- exemplo: `prompts/image-analysis/classify-intent/1.4.0.yaml` → `PromptId("image-analysis.classify-intent", "1.4.0")`

Fragmentos compartilhados:

- `_shared/{slug}.yaml` com versionamento explícito
- `include` nunca depende de alias implícito para produção

## 8. Estratégia de testes

Três projetos, três perguntas:

1. `DevComputaria.PromptKit.Tests`: o runtime funciona e é seguro?
2. `DevComputaria.Prompts.Tests`: o pacote empacota e resolve corretamente?
3. `DevComputaria.Prompts.Contract.Tests`: o contrato textual/schema está íntegro?

O terceiro projeto é gate de publicação: falhou, não publica.

## 9. Sample e tools

Sample em `samples/Consumer.ImageAnalysisIntent` existe para provar:

- pin explícito de prompt/version
- `DirectoryOverride` só em development
- render + hash + metadados sem provider real

Ferramentas em `tools/` (CLI e SourceGen) validam/geram artefatos do Git, mas não entram no runtime das libs consumidoras.

## 10. Governança de evolução (um repo vs dois repos)

Decisão atual:

- manter **um solution** com dois pacotes
- separar em dois repos apenas quando cadências divergirem de forma relevante

Critério prático para separar:

- `Prompts` com alta frequência (ex.: múltiplas publicações semanais)
- `PromptKit` com baixa frequência e API estável

Nesse cenário, extrair para repo próprio:

- `DevComputaria.Prompts`
- `prompts/`
- `schemas/`
- `evals/`

sem alterar layout interno já estabelecido.

## 11. Consequências

### Positivas

- separação de responsabilidades explícita
- pipeline e ownership mais claros
- menor risco de acoplamento com domínio/provider
- caminho direto para split em dois repos sem refatorar contratos

### Custos

- mais disciplina em boundaries
- necessidade de CI rigoroso para manter consistência
- gerenciamento de versionamento em duas camadas continua obrigatório (artefato + pacote)

## 12. Compliance obrigatório

1. Consumidor de produção depende de `PromptKit` + `Prompts`, não de arquivos locais.
2. `PromptKit` não pode introduzir dependência de provider ou de domínio.
3. Qualquer prompt público precisa estar em `catalog.yaml`.
4. Testes de contrato são gate de publicação.
5. Alias não substitui pin explícito em caminho crítico.

## 13. Decisão sobre documentação

Este conteúdo deve viver em **ADR novo** (este documento), não como adendo grande em ADR-001/002.

Motivo: ADR-001 e ADR-002 já cobrem decisões diferentes (modelo de catálogo e versionamento). Este ADR trata de **topologia física e boundaries de implementação**, com ciclo de revisão próprio.