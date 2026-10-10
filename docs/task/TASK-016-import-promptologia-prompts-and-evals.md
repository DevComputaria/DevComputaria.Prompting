# GH-016 — Import Promptologia prompts and implement eval/property suite

- **Status:** done
- **PR bucket:** PR-2
- **Phase:** F5
- **Source:** quality extension requested after GH-015

## Objective

Trazer os prompts executáveis do repositório Promptologia para o catálogo e
validá-los com casos de uso determinísticos e propriedades gerais do runtime.

## Scope

- Adaptar os dez artefatos de `grimorio/artefatos` ao contrato YAML versionado.
- Preservar licença, autoria e commit de origem.
- Criar um caso principal e um caso de limite/segurança para cada prompt.
- Executar evals sem provider externo e propriedades generativas com FsCheck.
- Tornar a suíte bloqueante em pull requests e pushes para `main`.

## Acceptance criteria

- [x] Todo prompt importado está no catálogo e possui proveniência documentada.
- [x] Toda versão importada possui ao menos dois casos de eval locais.
- [x] Fixtures validam renderização e oráculos positivos/negativos da resposta.
- [x] Propriedades cobrem determinismo, sandbox, required args, hash e redaction.
- [x] A suíte executa no workflow usado antes da geração/publicação de pacotes.

## Implementation notes

- A origem foi fixada no commit `4c45710dc2be5c1b2ea7d43ced62f7501629ce10`.
- Outputs são fixtures curadas, não afirmações produzidas em tempo de teste por um
  modelo. Isso mantém CI reproduzível e permite substituir a fixture por output de
  um modelo local em revisão explícita futura.
- A importação não inclui READMEs, templates de projeto, integrações Perplexity ou
  material legado de pentest fora de `grimorio/artefatos`.
