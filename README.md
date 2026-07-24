# Progress Log — Zero-Trust .NET API

A running record of what's built, what works, and where to pick up next.
For the project vision, architecture, and full roadmap, see `README.md`.

**Last updated:** Week 3, after Cran 2
**Status:** Weeks 1–2 complete ✅ · Week 3 mostly complete — BOLA defense demonstrated, Cran 3 remaining.

---

## Where things stand

| Phase | Status |
|-------|--------|
| Setup — scaffold, clean build, folder structure | ✅ Done |
| Week 1a — AuthServer issues a signed JWT | ✅ Done |
| Week 1b — Cran 1: refresh rotation + reuse detection | ✅ Done |
| Week 2 — persistence, `/refresh`, `/revoke`, RS256 + JWKS | ✅ Done |
| Week 3a — ResourceApi validates tokens via JWKS | ✅ Done |
| Week 3b — Documents resource + persistence | ✅ Done |
| Week 3c — **Cran 2: resource-based authorization (BOLA defense)** | ✅ Done |
| Week 3d — Cran 3: denylist + rate limiting | ⬜ Next |
| Week 4 — attack console, integration tests, threat model, report | ⬜ |

---

## Two live security demonstrations

The project now has **two runnable attack/defense demos** — these are the core of the defense presentation.

### Demo 1 — Refresh token reuse → family revocation

1. Log in → receive refresh token **R1**
2. `POST /refresh` with R1 → receive **R2** (R1 is now `Retired`)
3. Replay the stolen **R1** → `401`, and reuse is detected
4. Check the database: **both R1 and R2 are now `Revoked`** — the entire family died, including the legitimate live token

Verified live against Postgres. Other token families remain `Active`, proving the blast radius is contained to the compromised chain.

### Demo 2 — BOLA (Broken Object Level Authorization) → 403

Two users, one document:

| Request | Before the guard | After the guard |
|---------|------------------|-----------------|
| Bob reads Alice's document | `200 OK` + private content | **`403 Forbidden`** |
| Alice reads her own document | `200 OK` | `200 OK` |

Bob's token is valid in both cases — correct signature, issuer, audience, not expired. Authentication passes. The difference is that the API now also asks *"is this yours?"* This is OWASP API Security Top 10 **#1**.

---

## What was built, by week

### Week 1 — Token generation + rotation logic

- **`JwtSettings`**, **`AccessTokenGenerator`** (`AuthServer.Core/Tokens/`) — JWT construction and signing
- **`RefreshToken`** + **`RefreshTokenStatus`** (`Active → Retired → Revoked`) with a `FamilyId` linking a rotation chain
- **`IRefreshTokenStore`** — the storage port (no implementation in Core)
- **`RefreshTokenRotationService`** — the decision brain: rotate on valid, **revoke the whole family on reuse**, reject expired/revoked/unknown
- **`RotationResult`** / **`RotationFailure`** — explicit outcomes instead of exceptions for control flow

**Key concept:** login = birth of a family (new `FamilyId`); rotation = continue the family (same `FamilyId`); reuse = death of the family (revoke by `FamilyId`).

### Week 2 — Persistence, endpoints, asymmetric signing

- **EF Core + Postgres** (`AuthDbContext`, `EfRefreshTokenStore`) — the production body of the Week 1 port. The rotation service was unchanged: only the implementation behind the interface swapped.
- **`POST /token`** — login; issues an access token + starts a refresh token family
- **`POST /refresh`** — rotation running live; uniform `401` on every failure so error responses leak nothing
- **`POST /revoke`** — explicit logout; always returns `200` even for unknown tokens (RFC 7009), so it can't be used to probe for valid tokens
- **RS256 asymmetric signing** — private key signs, public key verifies. Verification no longer requires the power to forge.
- **`GET /.well-known/jwks.json`** — publishes the public key
- **`GET /.well-known/openid-configuration`** — OpenID Connect discovery document

### Week 3 — The ResourceApi

- **Cross-service token validation** — the ResourceApi fetches the AuthServer's public key via JWKS discovery. No shared secret between services.
- **Documents resource** — `Document` entity (Domain), `IDocumentRepository` port (Application), `EfDocumentRepository` + `ResourceDbContext` (Infrastructure), separate `ztapi_resources` database
- **Cran 2 — resource-based authorization:**
  - **`DocumentAccessPolicy.CanAccess(userId, document)`** (Application) — the rule as a pure, testable static method
  - **`DocumentOwnerRequirement`** + **`DocumentOwnerHandler`** (Api) — ASP.NET glue that extracts identity and delegates the decision inward
  - Endpoints call `authz.AuthorizeAsync(ctx.User, doc, "DocumentOwner")` — passing the **actual document instance**, which is what `[Authorize(Roles=...)]` fundamentally cannot do

