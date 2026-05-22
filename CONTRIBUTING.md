# Contributing to MultiTenantAuth.AspNetCore

Thank you for your interest in contributing! This document explains how to get
set up, the conventions we follow, and the process for submitting changes.

---

## Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [Prerequisites](#prerequisites)
3. [Getting Started](#getting-started)
4. [Project Structure](#project-structure)
5. [Development Workflow](#development-workflow)
6. [Coding Conventions](#coding-conventions)
7. [Testing](#testing)
8. [Security Vulnerabilities](#security-vulnerabilities)
9. [Submitting a Pull Request](#submitting-a-pull-request)
10. [Release Process](#release-process)

---

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md).
By participating you agree to abide by its terms.

---

## Prerequisites

| Tool | Minimum version |
|------|----------------|
| [.NET SDK](https://dot.net) | 8.0 |
| [Git](https://git-scm.com/) | 2.x |
| [GitHub CLI](https://cli.github.com/) *(optional)* | 2.x |

No third-party NuGet packages are required at runtime — the library depends
only on `Microsoft.AspNetCore.App`.

---

## Getting Started

```bash
# 1. Fork the repo on GitHub, then clone your fork
git clone https://github.com/<your-username>/multi-tenant-auth-aspnetcore.git
cd multi-tenant-auth-aspnetcore

# 2. Restore, build, and test
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

All 78 tests should pass on a clean checkout.

---

## Project Structure

```
src/
  MultiTenantAuth.AspNetCore/     # Library — net8.0 + net10.0
    Abstractions/                 # Public interfaces (ITenantResolver, etc.)
    Constants/                    # TenantConstants
    Extensions/                   # AddMultiTenantAuth / UseMultiTenantAuth
    Internal/                     # TenantContextAccessor, TenantFormatValidator
    Middleware/                    # MultiTenantAuthMiddleware
    Models/                       # TenantContext, result types
    Options/                      # MultiTenantAuthOptions, Validator
    Resolvers/                    # 5 built-in resolvers
    Validators/                   # DefaultTenantValidator
tests/
  MultiTenantAuth.AspNetCore.Tests/  # xUnit tests (net8.0)
samples/
  ApiOnlySample/                  # Controller-based API sample
  MinimalApiSample/               # Minimal API sample
  SubdomainTenantSample/          # Subdomain-based resolution sample
```

---

## Development Workflow

We use **feature branches + pull requests**. Direct pushes to `main` are blocked
by branch protection.

```bash
# Create a feature branch
git checkout -b feat/my-improvement

# Make your changes, then commit
git add -A
git commit -m "feat: describe what you did"

# Push and open a PR
git push origin feat/my-improvement
gh pr create --base main
```

### Branch naming

| Type | Prefix | Example |
|------|--------|---------|
| Feature | `feat/` | `feat/custom-resolver-cache` |
| Bug fix | `fix/` | `fix/header-trim-edge-case` |
| Security | `fix/` | `fix/security-vuln-05` |
| Documentation | `docs/` | `docs/update-readme` |
| Chore / deps | `chore/` | `chore/bump-xunit` |

### Commit messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <short description>

[optional body]

[optional footer(s)]
```

Types: `feat`, `fix`, `docs`, `test`, `chore`, `refactor`, `perf`, `ci`.

---

## Coding Conventions

- **C# version**: `latest` LangVersion with `<Nullable>enable</Nullable>` and
  `<ImplicitUsings>enable</ImplicitUsings>`.
- **`TreatWarningsAsErrors`** is enabled — the build fails on any warning.
- All public API surface must have XML doc comments (`<summary>`, `<param>`,
  `<returns>` where meaningful).
- Keep the library **zero external runtime dependencies** — only
  `Microsoft.AspNetCore.App` framework references are permitted.
- Internal types are `internal sealed` unless there is a specific reason to
  allow inheritance.
- Avoid `async void`; prefer `async Task` or `async ValueTask`.

---

## Testing

- Tests live in `tests/MultiTenantAuth.AspNetCore.Tests/` (xUnit 2.x).
- Use `TestServer` via `Microsoft.AspNetCore.Mvc.Testing` for integration tests.
- Every new feature or bug fix **must** include a corresponding test.
- Run the full suite before opening a PR:

  ```bash
  dotnet test -c Release
  ```

- Check for vulnerable packages:

  ```bash
  dotnet list package --vulnerable --include-transitive
  ```

---

## Security Vulnerabilities

**Please do not open public GitHub issues for security vulnerabilities.**

See [SECURITY.md](SECURITY.md) for the responsible-disclosure process.

---

## Submitting a Pull Request

1. Ensure all tests pass locally.
2. Update `CHANGELOG.md` under the `[Unreleased]` section.
3. Open a PR against `main` with a clear title and description.
4. At least one reviewer must approve before merge.
5. CI status checks must be green (build + test + vulnerability scan).
6. Use **squash merge** to keep the commit history clean.

---

## Release Process

Releases are owned by maintainers:

1. Move the `[Unreleased]` section in `CHANGELOG.md` to a new versioned heading
   (e.g., `## [1.2.0] — YYYY-MM-DD`).
2. Bump `<Version>` in
   `src/MultiTenantAuth.AspNetCore/MultiTenantAuth.AspNetCore.csproj`.
3. Merge to `main` via PR.
4. Push a semver tag — the CD pipeline publishes to NuGet.org automatically:

   ```bash
   git tag v1.2.0
   git push origin v1.2.0
   ```

NuGet publishing uses **OIDC Trusted Publishing** — no long-lived API key is
stored as a GitHub secret. Configure a Trusted Publisher on your NuGet.org
package page before tagging.
