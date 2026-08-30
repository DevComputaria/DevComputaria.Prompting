# ADR-002 — Regras de versionamento do catálogo de prompts

- **Status:** Aceito
- **Data:** 2026-08-29
- **Decisores:** Engenharia de harness / libs de integração com IA
- **Relacionado:** [ADR-001](./ADR-001-devcomputaria-prompt-catalog.md), [ADR-003](./ADR-003-repository-layout-and-packaging-boundaries.md)
- **Escopo:** Como versionar prompts, skills, schemas de saída e o lote NuGet `Dev.Prompts`
- **Fora de escopo:** Versionamento de `Dev.AI` (provider), semver de `PromptKit` além do impacto no loader, registry remoto

---

## 1. Contexto

O ADR-001 definiu o catálogo como lote NuGet gerado a partir de YAML no Git. Sem regras explícitas de versão, o time mistura três números:

- a versão do **texto** (`image-analysis.analyze-document@1.4.0`);
- a versão do **pacote** (`Dev.Prompts.3.8.0`);
- a versão da **lib de domínio** (`Dev.ImageAnalysis.2.3.0`).

Sintomas já conhecidos: editar YAML já publicado, pinar `latest`/`prod` em caminho de Image Analysis, colar JSON de saída só no texto do system, e anexar skill sem pin.

Este ADR congela as regras. O ADR-001 continua valendo para recorte de pacotes e injeção.

---

## 2. Decisão

Versionar em **duas camadas**. Git é histórico. NuGet é lote imutável. Consumidor pina artefato, não “o que veio no latest”.

### 2.1 Camadas

| Camada | Identifica | Exemplo | Quando sobe |
|---|---|---|---|
| Artefato | Um prompt, uma skill ou um schema de saída | `image-analysis.analyze-document@1.4.0` | Mudou aquele contrato ou texto |
| Lote | O nupkg `Dev.Prompts` inteiro | `Dev.Prompts.3.8.0` | Publicou o tree |

`Dev.ImageAnalysis` pede `PromptId("image-analysis.analyze-document", "1.4.0")`.
`3.8.0` só significa: este pacote **contém** esse arquivo (e o restante do tree da tag).

### 2.2 Imutabilidade

Arquivo publicado não se edita.

```
prompts/{dominio}/{slug}/{semver}.yaml
skills/{dominio}/{slug}/{semver}.yaml
schemas/output/{contrato}-vN.json
```

Mudança = path novo (`1.4.1.yaml`, `image-analysis-document-v2.json`).
Alterar bytes de `1.4.0.yaml` depois que ele entrou em um nupkg tagueado é violação deste ADR: o pin passa a mentir.

### 2.3 SemVer do artefato (prompt e skill)

- **PATCH** (`1.4.0` → `1.4.1`): typo, prosa, few-shot extra sem mudar variáveis, includes, tools ou schema de saída.
- **MINOR** (`1.4.1` → `1.5.0`): variável opcional nova, include novo, pin de skill novo sem quebrar tools, instrução mais rica. Caller antigo ainda renderiza.
- **MAJOR** (`1.x` → `2.0.0`): variável obrigatória adicionada/removida/renomeada; mudança de papéis; mudança de contrato de tool; mudança de schema de saída.

Skill segue a mesma escala. MAJOR de skill (parâmetros de tool, contrato do `body`) obriga o prompt que a anexa a atualizar o pin e, em geral, subir a própria versão.

### 2.4 Schema de saída

Schema não usa SemVer `x.y.z`. Usa **id de contrato**:

```
schemas/output/image-analysis-document-v1.json
schemas/output/image-analysis-document-v2.json
```

`v1` → `v2` quando o JSON quebra. Esse bump **obriga** MAJOR do prompt e MAJOR do DTO na lib de domínio (`ImageAnalysisDocumentResult`).

No YAML do prompt:

```yaml
output:
  kind: json
  schema_ref: schemas/output/image-analysis-document-v1.json
  inject_as: output_schema
  inject_format: compact_example
```

