# [PROJECT_NAME] Constitution
<!-- Example: Spec Constitution, TaskFlow Constitution, etc. -->

## Core Principles

## Software Engineering Rules
1. Use strong types, no 'var' are allowed.
2. Follow SOLID principles.
3. Ensure high cohesion and low coupling.
4. Write unit tests for all new functionality.
5. Ensure code is well-documented with XML comments.
6. Use ILogger nullable for Libraries. Use ILogger non-nullable for Applications.
7. ILogger parameter should be always the last parameter in method signatures.
8. IConfiguration should be injected via constructor and not method parameters.
9. IConfiguration should not be used directly in methods.
10. IConfiguration and IOptions<T> should not be mixed in the same class.
11. IConfiguration and IOptions should be the first parameters in constructor signatures.
12. Avoid using magic strings; use constants or enums instead.
13. Ensure proper exception handling with custom exceptions where appropriate.
14. Follow the Single Responsibility Principle for classes and methods.
15. Use meaningful names for variables, methods, and classes.
16. Ensure methods do not exceed 30 lines of code.
17. Avoid deep nesting; refactor into smaller methods if necessary.
18. Use LINQ for collections manipulation where appropriate.
19. One class per file.
20. The folder convention is Model will depends only on other models, Interface will depend only on other interfaces and models, Library can have any dependencies.
21. Keep line length under 120 characters.
22. Follow DRY (Don't Repeat Yourself) principle.
23. Use dependency injection for all services.
24. Avoid static classes except for extension methods.
25. Use async/await for all I/O operations.
26. Follow naming conventions: PascalCase for classes and methods, camelCase for variables and parameters.
27. No variables should be preceded by '_'.

### [PRINCIPLE_1_NAME]
<!-- Example: I. Library-First -->
[PRINCIPLE_1_DESCRIPTION]
<!-- Example: Every feature starts as a standalone library; Libraries must be self-contained, independently testable, documented; Clear purpose required - no organizational-only libraries -->

### [PRINCIPLE_2_NAME]
<!-- Example: II. CLI Interface -->
[PRINCIPLE_2_DESCRIPTION]
<!-- Example: Every library exposes functionality via CLI; Text in/out protocol: stdin/args → stdout, errors → stderr; Support JSON + human-readable formats -->

### [PRINCIPLE_3_NAME]
<!-- Example: III. Test-First (NON-NEGOTIABLE) -->
[PRINCIPLE_3_DESCRIPTION]
<!-- Example: TDD mandatory: Tests written → User approved → Tests fail → Then implement; Red-Green-Refactor cycle strictly enforced -->

### [PRINCIPLE_4_NAME]
<!-- Example: IV. Integration Testing -->
[PRINCIPLE_4_DESCRIPTION]
<!-- Example: Focus areas requiring integration tests: New library contract tests, Contract changes, Inter-service communication, Shared schemas -->

### [PRINCIPLE_5_NAME]
<!-- Example: V. Observability, VI. Versioning & Breaking Changes, VII. Simplicity -->
[PRINCIPLE_5_DESCRIPTION]
<!-- Example: Text I/O ensures debuggability; Structured logging required; Or: MAJOR.MINOR.BUILD format; Or: Start simple, YAGNI principles -->

## [SECTION_2_NAME]
<!-- Example: Additional Constraints, Security Requirements, Performance Standards, etc. -->

[SECTION_2_CONTENT]
<!-- Example: Technology stack requirements, compliance standards, deployment policies, etc. -->

## [SECTION_3_NAME]
<!-- Example: Development Workflow, Review Process, Quality Gates, etc. -->

[SECTION_3_CONTENT]
<!-- Example: Code review requirements, testing gates, deployment approval process, etc. -->

## Governance
<!-- Example: Constitution supersedes all other practices; Amendments require documentation, approval, migration plan -->

[GOVERNANCE_RULES]
<!-- Example: All PRs/reviews must verify compliance; Complexity must be justified; Use [GUIDANCE_FILE] for runtime development guidance -->

**Version**: [CONSTITUTION_VERSION] | **Ratified**: [RATIFICATION_DATE] | **Last Amended**: [LAST_AMENDED_DATE]
<!-- Example: Version: 2.1.1 | Ratified: 2025-06-13 | Last Amended: 2025-07-16 -->
