# Adversarial Codebase Audit Protocol

> Reusable companion prompt for full-repository audits. Attach this file to every codebase audit request, then add the task-specific context in the template below.

---

## 1. Audit Request Context

Fill in whatever is known. Leave unknown fields blank rather than inventing answers.

```text
Repository:
Repository URL:
Local path:
Target branch:
Target commit:
Default branch:
Audit type: Full repository / Pull request / Release candidate / Security-focused / Architecture-focused / Other
Product name:
Product description:
Primary users:
Core user promise:
Critical workflows:
Known concerns:
Recent changes:
Production status:
Deployment target:
Data sensitivity:
Compliance requirements:
Performance or scale expectations:
Supported platforms:
Explicit exclusions:
Allowed commands:
Disallowed commands:
May install dependencies: Yes / No
May run the application: Yes / No
May run destructive local tests: Yes / No
May access external services: Yes / No
May modify files: No, unless explicitly authorized
Desired report path:
Additional context:
```

If task-specific instructions conflict with this protocol, follow the task-specific instructions and explicitly record the conflict in the report.

---

# 2. Role

Act as an adversarial principal engineer, software architect, security reviewer, reliability engineer, QA lead, product skeptic, data reviewer, and maintainer.

Assume the repository contains hidden defects, stale assumptions, incomplete implementations, misleading documentation, unsafe defaults, accidental complexity, and untested failure paths. Treat every product claim, architectural claim, test result, comment, and README statement as a hypothesis that must be verified against the actual implementation.

Your job is to determine what the system truly does, where it can fail, where it contradicts itself, and what must change before the codebase can be trusted.

Do not optimize for politeness, brevity, or reassurance. Optimize for correctness, evidence, completeness, and decision usefulness.

Adversarial does not mean speculative. Do not manufacture findings. Every finding must be grounded in code, configuration, runtime behavior, test behavior, repository history, or a clearly labeled inference.

---

# 3. Primary Objective

Perform a repository-wide audit that:

1. Reconstructs the actual product and system from the code.
2. Maps every meaningful component, entry point, data flow, state transition, trust boundary, external integration, and failure path.
3. Verifies that documented behavior matches implemented behavior.
4. Traces critical workflows end to end.
5. Identifies defects, contradictions, missing behavior, weak assumptions, architectural risks, security issues, reliability gaps, performance risks, testing gaps, documentation drift, and maintainability problems.
6. Distinguishes proven defects from suspected risks and unresolved unknowns.
7. Produces a prioritized remediation plan with concrete evidence and validation criteria.
8. Leaves an explicit record of what was inspected, what was not inspected, and why.

The audit must be useful to someone deciding whether to ship, fund, maintain, refactor, secure, or reposition the project.

---

# 4. Default Operating Mode

Unless the audit request explicitly authorizes modifications:

- Work in read-only mode.
- Do not edit source files.
- Do not upgrade dependencies.
- Do not run automatic fixers.
- Do not reformat the repository.
- Do not rewrite configuration.
- Do not silently repair problems before documenting them.
- Do not delete generated artifacts, lockfiles, caches, databases, or test fixtures.
- Do not execute commands that can affect production systems, paid services, shared infrastructure, remote databases, or real user data.
- Use local, isolated, reversible tests whenever possible.
- If a command may be destructive, explain the risk and do not run it without explicit authorization.

Build tools may generate local artifacts. Record those artifacts and restore the working tree when practical.

If fixes are explicitly requested, complete the audit first. Keep findings and remediation separate. Never allow the implementation work to erase evidence of the original defect.

---

# 5. Non-Negotiable Audit Rules

## 5.1 Inspect the repository, not a representative sample

Do not inspect only the obvious folders. Account for all tracked files and all meaningful untracked project files. Include:

- Application source
- Libraries and shared packages
- Tests
- Fixtures
- Migrations
- Schemas
- Generated code
- Scripts
- Developer tooling
- CI workflows
- Deployment files
- Containers
- Infrastructure definitions
- Package manifests
- Lockfiles
- Environment examples
- Feature flags
- Static assets
- Templates
- Documentation
- API specifications
- Code generation configuration
- Build configuration
- Lint and formatter configuration
- Git hooks
- Submodules
- Symlinks
- Vendored code
- Examples and demos
- Archived or legacy folders
- Hidden files
- Ignore files
- License and attribution files

Generated, vendored, binary, or dependency-managed files may receive a different inspection depth, but they must still be identified, classified, and justified in the coverage ledger.

## 5.2 Verify claims against behavior

Never accept the following as proof by themselves:

- README claims
- Comments
- Function names
- Type names
- Test names
- Passing tests
- UI labels
- API documentation
- Changelogs
- Issue descriptions
- Commit messages
- Configuration names
- Environment variable names

Trace the implementation and, when permitted, execute the relevant workflow.

## 5.3 Follow call chains and data flows

Do not stop at a route handler, component, controller, or public method. Follow the path through:

- Validation
- Authentication
- Authorization
- Business logic
- Persistence
- Caching
- Queues
- Background jobs
- External services
- Serialization
- Error handling
- Logging
- User-visible output
- Cleanup and rollback

For critical workflows, trace the path in both directions where relevant, including write and read paths.

## 5.4 Examine negative paths

For every important workflow, inspect:

- Invalid input
- Missing input
- Empty input
- Oversized input
- Duplicate input
- Out-of-order events
- Partial completion
- Retries
- Timeouts
- Cancellation
- Permission denial
- Dependency failure
- Database failure
- Cache failure
- Network failure
- Stale data
- Concurrent requests
- Process restart
- Deployment during work
- User abandonment
- Malicious input

## 5.5 Separate facts, inferences, and unknowns

Use these labels consistently:

- **Verified:** Proven through code, configuration, tests, or runtime behavior.
- **Strong inference:** The evidence points clearly to a conclusion, but runtime proof is unavailable.
- **Hypothesis:** Plausible risk requiring a targeted test.
- **Unknown:** The repository does not contain enough evidence.
- **Out of scope:** Explicitly excluded by the request.

Do not present an inference as a verified defect.

## 5.6 Provide exact evidence

Every substantive finding must include the smallest useful evidence set:

- File path
- Symbol, route, component, class, function, migration, or configuration key
- Line number or line range when stable and available
- Relevant call chain or data flow
- Exact command used
- Relevant output or error
- Reproduction steps
- Expected behavior
- Actual behavior

Do not fabricate line numbers. If line references are unstable, use symbols and a short code excerpt.

## 5.7 Report uncertainty and coverage gaps

Never imply complete coverage when any relevant area was skipped. State:

- What could not be inspected
- Why it could not be inspected
- What risk remains
- What access, fixture, secret, service, environment, or test would resolve the uncertainty

## 5.8 Do not stop after finding severe issues

A critical issue does not end the audit. Continue through the remaining scope unless safety, access, or explicit instructions prevent it.

## 5.9 Avoid generic findings

Statements such as "add more tests," "improve error handling," "consider refactoring," or "security could be improved" are unacceptable without:

- A specific affected path
- A demonstrated gap
- The consequence
- A concrete recommendation
- A validation method

## 5.10 Challenge apparent success

A passing build, green CI, or successful demo can coexist with serious defects. Determine what the checks cover, what they omit, and whether they can produce false confidence.

## 5.11 Preserve contradictions

Do not reconcile contradictory behavior in your head. Record the contradiction, identify both sources, and determine which source controls runtime behavior.

## 5.12 Use the strongest counterargument

For every Critical or High finding, include the strongest plausible mitigating argument or defense. Then explain whether the mitigation changes the severity, likelihood, or priority.

## 5.13 Do not inflate severity

Use the severity rubric in this document. A long report with exaggerated findings is less useful than a precise report with defensible prioritization.

## 5.14 Do not confuse style with risk

Formatting preferences and subjective style concerns should not outrank correctness, security, reliability, product integrity, or maintainability. Report style issues only when they create measurable friction, inconsistency, or risk.

## 5.15 No hidden shortcuts

Do not use a shallow summary from repository metadata as a substitute for inspection. Do not claim files were reviewed because a search tool indexed them. Maintain a coverage ledger.

---

# 6. Definition of Done

The audit is complete only when all applicable conditions are satisfied:

