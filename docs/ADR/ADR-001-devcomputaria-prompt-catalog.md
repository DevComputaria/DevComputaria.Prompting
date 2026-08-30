# ADR-001 — Catálogo versionado de prompts como NuGet (PromptKit + Prompts)

- **Status:** Aceito
- **Data:** 2026-08-28
- **Decisores:** Engenharia de harness / libs de integração com IA
- **Escopo:** Gestão, versionamento e consumo de prompts por libs de domínio (Image Analysis, Prompt Management, fraude, etc.) e pela lib de providers de LLM
- **Fora de escopo:** Orquestração de agentes, tool-calling loop, playground, registry remoto, A/B server-side

---

## 1. Contexto

Várias libs de domínio vão usar LLM (exemplo: `Dev.ImageAnalysis` analisa documentos). A tentação inicial é:

1. colocar a string do prompt dentro da lib de domínio; e
2. fazer a chamada HTTP ao provider na mesma lib.

Isso gera drift entre componentes, rollback impossível, release acoplado (“publicar `Dev.ImageAnalysis` por causa de um few-shot”) e contrato de saída implícito.

Há três necessidades simultâneas:

- versionar o **texto** no Git (review, diff, histórico);
- entregar o texto de forma **reutilizável** para várias libs via NuGet;
- manter a chamada ao provider em **uma** lib (`Dev.AI`), sem o domínio conhecer HTTP/SDK do modelo.

O ecossistema já tem ferramentas (PromptLayer, Langfuse, Semantic Kernel, `promptlib`). Nenhuma resolve o recorte local-first + contrato pinado + várias libs .NET internas consumindo o mesmo lote.

---

## 2. Decisão

Adotar **três papéis** e **dois pacotes NuGet de prompt**, com o Git como fonte da verdade do texto.

### 2.1 Papéis

| Papel | Pacote | Responsabilidade | Não faz |
|---|---|---|---|
| Texto / catálogo | `Dev.Prompts` | YAML empacotado, IDs, aliases, schemas de saída | Chamar LLM, conhecer Image Analysis/Prompt Management |
| Motor | `DevComputaria.PromptKit` | Catálogo, render, validação de variáveis, hash, DI | Domínio, provider |
| Transporte | `Dev.AI` | Client do provider (OpenAI, Azure, Ollama…) | Guardar texto de prompt |
| Negócio | `Dev.ImageAnalysis`, `Dev.PromptManagement`, … | Caso de uso e DTO de saída | HTTP de LLM, string de prompt |

`PromptKit` e `Dev.Prompts` são as “libs de prompt”. `Dev.AI` é vizinha obrigatória no desenho, mas não faz parte do catálogo.

### 2.2 Fonte da verdade e injeção

- O YAML vive em Git, sob `prompts/{dominio}/{slug}/{semver}.yaml`.
- Identidade canônica: `PromptId("{dominio}.{slug}", "{semver}")`.
- No `dotnet pack`, a árvore inteira de `prompts/` vira `EmbeddedResource` de `Dev.Prompts`.
- Em runtime **não** se lê arquivo do disco em Production.
- A “injeção” é DI: `IPromptCatalog` / `IPromptRenderer` registrados por `AddPackedPrompts()`.
- A lib de domínio pede um `PromptId` pinado, recebe `RenderedPrompt` (mensagens + hash + metadados) e entrega isso ao `Dev.AI`.

Não existe `File.ReadAllText("prompt.yaml")` em produção.  
Não se passa o YAML como parâmetro do caso de uso.  
Não se busca prompt em Git/HTTP na hora da request.

### 2.3 Um catálogo, todos os prompts

Um único `Dev.Prompts` contém **todos** os prompts vigentes de todos os domínios (Image Analysis, Prompt Management, fraude, `_shared`, …).

- Consumidor restaura **um** `PackageReference`.
- Cada lib pina só os IDs que usa.
- Não há `Dev.Prompts.ImageAnalysis` no v1.

Fatiar por domínio só quando ciclo de release, ACL ou ruído de CI justificarem.

### 2.4 Duas camadas de versão

| Camada | Onde | Exemplo | SemVer |
|---|---|---|---|
| Prompt | YAML + `PromptId` | `image-analysis.analyze-document@1.4.0` | PATCH = typo; MINOR = var opcional / few-shot; MAJOR = contrato (vars obrigatórias ou schema de saída) |
| Pacote | NuGet + Git tag | `Dev.Prompts.3.8.0` | PATCH/MINOR = lote cresce de forma compatível; MAJOR = removeu versão pinável, quebrou loader/API |

