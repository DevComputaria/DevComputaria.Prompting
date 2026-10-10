# DevComputaria.Prompts.Cli

Ferramenta local para validar o contrato do catálogo e testar a renderização de
um prompt pinado antes de abrir um PR.

## Uso

Execute a partir da raiz do repositório:

```bash
dotnet run --project tools/DevComputaria.Prompts.Cli -- validate
```

`validate` verifica todos os YAMLs contra os schemas, a consistência entre
`catalog.yaml` e os arquivos, aliases, identidade `path => id@version`, includes
e referências a schemas de saída. Use `--root <caminho>` fora da raiz do repo.

Para renderizar o fixture base:

```bash
dotnet run --project tools/DevComputaria.Prompts.Cli -- render \
  image-analysis.analyze-document@1.0.0 \
  --arg country=BR \
  --arg document_type=identity-card \
  --arg ocr_text=OCR-123
```

Também é possível fornecer um objeto JSON com `--args caminho/args.json`.
Opções `--arg key=value` podem ser repetidas e sobrescrevem valores do arquivo.
O resultado de `render` é JSON com mensagens, hash e metadados.

## Exit codes

- `0`: comando concluído com sucesso;
- `1`: erro de uso, validação ou renderização. O diagnóstico é escrito em
  `stderr`.
