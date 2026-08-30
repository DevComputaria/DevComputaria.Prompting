## PRD: DevComputaria Prompting Library

## 1. Product overview

### 1.1 Document title and version

- PRD: DevComputaria Prompting Library
- Version: 1.1

### 1.2 Product summary

Este documento define requisitos de produto e engenharia para implementar e operar duas bibliotecas de prompt em .NET com boundaries explícitos: `DevComputaria.PromptKit` (runtime agnóstico de domínio) e `DevComputaria.Prompts` (catálogo packed baseado em Git). O objetivo é garantir previsibilidade de execução, governança de versão, rastreabilidade de produção e rollout seguro para consumidores como Image Analysis e Prompt Management.

A arquitetura segue os ADRs vigentes: Git como source of truth dos artefatos textuais (`prompts/`, `schemas/`, `evals/`), empacotamento em NuGet para consumo de runtime, separação de responsabilidades entre renderização e transporte LLM (`Dev.AI`, fora deste repo), e políticas rígidas de pin/versionamento para evitar drift e regressões silenciosas.

## 2. Goals

### 2.1 Business goals

- Reduzir incidentes de produção por quebra de prompt/contrato para 0 por release.
- Institucionalizar release previsível de catálogo via NuGet com gates automáticos.
- Permitir evolução frequente de texto sem republicar libs de domínio desnecessariamente.
- Reduzir tempo de rollback para minutos via pin explícito de pacote + prompt.
- Criar trilha auditável de execução por `prompt.id`, `prompt.version`, `prompt.sha256`.

### 2.2 User goals

- Como equipe de domínio, quero renderizar prompt pinado sem I/O de arquivo em production.
- Como equipe de plataforma, quero boundaries claros para evitar acoplamento com provider.
- Como QA/arquitetura, quero validar schema, aliases e imutabilidade antes de publish.
- Como release manager, quero critérios objetivos para PATCH/MINOR/MAJOR.

### 2.3 Non-goals

- Implementar cliente/provider LLM (OpenAI/SK/MEAI) neste repositório.
- Executar loop de agentes/tool-calling no runtime do `PromptKit`.
- Introduzir registry remoto como fonte principal de prompts no v1.
- Acoplar DTO/negócio de domínio dentro dos pacotes de prompt.

## 3. User personas

### 3.1 Key user types

- Platform engineer (.NET libs e DI).
- Domain engineer (Image Analysis/Prompt Management).
- QA de contrato e testes de regressão textual.
- DevOps/release engineer.
- Security reviewer.

### 3.2 Basic persona details

- **Platform engineer**: implementa abstrações, DI, render, hash e observabilidade.
- **Domain engineer**: consome `PromptId` pinado e integra com `Dev.AI`.
- **QA de contrato**: mantém fixtures, valida schemas e gates de publicação.
- **DevOps/release engineer**: opera workflow CI e versionamento do pacote.
- **Security reviewer**: valida redaction e limites de exposição de dados.

### 3.3 Role-based access

- **Maintainer**: altera `prompts/`, `schemas/`, `skills/`, APIs públicas e release policies.
- **Contributor**: altera implementação com PR sujeito a testes obrigatórios.
- **Release manager**: executa tag/release NuGet e valida changelog de remoções.
- **Security reviewer**: aprova mudanças em redaction/sandbox e controles de logging.

## 4. Functional requirements

- **FR-01 Runtime boundary (`PromptKit`)** (Priority: Must)
  - `PromptKit` expõe abstrações (`PromptId`, `PromptSpec`, `PromptArgs`, `RenderedPrompt`, `RenderedMessage`) e interfaces (`IPromptCatalog`, `IPromptRenderer`, `IPromptComposer`, `IPromptSanitizer`).
  - `PromptKit` não referencia SDKs de provider, não executa HTTP e não contém regra de domínio.
  - `PromptKit` deve compor includes `_shared`, validar variáveis obrigatórias, renderizar template sandboxed e gerar SHA-256 determinístico.

- **FR-02 Catalog boundary (`Prompts`)** (Priority: Must)
  - `Prompts` embute `prompts/**/*.yaml` via `EmbeddedResource` e expõe catálogo pronto para runtime.
  - `Prompts` carrega `catalog.yaml`, resolve aliases e versões e hidrata `PromptSpec`.
  - `Prompts` não implementa lógica de negócio nem transporte LLM.

- **FR-03 Deterministic identity and versioning** (Priority: Must)
  - Convenção de identidade: `prompts/{domain}/{slug}/{semver}.yaml` => `PromptId("{domain}.{slug}","{semver}")`.
  - Imutabilidade de artefato publicado: mudança exige novo arquivo/version.
  - Pin obrigatório em produção: `PackageReference Dev.Prompts + PromptId versioned + skill/schema refs`.

