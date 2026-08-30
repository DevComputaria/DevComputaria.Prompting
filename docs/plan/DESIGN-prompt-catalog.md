# Desenho final — DevComputaria.Prompting

Fonte da verdade: Git.
Publicação: lote NuGet `Dev.Prompts`.
Motor: `DevComputaria.PromptKit`.
Transporte LLM: `Dev.AI` (fora deste repo).
Domínio (`Dev.ImageAnalysis`, `Dev.PromptManagement`, …): pina `PromptId`, não lê arquivo.

---

## Mapa

```
Git (texto + schema + skill)
        │  pack
        ▼
Dev.Prompts.nupkg  ──embedded──►  IPromptCatalog
        │
PromptKit  ──render──►  RenderedPrompt (messages + tools + sha256)
        │
Dev.ImageAnalysis / Dev.PromptManagement  ──►  Dev.AI  ──►  provider
```

Três papéis, dois pacotes neste repo.

---

## Árvore completa

```
DevComputaria.Prompting/
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── nuget.config
├── version.json                          # Nerdbank.GitVersioning = versão do LOTE
├── DevComputaria.Prompting.slnx
├── README.md
├── CHANGELOG.md
├── LICENSE
├── AGENTS.md
│
├── docs/
│   ├── INDEX.md
│   ├── adr/
│   │   ├── ADR-001-devcomputaria-prompt-catalog.md
│   │   └── ADR-002-catalog-versioning-rules.md
│   └── DESIGN.md                         # este desenho
│
├── schemas/
│   ├── prompt.schema.json                # valida prompts/**/*.yaml
│   ├── skill.schema.json
│   ├── catalog.schema.json
│   └── output/                           # contratos de JSON de resposta
│       ├── image-analysis-document-v1.json
│       └── prompt-management-intent-v1.json
│
├── prompts/                              # SOURCE OF TRUTH — prompts
│   ├── catalog.yaml
│   ├── _shared/
│   │   ├── json-only.yaml
│   │   ├── no-invention.yaml
│   │   └── safety-ptbr.yaml
│   ├── image-analysis/
│   │   └── analyze-document/
│   │       ├── 1.0.0.yaml
│   │       └── 1.4.0.yaml
│   ├── prompt-management/
│   │   └── classify-intent/
│   │       └── 1.4.0.yaml
│   └── agent/
│       └── lucius/
│           └── 1.0.0.yaml
│
├── skills/                               # SOURCE OF TRUTH — skills
│   └── image-analysis/
│       └── document-extractor/
│           └── 1.0.0.yaml
│
├── evals/                                # NÃO entra no nupkg de runtime
│   └── image-analysis.analyze-document/
│       ├── 1.4.0.cases.json
│       └── 1.4.0.snapshots/
│
├── export/                               # gerado, opcional (Prompty / playground)
│   └── .gitkeep
│
├── src/
│   ├── DevComputaria.PromptKit/
│   │   ├── DevComputaria.PromptKit.csproj
│   │   ├── Abstractions/
│   │   │   ├── PromptId.cs
│   │   │   ├── SkillId.cs
│   │   │   ├── PromptSpec.cs
│   │   │   ├── PromptPart.cs
│   │   │   ├── PromptRole.cs
│   │   │   ├── PromptVariable.cs
│   │   │   ├── PromptArgs.cs
│   │   │   ├── ModelHints.cs
│   │   │   ├── OutputContract.cs
│   │   │   ├── SkillSpec.cs
│   │   │   ├── SkillRef.cs
│   │   │   ├── RenderedPrompt.cs
│   │   │   ├── RenderedMessage.cs
│   │   │   ├── IPromptCatalog.cs
│   │   │   ├── ISkillCatalog.cs
│   │   │   ├── IPromptRenderer.cs
│   │   │   ├── IPromptComposer.cs
│   │   │   └── IPromptSanitizer.cs
│   │   ├── Catalog/
│   │   │   ├── EmbeddedPromptCatalog.cs
│   │   │   ├── CompositePromptCatalog.cs
│   │   │   ├── DirectoryPromptCatalog.cs      # só Development
│   │   │   ├── AliasResolver.cs
│   │   │   └── Exceptions/
│   │   ├── Rendering/
│   │   │   ├── HandlebarsPromptRenderer.cs
│   │   │   ├── TemplateSandbox.cs
│   │   │   ├── VariableValidator.cs
│   │   │   ├── SchemaInjector.cs             # output.schema_ref → {{output_schema}}
│   │   │   └── PromptHasher.cs
│   │   ├── Composition/
│   │   │   ├── FragmentResolver.cs
│   │   │   └── SkillAttacher.cs
│   │   ├── Hosting/
│   │   │   ├── PromptKitOptions.cs
│   │   │   └── PromptKitServiceCollectionExtensions.cs
│   │   ├── Observability/
│   │   │   ├── PromptActivitySource.cs
│   │   │   └── PromptLogRedactor.cs
│   │   └── Internal/
│   │
│   └── DevComputaria.Prompts/
│       ├── DevComputaria.Prompts.csproj      # EmbeddedResource → ../../prompts|skills|schemas/output
│       ├── Catalog/
│       │   ├── PackedPromptCatalog.cs
│       │   ├── PackedSkillCatalog.cs
│       │   ├── PromptManifest.cs
│       │   └── YamlPromptLoader.cs
│       ├── Ids/
│       │   ├── PromptNames.cs
│       │   └── PromptPins.cs                 # gerado na fase 1.1
│       └── Hosting/
│           └── PromptsServiceCollectionExtensions.cs
│
├── tests/
│   ├── DevComputaria.PromptKit.Tests/
│   ├── DevComputaria.Prompts.Tests/
│   └── DevComputaria.Prompts.Contract.Tests/
│       ├── SchemaValidationTests.cs
│       ├── ImmutabilityGuardTests.cs
│       └── RenderFixtures/
│
├── samples/
│   └── Consumer.ImageAnalysis/
│       ├── Consumer.ImageAnalysis.csproj
│       ├── DocumentAnalyzer.cs
│       ├── Program.cs
│       └── appsettings.Development.json
│
├── tools/
│   ├── DevComputaria.Prompts.Cli/
│   │   └── Commands/
│   │       ├── ValidateCommand.cs
│   │       ├── ListCommand.cs
│   │       ├── DiffCommand.cs
│   │       ├── RenderCommand.cs
│   │       └── ExportPromptyCommand.cs       # opcional
│   └── DevComputaria.Prompts.SourceGen/
│
└── .github/
    └── workflows/
        ├── validate-prompts.yml
        ├── pack-promptkit.yml
        └── pack-prompts.yml
```

