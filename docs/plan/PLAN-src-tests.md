# Plano de desenvolvimento — src + tests

Repo: `DevComputaria.Prompting`  
Pacotes: `DevComputaria.PromptKit` (motor) + `DevComputaria.Prompts` (catálogo)  
Fora deste plano: `Dev.AI`, `Dev.ImageAnalysis` de produção (só o sample)

---

## 1. Estrutura `src/`

### 1.1 `src/DevComputaria.PromptKit/` — motor (sem YAML de domínio)

```
src/DevComputaria.PromptKit/
├── DevComputaria.PromptKit.csproj
├── README.md
│
├── Abstractions/
│   ├── PromptId.cs
│   ├── SkillId.cs
│   ├── PromptSpec.cs
│   ├── PromptPart.cs
│   ├── PromptRole.cs                    # System | Developer | User | Placeholder
│   ├── PromptVariable.cs
│   ├── PromptArgs.cs
│   ├── ModelHints.cs
│   ├── OutputContract.cs
│   ├── SkillSpec.cs
│   ├── SkillRef.cs
│   ├── SkillTool.cs
│   ├── RenderedPrompt.cs
│   ├── RenderedMessage.cs
│   ├── IPromptCatalog.cs
│   ├── ISkillCatalog.cs
│   ├── IPromptRenderer.cs
│   ├── IPromptComposer.cs
│   └── IPromptSanitizer.cs
│
├── Catalog/
│   ├── EmbeddedResourceCatalogBase.cs
│   ├── CompositePromptCatalog.cs
│   ├── DirectoryPromptCatalog.cs        # Development only
│   ├── AliasResolver.cs
│   └── Exceptions/
│       ├── PromptNotFoundException.cs
│       ├── PromptVersionMismatchException.cs
│       ├── MissingRequiredVariableException.cs
│       └── SkillNotFoundException.cs
│
├── Rendering/
│   ├── HandlebarsPromptRenderer.cs
│   ├── TemplateSandbox.cs
│   ├── VariableValidator.cs
│   ├── SchemaInjector.cs                # fase 1.1
│   └── PromptHasher.cs
│
├── Composition/
│   ├── FragmentResolver.cs
│   └── SkillAttacher.cs                 # fase 1.1
│
├── Hosting/
│   ├── PromptKitOptions.cs
│   └── PromptKitServiceCollectionExtensions.cs
│
├── Observability/
│   ├── PromptActivitySource.cs
│   └── PromptLogRedactor.cs
│
└── Internal/
    ├── CanonicalJson.cs
    └── EmbeddedResourceReader.cs
```

`csproj`: `net8.0`, `EnablePackageValidation`, dependências só de:

- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Options`
- `Handlebars.Net`
- `System.Diagnostics.DiagnosticSource`

Proibido no Kit: YamlDotNet de domínio, SK, OpenAI, HTTP.

### 1.2 `src/DevComputaria.Prompts/` — lote packed

```
src/DevComputaria.Prompts/
├── DevComputaria.Prompts.csproj
├── README.md
│
├── Catalog/
│   ├── PackedPromptCatalog.cs
│   ├── PackedSkillCatalog.cs
│   ├── PromptManifest.cs
│   ├── YamlPromptLoader.cs
│   └── YamlSkillLoader.cs
│
├── Ids/
│   ├── PromptNames.cs                   # constantes manuais no v1
│   └── PromptPins.cs                    # source gen na fase 1.1
│
└── Hosting/
    └── PromptsServiceCollectionExtensions.cs
        # AddPackedPrompts() → IPromptCatalog + ISkillCatalog
```

`csproj` referencia o Kit e embute:

```xml
<ItemGroup>
  <EmbeddedResource Include="..\..\prompts\**\*.yaml" />
  <EmbeddedResource Include="..\..\prompts\**\*.md" />
  <EmbeddedResource Include="..\..\skills\**\*.yaml" />
  <EmbeddedResource Include="..\..\schemas\output\**\*.json" />