---

## Architecture notes worth defending

**The dependency rule enforced itself.** When the `DocumentOwnerHandler` was first placed in the Application layer, the build failed — that layer has no ASP.NET reference by design. The architecture caught the mistake rather than relying on discipline.

**Decision vs enforcement.** The ownership rule lives in the Application core (pure, unit-testable, framework-free). The handler that triggers it lives in the Api layer. Same separation that allowed the rotation logic to be tested with no database.

**Fail-closed by default.** The authorization handler never calls "deny" — it simply doesn't call `Succeed()`. Absence of success is denial, so a bug that skips the logic denies access rather than granting it.

---

## Tests

`tests/SecureApi.UnitTests` — **8 passing.**

- **AccessTokenGenerator (3):** produces a readable JWT, embeds expected claims, sets a future expiry
- **Refresh rotation (5):** valid token rotates; rotated token becomes retired; **reusing a retired token is detected as theft**; **reuse revokes the entire family**; unknown token rejected

The `InMemoryRefreshTokenStore` fake implements the port with a dictionary, so rotation logic is tested in complete isolation.

*(Still to add in Week 4: integration tests asserting `403` on the BOLA attack, and unit tests for `DocumentAccessPolicy`.)*

---

## Difficulties encountered (for the report)

Real problems debugged, each with a lesson:

- **JWKS discovery** — `options.Authority` fetches `/.well-known/openid-configuration` *before* the JWKS. Without that discovery document, validation fails with `"The signature key was not found"` even though keys and tokens are perfectly valid.
- **Ephemeral key caching** — restarting the AuthServer generates a new key pair, invalidating the ResourceApi's cached public key. This is the concrete cost of the dev-only ephemeral-key choice, and precisely why production persists keys and rotates them through the JWKS array.
- **Middleware ordering** — `UseAuthentication()` must precede `UseAuthorization()`. Reversed, authorization inspects an empty `ctx.User`, rejects the request, and *then* authentication succeeds — producing a bare `Bearer` challenge with a valid token.
- **Claim name remapping** — ASP.NET remaps JWT claims to XML schema URIs by default, so `FindFirst("sub")` returns null. Fixed with `JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear()`.
- **Issuer mismatch** — the token's `iss` must exactly match the validator's `ValidIssuer` (`http` vs `https` matters). The most common cause of an unexplained `401`.
- **Migrations across Clean Architecture layers** — EF needs `--project` (where the DbContext lives) and `--startup-project` (where config lives) as separate flags.

**Debugging lesson learned repeatedly:** trust `dotnet build` output over IDE squiggles. When a type fails to compile, the editor marks every *usage* as an error and suggests wrong fixes. And when a paste goes wrong, replace the whole file rather than patching fragments.

---

## Known simplifications (deliberate, documented)

| Simplification | Production approach |
|---|---|
| Hardcoded users (`alice`, `bob`) | Real user store with hashed passwords |
| Ephemeral RSA key (new pair each restart) | Persisted key in a secrets manager + graceful rotation via the JWKS array |
| Signing key / DB passwords in `appsettings.json` | User-secrets, environment variables, or a vault |
| `RequireHttpsMetadata = false`, plain HTTP locally | HTTPS everywhere; JWKS over TLS only |
| Bearer access tokens | DPoP (RFC 9449) sender-constrained tokens — see Future Work |

---

## Pick up here (Week 3d — Cran 3)

1. **Token denylist** — revoked access tokens rejected *immediately* rather than living out their 15 minutes. Uses the `jti` claim already embedded in every token.
2. **Rate limiting** — protect the auth endpoints from brute force (native in .NET since 7).

Then Week 4: attack console, integration tests, threat model, report.

**How to run what exists:**
```bash
docker compose up -d                                       # Postgres

dotnet run --project src/AuthServer/AuthServer.Api         # Terminal 1 → :5001
dotnet run --project src/ResourceApi/ResourceApi.Api       # Terminal 2 → :5272

dotnet test tests/SecureApi.UnitTests                       # 8 passing
```

**Important:** always start the AuthServer *before* the ResourceApi. The ResourceApi caches the AuthServer's public key at startup, and the key is regenerated on every AuthServer restart.

**Users:** `alice` / `password123` and `bob` / `password123`.