- `schema_ref` é o contrato formal (CI, `CompleteJsonAsync<T>`).
- `inject_as` + `inject_format` definem o que o **modelo** vê no `{{output_schema}}` (exemplo compacto, não dump de JSON Schema).
- O domínio **não** passa o schema em `PromptArgs`.
- Preferir não mutar `image-analysis-document-v1.json`. Cortar `v2`.

O JSON que hoje ia no final da instrução enviada vive no `template` (texto) **e/ou** em `{{output_schema}}` resolvido do ref. São a vista humana/LLM. A fonte da verdade para código é o arquivo em `schemas/output/`.

### 2.5 Skills anexadas

Skill é artefato irmão, ID próprio, mesmo lote.

```yaml
skills:
  - id: image-analysis.document-extractor
    version: 1.0.0
    attach: system          # body no system
    # attach: tool          # tools[] no Dev.AI
```

- Prompt aponta pin explícito. Não existe “mande a skill current”.
- `attach: system` concatena `body` no request.
- `attach: tool` / `kind: tool|both` entrega `RenderedPrompt.Tools` para `Dev.AI`.
- Mudou a skill → arquivo novo. Prompt que quer a skill nova sobe só o pin (`1.0.0` → `1.1.0`): MINOR do prompt se a saída não mudou.
- Hash do render inclui `body` + tools da skill.

### 2.6 SemVer do lote (`Dev.Prompts`)

- **PATCH**: só PATCH de artefato; nenhum pin removido; aliases iguais ou apontando para PATCH.
- **MINOR**: artefato novo ou MINOR de existente; pins antigos **permanecem** no nupkg.
- **MAJOR**: removeu versão ainda pinável, quebrou `catalog.yaml` / loader / `LogicalName` dos resources.

Versão do lote: `Nerdbank.GitVersioning`. Tag `v3.8.0` = aquele tree de `prompts/` + `skills/` + `schemas/output/`.

### 2.7 Pins obrigatórios em produção

Três pins distintos:

```
PackageReference Include="Dev.Prompts" Version="3.8.0"
PromptId("image-analysis.analyze-document", "1.4.0")
skills[].version / output.schema_ref
```

- Pin do nupkg → restore reproduzível.
- Pin do `PromptId` → domínio não flutua se o alias do catálogo andar.
- Pin de skill/schema → hash estável.

Aliases em `catalog.yaml` (`image-analysis.analyze-document: 1.4.0`) servem sample e Development. Caminho de Image Analysis / Prompt Management / fraude / pagamento usa versão explícita.

Proibido como seletor único em lib de domínio: `latest`, `prod`, omitir versão.

### 2.8 O que o lote guarda

No nupkg vigente:

- todo prompt/skill **em uso**;
- toda versão ainda pinada por lib de domínio publicada;
- última versão da major anterior, por janela combinada.

Histórico longo fica no Git. Tirar arquivo ainda pinável do lote = MAJOR do NuGet + nota no changelog (`removed image-analysis.analyze-document@1.0.0`).

### 2.9 Hash de render

`ContentSha256` cobre:

- YAML do prompt após `includes`;
- vista injetada do schema (`{{output_schema}}`), se houver;
- `body` e tools das skills anexadas.

Mesmo `PromptId` + mesmo nupkg + mesmos args ⇒ mesmo hash. Incidente reproduz o lote, não o `main` do Git.

Caller que enviar `output_schema` (ou texto de skill) em `PromptArgs` em Production é erro: o ref é a fonte.

### 2.10 Quem publica o quê

| Mudança | Sobe |
|---|---|
| Só prosa do prompt | artefato PATCH + lote PATCH/MINOR |
| Variável opcional / skill pin novo | artefato MINOR + lote MINOR |
| Shape do JSON de saída | schema `vN+1` + prompt MAJOR + domínio MAJOR |
| Tool/contrato da skill | skill MAJOR + prompt que anexa (MINOR ou MAJOR) |
| Loader / API do Kit | `PromptKit` (pacote separado) |
| HTTP do provider | `Dev.AI` apenas |

`Dev.AI` não versiona texto. Domínio não versiona texto. Domínio só sobe quando o DTO ou o pin de `PromptId` muda.

