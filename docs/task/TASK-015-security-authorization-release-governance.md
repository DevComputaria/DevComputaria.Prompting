# GH-015 — Security, authorization and release governance

- **Status:** done
- **PR bucket:** PR-2
- **Phase:** F3-F5
- **Source:** PRD 10.15, ADR-001 section 6, ADR-002 section 2

## Objective

Formalizar políticas de segurança, acesso e governança de release para operação contínua.

## Scope

- Definir papéis (`maintainer`, `contributor`, `release manager`, `security reviewer`).
- Garantir redaction de variáveis sensíveis em logs.
- Aplicar checklist de SemVer (PATCH/MINOR/MAJOR) no processo de release.

## Deliverables

- Checklist de release e compliance.
- Evidências de testes de redaction e de gates de governança.

## Dependencies

- GH-012.

## Acceptance criteria

- [x] Papéis e responsabilidades publicados.
- [x] Redaction validada por testes.
- [x] Processo de versionamento aplicado de forma verificável.

## Implementation notes

- `docs/SECURITY-AND-RELEASE-GOVERNANCE.md` publica papéis, matriz de autorização,
  política de logging, controles do repositório e checklist de release/compliance.
- `PromptLogRedactor` mascara variáveis `redacted_in_logs`, omite argumentos não
  declarados e está registrado no bootstrap de DI.
- `release-check` calcula o impacto mínimo do diff, impede mutação retroativa de
  artefatos e valida a transição concreta `X.Y.Z` do pacote.
- `release-governance.yml` exige um único label `semver:*`; os workflows de pack
  validam base, impacto e versões antes de criar o artefato.
- A evidência automatizada está em `PromptLogRedactorTests`,
  `ReleaseGovernanceValidatorTests` e nos gates de CI.
