# DevComputaria.Prompting

Monorepo para uma proposta de biblioteca de prompts em .NET baseada em **Git como source of truth**, **NuGet como unidade de distribuição** e **boundaries explícitos** entre catálogo, runtime e transporte de provider.

O repositório implementa dois pacotes principais:

- `DevComputaria.PromptKit`: runtime agnóstico de domínio e de provider.
- `DevComputaria.Prompts`: catálogo packed com prompts versionados e embedded resources.

O desenho arquitetural segue os ADRs `ADR-001`, `ADR-002` e `ADR-003`.

## Proposta da biblioteca

Esta biblioteca existe para resolver um problema comum em integrações com LLM: o prompt costuma ficar espalhado no código de domínio, acoplado ao client HTTP do provider, sem versionamento claro, sem rollback previsível e sem contrato explícito de saída.

A proposta deste repositório é separar responsabilidades:

- o **texto do prompt** vive versionado no Git;
- o **catálogo** é empacotado e distribuído via NuGet;
- o **runtime** resolve, compõe, valida e renderiza prompts;
- o **transporte para o provider** fica fora deste repo, em uma biblioteca vizinha como `Dev.AI`.

Na prática, isso permite:

- evoluir prompts sem republicar bibliotecas de domínio desnecessariamente;
- pinar versões explícitas de prompt em produção;
- reproduzir comportamento por `prompt.id`, `prompt.version` e `prompt.sha256`;
- reduzir drift entre times e consumidores.

## Arquitetura em alto nível

O modelo adotado usa quatro papéis distintos:

| Papel | Responsabilidade |
|---|---|
| `DevComputaria.Prompts` | empacotar YAML, aliases e referências de schema |
| `DevComputaria.PromptKit` | abstrações, renderização, composição, validação e hash |
| `Dev.AI` | transporte para provider LLM |
| libs de domínio | caso de uso, DTOs e orquestração de negócio |

### Princípios principais

- **Git é a fonte da verdade** para prompts, schemas e evals.
- **Production não lê prompt do disco** nem busca conteúdo remoto em runtime.
- **PromptKit não conhece provider** e não faz HTTP.
- **Domínio não carrega string solta de prompt** como contrato principal.
- **Alias não substitui pin explícito** em caminho crítico de produção.

## Pacotes e responsabilidades

### `DevComputaria.PromptKit`

Biblioteca de runtime responsável por:

- contratos públicos como `PromptId`, `PromptSpec`, `PromptArgs`, `RenderedPrompt` e `RenderedMessage`;
- interfaces como `IPromptCatalog`, `IPromptRenderer`, `IPromptComposer` e `IPromptSanitizer`;
- composição de fragmentos compartilhados;
- validação de variáveis;
- renderização segura;
- geração de hash determinístico;
- integração futura com DI e observabilidade.

Restrições explícitas:

- sem SDK de provider;
- sem HTTP;
- sem regra de negócio;
- sem acoplamento direto à árvore física de `prompts/`.

### `DevComputaria.Prompts`

Biblioteca de catálogo responsável por:

- empacotar `prompts/**/*.yaml` como `EmbeddedResource`;
- carregar `catalog.yaml`;
- resolver aliases e versões;
- hidratar `PromptSpec` para o runtime;
- expor registro pronto para consumo via DI.

Restrições explícitas:

- sem lógica de domínio;
- sem provider;
- sem execução de evals em runtime.

## Fluxo esperado de consumo

O fluxo de uso pretendido é:

1. a lib de domínio define um `PromptId` pinado;
2. o runtime resolve e renderiza o prompt;
3. o resultado vira um `RenderedPrompt` com mensagens e metadados;
4. uma biblioteca externa de transporte envia isso ao provider.

Exemplo conceitual:

```csharp
private static readonly PromptId AnalyzeDocument =
	new("image-analysis.analyze-document", "1.0.0");

var rendered = await promptRenderer.RenderAsync(
	AnalyzeDocument,
	new PromptArgs(new Dictionary<string, object?>
	{
		["document_type"] = "identity-card",
		["country"] = "BR",
		["ocr_text"] = ocrText
	}));
```