Regras:

- Versão publicada de prompt é **imutável**. Mudança = arquivo novo (`1.4.1.yaml`, não edit de `1.4.0.yaml`).
- O nupkg é um **lote**. Pode (e deve) conter várias versões do mesmo ID.
- Lib de domínio pina **PromptId + versão do nupkg**. Não usa `latest`.
- Alias (`prod`, nome sem versão) é atalho de dev/demo. Caminho de Image Analysis/Prompt Management/fraude em produção usa versão explícita.
- História longa fica no Git. O nupkg guarda o vigente + versões ainda pinadas + última da major anterior.
- Remover uma versão do lote = major do NuGet.

Versionamento do pacote: `Nerdbank.GitVersioning` (`version.json`). Tag `v3.8.0` = aquele tree de `prompts/`.

### 2.5 Contrato de consumo (obrigatório)

Libs de domínio usam a **Forma A**:

```
Dev.ImageAnalysis → IPromptRenderer.Render(PromptId, args)
       → IChatClient.CompleteJson<T>(RenderedPrompt)
```

`Dev.AI` não conhece `image-analysis.analyze-document`.
`Dev.Prompts` não conhece o client HTTP.  
Schema de saída versiona **junto** com o prompt (MAJOR compartilhado com o DTO da lib de domínio).

### 2.6 Contrato de runtime mínimo

```
PromptId
PromptSpec
IPromptCatalog
IPromptRenderer
RenderedPrompt          // messages + PromptId + ContentSha256 + PackageVersion + hints
```

Observabilidade obrigatória nos spans: `prompt.id`, `prompt.version`, `prompt.sha256`.

Override de pasta local (`DirectoryPromptCatalog`) existe **somente** em Development, atrás de options. Production ignora.

### 2.7 O que a lib de prompt não é

Fora do v1 e fora deste ADR:

- loop de agente / tools / circuit breaker;
- Semantic Kernel / MEAI / SDK de provider como dependência do Kit;
- registry remoto, fetch em runtime, A/B;
- UI / playground;
- um nupkg por prompt;
- persistência em banco “prompt registry”.

---

## 3. Estrutura canônica

```
DevComputaria.Prompting/
├── version.json
├── schemas/                      # prompt.schema.json, catalog.schema.json, output/*
├── prompts/                      # SOURCE OF TRUTH
│   ├── catalog.yaml
│   ├── _shared/
│   └── {dominio}/{slug}/{semver}.yaml
├── evals/                        # não entra no nupkg de runtime
├── src/
│   ├── DevComputaria.PromptKit/  # motor
│   └── DevComputaria.Prompts/    # catálogo (embedded + PackedPromptCatalog)
├── tests/
│   ├── PromptKit.Tests
│   ├── Prompts.Tests
│   └── Prompts.Contract.Tests    # schema + fixtures + snapshot
├── samples/Consumer.PromptManagementIntent/
└── tools/Prompts.Cli/            # validate, list, diff, render dry-run
```

`Dev.Prompts.csproj` referencia a raiz `prompts/` como `EmbeddedResource`.  
`evals/` e `schemas/` de validação ficam no Git e no job de CI. Schema de *output* só entra no nupkg se o consumidor validar JSON em produção.

---

## 4. Fluxo de release

```
editar/criar prompts/{domínio}/{slug}/{semver}.yaml
  → atualizar catalog.yaml
  → PR + Contract.Tests + CLI validate
  → merge + tag vX.Y.Z
  → pack/push Dev.Prompts X.Y.Z
  → consumidor sobe PackageReference
  → lib de domínio só troca PromptId quando aceitar o contrato novo
```

Rollback: pin anterior do `PromptId` (se o lote ainda contém a versão) **ou** `PackageReference` anterior. Nunca “corrigir no lugar” um YAML já publicado.

---

## 5. Alternativas consideradas

### 5.1 String no código da lib de domínio

Descartado. Drift, sem diff de prompt isolado, release de negócio acoplado ao texto.

### 5.2 Fetch runtime (Git raw, HTTP, Langfuse, PromptLayer)

Descartado no v1. Quebra determinismo, offline, auditoria e o requisito local-first. Introduz cache, indisponibilidade e superfície de tampering. Pode coexistir depois como *overlay* explícito, nunca como source of truth das libs internas.

### 5.3 Um nupkg por prompt

Descartado. Inferno de restore e de alinhamento de versões entre Image Analysis/Prompt Management/fraude.