---

## O que cada pasta é

| Pasta | Vai no nupkg runtime? | Papel |
|---|---|---|
| `prompts/` | Sim | Texto versionado |
| `skills/` | Sim | Skill versionada, pinada pelo prompt |
| `schemas/output/` | Sim, se o consumidor valida JSON em prod | Contrato de saída |
| `schemas/*.schema.json` | Não | Validação de CI |
| `evals/` | Não | Qualidade |
| `export/` | Não | Prompty / playground |
| `src/PromptKit` | Pacote `PromptKit` | Motor |
| `src/Prompts` | Pacote `Dev.Prompts` | Catálogo packed |
| `docs/adr/` | Não | Decisões |

`csproj` do catálogo:

```xml
<ItemGroup>
  <EmbeddedResource Include="..\..\prompts\**\*.yaml" />
  <EmbeddedResource Include="..\..\skills\**\*.yaml" />
  <EmbeddedResource Include="..\..\schemas\output\**\*.json" />
</ItemGroup>
```

---

## Identidade no disco

```
prompts/{dominio}/{slug}/{semver}.yaml     →  PromptId("{dominio}.{slug}", "{semver}")
skills/{dominio}/{slug}/{semver}.yaml      →  SkillId("{dominio}.{slug}", "{semver}")
schemas/output/{contrato}-vN.json          →  output.schema_ref
```

Arquivo publicado não se edita. Mudança = arquivo novo.

