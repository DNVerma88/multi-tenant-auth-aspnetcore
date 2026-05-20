# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | :white_check_mark: |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

To report a vulnerability:

1. Go to the repository's **Security** tab.
2. Click **Report a vulnerability**.
3. Fill in the details.

Or email: open a [GitHub issue](https://github.com/DNVerma88/multi-tenant-auth-aspnetcore/issues) marked **[security]**, or use GitHub's private vulnerability reporting.

We aim to acknowledge reports within **48 hours** and provide a fix or mitigation
within **14 days** for confirmed high/critical issues.

## Security Design Principles

This library follows these security principles:

- **Zero secrets in logs** — tenant IDs and user claims are not written to logs.
- **Format validation** — all tenant identifiers are validated against a configurable pattern.
- **Query string disabled by default** — prevents trivial tenant spoofing via link manipulation.
- **No dynamic code** — no `Assembly.Load`, `Activator.CreateInstance` with user input, or reflection in the hot path.
- **No unsafe code** — the library uses only managed, safe .NET APIs.
- **Fail closed** — unauthenticated users and unknown tenants are rejected by default.

## Known Limitations

- This library validates the **format** of tenant identifiers but does not verify whether a tenant actually exists in your database. Implement a custom `ITenantValidator` to perform existence checks.
- Header-based tenant resolution can be spoofed if your infrastructure does not strip tenant headers at the edge. Always validate using claim-based or signed tokens when possible.
- Query string resolution is disabled by default and should remain disabled unless you have additional CSRF/SSRF protections.