</ItemGroup>
```

Dependências extras: `YamlDotNet` (só neste pacote).

---

## 2. Estrutura `tests/`

Três projetos = três perguntas.

### 2.1 `tests/DevComputaria.PromptKit.Tests/` — o motor funciona?

```
tests/DevComputaria.PromptKit.Tests/
├── DevComputaria.PromptKit.Tests.csproj
├── Catalog/
│   ├── AliasResolverTests.cs
│   ├── DirectoryOverrideTests.cs
│   └── CompositeCatalogTests.cs
├── Rendering/
│   ├── HandlebarsRendererTests.cs
│   ├── MissingRequiredVariableTests.cs
│   ├── TemplateSandboxTests.cs
│   └── HasherStabilityTests.cs
├── Composition/
│   └── FragmentResolverTests.cs
├── Observability/
│   └── LogRedactorTests.cs
└── Hosting/
    └── DiRegistrationTests.cs
```

Fixtures de template **inline** (não depende do catálogo packed).

### 2.2 `tests/DevComputaria.Prompts.Tests/` — o lote empacota certo?

```
tests/DevComputaria.Prompts.Tests/
├── DevComputaria.Prompts.Tests.csproj
├── ManifestLoadsAllFilesTests.cs
├── PackedResourceNamesTests.cs
├── AliasResolvesToExistingVersionTests.cs
├── YamlLoaderRoundtripTests.cs
└── AddPackedPromptsDiTests.cs
```

Referencia `DevComputaria.Prompts` (traz os YAML reais do tree).

### 2.3 `tests/DevComputaria.Prompts.Contract.Tests/` — o contrato do texto não quebrou?

```
tests/DevComputaria.Prompts.Contract.Tests/
├── DevComputaria.Prompts.Contract.Tests.csproj
├── SchemaValidationTests.cs             # yaml ⊨ prompt.schema.json
├── CatalogConsistencyTests.cs           # catalog.yaml ↔ arquivos
├── ImmutabilityGuardTests.cs            # bytes de versão tagueada
├── FromRefResolutionTests.cs            # parts[].from
├── RenderFixtures/
│   └── image-analysis.analyze-document.1.4.0/
│       ├── input.json
│       └── expected.messages.json
└── OutputSchemaTests.cs
```

Este projeto é o gate de pack. Falhou → não publica nupkg.

---

## 3. Dependência entre projetos

```
PromptKit
    ▲
    │
Prompts ──────────────► Prompts.Tests
    ▲                   Prompts.Contract.Tests
    │
PromptKit.Tests