- **FR-04 Security and sanitization** (Priority: Must)
  - `TemplateSandbox` bloqueia helpers inseguros (I/O, rede, execução arbitrária).
  - `PromptLogRedactor` mascara variáveis sensíveis (`redacted_in_logs`).
  - Erros devem preservar diagnóstico sem vazar payload sensível.

- **FR-05 Observability contract** (Priority: Must)
  - Todo render relevante deve registrar `prompt.id`, `prompt.version`, `prompt.sha256`.
  - `RenderedPrompt` inclui `PromptId`, `ContentSha256`, `PackageVersion`, `messages`, `hints`, e `tools` quando aplicável.

- **FR-06 Packaging policy** (Priority: Must)
  - `prompts/` entra no pacote de runtime.
  - `schemas/` e `evals/` ficam fora por padrão; `schemas/output` só entra quando necessário em production.
  - Remoção de versão pinável no lote exige MAJOR do pacote.

- **FR-07 Contract tests as publication gate** (Priority: Must)
  - `DevComputaria.Prompts.Contract.Tests` é gate obrigatório de publicação.
  - Falhas de schema/consistência/imutabilidade/fixtures bloqueiam publish.

- **FR-08 Extensibility v1.1** (Priority: Should)
  - `parts[].from` com `.md` irmão impactando hash.
  - `SchemaInjector` via `inject_as`/`inject_format`.
  - `SkillAttacher` e `PromptPins` (source generation).

## 5. User experience

### 5.1 Entry points & first-time user flow

- Host registra `AddPromptKit()` e `AddPackedPrompts()`.
- Domínio escolhe `PromptId` explícito (ex.: `image-analysis.analyze-document@1.0.0`).
- Chamada de render retorna mensagens + hash + metadados.
- Pipeline valida catálogo e bloqueia regressões contratuais.

### 5.2 Core experience

- **Pin explícito de versão**: evita flutuação de alias em produção.
- **Render com validação**: reduz erro tardio e garante previsibilidade.
- **Contrato de saída formal**: estabiliza parsing/DTO por versão.
- **Observabilidade orientada a conteúdo**: facilita replay de incidentes.

### 5.3 Advanced features & edge cases

- Alias para versão inexistente deve falhar em teste.
- Prompt com required ausente deve lançar erro específico.
- Divergência manifesto↔resources deve falhar no gate.
- Hash deve mudar quando include/skill/schema injetado mudar.
- `DirectoryOverride` só funciona em development; production ignora.

### 5.4 UI/UX highlights

- API de consumo pequena e orientada a contratos.
- Erros explícitos (`PromptNotFound`, `PromptVersionMismatch`, `MissingRequiredVariable`).
- Integração por DI previsível e auditável.

## 6. Narrative

A equipe de Image Analysis precisa evoluir prompts rapidamente sem arriscar regressão silenciosa. O texto é versionado no Git, empacotado em `Dev.Prompts`, e consumido em runtime pelo `PromptKit`, que valida variáveis, renderiza com sandbox e gera hash determinístico. O domínio apenas pina `PromptId` e envia `RenderedPrompt` para `Dev.AI`. Se um alias quebrar, um schema divergir, ou uma versão publicada for alterada indevidamente, os testes de contrato bloqueiam a entrega. Assim, o projeto equilibra velocidade de evolução textual e segurança técnica.

## 7. Success metrics

### 7.1 User-centric metrics

- 100% dos consumidores críticos com pin explícito de `PromptId`.
- Tempo médio de adoção de nova versão de prompt < 1 sprint.
- Taxa de falha por variável obrigatória ausente < 1% após estabilização.

### 7.2 Business metrics

- 0 incidentes de produção por prompt não versionado.
- Redução ≥ 50% em retrabalho de hotfix textual.
- Release cadence previsível com aprovação de contrato em CI.

### 7.3 Technical metrics

- 100% dos YAMLs validando contra schemas oficiais.
- 100% de consistência manifesto↔resources↔aliases.
- 100% de estabilidade hash para mesmo spec+args+refs.
- 100% dos PRs de catálogo executando `Contract.Tests` antes de merge.

## 8. Technical considerations

### 8.1 Integration points

- `DevComputaria.PromptKit` (runtime).
- `DevComputaria.Prompts` (packed catalog).
- `Dev.AI` (transporte; fora deste repo).
- Domínios consumidores (`ImageAnalysis`, `PromptManagement`).