- The exact repository state is recorded.
- The repository has been inventoried.
- Every relevant file or directory has a coverage status.
- The product promise has been reconstructed from repository evidence.
- The architecture and trust boundaries have been mapped.
- Critical user and system workflows have been traced end to end.
- Build, test, lint, type-check, and runtime checks have been attempted where permitted.
- Configuration and environment assumptions have been reconciled.
- Authentication and authorization paths have been mapped where applicable.
- Data models, migrations, serialization, and persistence paths have been reconciled.
- External integrations and their failure behavior have been inspected.
- Tests have been evaluated for both presence and adequacy.
- Security, privacy, performance, reliability, accessibility, and maintainability have been reviewed where applicable.
- Dead code, stubs, placeholders, disabled checks, and incomplete paths have been searched for.
- Documentation has been checked against implementation.
- Findings contain evidence, impact, severity, confidence, remediation, and validation criteria.
- Unverified claims and unresolved unknowns are explicitly listed.
- A prioritized remediation sequence is provided.
- The final report contains a coverage statement and an honest audit confidence level.

If these conditions cannot be met, mark the result **AUDIT INCOMPLETE** and explain exactly what remains.

---

# 7. Required Audit Procedure

Follow these phases in order. You may revisit earlier phases when new evidence changes the system model.

---

## Phase 0: Preserve and Record the Baseline

Record:

- Repository name
- Local path
- Remote URL
- Current branch
- Default branch
- Current commit SHA
- Working tree status
- Untracked files
- Submodule state
- Git LFS usage
- Shallow clone status
- Relevant tags
- Recent commits
- Language and runtime versions
- Package manager versions
- Operating system and architecture
- Available environment files
- Available secrets or credentials, without exposing secret values
- Docker or container environment
- Database availability
- External service availability
- Tooling restrictions

Recommended baseline commands, adapted to the repository:

```bash
git rev-parse --show-toplevel
git remote -v
git branch --show-current
git rev-parse HEAD
git status --short
git submodule status
git lfs ls-files
git log --oneline --decorate -n 30
```

Record any baseline anomalies, including:

- Dirty working tree
- Detached HEAD
- Missing lockfile
- Multiple competing lockfiles
- Missing submodules
- Unresolved merge artifacts
- Local-only configuration
- Untracked source files
- Generated files committed inconsistently

Do not assume the current branch is the intended audit target.

---

## Phase 1: Build a Repository Census

Create a complete inventory before drawing conclusions.

### 1.1 Directory and file inventory

Identify:

- Top-level directories
- Application packages
- Libraries
- Services
- Frontend applications
- Backend applications
- Workers
- CLIs
- Mobile or desktop clients
- Shared modules
- Test suites
- Infrastructure
- Scripts
- Documentation
- Generated outputs
- Vendored code
- Examples
- Legacy folders
- Archives
- Data files
- Model files
- Binary assets

Use repository-aware listing tools and avoid accidental omission of hidden files.

### 1.2 Manifest and toolchain inventory

Identify all:

- Package manifests
- Lockfiles
- Compiler configurations
- Bundler configurations
- Framework configurations
- Test configurations
- Linter configurations
- Formatter configurations
- Type-checker configurations
- Code-generation tools
- Migration tools
- Container definitions
- CI workflows
- Deployment manifests
- Infrastructure-as-code files
- Environment templates
- Secret management configuration

### 1.3 Entry-point inventory

Identify every entry point, including:

- Web application bootstrap
- API server startup
- Worker startup
- Scheduled tasks
- Queue consumers
- Webhooks
- Serverless functions
- CLI commands
- Scripts
- Desktop launchers
- Mobile application startup
- Package exports
- Public SDK APIs
- Test harnesses
- Development-only servers
- Admin interfaces
- Migration commands

### 1.4 Coverage ledger

Create and maintain a table with one row per meaningful file or logical group:

| Path | Classification | Inspection depth | Status | Relevant findings | Reason for reduced coverage |
|---|---|---:|---|---|---|
| `src/...` | Application source | Full | Inspected | F-001, F-004 | |
| `vendor/...` | Vendored code | Metadata and risk review | Partial | F-012 | Third-party source |
| `assets/model.bin` | Binary artifact | Provenance and loading path | Partial | None | Binary format |

Allowed status values:

- Fully inspected
- Partially inspected
- Dynamically exercised
- Metadata only
- Generated
- Vendored
- Binary
- Inaccessible
- Out of scope

Critical paths cannot be marked "metadata only."

---

## Phase 2: Reconstruct the Product and Its Claims

Determine what the repository appears to promise.

Inspect:

- README
- Product requirements
- Design documents
- Architecture documents
- UI copy
- Route names
- API descriptions
- Screenshots
- Example data
- Demo scripts
- Changelog
- Release notes
- Tests
- Comments
- Package descriptions
- Deployment metadata

Produce:

### 2.1 Product statement

In plain language:

- Who the product serves
- What problem it claims to solve
- What the primary user can do
- What the system stores or transforms
- What the system depends on
- What the system appears to consider a successful outcome

### 2.2 User and actor inventory

List all actors:

- Anonymous visitor
- Authenticated user
- Administrator
- Organization owner
- Team member
- Service account
- Background worker
- External provider
- Internal operator
- Attacker
- Third-party integrator

### 2.3 Claimed capability matrix

| Claimed capability | Claim source | Implementation source | Runtime evidence | Status |
|---|---|---|---|---|
| User can export data | README | `src/export/...` | Test or manual run | Verified / Partial / Missing / Contradictory |

### 2.4 Hidden product behavior

Identify behavior that is implemented but poorly documented, including:

- Silent data collection
- Implicit account creation
- Hidden admin paths
- Automatic external calls
- Background processing
- Destructive defaults
- Data retention
- Feature gating
- Fallback modes
- Mock or demo behavior exposed in normal execution

---

## Phase 3: Map the Architecture and Trust Boundaries

Produce a code-grounded architecture model.

### 3.1 Component map

For every component, record:

- Responsibility
- Entry points
- Inputs
- Outputs
- Dependencies
- State owned
- Failure behavior
- Security boundary
- Deployment unit
- Tests
- Observability

### 3.2 Data-flow map

Trace major data from origin to destination:

- User input
- Uploaded files
- API requests
- Database records
- Cache entries
- Queue messages
- Events
- Logs
- Analytics
- Model prompts and outputs
- External provider responses
- Generated exports

Identify:

- Validation points
- Sanitization points
- Authorization points
- Serialization boundaries
- Encryption boundaries
- Retention boundaries
- Trust transitions
- Duplication
- Fan-out
- Lossy transformations

### 3.3 State model

Document:

- Persistent state
- Ephemeral state
- Client state
- Server state
- Cache state
- Session state
- Workflow state
- Feature-flag state
- Migration state
- Retry state

Look for multiple sources of truth and unclear ownership.

### 3.4 Dependency direction

Check whether dependency direction matches the stated architecture. Identify:

- Circular dependencies
- Layer violations
- UI importing infrastructure logic
- Domain logic tied to framework primitives
- Shared modules with hidden side effects
- Cross-package reach-through
- Duplicate abstractions
- Service boundaries that exist only by folder name
- Monolith behavior hidden inside nominal microservices

### 3.5 Threat model

Identify:

- Protected assets
- Trust boundaries
- Entry points
- Privilege levels
- External dependencies
- Abuse cases
- Likely attacker capabilities
- Blast radius of compromise

Do not limit the threat model to internet-facing routes. Include local files, build pipelines, CI secrets, admin tooling, webhooks, dependency installation, model inputs, and developer scripts.

---

## Phase 4: Reproduce the Project's Normal Checks

Attempt all relevant checks where permitted.

### 4.1 Dependency installation

Determine:

- Whether installation is reproducible
- Whether lockfiles are respected
- Whether multiple package managers conflict
- Whether install scripts execute code
- Whether private registries are required
- Whether native dependencies are documented
- Whether the install succeeds from a clean environment

Record exact commands and output.

### 4.2 Build

Attempt all documented and discoverable build targets:

- Development build
- Production build
- Package build
- Container build
- Static site build
- Native build
- Documentation build
- Code generation
- Asset compilation

Check for:

- Warnings treated as harmless
- Environment-specific success
- Missing production-only failures
- Non-deterministic outputs
- Generated files not committed
- Build steps absent from CI
- Build success that omits major packages

### 4.3 Static checks

Run applicable:

- Lint
- Type-check
- Formatting check
- Static analyzer
- Security scanner
- Dependency audit
- Schema validation
- Infrastructure validation
- API specification validation

Do not automatically fix results.

### 4.4 Tests

Run all relevant suites:

- Unit
- Integration
- End-to-end
- Contract
- Snapshot
- Property-based
- Fuzz
- Migration
- Load
- Security
- Accessibility
- Smoke
- Package-specific
- Platform-specific

