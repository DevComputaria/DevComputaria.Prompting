# DevComputaria.Prompts.SourceGen

Incremental source generator for compile-time prompt pins. It reads `prompts/catalog.yaml`
as an `AdditionalFile` and emits one strongly typed `PromptId` property per published
version. Removing a version from the batch therefore removes its generated member and
causes consumers of that pin to fail compilation.