### 8.2 Data storage & privacy

- Source of truth textual no Git.
- Runtime de production sem leitura direta de arquivo local/remoto.
- Variáveis sensíveis com redaction obrigatória em logs.

### 8.3 Scalability & performance

- Catálogo embedded reduz dependência de I/O e aumenta determinismo.
- Hash canônico habilita deduplicação e correlação de incidentes.
- Growth path: split em dois repositórios sem quebrar layout interno.

### 8.4 Potential challenges

- Drift entre aliases e versões se governança for fraca.
- Pressão para usar alias em produção em vez de pin explícito.
- Risco de acoplamento indevido do `PromptKit` com provider/domínio.
- Crescimento de artefatos por política de imutabilidade.

## 9. Milestones & sequencing

### 9.1 Project estimate

- Médio: 8 a 12 semanas para baseline + hardening.

### 9.2 Team size & composition

- 4 a 6 pessoas: 2 engenheiros .NET, 1 QA contrato, 1 DevOps, 1 TL/PM compartilhado.

### 9.3 Suggested phases

- **Fase 0**: schemas + catálogo mínimo + 1 prompt/version.
- **Fase 1**: `PromptKit` compilável + render + hash + DI + testes base.
- **Fase 2**: `Prompts` packed + loader + manifest + testes de pacote.
- **Fase 3**: contract tests + CLI validate/render + workflow gate.
- **Fase 4**: sample consumidor comprovando fluxo end-to-end sem provider real.
- **Fase 5**: extensões 1.1 (`from`, schema inject, skills, source gen).

### 9.4 Delivery split in two pull requests

- **PR-1 (Foundation + Runtime + Packed Catalog)**
  - Escopo: F0 + F1 + F2.
  - Conteúdo mínimo:
    - Estrutura `schemas/`, `prompts/`, `src/`, `tests/`.
    - `PromptKit` com abstrações, validator, renderer sandbox e hasher.
    - `Prompts` com embedded resources, `YamlPromptLoader`, `PackedPromptCatalog`, DI.
    - Testes: `PromptKit.Tests` + `Prompts.Tests` verdes.
  - Exit criteria:
    - Build de solution verde.
    - `Get("image-analysis.analyze-document","1.0.0")` funcional.
    - Hash determinístico comprovado.

- **PR-2 (Contract Gates + Tooling + Sample + Hardening)**
  - Escopo: F3 + F4 + F5 (quando habilitado).
  - Conteúdo mínimo:
    - `Prompts.Contract.Tests` completos (schema, consistency, immutability, fixtures).
    - CLI (`validate`, `render`, opcional `diff/list`) com exit codes.
    - Workflow `validate-prompts.yml` bloqueando publish em falha.
    - `samples/Consumer.ImageAnalysis` com `DirectoryOverride` dev-only.
    - Extensões 1.1 (`parts[].from`, `SchemaInjector`, `SkillAttacher`, `PromptPins`) se aprovadas.
  - Exit criteria:
    - Pipeline CI bloqueia alterações inválidas do catálogo.
    - Sample demonstra render + hash + metadados.
    - Regras de versionamento de ADR-002 operacionais.

## 10. User stories

### 10.1 Establish canonical repository contracts

- **ID**: GH-001
- **Description**: Como maintainer, quero layout canônico (`prompts/`, `schemas/`, `evals/`, `src/`, `tests/`) para padronizar ownership e governança.
- **Acceptance criteria**:
  - Estrutura mínima criada e documentada.
  - Convenção de identidade path=>id validada em testes.
  - `catalog.yaml` lista somente artefatos existentes.

### 10.2 Implement `PromptKit` core abstractions

- **ID**: GH-002
- **Description**: Como platform engineer, quero tipos imutáveis e interfaces estáveis para reduzir acoplamento com domínio/provider.
- **Acceptance criteria**:
  - Records/classes imutáveis para `PromptId`, `PromptSpec`, `PromptArgs`, `RenderedPrompt`.
  - Interfaces públicas cobertas por testes básicos de integração.
  - Sem dependências de provider no projeto.

### 10.3 Resolve prompt by id/version

- **ID**: GH-003
- **Description**: Como domain engineer, quero lookup por `id + version` para consumo determinístico.
- **Acceptance criteria**:
  - Retorno bem-sucedido para versão existente.
  - Exceção específica para prompt inexistente.
  - Mensagens de erro com contexto suficiente para troubleshooting.

### 10.4 Enforce variable validation

- **ID**: GH-004
- **Description**: Como QA, quero falha antecipada para required ausente.
- **Acceptance criteria**:
  - Required ausente dispara `MissingRequiredVariableException`.
  - Opcionais não bloqueiam render.
  - Cobertura de cenários positivos/negativos.

