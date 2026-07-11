# Progress Log — Zero-Trust .NET API

A running record of what's built, what works, and where to pick up next.
For the project vision, architecture, and full roadmap, see `README.md`.

**Last updated:** end of Week 1
**Status:** Week 1 complete ✅ — AuthServer issues JWTs, refresh rotation + reuse detection done and unit-tested.

---

## Where things stand

| Phase | Status |
|-------|--------|
| Setup — scaffold, clean build, folder structure | ✅ Done |
| Week 1a — AuthServer issues a signed JWT | ✅ Done |
| Week 1b — Cran 1: refresh rotation + reuse detection | ✅ Done |
| Week 2 — persistence, /refresh, /revoke, JWKS | ⬜ Next |
| Week 3 — Cran 2 + Cran 3 (resource authz, denylist, rate limiting) | ⬜ |
| Week 4 — attack demos, integration tests, threat model, report | ⬜ |

---

## What was built in Week 1

### 1. Access token generation (Week 1a)

The AuthServer issues signed JWT access tokens over HTTP.

- **`JwtSettings`** (`AuthServer.Core/Tokens/`) — signing key, issuer, audience, token lifetime. Bound from the `Jwt` section of `appsettings.json`.
- **`AccessTokenGenerator`** (`AuthServer.Core/Tokens/`) — builds and signs a JWT (HMAC-SHA256) with claims: `sub`, `unique_name`, `jti`, `nbf`, `exp`, `iss`, `aud`.
- **`POST /token` endpoint** (`AuthServer.Api/Program.cs`) — takes username/password, returns a signed JWT + `expiresIn`.
- **Token contracts** (`SecureApi.Shared/Auth/`) — `TokenRequest`, `TokenResponse` DTOs shared across projects.

**Verified working:** `curl POST /token` returns a valid JWT that decodes to the expected claims with a 15-minute expiry.

### 2. Refresh token rotation + reuse detection (Week 1b — Cran 1)

The headline security feature. Pure logic in `AuthServer.Core`, no database needed.

- **`RefreshToken`** + **`RefreshTokenStatus`** (`AuthServer.Core/Entities/`) — a token with a lifecycle: `Active → Retired → Revoked`, and a `FamilyId` linking a rotation chain.
- **`IRefreshTokenStore`** (`AuthServer.Core/Abstractions/`) — the storage port: find, add, update, revoke-family. No implementation in Core (dependency rule).
- **`RefreshTokenRotationService`** (`AuthServer.Core/RefreshRotation/`) — the decision brain:
  - unknown token → reject (`NotFound`)
  - **retired token presented → REUSE DETECTED → revoke the whole family**
  - revoked family → reject (`Revoked`)
  - expired → reject (`Expired`)
  - valid + active → rotate (retire old, issue new in same family)
- **`RotationResult`** + **`RotationFailure`** — explicit outcome type instead of exceptions for control flow.

**The mechanism:** each refresh token is single-use. Using it issues a new one and retires the old. If a retired token ever reappears, that's proof of theft → the entire token family is revoked, forcing re-login. This is the automatic reuse-detection pattern used by real identity providers.

---

## Tests

`tests/SecureApi.UnitTests` — **8 passing, 0 failing.**

- **AccessTokenGenerator (3):** produces a readable JWT, embeds expected claims, sets a future expiry.
- **Refresh rotation (5):** valid token rotates; rotated token becomes retired; **reusing a retired token is detected as theft**; **reuse revokes the entire family**; unknown token rejected.

The `InMemoryRefreshTokenStore` fake (`tests/.../Fakes/`) implements the port with a dictionary, so the rotation logic is tested in complete isolation — no database.

**The key test:** `Reuse_revokes_the_entire_family` — grows a chain (token-1 → token-2 → token-3), replays the stolen token-1, and asserts the currently-live token is now `Revoked`. This is Cran 1 proven in executable form. Demo this at the defense.

---

## Known Week-1 simplifications (deliberate, addressed later)

These are intentional shortcuts, not bugs — each has a planned resolution:

- **Symmetric signing key (HMAC-SHA256).** Same secret signs and verifies. Week 2 moves to asymmetric keys (private signs, public verifies via JWKS).
- **Hardcoded credentials** (`iliass` / `password123`). Week 2 adds a real user store with hashed passwords.
- **Signing key in `appsettings.json`.** Fine for dev; production uses user-secrets / environment variables. Worth a sentence in the report.
- **No persistence yet.** Rotation logic is tested against an in-memory fake. Week 2 wires EF Core + Postgres.

---

## Housekeeping done

- `.gitignore` added (`dotnet new gitignore`); build artifacts (`bin/`, `obj/`) untracked and removed from the repo.
- Template placeholder `Class1.cs` files removed from Core projects.
- *(Minor leftover: `tests/SecureApi.IntegrationTests/UnitTest1.cs` still present — replaced with real tests in Week 4.)*

---

## Pick up here (Week 2)

**Goal:** give the tokens a real home and run the rotation brain over HTTP.

1. EF Core + Postgres persistence — a real `IRefreshTokenStore` implementation backing the interface the tests already use.
2. `/refresh` endpoint — calls `RefreshTokenRotationService.RotateAsync`, returns a new access + refresh token pair, or the right error on reuse/expiry.
3. `/revoke` endpoint — explicit logout / token revocation.
4. Asymmetric signing + `/.well-known/jwks.json` — so the ResourceApi can validate tokens using a public key.

**How to run what exists:**
```bash
dotnet build
dotnet test tests/SecureApi.UnitTests          # 8 passing
dotnet run --project src/AuthServer/AuthServer.Api
# then: curl -X POST http://localhost:<port>/token -H "Content-Type: application/json" -d '{"username":"iliass","password":"password123"}'
```