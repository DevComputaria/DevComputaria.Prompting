# Security policy

## Reporting a vulnerability

Report vulnerabilities through GitHub private vulnerability reporting for this repository.
Do not open a public issue containing secrets, prompt payloads, customer documents, exploit
details, or package credentials.

Include the affected component/version, reproduction steps, expected impact, and any known
mitigation. A security reviewer will acknowledge the report, coordinate remediation on a
private branch, and disclose details only after a fixed package is available.

If private vulnerability reporting is unavailable, contact an organization owner through a
private channel and reference this repository without including sensitive payloads in the
initial message.

## Supported versions

Security fixes target the latest supported package line. Older prompt pins remain available
for deterministic rollback, but consumers should upgrade the runtime/package when a security
advisory marks an earlier version unsupported.

See [security and release governance](docs/SECURITY-AND-RELEASE-GOVERNANCE.md) for roles,
logging controls, approvals, and the release checklist.

