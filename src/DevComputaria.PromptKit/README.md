# DevComputaria.PromptKit

Runtime prompt engine package (domain-agnostic). Consumers that emit argument-level
diagnostics must resolve `PromptLogRedactor` from DI and log only the dictionary
returned by `RedactArguments`; rendered messages and raw `PromptArgs` are not safe
production log payloads.