### 5.4 Git submodule da pasta `prompts/` em cada consumidor

Descartado como contrato. Funciona em time pequeno e disciplinado; NuGet + CI é o contrato que o restante do ecossistema .NET já opera.

### 5.5 Semantic Kernel YAML como formato interno único

Descartado como *único* formato. SK é export opcional. Dependência de SK no Kit vaza para toda lib de domínio.

### 5.6 Forma B — `Dev.AI` recebe `PromptId` e renderiza por baixo

Adiada. Deixa o domínio mais limpo, mas transforma o client de provider em orquestrador que conhece IDs de negócio. Só revisitar se `Dev.AI` for explicitamente uma lib de *aplicação* e não de transporte.

### 5.7 Pacotes `Dev.Prompts.ImageAnalysis` / `Dev.Prompts.PromptManagement` no dia 1

Adiado. YAML é barato; o custo é processo. Fatiar quando o lote único virar ruído de release ou requisito de ACL.

---

## 6. Consequências

### Positivas

- Texto reviewável no Git; lote reproduzível no NuGet.
- Uma mudança de few-shot não publica `Dev.ImageAnalysis`.
- Várias libs compartilham o mesmo catálogo sem copiar string.
- Hash + ID no span tornam incidente auditável (“qual prompt estava em prod”).
- Contrato de saída e prompt sobem de major juntos.
- Override local permite iterar sem packar.

### Negativas / custo

- Dois números para gerenciar (prompt + nupkg). Disciplina de pin é obrigatória.
- Lote único: patch de fraude republica o pacote que o domínio de Image Analysis restaura (sem quebrar, mas gera ruído de versão).
- Imutabilidade exige arquivo novo por mudança — pasta cresce.
- Sem eval no mesmo PR, o Git só versiona texto, não qualidade. `Prompts.Contract.Tests` é parte da decisão, não opcional.
- Consumidor desatualizado pode pinar versão ausente depois de um major que limpou o lote.

### Obrigações de compliance da decisão

1. Todo prompt público listado em `catalog.yaml`.
2. CI recusa pack se YAML órfão, alias quebrado ou schema inválido.
3. Lib de domínio em caminho de produção declara versão explícita.
4. Spans de chamada LLM carregam `prompt.id` / `version` / `sha256`.
5. `Dev.Prompts` não referencia SDK de provider.
6. `Dev.AI` não embute YAML de domínio.
7. Production não habilita `DirectoryOverride`.

---

## 7. Exemplo de amarração

```csharp
// Dev.ImageAnalysis — negócio
private static readonly PromptId AnalyzeDoc =
    new("image-analysis.analyze-document", "1.4.0");

public Task<ImageAnalysisDocumentResult> AnalyzeAsync(OcrDocument doc, CancellationToken ct)
{
    var rendered = _prompts.Render(AnalyzeDoc, new PromptArgs
    {
        ["document_type"] = doc.Type,
        ["ocr_text"] = doc.Text
    });
    return _ai.CompleteJsonAsync<ImageAnalysisDocumentResult>(rendered, ct);
}
```

```csharp
// Host
services.AddPackedPrompts();           // Dev.Prompts
services.AddAiClient(configuration);   // Dev.AI
services.AddImageAnalysis();           // Dev.ImageAnalysis
```

```xml
<!-- Host / Dev.ImageAnalysis -->
<PackageReference Include="DevComputaria.PromptKit" Version="1.x" />
<PackageReference Include="Dev.Prompts" Version="3.8.0" />
<PackageReference Include="Dev.AI" Version="1.1.0" />
```

---

## 8. Critérios de revisão deste ADR

Reabrir a decisão se qualquer um ocorrer:

- necessidade comprovada de editar prompt em produção sem republish de nupkg;
- ACL entre domínios (fraude não pode ver crédito);
- cadência de publish do lote único inviabilizar o restante das libs;
- exigência regulatória de registry auditável fora do assembly.

Até lá, Git = texto; NuGet = lote imutável; domínio pina ID; `Dev.AI` só transporta.

---

## 9. Referências internas

- Análise de recorte: duas libs (Kit + catálogo), YAML como recurso embutido
- Versionamento em duas camadas (prompt × pacote)
- Política de catálogo único no v1
- Consumo Forma A (domínio renderiza, `Dev.AI` envia)
- Layout físico e boundaries de implementação: [ADR-003](./ADR-003-repository-layout-and-packaging-boundaries.md)
