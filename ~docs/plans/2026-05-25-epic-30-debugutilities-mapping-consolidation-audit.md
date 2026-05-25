# Epic 30 DebugUtilities Mapping Consolidation Audit (2026-05-25)

## Goal

Reduce duplicated CpuType/MemoryType mapping logic and centralize debugger mapping policies outside coordinator-heavy callsites.

## Focus Areas

1. Shared CpuType metadata helper for memory mapping and program-counter width.
2. Deterministic regression coverage for mapping compatibility.
3. Refactor callsites to consume consolidated helper outputs.

## Initial Slice Trio

1. #2530 - Add shared CpuType metadata helper.
2. #2531 - Add full mapping regression tests.
3. #2532 - Refactor DebugUtilities callsites to helper mappings.

## Acceptance

- Mapping functions preserve behavior.
- Regression tests cover supported CpuType mappings.
- DebugUtilities code has lower duplication and clearer mapping ownership.