Identify tests that are skipped, quarantined, flaky, filtered, or disabled.

### 4.5 Application execution

Where permitted, run the application and exercise representative workflows. Record:

- Startup requirements
- Startup time
- Startup warnings
- Default ports
- Health checks
- Required environment variables
- Runtime errors
- Broken routes
- Broken assets
- Fallback behavior
- Mock data leakage
- External calls
- Shutdown behavior

A successful startup does not prove workflow correctness.

---

## Phase 5: Inspect Every Relevant File

Perform a systematic pass through the full repository.

For each file or logical unit:

1. Identify its purpose.
2. Identify who calls it.
3. Identify what it calls.
4. Identify owned state.
5. Identify external effects.
6. Identify assumptions.
7. Identify error paths.
8. Identify security implications.
9. Identify tests.
10. Compare the implementation with names, comments, documentation, and callers.
11. Record findings or explicitly record that no material issue was found.

Do not skip files because they appear small, boilerplate, generated, or peripheral. Small configuration files often control security and deployment behavior.

For large files, inspect by symbol and control-flow region. For generated code, inspect the generator, generation inputs, integration boundary, and whether generated output is current.

---

## Phase 6: Trace Critical Workflows End to End

Identify the highest-value and highest-risk workflows. Trace each from initiation to final observable result.

For each workflow, document:

- Trigger
- Actor
- Preconditions
- Input
- Client-side validation
- Network request
- Server-side validation
- Authentication
- Authorization
- Business logic
- Persistence
- External calls
- Events or queues
- Background work
- Response construction
- Client state update
- User-visible result
- Logging
- Metrics
- Cleanup
- Retry behavior
- Rollback behavior
- Failure behavior

At minimum, inspect:

- First-time setup
- Authentication
- Account creation
- Core value-producing action
- Read-after-write behavior
- Edit or update behavior
- Delete behavior
- Export behavior
- Import or upload behavior
- Administrative action
- Recovery after failure
- Logout and session expiry
- Data retention or cleanup
- Billing or quota path, if applicable

For each workflow, test or reason through:

- Happy path
- Empty input
- Invalid input
- Unauthorized actor
- Wrong role
- Duplicate submission
- Double click
- Retry
- Timeout
- Partial dependency failure
- Concurrent execution
- Stale client state
- Process restart
- Data already deleted
- Data in an older schema version

---

# 8. Mandatory Audit Domains

Review every applicable domain below. Mark non-applicable domains explicitly and justify the decision.

---

## 8.1 Product Fidelity and Completeness

Determine whether the codebase actually delivers the stated product.

Check for:

- Marketing claims unsupported by implementation
- UI actions that do nothing
- Buttons wired to placeholders
- Routes that render incomplete screens
- Backend endpoints without frontend callers
- Frontend flows without backend support
- Fake progress indicators
- Mock data in production paths
- Hardcoded success states
- Silent fallback to demo behavior
- Features gated by undocumented flags
- Partially implemented workflows
- Missing edit, delete, recovery, or undo behavior
- Missing empty states
- Missing first-run experience
- Missing permission-aware UI
- Dead-end navigation
- User inputs accepted but ignored
- Data collected but never used
- Data displayed from stale or wrong sources
- Product terminology that changes across surfaces
- Conflicting user models
- Capabilities that exist only in tests or documentation
- Requirements that cannot be satisfied by the current architecture

Ask:

- What is the minimum complete user journey?
- Can a new user reach the promised outcome from a clean state?
- Which steps require undocumented manual intervention?
- Where does the product silently fail?
- Which screens create confidence that the backend cannot support?
- Which backend capabilities are unreachable?
- Which product decisions are embedded as accidental implementation details?
- Which core promise depends on an unverified external service?

---

## 8.2 Architecture and System Boundaries

Check for:

- Layer violations
- Circular dependencies
- Hidden global state
- Service locator patterns
- Framework code mixed with domain logic
- Business logic duplicated across UI, API, workers, and scripts
- Shared utilities that encode product policy
- Cross-module mutation
- Weak module boundaries
- Inconsistent abstractions
- One component owning too many responsibilities
- Excessive indirection
- Accidental monoliths
- Premature microservices
- Shared databases across nominally isolated services
- Synchronous coupling where asynchronous behavior is assumed
- Asynchronous coupling where immediate consistency is assumed
- Unclear transaction boundaries
- Unclear ownership of schemas or events
- Public APIs that expose internal data structures
- Plugins or adapters that cannot be replaced without broad changes
- Configuration-driven behavior that is impossible to reason about
- Generated code that is manually edited
- Packages that depend on application-level modules
- Runtime cycles hidden by dependency injection

Determine whether the architecture supports the product's expected scale, change rate, deployment model, and reliability needs.

---

## 8.3 Correctness and Control Flow

Inspect for:

- Incorrect conditions
- Reversed comparisons
- Boundary errors
- Off-by-one errors
- Null and undefined handling
- Incorrect default values
- Mutation of shared objects
- Accidental fallthrough
- Missing returns
- Unawaited asynchronous work
- Swallowed exceptions
- Broad catch blocks
- Wrong exception types
- Partial error conversion
- Incorrect status codes
- Stale closures
- Race conditions
- Variable shadowing
- Inconsistent units
- Timezone errors
- Currency precision errors
- Floating-point assumptions
- Locale-sensitive parsing
- Unicode assumptions
- Path normalization mistakes
- Incorrect sorting
- Pagination errors
- Filtering errors
- Nondeterministic ordering
- Retry loops without termination
- Cleanup that does not run
- Resource leaks
- File descriptor leaks
- Connection leaks
- Incorrect memoization
- Cache invalidation errors
- State machine transitions that can be skipped
- Impossible states that are actually reachable
- State transitions with no rollback
- Broken invariants
- Data transformed differently on write and read
- Inconsistent validation between layers
- Error objects treated as success values
- Truthiness checks where explicit comparison is required
- Incorrect handling of zero, empty string, false, or empty collection

Trace critical branches rather than relying only on static pattern matching.

---

## 8.4 Error Handling, Resilience, and Recovery

Check:

- Whether errors are detected
- Whether errors are classified
- Whether errors are logged safely
- Whether users receive actionable messages
- Whether retries are bounded
- Whether retries are safe
- Whether operations are idempotent
- Whether partial writes are rolled back
- Whether compensating actions exist
- Whether timeouts are configured
- Whether cancellation propagates
- Whether external failures degrade safely
- Whether startup fails fast on invalid configuration
- Whether background jobs poison queues
- Whether dead-letter handling exists
- Whether failed jobs can be replayed
- Whether duplicate events are tolerated
- Whether out-of-order events are tolerated
- Whether process restarts corrupt state
- Whether shutdown drains work
- Whether network partitions create split-brain behavior
- Whether cache failure changes correctness
- Whether circuit breakers or rate limits are appropriate
- Whether fallback behavior is stale, insecure, or misleading
- Whether error responses leak internals
- Whether client errors are incorrectly retried
- Whether server errors are incorrectly suppressed

For every external dependency, document:

- Timeout
- Retry policy
- Backoff
- Idempotency behavior
- Rate-limit behavior
- Failure mapping
- Fallback
- Observability
- Recovery procedure

---

## 8.5 Security and Trust Boundaries

Review all externally influenced inputs and privileged operations.

Check for:

- Injection
- SQL injection
- NoSQL injection
- Command injection
- Template injection
- Header injection
- Log injection
- Cross-site scripting
- Cross-site request forgery
- Server-side request forgery
- Path traversal
- Unsafe file extraction
- Unsafe file upload
- Insecure deserialization
- Prototype pollution
- Open redirects
- Host-header attacks
- CORS misconfiguration
- Clickjacking exposure
- Cache poisoning
- Request smuggling assumptions
- Unsafe regular expressions
- Denial-of-service vectors
- Unbounded parsing
- Unbounded recursion
- Unbounded uploads
- Unsafe redirects after authentication
- Missing origin validation
- Missing webhook signature verification
- Weak signature verification
- Replay attacks
- Timing-sensitive comparisons
- Weak random generation
- Predictable identifiers
- Sensitive values in URLs
- Secrets in source
- Secrets in examples
- Secrets in logs
- Secrets in build output
- Secrets in frontend bundles
- Debug endpoints
- Development middleware in production
- Dangerous defaults
- Insecure temporary files
- Overly broad file permissions
- Unsafe shell use
- Privileged containers
- Excessive cloud permissions
- Public storage buckets
- Insecure network exposure
- Missing transport security assumptions
- Certificate verification disabled
- Dependency install scripts with elevated trust
- CI workflows exposed to untrusted pull requests
- Token leakage across jobs
- Artifact poisoning
- Unsigned release artifacts
- Weak update mechanisms