### 10.5 Render with secure sandbox

- **ID**: GH-005
- **Description**: Como security reviewer, quero impedir helpers inseguros em templates.
- **Acceptance criteria**:
  - Substituição de variáveis e condicionais suportadas.
  - Helpers de I/O/rede bloqueados.
  - Logs de erro sem vazamento de dados sensíveis.

### 10.6 Produce stable content hash

- **ID**: GH-006
- **Description**: Como SRE, quero hash determinístico para replay e auditoria.
- **Acceptance criteria**:
  - Mesmo spec+args+refs => mesmo SHA-256.
  - Mudanças relevantes alteram hash.
  - Teste de estabilidade contínuo.

### 10.7 Register services via DI

- **ID**: GH-007
- **Description**: Como integrador, quero bootstrap simples por `AddPromptKit`/`AddPackedPrompts`.
- **Acceptance criteria**:
  - Serviços essenciais resolvidos por DI.
  - Opções (`StrictPins`, `AllowDirectoryOverride`) testadas.
  - Configuração de production ignora override local.

### 10.8 Implement packed catalog loader

- **ID**: GH-008
- **Description**: Como maintainer, quero converter YAML em `PromptSpec` e servir via catálogo embedded.
- **Acceptance criteria**:
  - `YamlPromptLoader` cobre campos canônicos.
  - `PackedPromptCatalog` entrega prompts válidos.
  - `LogicalName` de resources está estável.

### 10.9 Enforce manifest/resource consistency

- **ID**: GH-009
- **Description**: Como release manager, quero detectar drift entre manifesto e recursos embutidos.
- **Acceptance criteria**:
  - Testes falham para órfãos e referências quebradas.
  - Aliases inválidos falham no gate.
  - Publicação bloqueada em inconsistência.

### 10.10 Validate schema and immutability gates

- **ID**: GH-010
- **Description**: Como QA contrato, quero garantir validade semântica e imutabilidade de versões publicadas.
- **Acceptance criteria**:
  - YAML inválido contra schema falha.
  - Alteração retroativa de bytes falha.
  - Fixtures de render detectam regressão de conteúdo.

### 10.11 Provide CLI contract commands

- **ID**: GH-011
- **Description**: Como contributor, quero `validate`/`render` para feedback local antes de abrir PR.
- **Acceptance criteria**:
  - Exit code 0/1 coerente.
  - Output legível para erro de schema/alias/include.
  - Documentação mínima de uso presente.

### 10.12 Implement CI publish gates

- **ID**: GH-012
- **Description**: Como DevOps, quero workflow que bloqueie merge/publish em quebra de contrato.
- **Acceptance criteria**:
  - Workflow executa bateria de testes relevante.
  - Falha de contrato interrompe pipeline.
  - Gate obrigatório para release de pacote.

### 10.13 Deliver end-to-end sample

- **ID**: GH-013
- **Description**: Como domínio consumidor, quero sample sem provider real para validar integração completa.
- **Acceptance criteria**:
  - `samples/Consumer.ImageAnalysis` compila.
  - Render exibe `messages`, `sha256`, metadados.
  - `DirectoryOverride` ativo somente em development.

### 10.14 Implement extension set v1.1

- **ID**: GH-014
- **Description**: Como plataforma, quero avançar contrato com `parts[].from`, schema injection e skills anexadas.
- **Acceptance criteria**:
  - `parts[].from` resolvido e hash atualizado.
  - `SchemaInjector` funcional por `inject_as`.
  - `SkillAttacher` preenche `RenderedPrompt.Tools`/messages.

### 10.15 Security, authorization and release governance

- **ID**: GH-015
- **Description**: Como security/release reviewer, quero políticas de acesso, redaction e SemVer formalizadas.
- **Acceptance criteria**:
  - Papéis de maintainer/contributor/release manager definidos.
  - Redaction validada por testes.
  - Regras de MAJOR/MINOR/PATCH aplicadas em checklist de release.

## 11. Out of scope safeguards

- Não introduzir `latest` como seletor de produção.
- Não embutir DTO de domínio nas libs de prompt.
- Não acoplar `PromptKit` ao provider.
- Não mover source of truth para banco/serviço remoto no v1.

## 12. Approval

Com aprovação deste PRD revisado, a execução deve abrir **dois PRs** conforme seção 9.4. Em seguida, as user stories podem ser convertidas em issues rastreáveis com labels por fase (`F0`..`F5`) e vínculo ao PR correspondente (`PR-1` ou `PR-2`).