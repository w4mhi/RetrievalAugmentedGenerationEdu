<!--
Sync Impact Report:
Version: 1.0.0 (Initial version based on existing OmniRAG codebase)
Created: 2025-12-28
Modified Principles: N/A (initial creation)
Added Sections: Core Principles (33 rules), Clean Architecture, Technology Stack, Testing Requirements, Governance
Removed Sections: N/A
Templates Status:
  ✅ plan-template.md - Constitution Check section aligns with new principles
  ✅ spec-template.md - User story structure supports test-first approach
  ✅ tasks-template.md - Task tracking ready for principle-driven categorization
Follow-up TODOs: None - all placeholders filled from existing codebase context
-->

# OmniRAG Constitution

## Core Principles

This constitution defines the non-negotiable software engineering rules and architectural principles for the OmniRAG Enterprise RAG System. All code, documentation, and decisions MUST align with these principles.

## Software Engineering Rules

The following 33 rules are mandatory for all code contributions:

### I. Type Safety & Code Quality (Rules 1-6)

**Rules:**
1. Use strong types, no `var` are allowed
2. Follow SOLID principles (Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion)
3. Ensure high cohesion and low coupling
4. Write unit tests for all new functionality
5. Ensure code is well-documented with XML comments
6. Use ILogger nullable (`ILogger<T>?`) for Libraries; Use ILogger non-nullable (`ILogger<T>`) for Applications

**Rationale:** Strong typing prevents runtime errors, SOLID principles ensure maintainability, XML documentation enables IntelliSense and API documentation generation. Logger nullability distinguishes library code (defensive) from application code (guaranteed DI).

**Testable:** Code reviews verify explicit types, SOLID compliance, XML comments presence, and correct ILogger nullability.

### II. Dependency Injection & Configuration (Rules 7-11)

**Rules:**
7. ILogger parameter MUST be always the last parameter in constructor and method signatures
8. IConfiguration MUST be injected via constructor only, never as method parameters
9. IConfiguration MUST NOT be used directly in method bodies; use IOptions<T> pattern for typed configuration
10. IConfiguration and IOptions<T> MUST NOT be mixed in the same class; choose one pattern
11. IConfiguration and IOptions<T> MUST be the first parameters in constructor signatures

**Rationale:** Consistent parameter ordering (configuration first, logger last) improves code readability and maintainability. IOptions<T> provides strong typing and validation. Separation prevents confusion between configuration approaches.

**Testable:** Static analysis tools and code reviews verify parameter positions. Build fails if mixing IConfiguration and IOptions<T>.

### III. Constants & Magic Values (Rules 12, 31-32)

**Rules:**
12. Avoid using magic strings; use constants or enums instead
31. Do NOT use hard-coded values; define them as constants in code
32. All values that can change per environment MUST be defined in appsettings.json

**Rationale:** Magic values are unmaintainable and error-prone. Constants enable compile-time checking and IDE refactoring. Environment-specific configuration in appsettings.json follows 12-factor app methodology.

**Testable:** Code reviews flag magic strings/numbers. Configuration validation ensures all environment values are in appsettings.json.

### IV. Exception Handling & Responsibility (Rules 13-15)

**Rules:**
13. Ensure proper exception handling with custom exceptions where appropriate
14. Follow the Single Responsibility Principle for classes and methods
15. Use meaningful names for variables, methods, and classes

**Rationale:** Custom exceptions provide domain context. SRP ensures each class/method has one reason to change. Meaningful names serve as inline documentation.

**Testable:** Code reviews verify custom exception usage, SRP compliance, and name clarity.

### V. Method & Code Structure (Rules 16-19, 24)

**Rules:**
16. Ensure methods do NOT exceed 30 lines of code (refactor if longer)
17. Avoid deep nesting; refactor into smaller methods if nesting exceeds 3 levels
18. Use LINQ for collections manipulation where appropriate
19. One class per file; file name MUST match class name
24. Avoid static classes except for extension methods

**Rationale:** Short methods improve readability and testability. Shallow nesting reduces cognitive load. LINQ provides declarative, readable collection operations. One class per file enables navigation. Static classes violate OOP principles except for stateless extensions.