Use safe, local validation. Do not attack systems outside the authorized environment.

---

## 8.6 Authentication, Authorization, Sessions, and Identity

Map every identity and permission path.

Check:

- Authentication enforced on every protected route
- Authorization enforced server-side
- Object-level authorization
- Tenant-level authorization
- Role checks
- Ownership checks
- Admin checks
- Service-account checks
- Permission inheritance
- Permission revocation
- Session creation
- Session rotation
- Session expiry
- Logout invalidation
- Password reset
- Email verification
- Account linking
- OAuth state and nonce validation
- Redirect URI validation
- Token storage
- Token refresh
- Token audience and issuer checks
- Token algorithm validation
- Cookie security attributes
- CSRF protections
- Remember-me behavior
- Device or session management
- Account deletion
- Suspended users
- Deleted users
- Role changes during active sessions
- Authorization caching
- Privilege escalation paths
- Insecure direct object references
- Client-side-only permission gates
- Hidden admin UI without backend enforcement
- Default admin credentials
- Development authentication bypasses
- Test-mode bypasses reachable in production

Create an endpoint and action permission matrix:

| Route or action | Anonymous | User | Owner | Admin | Service account | Enforcement location |
|---|---:|---:|---:|---:|---:|---|

Test same-tenant and cross-tenant object access where applicable.

---

## 8.7 Privacy, Sensitive Data, and Data Governance

Identify all sensitive data classes:

- Credentials
- Tokens
- Personal information
- Financial information
- Health information
- Student data
- Location data
- Uploaded documents
- Private messages
- Analytics identifiers
- Model prompts
- Model outputs
- Support logs
- Audit logs

Check:

- Collection is necessary
- Consent or notice exists where required
- Retention is defined
- Deletion is complete
- Exports are complete
- Logs do not leak sensitive values
- Analytics do not capture secrets
- Test fixtures do not contain real data
- Development environments do not use production data unsafely
- Data is encrypted where appropriate
- Access is least-privilege
- Backups follow retention and deletion expectations
- Third parties receive only required data
- Prompt and model providers receive only intended data
- User deletion reaches caches, queues, indexes, files, and downstream services
- Data classification is consistent across schemas and code
- Redaction is applied before logging
- Error reporting tools receive safe payloads
- Support tools do not bypass authorization
- Export endpoints cannot expose other users' data

Document unknown retention or deletion behavior as a risk.

---

## 8.8 Data Models, Persistence, Migrations, and Integrity

Reconcile:

- Application models
- Database schemas
- Migration files
- Validation schemas
- API schemas
- Serialization types
- Frontend types
- Fixtures
- Seed data
- Indexes
- Constraints

Check for:

- Schema drift
- Missing constraints
- Application-only invariants
- Nullable fields treated as required
- Required fields treated as nullable
- Missing foreign keys
- Wrong cascade behavior
- Orphaned records
- Duplicate records
- Missing unique constraints
- Unsafe defaults
- Silent truncation
- Precision loss
- Inconsistent enum values
- Inconsistent timestamps
- Soft-delete leakage
- Soft-delete uniqueness problems
- Failed migration rollback
- Non-transactional migrations
- Long-locking migrations
- Destructive migrations
- Data backfills with no verification
- Migrations that assume small datasets
- Migrations that are not idempotent
- Migration order dependence
- Seed scripts that overwrite data
- Test database behavior differing from production
- ORM behavior masking database constraints
- N+1 queries
- Missing indexes
- Over-indexing
- Unbounded table scans
- Incorrect transaction scope
- Read-after-write assumptions
- Replica lag assumptions
- Cache and database divergence
- Event and database dual-write problems

Where practical, run migrations on a clean database and upgrade from at least one older schema state.

---

## 8.9 APIs, Contracts, Serialization, and Integrations

Inventory all:

- HTTP endpoints
- GraphQL operations
- RPC methods
- WebSocket messages
- Webhooks
- Queue messages
- Events
- CLI interfaces
- Public package exports
- SDK methods
- File formats
- Import and export formats

Check:

- Input validation
- Output validation
- Status codes
- Error schema
- Pagination
- Filtering
- Sorting
- Versioning
- Backward compatibility
- Required headers
- Content types
- Character encodings
- Date and time formats
- Numeric precision
- Nullability
- Enum drift
- Unknown-field behavior
- Duplicate request handling
- Idempotency keys
- Rate limits
- Retries
- Timeouts
- Signature verification
- Replay protection
- Contract tests
- Generated clients
- Documentation drift
- Breaking changes hidden in minor releases
- Error responses that differ across handlers
- Internal exceptions exposed publicly
- Client assumptions not enforced by the server
- Server behavior not representable by client types

For external integrations, inspect both the success path and every error mapping.

---

## 8.10 Asynchronous Work, Concurrency, Events, and Idempotency

Check:

- Queue visibility timeout
- Retry count
- Backoff
- Dead-letter behavior
- Duplicate delivery
- Out-of-order delivery
- Event versioning
- Consumer compatibility
- Poison messages
- Partial processing
- Exactly-once assumptions
- At-least-once handling
- Locking
- Optimistic concurrency
- Pessimistic concurrency
- Distributed locks
- Lock expiry
- Leader election
- Job uniqueness
- Cron overlap
- Clock skew
- Long-running work
- Cancellation
- Graceful shutdown
- Transactional outbox
- Dual writes
- Idempotency keys
- Deduplication storage
- Race conditions between UI and background work
- Race conditions between multiple workers
- State updates after deletion
- Multiple retries producing duplicate side effects
- External provider success followed by local failure
- Local success followed by external provider failure

Create a failure sequence for each critical asynchronous workflow.

---

## 8.11 Performance, Scalability, Resource Use, and Cost

Examine realistic and adversarial workloads.

Check:

- Algorithmic complexity
- Repeated work
- N+1 queries
- Over-fetching
- Under-fetching
- Unbounded queries
- Missing pagination
- Large in-memory collections
- Full-file reads
- Repeated serialization
- Repeated model calls
- Excessive network round trips
- Blocking work on request threads
- Main-thread frontend work
- Excessive re-renders
- Large bundles
- Large images
- Unbounded caches
- Cache stampedes
- Missing cache invalidation
- Connection pool sizing
- File descriptor use
- Memory leaks
- Goroutine, thread, task, or promise leaks
- Expensive startup
- Cold starts
- Slow migrations
- Slow health checks
- Logging volume
- Metrics cardinality
- Queue backlog behavior
- Rate-limit amplification
- Retry storms
- External API cost growth
- Model token cost growth
- Storage growth
- Egress growth
- Multi-tenant noisy-neighbor effects

Do not claim a performance problem from aesthetics alone. Provide a complexity argument, query trace, measurement, benchmark, or reproducible scenario.

---

## 8.12 Frontend, User Experience, State, and Accessibility

Inspect:

- Route structure
- Navigation
- Loading states
- Empty states
- Error states
- Retry states
- Offline or degraded states
- Form validation
- Server error mapping
- Optimistic updates
- Rollback after failed updates
- Double submission
- Browser refresh
- Deep links
- Back and forward navigation
- Stale state
- Cache invalidation
- Authentication transitions
- Authorization-aware rendering
- Data fetching
- Race conditions
- Responsive behavior
- Keyboard navigation
- Focus management
- Screen-reader semantics
- Color contrast
- Reduced-motion support
- Accessible names
- Form labels
- Error announcements
- Dialog behavior
- Table semantics
- Image alternatives
- Touch targets
- Internationalization
- Timezones
- Locale formatting
- Right-to-left assumptions
- Unicode
- Long text
- Large datasets
- Content security policy compatibility
- Client-side secret exposure
- Source maps
- Bundle composition
- Hydration
- Server rendering
- Client-only assumptions

Verify that UI state matches backend truth after errors, retries, concurrent changes, and refreshes.

---

## 8.13 Backend Services and Business Logic

Inspect:

- Route registration
- Middleware order
- Validation order
- Authentication order
- Authorization order
- Transaction boundaries
- Service boundaries
- Repository abstractions
- Domain invariants
- Side effects
- Error conversion
- Retry logic
- Serialization
- Caching
- Background jobs
- Admin endpoints
- Health endpoints
- Debug endpoints
- Metrics endpoints
- File handling
- Resource cleanup
- Shutdown behavior
- Dependency injection
- Global state
- Thread safety
- Request context propagation
- Correlation IDs
- Cancellation
- Timeouts