samples/Consumer.ImageAnalysis → PromptKit + Prompts
tools/Prompts.Cli    → PromptKit + Prompts
```

---

## 4. Fases

Ordem fixa. Fase N não começa sem o aceite de N-1.

### Fase 0 — Disco canônico (sem C# ainda)

**Objetivo:** tree de dados que o pack vai embutir.

| ID | Task | Aceite |
|---|---|---|
| F0-1 | `schemas/prompt.schema.json` + `catalog.schema.json` | um YAML inválido falha no validador (CLI ou teste depois) |
| F0-2 | `prompts/catalog.yaml` com Image Analysis 1.0.0 | lista bate com arquivos |
| F0-3 | `prompts/image-analysis/analyze-document/1.0.0.yaml` | `id`+`version` = path |
| F0-4 | `schemas/output/image-analysis-document-v1.json` | schema JSON válido |

**Não fazer:** skills, lucius, prompt management, MD `from:`.

### Fase 1 — PromptKit compilável

**Objetivo:** render + validar variável + hash, sem YAML.

| ID | Task | Depende | Aceite |
|---|---|---|---|
| F1-1 | `PromptId`, `PromptSpec`, `PromptArgs`, `RenderedPrompt` | F0 | record imutável, equality por valor |
| F1-2 | `IPromptCatalog` + stub in-memory | F1-1 | Get por id+versão / not found |
| F1-3 | `VariableValidator` | F1-1 | required ausente → exception |
| F1-4 | `HandlebarsPromptRenderer` + sandbox | F1-3 | `{{var}}` + `{% if %}`; helper de IO recusado |
| F1-5 | `PromptHasher` | F1-4 | mesmo spec+args ⇒ mesmo sha256 |
| F1-6 | `AddPromptKit()` + options | F1-2 | DI resolve `IPromptRenderer` |
| F1-7 | testes Kit da pasta `Rendering/` + `Hosting/` | F1-4..6 | verde |

**Não fazer:** loader YAML, embedded resource, skill.

### Fase 2 — Pacote catálogo

**Objetivo:** YAML do Git vira `IPromptCatalog` packed.

| ID | Task | Depende | Aceite |
|---|---|---|---|
| F2-1 | `YamlPromptLoader` | F1-1, F0-3 | YAML → `PromptSpec` |
| F2-2 | Embedded resources no csproj | F2-1 | `dotnet pack` contém o YAML de Image Analysis |
| F2-3 | `PackedPromptCatalog` + `PromptManifest` | F2-2 | `Get("image-analysis.analyze-document","1.0.0")` funciona |
| F2-4 | `AddPackedPrompts()` | F2-3, F1-6 | host resolve catálogo packed |
| F2-5 | `DirectoryPromptCatalog` atrás de options | F2-4 | prod ignora override |
| F2-6 | testes `Prompts.Tests` | F2-3 | manifesto ↔ resources |

### Fase 3 — Contrato e CI

**Objetivo:** pack recusado se o lote mentir.

| ID | Task | Depende | Aceite |
|---|---|---|---|
| F3-1 | `SchemaValidationTests` | F0-1, F2-1 | YAML fora do schema falha |
| F3-2 | `CatalogConsistencyTests` | F2-3 | órfão / alias morto falha |
| F3-3 | fixture de render Image Analysis 1.0.0 | F2-3 | snapshot de messages |
| F3-4 | CLI `validate` / `render` | F3-1 | `dotnet run -- validate` exit 0/1 |
| F3-5 | workflow `validate-prompts.yml` | F3-4 | PR vermelho se contrato quebrar |

### Fase 4 — Sample consumidor

**Objetivo:** provar Forma A (domínio renderiza, AI finge).

| ID | Task | Depende | Aceite |
|---|---|---|---|
| F4-1 | `samples/Consumer.ImageAnalysis` pina `1.0.0` | F2-4 | compila, render sem provider real |
| F4-2 | fake `IChatClient` no sample | F4-1 | mostra messages + hash no console |

`Dev.AI` de verdade fica **fora**. Sample não fala HTTP.

### Fase 5 — Extensões do contrato (1.1)

Só depois do sample verde.

| ID | Task | Aceite |
|---|---|---|
| F5-1 | `parts[].from` + `.md` irmão | MD entra no hash e no pack |
| F5-2 | `SchemaInjector` (`inject_as`) | Image Analysis não passa schema no Args |
| F5-3 | `skills/` + `SkillAttacher` | messages/tools no `RenderedPrompt` |
| F5-4 | `includes` `_shared` | fragmento aparece no system |
| F5-5 | Source gen `PromptPins` | compilação falha se pin sumir do lote |

### Fora do plano (não abrir task)

- Client OpenAI / SK / MEAI
- Mongo / ConfigMap como fonte
- Fetch Git em runtime
- Export Prompty (pode ser F5-6 se sobrar)
- Prompt Management, Lucius, fraude no catálogo (um prompt de Image Analysis basta até F4)

---

## 5. Ordem de execução imediata

```
F0-1 schema
  → F0-2 catalog.yaml
    → F0-3 yaml Image Analysis 1.0.0
  → F0-4 output schema
  → F1-1..F1-7 Kit + testes
  → F2-1..F2-6 Prompts + testes
  → F3 contrato + CLI
  → F4 sample
  → F5 só se F4 aceito
```

Nenhuma task de F2 começa com F1 vermelho.

---

## 6. Critério de “esqueleto pronto”

Pode declarar a estrutura construída quando:

- pastas `src/` e `tests/` existem com os `.csproj`
- `PromptKit` e `Prompts` compilam vazio (ou com stubs)
- solution referencia os 5 projetos (2 src + 3 test)
- este plano está em `docs/` do repo

Implementação de comportamento = F1 em diante, task a task.
