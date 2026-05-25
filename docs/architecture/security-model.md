# Security Model

## Principle

Avalonia.RemoteControl exposes inspection and mutation capabilities for a running app. It must be safe by default and explicit by design.

Security requirements are split between functional behavior (`FR-SEC-*`) and technical enforcement (`TR-SEC-*`). There are no standalone `SR-*` requirements.

## Default State

The remote-control server is disabled by default. An app developer must explicitly enable it through code or configuration.

Production builds must have a documented way to keep the server disabled. The default production posture is off.

## Authentication

Every RPC requires bearer authentication, including:

- loopback sessions
- ADB tunnel sessions
- LAN/TLS sessions

Tokens must be configurable and rotatable. Tokens must not be logged, emitted in exceptions, written to traces, stored in package artifacts, or shown in normal diagnostic output.

## Transport

Default listener behavior:

- bind to loopback only
- require authentication

LAN behavior:

- requires explicit endpoint configuration
- requires TLS certificate configuration
- rejects cleartext startup

ADB behavior:

- may use cleartext h2c only through an explicitly detected/configured localhost ADB tunnel
- still requires bearer authentication
- cleans up forwarding on disconnect by default

## Redaction

Property exposure and log streaming redact sensitive names by default.

Default sensitive name fragments:

- password
- token
- secret
- key
- credential
- auth
- cookie
- connection string

Redaction should preserve enough metadata to explain that a value exists and was redacted when doing so is safe.

Current server implementation:

- property snapshots redact values when public property names contain configured sensitive fragments
- property mutation blocks configured sensitive property names even when the property is otherwise allow-listed
- log streaming redacts rendered log messages containing sensitive fragments
- structured log values are redacted when their key contains a configured sensitive fragment

## Mutation Authorization

Property mutation is deny-by-default.

All mutation and action commands pass through a command authorization policy before execution.

Blocked operations return sanitized failure summaries.

Raw exception dumps are not sent to clients.

## Audit Logging

Every remote mutation/action emits an audit log entry with:

- timestamp
- authenticated client identity
- node ID
- command type
- result
- sanitized details

Security failures also emit audit logs:

- rejected authentication
- rejected authorization
- blocked property access
- failed mutation attempts

## Client Storage

The client stores endpoint, token, and certificate settings only in user-scoped storage.

The client must let users forget saved connection settings.

The client must not log tokens.

## Requirement Mapping

Functional requirements:

- `FR-SEC-001`
- `FR-SEC-002`
- `FR-SEC-003`
- `FR-SEC-004`
- `FR-SEC-005`
- `FR-SEC-006`
- `FR-SEC-007`
- `FR-SEC-008`
- `FR-SEC-009`

Technical requirements:

- `TR-SEC-001`
- `TR-SEC-002`
- `TR-SEC-003`
- `TR-SEC-004`
- `TR-SEC-005`
- `TR-SEC-006`
- `TR-SEC-007`
- `TR-SEC-008`
- `TR-SEC-009`
- `TR-SEC-010`
- `TR-SEC-011`
- `TR-SEC-012`
- `TR-SEC-013`
- `TR-SEC-014`
- `TR-SEC-015`
- `TR-SEC-016`

Testing requirements:

- `TEST-SEC-001`
- `TEST-SEC-002`
- `TEST-SEC-003`
- `TEST-SEC-004`
- `TEST-SEC-005`
- `TEST-SEC-006`
- `TEST-SEC-007`