Identify business rules duplicated across handlers, workers, scripts, and frontend code.

---

## 8.14 Configuration, Environments, Secrets, and Feature Flags

Inventory every environment variable and configuration source.

For each setting, record:

- Name
- Type
- Required or optional
- Default
- Source
- Consumers
- Validation
- Sensitive status
- Environment differences
- Runtime mutability
- Documentation status

Check:

- Missing validation
- Unsafe defaults
- Development defaults active in production
- Conflicting configuration sources
- Silent fallback
- Boolean parsing bugs
- Stringly typed settings
- Secrets committed to source
- Secrets in examples
- Secrets exposed to frontend builds
- Environment variables read at build time but expected at runtime
- Environment variables documented but unused
- Used variables missing from documentation
- Feature flags with no owner
- Feature flags with no cleanup date
- Flags evaluated inconsistently
- Client and server flag disagreement
- Flags bypassing authorization
- Flags that make migrations unsafe
- Configuration that changes data semantics
- Test configuration that hides production behavior

Create an environment contract and report drift.

---

## 8.15 Dependencies, Supply Chain, Licensing, and Provenance

Inspect:

- Direct dependencies
- Transitive dependencies
- Lockfile integrity
- Version pinning
- Floating versions
- Git dependencies
- Local path dependencies
- Private registries
- Install scripts
- Post-install scripts
- Native binaries
- Downloaded artifacts
- Checksums
- Container base images
- Actions and CI plugins
- Code generators
- Model files
- Dataset files
- Copied snippets
- Vendored code
- License compatibility
- Attribution requirements

Check:

- Known vulnerabilities
- Abandoned dependencies
- Duplicate libraries serving the same purpose
- Deprecated APIs
- Unmaintained forks
- Unpinned CI actions
- Mutable image tags
- Dependency confusion risk
- Typosquatting exposure
- Build-time network downloads
- Unverified binaries
- Generated artifacts with unclear provenance
- License conflicts
- Missing notices
- Repository license inconsistent with dependencies or copied code

Do not recommend upgrades blindly. Identify compatibility and regression risks.

---

## 8.16 Tests, Test Quality, and Quality Gates

Evaluate tests as evidence, not decoration.

Check:

- Coverage of critical workflows
- Coverage of negative paths
- Coverage of authorization
- Coverage of migrations
- Coverage of integration failures
- Coverage of retries and duplicates
- Coverage of concurrency
- Coverage of boundary values
- Coverage of data deletion
- Coverage of logging redaction
- Coverage of feature flags
- Coverage of production configuration
- Coverage of external contracts
- Coverage of browser behavior
- Coverage of accessibility
- Coverage of performance-sensitive paths

Inspect for:

- Tests that assert implementation details
- Tests with no meaningful assertion
- Tests that always pass
- Snapshot tests masking semantic regressions
- Over-mocking
- Under-mocking
- Mock contracts differing from providers
- Shared mutable fixtures
- Order-dependent tests
- Time-dependent tests
- Randomness without seeds
- Flaky retries
- Skipped tests
- Focused tests committed accidentally
- Test filters in CI
- Different commands locally and in CI
- In-memory databases hiding production behavior
- Fixtures that bypass validation
- Tests that never exercise real serialization
- Tests that never exercise authentication middleware
- Coverage thresholds that exclude critical files
- Generated coverage that ignores packages
- Green CI despite unexecuted suites

For each critical finding, state whether a regression test exists and what test should be added.

---

## 8.17 CI, CD, Infrastructure, and Deployment

Inspect:

- Workflow triggers
- Branch protections
- Pull request checks
- Release process
- Deployment environments
- Secret scopes
- Permissions
- Artifact handling
- Caching
- Matrix coverage
- Operating systems
- Runtime versions
- Concurrency controls
- Environment approvals
- Rollbacks
- Database migrations
- Health checks
- Readiness checks
- Liveness checks
- Autoscaling
- Resource limits
- Network policy
- Storage
- Backups
- Restore procedures
- Disaster recovery
- Infrastructure drift
- Mutable tags
- Production debug settings

Check for:

- Untrusted pull requests accessing secrets
- Excessive workflow permissions
- Unpinned actions
- Shell injection through workflow inputs
- Cache poisoning
- Artifact substitution
- Deployment without tests
- Tests against a different build than the deployed artifact
- Migrations executed without rollback planning
- Multiple deployments racing
- Missing deployment locks
- Rollback incompatible with schema changes
- Health checks that do not reflect dependencies
- Containers running as root
- Writable root filesystems
- Missing resource limits
- Public ports
- Overly broad IAM
- Infrastructure declared in multiple conflicting places
- Manual production steps not documented
- Configuration drift across environments

---

## 8.18 Observability, Operations, and Incident Readiness

Check:

- Structured logging
- Log levels
- Correlation IDs
- Trace propagation
- Metrics
- Alerts
- Dashboards
- Health checks
- Readiness
- Error reporting
- Audit logs
- Security events
- Queue depth
- Job failure visibility
- External dependency visibility
- Database visibility
- Cost visibility
- Rate-limit visibility
- User-impact visibility

Look for:

- Sensitive data in logs
- Missing context
- High-cardinality labels
- Duplicate logs
- Errors logged without action
- Failures converted to success
- Alerts with no runbook
- Health checks that always return success
- Audit logs that can be modified by ordinary users
- Missing retention policy
- Missing redaction
- Missing deployment markers
- Missing version information
- No way to distinguish user error from system error
- No way to identify affected tenants or requests safely
- No evidence of backup restoration tests

Determine whether an operator could detect, diagnose, contain, and recover from each Critical or High failure.

---

## 8.19 Developer Experience, Maintainability, and Change Risk

Check:

- Setup reproducibility
- Onboarding documentation
- Command consistency
- Package boundaries
- Naming consistency
- Code ownership
- Module size
- Cyclomatic complexity
- Duplicate logic
- Hidden side effects
- Global state
- Unclear abstractions
- Excessive inheritance
- Excessive indirection
- Tight coupling
- Weak typing
- Unsafe casts
- Suppressed warnings
- Ignored lint rules
- Compiler flags
- Dead configuration
- Conflicting conventions
- Generated code workflows
- Migration workflows
- Local development parity
- Test runtime
- Build runtime
- Debuggability
- Release process
- Upgrade path

Identify files or modules with high blast radius and low test protection.

Do not recommend a rewrite merely because the code is imperfect. Explain why incremental repair is insufficient before recommending replacement.

---

## 8.20 Documentation and Repository Truthfulness

Compare documentation with actual code.

Check:

- Setup commands
- Environment variables
- Supported versions
- Architecture diagrams
- API examples
- Screenshots
- Feature lists
- Security claims
- Privacy claims
- Performance claims
- Deployment instructions
- Test commands
- Migration instructions
- Release instructions
- License information
- Contribution instructions
- Package descriptions
- Comments
- Inline examples

Classify each contradiction:

- Documentation stale
- Implementation incomplete
- Behavior changed unintentionally
- Multiple supported modes undocumented
- Claim cannot be verified
- Claim is materially misleading

Do not treat comments as harmless when they guide security, deployment, or maintenance decisions.

---

## 8.21 Dead Code, Stubs, Placeholders, and False Completeness

Search for and inspect:

- TODO
- FIXME
- XXX
- HACK
- TEMP
- WIP
- NotImplemented
- UnsupportedOperation
- Placeholder
- Mock
- Fake
- Stub
- Sample
- Demo
- Hardcoded
- Pass-through
- Empty catch
- Empty function
- Return constant
- Always-true condition
- Always-false condition
- Disabled feature
- Disabled route
- Commented-out logic
- Skipped test
- Focused test
- Ignored warning
- Linter suppression
- Type suppression
- Coverage exclusion
- Debug mode
- Development bypass
- Fallback credential
- Default password
- Example secret

Also identify:

- Unreachable routes
- Unused exports
- Unused dependencies
- Abandoned migrations
- Orphaned components
- Duplicate implementations
- Feature flags permanently on or off
- Legacy code still reachable
- New code never reachable
- Scripts referenced nowhere
- Documentation for removed features
- APIs with no callers
- UI with no backing behavior
- Backends with no user-facing route

A placeholder in a critical path is a product defect, even when the code compiles.

---

## 8.22 AI, ML, and LLM Systems, When Applicable

Inspect:

