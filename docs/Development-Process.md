# Byrd Development Process

This repository follows the Byrd Development Process initialized from `F:\GitHub\McpServer\docs\Development-Process-draft-v3.md`.

## Summary

The process is an iterative, requirements-first lifecycle. It resembles a series of small implementation-validation-deployment cycles, with a strong bias toward documented requirements, public interface clarity, testability, and proof before speed.

## Planning

Planning produces:

- Functional Requirements: what user-visible work the system must do.
- Technical Requirements: how the system must operate.
- Testing Requirements: how requirements will be proven.
- Traceability: how each requirement maps to implementation slices, tests, CI gates, and acceptance evidence.
- Iterative Phases: the sequence of scoped work slices.

Implementation does not start until the relevant requirements, public surfaces, acceptance criteria, and testing strategy are documented.

## Implementation

Each implementation slice starts with tests derived from acceptance criteria. Mocked or contract-level tests may be used to prove that the test expresses the requirement before production code is added.

Implementation then makes those tests pass through the real system.

## Validation

To leave an implementation slice:

- all current-slice tests must pass
- all tests from previously completed slices must still pass
- the executed validation scope must have zero failures
- the executed validation scope must have zero skipped tests

Skipped tests are deferred work signals, not passing evidence.

## Deployment

Deployment and release work is automated through CI/CD. For this project, deployment means package production and publish readiness:

- server NuGet package
- client .NET tool package
- GitHub Actions validation
- Azure Pipelines validation
- tagged release publishing through protected secrets

## Local Adaptation

Avalonia.RemoteControl is a debugging and remote-mutation tool. Security and Android transport feasibility are first-order planning concerns. The requirements gate must include:

- security requirements split into `FR-SEC-*` and `TR-SEC-*`
- Android/ADB connectivity requirements
- a Technical Spike 0 proving Android app-side transport viability
- traceability from each remote-control capability to tests and acceptance evidence