**Testable:** Static analysis tools enforce line count, nesting depth, and one-class-per-file. Code reviews verify LINQ usage and static class justification.

### VI. Code Style & Formatting (Rules 21, 26-28, 33)

**Rules:**
21. Keep line length under 120 characters
26. Follow naming conventions: PascalCase for classes/methods/properties, camelCase for variables/parameters
27. NO variables should be preceded by underscore (`_fieldName` is prohibited)
28. Use `this.` prefix ALWAYS for instance members (signature style)
33. Remove all unneeded `using` statements

**Rationale:** 120-char limit ensures readability on standard screens. Consistent naming follows C# conventions. No underscore prefix aligns with modern C# style. `this.` prefix provides explicit context (project signature). Clean using statements reduce cognitive noise.

**Testable:** EditorConfig enforces line length and naming. Code reviews verify `this.` usage and clean using statements.

### VII. DRY Principle & Reusability (Rule 22)

**Rules:**
22. Follow DRY (Don't Repeat Yourself) principle; extract common logic to reusable methods/classes

**Rationale:** Code duplication increases maintenance burden and bug surface area. Abstraction enables testing and evolution.

**Testable:** Code reviews flag duplicate logic blocks (>5 lines identical).

### VIII. Dependency Injection & Async (Rules 23, 25)

**Rules:**
23. Use dependency injection for all services; constructor injection is mandatory
25. Use async/await for ALL I/O operations (database, HTTP, file system)

**Rationale:** DI enables testability, loose coupling, and lifecycle management. Async prevents thread blocking and improves scalability.

**Testable:** Code reviews verify constructor injection. Static analysis detects synchronous I/O calls.

### IX. Explicit Configuration (Rules 29-30)

**Rules:**
29. Always use explicit includes: `<ImplicitUsings>disable</ImplicitUsings>` in all .csproj files
30. Initialize logger to console for development environment (enable debugging)

**Rationale:** Explicit using statements make dependencies clear and prevent namespace pollution. Console logging ensures observability during local development.

**Testable:** Build system verifies ImplicitUsings=disable. Startup code validates console logger registration.

### X. Folder & Layer Dependencies (Rule 20)

**Rules:**
20. The folder convention is: Models depend only on other models, Interfaces depend only on other interfaces and models, Services/Implementations can have any dependencies (respecting layer rules)

**Rationale:** Clear dependency rules prevent circular references and maintain Clean Architecture boundaries.

**Testable:** Dependency analysis tools verify layer compliance.

## Clean Architecture

**Layer Structure:** OmniRAG follows strict Clean Architecture with dependency flow: Presentation → Core → Infrastructure

**Layers:**
- **Core (OmniRAG.Core):** Domain models, interfaces, business logic. ZERO external dependencies.
- **Infrastructure (OmniRAG.Infrastructure):** Implementations (PDF loading, embeddings, vector stores, language models). Depends on Core.
- **Presentation (OmniRAG.Console):** CLI application, configuration, DI container. Depends on Core and Infrastructure.
- **Tests (OmniRAG.Tests):** Unit and integration tests. Depends on all layers.

**Folder Convention:**
- Models depend only on other models
- Interfaces depend only on other interfaces and models
- Services/Implementations can have any dependencies (but follow layer rules)

**Testable:** Dependency analysis tools verify no Core → Infrastructure references.

## Technology Stack

**Mandatory Technologies:**
- **Language:** C# 12 with .NET 9.0
- **Testing:** xUnit, Moq, FluentAssertions (minimum 90% coverage on critical paths)
- **Logging:** ILogger from Microsoft.Extensions.Logging (structured logging)
- **DI Container:** Microsoft.Extensions.DependencyInjection
- **AI/ML:** Semantic Kernel 1.31+, Microsoft.ML.OnnxRuntime (local inference)
- **Python Interop:** Python.NET 3.0.5 (for ChromaDB and sentence-transformers)

**Approved Design Patterns:**
- Repository Pattern (data access abstraction)
- Strategy Pattern (swappable algorithms: embedding, chunking, retrieval, language models)
- Factory Pattern (configuration-driven object creation)
- Observer Pattern (file system monitoring)
- Circuit Breaker Pattern (resilience)
- Retry Pattern (transient fault handling)
- Decorator Pattern (caching, logging)

## Testing Requirements

### Test-First Development (NON-NEGOTIABLE)

**Workflow:** Tests written → User approved → Tests fail (red) → Implementation → Tests pass (green) → Refactor

**Mandatory Test Types:**
1. **Unit Tests:** All business logic, validation, state machines (90%+ coverage)
2. **Integration Tests:** PDF loading, embedding generation, vector search, language model inference
3. **Performance Tests:** Query latency (<8s end-to-end), embedding throughput (>10k tokens/sec)

**Test Structure:**
- Arrange-Act-Assert pattern
- One logical assertion per test method
- Test data builders for complex entities
- Mocks for external dependencies (ChromaDB, language model APIs)

**Coverage Targets:**
- Core layer: 100% (business logic is critical)
- Infrastructure layer: 90%+ (integration tests cover I/O)
- Presentation layer: 80%+ (CLI parsing and orchestration)

**Testable:** CI/CD pipeline enforces coverage thresholds. Tests must pass before merge.

## Observability

**Structured Logging Levels:**
- **Trace:** Method entry/exit, parameter values (development only)
- **Information:** Operation events (document indexed, query executed, model loaded)
- **Warning:** Degraded behavior (slow query, retry triggered, circuit breaker opened)
- **Error:** Failures that don't crash the system (validation errors, transient faults)
- **Critical:** System failures requiring immediate attention (Python runtime initialization failure)

**Mandatory Log Events:**
- All I/O operations (PDF load, vector store query, language model inference)
- All errors with exception details and context
- Performance metrics (query duration, token throughput, chunk count)
- Health status changes (circuit breaker state transitions)

**Testable:** Log output validation in unit tests. Observability dashboards in production.

## Performance Standards

**Targets:**
- PDF chunking: <1ms per chunk (semantic boundaries)
- Embedding generation: >10,000 tokens/second (batch processing)
- Vector search: <50ms (Top-K from ChromaDB)
- Language model inference: 2-8 seconds (depends on model: local vs. cloud)
- End-to-end query: <8 seconds (from question to answer with sources)

**Scalability:**
- Support ≥100 PDF documents (≥10,000 chunks total)
- Support ≥50 concurrent queries
- Memory usage: <2GB per process (excluding language model)

**Testable:** Performance tests in CI/CD validate latency and throughput.

## Governance

### Amendment Process

1. **Proposal:** Submit GitHub issue with rationale, impact analysis, and migration plan
2. **Review:** Maintainers review within 7 business days
3. **Approval:** Requires 2 maintainer approvals
4. **Migration:** Update code, tests, documentation, and templates
5. **Version Bump:** Increment constitution version (semantic versioning)

### Versioning Policy

- **MAJOR:** Breaking changes (rule removal, incompatible redefinition)
- **MINOR:** Additive changes (new rules, expanded guidance)
- **PATCH:** Clarifications, typo fixes, non-semantic refinements

### Compliance Review

- **Pre-commit:** Developers self-check against constitution
- **Code Review:** Reviewers verify compliance with all 33 rules
- **CI/CD:** Static analysis enforces automated checks (line length, naming, using statements)
- **Quarterly Audit:** Team reviews constitution relevance and proposes updates

### Conflict Resolution

Constitution supersedes all other practices. When conflicts arise:
1. Constitution rules take precedence over team preferences
2. Exceptions require documented justification and maintainer approval
3. Complexity must be justified with performance data or architectural necessity

### Development Guidance

For runtime development guidance, see:
- `.github/copilot-instructions.md` - AI-assisted development guidelines
- `Documentation/ARCHITECTURE.md` - Detailed technical design
- `Documentation/IMPROVEMENTS.md` - Enhancement roadmap

**Version:** 1.0.0 | **Ratified:** 2025-12-28 | **Last Amended:** 2025-12-28