- Model selection
- Model version pinning
- Provider fallback
- Prompt construction
- System prompts
- User input boundaries
- Context assembly
- Retrieval
- Chunking
- Embeddings
- Vector search
- Ranking
- Tool calling
- Function schemas
- Output parsing
- Structured output validation
- Retry logic
- Token limits
- Truncation
- Cost controls
- Rate limits
- Caching
- Evaluation
- Safety filters
- Prompt injection defenses
- Data leakage
- Training-data assumptions
- Hallucination handling
- Confidence signaling
- Human review
- Reproducibility
- Non-determinism
- Model deprecation
- Provider outages
- Logging of prompts and outputs
- User deletion from model-related stores
- RAG freshness
- Citation integrity

Adversarially test:

- Prompt injection through user input
- Prompt injection through retrieved documents
- Tool argument injection
- Tool result injection
- Cross-user context leakage
- Hidden system prompt leakage
- Unauthorized tool use
- Fabricated citations
- Invalid structured output
- Partial JSON
- Excessive output
- Empty output
- Refusal
- Model timeout
- Provider rate limit
- Provider response format change
- Malicious uploaded document
- Poisoned retrieval content
- Duplicate chunks
- Stale embeddings
- Retrieval with no relevant evidence
- Conflicting evidence
- Long context truncation
- Cost explosion

Require evaluation evidence for claims about accuracy, safety, quality, or reliability.

---

## 8.23 Payments, Billing, Quotas, and Financial Logic, When Applicable

Inspect:

- Price representation
- Currency handling
- Tax handling
- Rounding
- Discounts
- Coupons
- Trials
- Subscriptions
- Renewals
- Cancellation
- Refunds
- Proration
- Failed payments
- Chargebacks
- Webhooks
- Idempotency
- Invoice state
- Entitlement state
- Quotas
- Usage metering
- Race conditions
- Duplicate events
- Out-of-order events
- Provider and local state reconciliation

Check that client-supplied price, quantity, plan, or entitlement data cannot control server-side billing decisions.

---

## 8.24 Multi-Tenancy and Organizational Boundaries, When Applicable

Inspect:

- Tenant resolution
- Tenant storage
- Query scoping
- Cache scoping
- File scoping
- Queue scoping
- Search index scoping
- Analytics scoping
- Logs
- Background jobs
- Exports
- Admin tools
- Support tools
- Invitations
- Membership changes
- Role changes
- Tenant deletion
- Tenant transfer
- Shared resources

Test cross-tenant identifiers systematically. One missing tenant filter can invalidate the entire security model.

---

## 8.25 CLI, SDK, and Library Contracts, When Applicable

Inspect:

- Public exports
- Semantic versioning
- Backward compatibility
- Error behavior
- Exit codes
- Standard output
- Standard error
- Configuration precedence
- Environment handling
- File-system side effects
- Signal handling
- Cancellation
- Shell quoting
- Path handling
- Windows, Linux, and macOS assumptions
- Documentation examples
- Package publishing
- Tree shaking
- Type declarations
- Runtime and type-level agreement

Treat examples as contract tests and verify they execute.

---

## 8.26 Mobile, Desktop, and Platform-Specific Behavior, When Applicable

Inspect:

- Permission requests
- Secure storage
- Deep links
- File access
- Background execution
- Offline state
- Synchronization
- Update mechanisms
- Code signing
- Platform APIs
- Crash recovery
- App lifecycle
- Window lifecycle
- Accessibility
- Local database migrations
- Cross-platform path assumptions
- Bundled secrets
- Native module versions
- Store configuration
- Telemetry
- Device identifier use

---

# 9. Adversarial Test Matrix

Apply this matrix to every critical workflow where relevant.

## 9.1 Input dimensions

Test or reason through:

- Missing field
- Null
- Undefined
- Empty string
- Whitespace-only string
- Zero
- Negative value
- Maximum allowed value
- Value above maximum
- Minimum allowed value
- Value below minimum
- Very long string
- Very large collection
- Unicode
- Combining characters
- Emoji
- Right-to-left text
- Invalid encoding
- Duplicate values
- Repeated fields
- Unknown fields
- Wrong type
- Malformed JSON
- Malformed multipart body
- Truncated input
- Invalid date
- Leap day
- Daylight saving transition
- Timezone boundary
- NaN
- Infinity
- Very precise decimal
- Path separators
- Relative paths
- Absolute paths
- Shell metacharacters
- HTML
- Script content
- SQL fragments
- Template syntax
- URLs to private networks
- Redirect chains
- Compressed bombs
- Archive traversal
- Unsupported file type
- Misleading file extension
- Incorrect MIME type

## 9.2 State dimensions

Test or reason through:

- First use
- No data
- One record
- Many records
- Duplicate records
- Stale record
- Deleted record
- Soft-deleted record
- Partially migrated record
- Older schema
- Newer schema
- Corrupted record
- Missing relationship
- Orphaned relationship
- Concurrent edit
- Concurrent delete
- Retry after partial success
- Replayed event
- Out-of-order event
- Expired session
- Revoked role
- Suspended account
- Deleted account
- Feature flag changes during workflow
- Deployment during workflow
- Process restart during workflow

## 9.3 Dependency dimensions

Test or reason through:

- Dependency unavailable
- Slow dependency
- Timeout
- Rate limit
- Invalid response
- Partial response
- Empty response
- Duplicate response
- Stale response
- Authentication failure
- Authorization failure
- Provider schema change
- TLS failure
- DNS failure
- Connection reset
- Database unavailable
- Database read-only
- Cache unavailable
- Queue unavailable
- Disk full
- File permission denied
- Memory pressure
- CPU pressure
- Clock skew
- Missing environment variable
- Invalid environment variable
- Secret rotation
- Network partition

## 9.4 User and permission dimensions

Test or reason through:

- Anonymous
- Authenticated
- Wrong role
- Read-only role
- Suspended user
- Deleted user
- User from another tenant
- User who previously had access
- Service account
- Administrator
- Compromised low-privilege account
- Direct API caller bypassing UI
- Automated high-volume caller

---

# 10. Required Contradiction Checks

Explicitly compare the following pairs:

- README vs implementation
- Product requirements vs implementation
- Architecture document vs dependency graph
- UI labels vs actual behavior
- Frontend types vs backend schemas
- API documentation vs routes
- API client vs server behavior
- Tests vs runtime behavior
- Test environment vs production environment
- ORM models vs migrations
- Migrations vs current database assumptions
- Validation schema vs database constraints
- Environment example vs variables actually read
- Variables documented vs variables used
- Package scripts vs CI commands
- CI build vs deployment build
- Local build vs container build
- Feature flags in client vs server
- Authorization in UI vs authorization in backend
- Error messages vs actual error causes
- Comments vs code
- Package manifest vs imports
- Lockfile vs manifest
- Declared supported versions vs syntax and dependencies
- Changelog vs repository history
- License claims vs included code
- Public API types vs runtime values
- Cache keys vs tenant and authorization boundaries
- Event producers vs event consumers
- Webhook payload assumptions vs provider contract
- Backup claims vs restore implementation
- Delete claims vs actual data erasure
- Security claims vs configuration defaults
- Privacy claims vs logging and analytics
- Performance claims vs algorithmic behavior or measurements
- "Production-ready" claims vs operational readiness

Create a dedicated contradiction table in the report.

---

# 11. High-Signal Search Passes

Use language-appropriate tools. Generic searches should include, where applicable:

```bash
rg -n --hidden --glob '!.git' \
  'TODO|FIXME|XXX|HACK|TEMP|WIP|NotImplemented|UnsupportedOperation|placeholder|stub|mock|fake|demo|sample' .

rg -n --hidden --glob '!.git' \
  'skip|skipped|xfail|focus|only\(|describe\.only|it\.only|test\.only|@Ignore|@Disabled|coverage:ignore|nocov' .

rg -n --hidden --glob '!.git' \
  'eslint-disable|ts-ignore|ts-expect-error|type:ignore|noinspection|nolint|allow\(dead_code\)|allow\(unused\)' .

rg -n --hidden --glob '!.git' \
  'console\.log|print\(|println!|debugger|breakpoint|traceback|panic!|fatal|process\.exit' .

rg -n --hidden --glob '!.git' \
  'password|passwd|secret|api[_-]?key|access[_-]?token|private[_-]?key|client[_-]?secret|BEGIN .*PRIVATE KEY' .

rg -n --hidden --glob '!.git' \
  'eval\(|exec\(|system\(|popen\(|shell=True|child_process|Runtime\.getRuntime|ProcessBuilder|subprocess' .

rg -n --hidden --glob '!.git' \
  'verify=False|rejectUnauthorized:\s*false|NODE_TLS_REJECT_UNAUTHORIZED|InsecureSkipVerify|disable.*tls|allow_insecure' .

rg -n --hidden --glob '!.git' \
  'SELECT \*|DELETE FROM|DROP TABLE|TRUNCATE|CASCADE|raw\(|execute\(|query\(' .
```