O objeto renderizado é o contrato que segue adiante para o client do provider. O domínio continua limpo; o runtime continua neutro. Todo mundo feliz, inclusive o futuro rollback.

## Modelo de versionamento

O repositório adota **duas camadas de versão**:

### Versão do artefato

Cada prompt possui identidade canônica:

- path: `prompts/{domain}/{slug}/{semver}.yaml`
- id: `{domain}.{slug}`

Exemplo:

- arquivo: `prompts/image-analysis/analyze-document/1.0.0.yaml`
- id lógico: `image-analysis.analyze-document`

Regras de evolução:

- **PATCH**: ajustes textuais sem quebra de contrato;
- **MINOR**: adição compatível, como variável opcional ou include novo;
- **MAJOR**: quebra de contrato, como variável obrigatória nova ou schema de saída incompatível.

### Versão do pacote NuGet

O pacote `DevComputaria.Prompts` versiona o lote publicado.

- PATCH/MINOR: crescimento compatível do catálogo;
- MAJOR: remoção de versão pinável, quebra de loader ou quebra contratual do pacote.

Regras importantes:

- artefato publicado é **imutável**;
- mudança de prompt publicado gera **arquivo novo**, nunca edição no lugar;
- produção deve pinar **pacote + prompt + referências relevantes**.

## Estrutura do repositório

```text
DevComputaria.Prompting/
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── nuget.config
├── version.json
├── DevComputaria.Prompting.sln
├── DevComputaria.Prompting.slnx
├── README.md
├── CHANGELOG.md
├── LICENSE
│
├── schemas/
│   ├── prompt.schema.json
│   ├── catalog.schema.json
│   └── output/
│       └── image-analysis-document-v1.json
│
├── prompts/
│   ├── catalog.yaml
│   ├── _shared/
│   │   └── json-only.yaml
│   └── image-analysis/
│       └── analyze-document/
│           └── 1.0.0.yaml
│
├── evals/
│   └── image-analysis.analyze-document/
│       └── 1.0.0.cases.json
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
│   └── Consumer.ImageAnalysis/
│
├── tools/
│   ├── DevComputaria.Prompts.Cli/
│   └── DevComputaria.Prompts.SourceGen/
│
├── docs/
│   ├── ADR/
│   ├── PRD/
│   ├── plan/
│   └── task/
│
└── .github/
	└── workflows/
		├── validate-prompts.yml
		├── pack-promptkit.yml
		└── pack-prompts.yml
```

### Solution

- `DevComputaria.Prompting.sln`
- `DevComputaria.Prompting.slnx`

## Estratégia de qualidade e governança

O modelo foi desenhado para tratar prompt como contrato versionado, não como string incidental.

Isso implica:

- testes de runtime em `DevComputaria.PromptKit.Tests`;
- testes de empacotamento em `DevComputaria.Prompts.Tests`;
- testes de contrato em `DevComputaria.Prompts.Contract.Tests`;
- validação de consistência de `catalog.yaml`;
- observabilidade via `prompt.id`, `prompt.version` e `prompt.sha256`.

Os testes de contrato são parte da governança de publish: se o contrato quebra, o pacote não deveria seguir adiante.

## Documentação

### Arquitetura e decisões

- `docs/ADR/ADR-001-devcomputaria-prompt-catalog.md`
- `docs/ADR/ADR-002-catalog-versioning-rules.md`
- `docs/ADR/ADR-003-repository-layout-and-packaging-boundaries.md`

### Produto, plano e convenções

- `docs/PRD/PRD-DevComputaria.Prompting.md`
- `docs/plan/DESIGN-prompt-catalog.md`
- `docs/plan/PLAN-src-tests.md`
- `docs/CONVENTIONS-prompt-catalog.md`
- `docs/INDEX.md`

### Histórico

- `CHANGELOG.md`

## Resumo

`DevComputaria.Prompting` é uma proposta de biblioteca para transformar prompts em **artefatos versionados, auditáveis e distribuíveis**, com separação clara entre:

- catálogo;
- runtime;
- transporte;
- domínio.

O objetivo não é só “guardar prompts”, mas oferecer um contrato operacional robusto para uso real em produção .NET.