---

## `catalog.yaml` (índice do lote)

```yaml
package: DevComputaria.Prompts
schema: 1
aliases:                              # demo / Development — não usar em Image Analysis prod
  image-analysis.analyze-document: 1.4.0
  prompt-management.classify-intent: 1.4.0
prompts:
  - id: image-analysis.analyze-document
    versions: [1.0.0, 1.4.0]
    tags: [image-analysis]
  - id: prompt-management.classify-intent
    versions: [1.4.0]
    tags: [prompt-management]
skills:
  - id: image-analysis.document-extractor
    versions: [1.0.0]
```

---

## Prompt canônico

```yaml
id: image-analysis.analyze-document
version: 1.4.0
includes: [_shared/json-only]
variables:
  document_type: { type: string, required: true }
  country: { type: string, required: true }
  ocr_text: { type: string, required: true, redacted_in_logs: true }
output:
  kind: json
  schema_ref: schemas/output/image-analysis-document-v1.json
  inject_as: output_schema
  inject_format: compact_example
skills:
  - id: image-analysis.document-extractor
    version: 1.0.0
    attach: system
hints:
  temperature: 0
  max_tokens: 800
parts:
  - role: system
    template: |
      You extract image analysis.
      Responda APENAS JSON:

      {{output_schema}}
  - role: user
    template: |
      País: {{country}}
      Tipo: {{document_type}}
      OCR:
      {{ocr_text}}
```

- `output.schema_ref` → validação no código (`CompleteJsonAsync<T>`).
- `{{output_schema}}` → o que o modelo vê (exemplo compacto, injetado pelo Kit).
- Skill não é copiada no prompt; é ref.

---

## Runtime

```
Host
  AddPackedPrompts()          # Dev.Prompts
  AddPromptKit()              # motor
  AddAiClient(config)         # outro repo
  AddImageAnalysis()

Dev.ImageAnalysis
  Render(PromptId("image-analysis.analyze-document", "1.4.0"), args)
  CompleteJsonAsync<ImageAnalysisDocumentResult>(rendered)
```

Production: catálogo embedded.  
Development: `DirectoryOverride` aponta para `prompts/` + `skills/` no clone.  
Nenhum `File.Read` / Mongo / Hub no caminho de produção.

`RenderedPrompt` carrega `PromptId`, `ContentSha256`, `PackageVersion`, messages, tools das skills, hints.

Span: `prompt.id`, `prompt.version`, `prompt.sha256`.

---

## Versionamento (resumo operacional)

| Mudança | Artefato | Lote NuGet | Domínio |
|---|---|---|---|
| Prosa | PATCH do YAML | PATCH/MINOR | nada |
| Var opcional / skill pin novo | MINOR | MINOR | nada |
| JSON de saída | schema vN+1 + prompt MAJOR | MINOR ou MAJOR | MAJOR (DTO) |
| Remove versão pinável do lote | — | MAJOR | quem ainda pinava quebra |
| Loader do Kit | — | PromptKit sobe | se API quebrar |

Dois pins em produção: `Dev.Prompts 3.8.0` e `PromptId(..., "1.4.0")`.

---

## Fora deste repo

```
Dev.AI/                 # HTTP providers — não empacota YAML
Dev.ImageAnalysis/      # pins PromptId
Dev.PromptManagement/
```

Mongo, Markdown solto e Prompty não são source of truth.

- Markdown: só se virar `{semver}.md` + frontmatter (mesmo contrato).
- Mongo: réplica/índice escrita pelo CI, leitura proibida no render de prod.
- Prompty: `export/` gerado pela CLI, para playground/SK.

---

## Ordem de construção

1. `schemas/prompt.schema.json` + `catalog.schema.json`
2. `prompts/catalog.yaml` + um prompt + um schema de output
3. `PromptKit` (catalog + render + hash)
4. `Dev.Prompts` (embedded + `AddPackedPrompts`)
5. Contract tests + CLI validate
6. Sample `Consumer.ImageAnalysis`
7. Skill + `SchemaInjector` + source gen (fase 1.1)