### 2.11 Gates de CI (fazem parte da regra)

Pack recusado se:

- arquivo no tree fora de `catalog.yaml`, ou entrada no catálogo sem arquivo;
- alias aponta versão inexistente;
- `schema_ref` ou pin de skill ausente;
- path não bate com `id` + `version`;
- bytes de artefato já tagueado mudaram sem arquivo de versão novo.

---

## 3. Alternativas consideradas

### 3.1 Um número só (versão do nupkg = versão do prompt)

Descartado. Um lote tem dezenas de artefatos. Image Analysis não pode virar `3.8.0` porque fraude ganhou um PATCH.

### 3.2 Alias `prod` como contrato das libs de domínio

Descartado em produção. Alias anda sem o domínio saber. Rollback deixa de ser um pin.

### 3.3 Mutar o YAML publicado e compensar no Git

Descartado. Pin deixa de ser reproduzível; hash mente; incidente não replaya.

### 3.4 Schema só no texto final do system, sem `output.schema_ref`

Descartado como fonte da verdade. O modelo precisa do JSON no `template` ou em `{{output_schema}}`; o código precisa do arquivo formal. Os dois descrevem a mesma saída; só o arquivo valida.

### 3.5 Skill colada no `system` do prompt, sem artefato próprio

Descartado quando a skill é reusada ou tem tools. Duplica texto e impede versionar a skill sem republicar o prompt por motivo errado. Skill mínima one-shot pode viver no `template`; skill enviada “junto” de forma recorrente vira artefato.

### 3.6 Um nupkg por prompt

Já descartado no ADR-001. Permanece descartado.

---

## 4. Consequências

### Positivas

- Rollback = pin anterior ou nupkg anterior.
- Review de prompt não publica `Dev.ImageAnalysis`.
- Schema e DTO sobem de major juntos.
- Skill reutilizável entre prompts com pin explícito.
- Span com `prompt.id` / `version` / `sha256` auditável.

### Custo

- Dois números visíveis (artefato + lote). Disciplina de pin é regra, não estilo.
- Pasta cresce (`1.4.0`, `1.4.1`, `1.5.0` lado a lado).
- Lote único: PATCH de fraude republica o pacote que o domínio de Image Analysis restaura (sem quebrar o pin).
- CI de imutabilidade é obrigatória; sem ela a regra não existe.

### Obrigações

1. Nenhum artefato publicado é editado no lugar.
2. Produção declara `PromptId` com versão, `PackageReference` com versão, refs de skill/schema com versão/path.
3. `ContentSha256` inclui schema injetado e skills.
4. Remoção de versão do lote só em MAJOR do NuGet.
5. Alias não é seletor de caminho crítico.

---

## 5. Exemplo

```
Dev.Prompts 3.8.0
  prompts/image-analysis/analyze-document/1.4.0.yaml
    output.schema_ref: schemas/output/image-analysis-document-v1.json
    skills: [{ id: image-analysis.document-extractor, version: 1.0.0 }]
  skills/image-analysis/document-extractor/1.0.0.yaml
  schemas/output/image-analysis-document-v1.json

Dev.ImageAnalysis 2.3.0
  pina PromptId("image-analysis.analyze-document", "1.4.0")
  PackageReference Dev.Prompts 3.8.0
```

Mudou só um parágrafo do system → `1.4.1.yaml` + `Dev.Prompts 3.8.1`. Image Analysis 2.3.0 segue válida no `1.4.0` enquanto o lote ainda o empacota.

Mudou o JSON de resposta → `image-analysis-document-v2.json` + prompt `2.0.0.yaml` + `Dev.ImageAnalysis 3.0.0`.

---

## 6. Critérios de revisão

Reabrir se:

- for obrigatório editar prompt em produção sem novo nupkg;
- ACL exigir fatiar o lote por domínio (aí o SemVer do lote se replica por pacote);
- hash com schema injetado ficar instável por formatação JSON (aí canonicar o serialize no Kit).

Até lá: arquivo novo por mudança; pin em tudo que entra no request ou na validação; lote versiona o tree; domínio versiona o contrato de negócio.