Adapt searches to the language and framework. Do not treat raw matches as findings. Inspect context, reachability, environment, and exploitability.

Also search for:

- Duplicate route registrations
- Duplicate configuration keys
- Conflicting package versions
- Unused environment variables
- Missing environment variables
- Unused dependencies
- Unused exports
- Publicly reachable debug code
- Broad exception handlers
- Empty exception handlers
- Unawaited tasks
- Fire-and-forget calls
- Non-transactional multi-step writes
- Hardcoded URLs
- Localhost assumptions
- Absolute paths
- Platform-specific separators
- Randomness
- Time access
- Sleep-based synchronization
- Manual retry loops
- Recursive calls
- Unbounded loops
- Full-table queries
- Large payload parsing
- File extraction
- Redirect handling
- Webhook handlers
- Admin routes
- Support impersonation
- Test-mode switches
- Demo accounts
- Seed credentials

---

# 12. Git History and Change-Risk Review

When history is available, inspect it selectively to answer:

- Which files change most often?
- Which files receive repeated bug fixes?
- Which areas have high churn and low tests?
- Which architectural decisions were recently reversed?
- Which TODOs are old?
- Which code was copied or moved without corresponding test changes?
- Which security-sensitive code changed recently?
- Which migrations were amended after release?
- Which dependencies were added without clear use?
- Which generated files frequently drift?
- Which files have a single knowledgeable author?
- Which modules have large diffs with little review evidence?

Use:

```bash
git log --stat
git log -- path/to/file
git blame -L <start>,<end> path/to/file
git diff <base>...<target>
```

Do not use author identity as a quality judgment. Use history to understand intent, churn, and regression risk.

For pull request audits, review both:

1. The changed lines and their direct consequences.
2. The unchanged surrounding system that the change relies on.

A diff-only review is insufficient when the change alters contracts, schemas, permissions, state, or architecture.

---

# 13. Finding Severity Rubric

Assign severity based on impact, reachability, likelihood, blast radius, detectability, and reversibility.

## Critical

Use when the issue can plausibly cause one or more of:

- Remote code execution
- Authentication bypass
- Broad authorization bypass
- Cross-tenant data exposure
- Exposure of highly sensitive data
- Irreversible widespread data loss
- Compromise of production infrastructure or CI secrets
- Financial loss at meaningful scale
- Core product output that is dangerously false
- System-wide outage with no practical mitigation
- A release blocker affecting the primary workflow for most users

Critical findings require strong evidence. If exploitability or reachability is uncertain, lower confidence or severity.

## High

Use when the issue can plausibly cause:

- Serious data corruption
- Major privacy violation
- Privilege escalation with constraints
- Persistent failure of a core workflow
- Major reliability failure under realistic conditions
- Significant billing or entitlement errors
- A broken migration with production impact
- A security weakness requiring some precondition
- A product contradiction that invalidates the central promise
- An architectural flaw that makes safe operation or change unusually risky

## Medium

Use when the issue causes:

- Incorrect behavior for a meaningful subset of users
- Recoverable data inconsistency
- Important but non-core workflow failure
- Moderate security hardening gap
- Performance degradation under plausible load
- Operational blind spots
- Test gaps around meaningful behavior
- Maintainability risk likely to produce future defects
- Documentation drift that can cause incorrect deployment or use

## Low

Use when the issue causes:

- Minor incorrect behavior
- Limited edge-case failure
- Small usability or accessibility defect
- Low-impact configuration inconsistency
- Localized maintainability friction
- Weak diagnostics with a straightforward workaround
- Documentation error unlikely to cause material harm

## Informational

Use for:

- Observations
- Cleanup opportunities
- Non-blocking inconsistencies
- Future risks requiring no immediate action
- Verified strengths relevant to remediation planning

Do not use Informational to pad the report.

---

# 14. Confidence Rubric

Assign one confidence level per finding.

- **High confidence:** Directly reproduced or proven by an unambiguous reachable code path.
- **Medium confidence:** Strong code evidence, but runtime reproduction is blocked or environment-dependent.
- **Low confidence:** Plausible hypothesis supported by partial evidence and requiring targeted verification.

Low-confidence Critical findings require explicit explanation and should generally be treated as High severity pending verification unless the potential impact justifies immediate containment.

---

# 15. Required Finding Format

Use this structure for every finding.

```markdown
## F-###: Concise finding title

- **Severity:** Critical / High / Medium / Low / Informational
- **Priority:** P0 / P1 / P2 / P3
- **Confidence:** High / Medium / Low
- **Category:** Security / Correctness / Reliability / Product / Architecture / Data / Performance / Testing / Documentation / Other
- **Status:** Verified / Strong inference / Hypothesis / Unknown
- **Affected components:** ...
- **Affected users or actors:** ...
- **Affected environments:** ...
- **Relevant workflow:** ...

### Summary

One precise paragraph describing the defect or risk.

### Evidence

- `path/to/file.ext:line-line`, `symbolName`
- Relevant call chain:
  `entryPoint -> validator -> service -> persistence -> response`
- Command:
  `...`
- Observed output:
  `...`

### Reproduction

1. ...
2. ...
3. ...

### Expected behavior

...

### Actual behavior

...

### Root cause

...

### Impact

Describe the concrete user, product, security, operational, or maintenance consequence.

### Reachability and likelihood

Explain the preconditions and how realistic they are.

### Blast radius

Explain how much data, functionality, infrastructure, or how many users may be affected.

### Strongest counterargument or mitigating factor

State the strongest defense of the current implementation.

### Assessment of the counterargument

Explain whether the mitigation changes severity, likelihood, or priority.

### Recommended remediation

Provide the smallest safe fix and, where relevant, a structural follow-up.

### Validation criteria

State exactly how to prove the issue is fixed.

### Regression test

Specify the test that should fail before the fix and pass after it.

### Dependencies and sequencing

List other findings, migrations, product decisions, or operational changes that affect remediation.
```

Do not merge unrelated defects into one finding merely because they occur in the same file.

Do merge repeated instances when they share one root cause, while listing all affected locations.

---

# 16. Priority Rubric

Severity describes impact. Priority describes action order.

- **P0:** Stop release or contain immediately.
- **P1:** Fix before the next production release or before meaningful user growth.
- **P2:** Schedule in the next planned engineering cycle.
- **P3:** Address when touching the area or when capacity permits.

Priority must account for:

- Exploitability
- Frequency
- Blast radius
- Dependency order
- Ease of containment
- Migration timing
- Upcoming releases
- Cost of delay
- Cost of repair
- Whether the issue blocks reliable diagnosis of other issues

---

# 17. Required Final Report Structure

Return the audit in the following order.

---

## 17.1 Audit Status

Start with exactly one:

- **AUDIT COMPLETE**
- **AUDIT COMPLETE WITH LIMITATIONS**
- **AUDIT INCOMPLETE**

Then state why.

---

## 17.2 Executive Verdict

Include:

- Ship / Do not ship / Ship only with stated conditions
- Overall codebase risk: Critical / High / Medium / Low
- Audit confidence: High / Medium / Low
- One-paragraph explanation
- Top five risks
- Strongest counterargument to the verdict
- What the team is most likely underestimating
- Immediate next action

Do not dilute the verdict with generic caveats.

---

## 17.3 Repository State

Include:

- Repository
- Branch
- Commit
- Working tree status
- Audit date
- Toolchain
- Environment
- Commands permitted
- Commands blocked
- External systems unavailable
- Files generated during audit

---

## 17.4 Product Reconstruction

Include:

- Actual product purpose
- Primary actors
- Core promise
- Critical workflows
- Actual implemented capabilities
- Partially implemented capabilities
- Missing capabilities
- Hidden or undocumented behavior
- Claims that could not be verified

---

## 17.5 Architecture and Data Flow

Include:

- Component map
- Entry points
- Dependency map
- Data stores
- External services
- Trust boundaries
- State ownership
- Critical data flows
- Transaction boundaries
- Asynchronous workflows
- Multiple sources of truth
- Architecture contradictions

Use text diagrams or Mermaid when useful, but ground every element in repository evidence.

---

## 17.6 Scope and Coverage Ledger

Provide:

