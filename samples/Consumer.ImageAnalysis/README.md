# Consumer.ImageAnalysis sample

Este projeto demonstra o fluxo ponta a ponta sem acessar um provider real:

1. registra o runtime e o catálogo packed por DI;
2. seleciona `image-analysis.analyze-document@1.0.0` com pin explícito;
3. renderiza mensagens, hash e metadados;
4. envia o `RenderedPrompt` para um `IChatClient` fake, sem HTTP.

## Executar

Na raiz do repositório:

```bash
dotnet run --project samples/Consumer.ImageAnalysis
```

O primeiro argumento opcional substitui o texto de OCR:

```bash
dotnet run --project samples/Consumer.ImageAnalysis -- "OCR de exemplo"
```

## Directory override

O override é solicitado por `PROMPTS_DIRECTORY_OVERRIDE`, mas o registro do
runtime só mantém essa opção ativa quando `DOTNET_ENVIRONMENT=Development`.

Linux/macOS:

```bash
DOTNET_ENVIRONMENT=Development \
PROMPTS_DIRECTORY_OVERRIDE=./prompts \
dotnet run --project samples/Consumer.ImageAnalysis
```

Em Production, mesmo que um caminho seja informado, a opção efetiva permanece
desabilitada:

```bash
DOTNET_ENVIRONMENT=Production \
PROMPTS_DIRECTORY_OVERRIDE=./prompts \
dotnet run --project samples/Consumer.ImageAnalysis
```

O sample imprime a configuração efetiva, o prompt pinado, as mensagens, o
SHA-256, os hints do renderer e a resposta determinística do client fake.
