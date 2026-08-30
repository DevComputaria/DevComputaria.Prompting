# GH-015 — Security, authorization and release governance

- **Status:** todo
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

- [ ] Papéis e responsabilidades publicados.
- [ ] Redaction validada por testes.
- [ ] Processo de versionamento aplicado de forma verificável.