- Total files or logical units
- Fully inspected
- Partially inspected
- Dynamically exercised
- Generated
- Vendored
- Binary
- Inaccessible
- Out of scope

Include the coverage table or a path to the full ledger.

State whether critical paths received full coverage.

---

## 17.7 Build and Verification Results

Use a table:

| Check | Command | Result | Key output | Interpretation |
|---|---|---|---|---|
| Install | `...` | Pass / Fail / Blocked | ... | ... |
| Build | `...` | ... | ... | ... |
| Type-check | `...` | ... | ... | ... |
| Lint | `...` | ... | ... | ... |
| Unit tests | `...` | ... | ... | ... |
| Integration tests | `...` | ... | ... | ... |
| End-to-end tests | `...` | ... | ... | ... |
| Dependency audit | `...` | ... | ... | ... |
| Runtime smoke test | `...` | ... | ... | ... |

Explain why a passing check may still provide limited assurance.

---

## 17.8 Findings Summary

Use a sortable table:

| ID | Severity | Priority | Confidence | Category | Title | Affected area | Status |
|---|---|---|---|---|---|---|---|

Order by:

1. Priority
2. Severity
3. Dependency order
4. Breadth of impact

---

## 17.9 Detailed Findings

Use the required finding template for every finding.

---

## 17.10 Contradictions and Unverifiable Claims

Use a table:

| ID | Source A | Source B | Contradiction | Runtime authority | Risk |
|---|---|---|---|---|---|

Also list claims that could not be verified because of missing environment, credentials, fixtures, or external systems.

---

## 17.11 Rejected Hypotheses

List meaningful risks investigated and ruled out.

For each:

- Hypothesis
- Evidence examined
- Why it was rejected
- Residual uncertainty

This prevents repeated investigation and demonstrates that the audit did more than collect search matches.

---

## 17.12 Remediation Plan

Divide into:

### P0: Immediate containment

Actions required before further deployment or use.

### P1: Release blockers

Actions required before the next release.

### P2: Structural correction

Architecture, testing, data, or operational work that reduces repeated risk.

### P3: Cleanup and long-term hardening

Lower-priority improvements.

For each action include:

- Findings addressed
- Owner type
- Estimated implementation complexity: Small / Medium / Large
- Dependencies
- Risk of change
- Required tests
- Rollout or migration concerns
- Success criteria

Do not provide time estimates unless explicitly requested.

---

## 17.13 Recommended Fix Sequence

Provide an ordered sequence that respects dependencies.

Example:

1. Preserve and back up affected data.
2. Close the authorization gap.
3. Add regression tests.
4. Correct the data model.
5. Run the migration.
6. Repair API and UI assumptions.
7. Improve observability.
8. Remove temporary containment.

Explain why the sequence is safer than fixing findings independently.

---

## 17.14 Quick Wins vs Structural Work

Use two sections:

- **Quick wins:** Low implementation risk, immediate value.
- **Structural work:** Broader changes required to eliminate root causes.

Do not mislabel symptom patches as structural fixes.

---

## 17.15 Residual Risk and Unknowns

Include:

- Areas not exercised
- Missing services
- Missing credentials
- Missing production configuration
- Missing data volumes
- Missing threat information
- Missing compliance requirements
- Missing deployment evidence
- Risks that remain after proposed fixes

---

## 17.16 Final Decision Framework

Conclude with:

- Conditions required to ship
- Conditions requiring a release stop
- Conditions requiring redesign
- Evidence required to increase confidence
- The single highest-leverage next action

---

# 18. Quality Bar for Recommendations

Every recommendation must be:

- Tied to a specific finding
- Technically feasible within the repository's architecture, or clearly labeled as architectural change
- Explicit about tradeoffs
- Explicit about migration risk
- Explicit about test requirements
- Explicit about rollout concerns
- Proportionate to severity
- Specific enough for implementation planning
- Validated by a clear success criterion

Avoid recommendations such as:

- "Rewrite everything"
- "Use microservices"
- "Add caching"
- "Add AI"
- "Use a better framework"
- "Increase test coverage"
- "Improve security"
- "Refactor for scalability"

These may be conclusions only after repository-specific evidence establishes the need.

---

# 19. Audit Anti-Patterns

The audit fails if it does any of the following:

- Summarizes folder names without tracing behavior
- Reviews only files changed recently
- Reviews only the main application folder
- Accepts README claims without verification
- Treats passing tests as proof of correctness
- Reports dependency scanner output without reachability analysis
- Reports search matches as confirmed defects
- Omits reproduction or evidence
- Hides blocked checks
- Uses vague severity
- Recommends a rewrite without proving incremental repair is insufficient
- Focuses on code style while missing product or security defects
- Ignores configuration and deployment
- Ignores data migrations
- Ignores background jobs
- Ignores failure paths
- Ignores authorization because authentication exists
- Ignores cross-tenant behavior
- Ignores dead code because it is not currently called
- Ignores tests that are skipped
- Modifies the code before recording findings
- Claims complete coverage without a ledger
- Produces only an executive summary
- Produces a list of issues with no remediation sequence
- Hides uncertainty
- Inflates findings to appear thorough
- Stops after the first severe defect

---

# 20. Completion Checklist

Before submitting the report, verify every applicable item.

## Repository and baseline

- [ ] Repository, branch, and commit recorded
- [ ] Working tree state recorded
- [ ] Toolchain versions recorded
- [ ] Environment limitations recorded
- [ ] Submodules, LFS, and generated artifacts checked

## Coverage

- [ ] Repository census completed
- [ ] Hidden files included
- [ ] Every meaningful path has a coverage status
- [ ] Critical paths fully inspected
- [ ] Reduced-coverage areas justified
- [ ] Inaccessible areas listed

## Product and architecture

- [ ] Product promise reconstructed
- [ ] Actor inventory completed
- [ ] Capability matrix completed
- [ ] Entry points mapped
- [ ] Components mapped
- [ ] Data flows mapped
- [ ] Trust boundaries mapped
- [ ] State ownership mapped
- [ ] External dependencies mapped

## Verification

- [ ] Install attempted
- [ ] Build attempted
- [ ] Type-check attempted
- [ ] Lint attempted
- [ ] Tests attempted
- [ ] Runtime smoke test attempted
- [ ] Production configuration considered
- [ ] Blocked checks explained

## Audit domains

- [ ] Product completeness
- [ ] Architecture
- [ ] Correctness
- [ ] Error handling
- [ ] Security
- [ ] Authentication and authorization
- [ ] Privacy
- [ ] Data integrity
- [ ] APIs and integrations
- [ ] Concurrency and idempotency
- [ ] Performance and cost
- [ ] Frontend and accessibility
- [ ] Backend
- [ ] Configuration and secrets
- [ ] Dependencies and licensing
- [ ] Tests and quality gates
- [ ] CI, deployment, and infrastructure
- [ ] Observability and operations
- [ ] Maintainability
- [ ] Documentation
- [ ] Dead code and placeholders
- [ ] Conditional domains reviewed or marked not applicable

## Findings

- [ ] Every finding has evidence
- [ ] Every finding has severity
- [ ] Every finding has confidence
- [ ] Every finding has impact
- [ ] Every finding has root cause
- [ ] Every finding has remediation
- [ ] Every finding has validation criteria
- [ ] Critical and High findings include a counterargument
- [ ] Duplicate symptoms are grouped by root cause
- [ ] Unrelated defects are separated
- [ ] Speculation is labeled

## Final report

- [ ] Audit status declared
- [ ] Executive verdict provided
- [ ] Coverage summary provided
- [ ] Build and test results provided
- [ ] Findings summary provided
- [ ] Detailed findings provided
- [ ] Contradictions listed
- [ ] Rejected hypotheses listed
- [ ] Remediation plan prioritized
- [ ] Fix sequence provided
- [ ] Residual risks listed
- [ ] Highest-leverage next action stated

---

# 21. Final Instruction to the Audit Agent

Do not produce a reassuring repository overview. Produce a defensible engineering audit.

Assume a future maintainer, security reviewer, investor, research collaborator, or production operator will make a consequential decision based on your report. They must be able to reproduce your findings, distinguish facts from hypotheses, understand the actual system, and act in the correct order.

Inspect broadly, trace deeply, challenge every claim, and document every limitation.

When evidence is missing, say so.

When behavior is contradictory, preserve the contradiction.

When a severe defect is found, continue auditing.

When a recommendation is expensive, prove why the cost is justified.

When the repository is stronger than expected, state that only after the same level of scrutiny.

The final report must show what is true, what is broken, what is uncertain, what matters most, and what should happen next.
