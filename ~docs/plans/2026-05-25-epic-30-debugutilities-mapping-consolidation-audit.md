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

## Continued Slice Trio

1. #2535 - Extend CpuType metadata helper with PRG-ROM mapping.
2. #2536 - Add PRG-ROM mapping compatibility regression tests.
3. #2537 - Refactor PRG-ROM mapping callsite to metadata helper.

## Progress

- Completed: #2532, #2533, #2534 (shared CpuType metadata for CPU-memory and PC-width mappings).
- Completed: #2535, #2536, #2537 (PRG-ROM mapping consolidation).

## Third Slice Trio

1. #2538 - Add base CPU-memory ownership lookup helper from CpuType metadata.
2. #2541 - Add base CPU-memory ownership mapping regression tests.
3. #2543 - Refactor ToCpuType base-memory path to metadata helper.

## Current Status

- In Progress: #2538, #2541, #2543 (base-memory ownership path consolidation in ToCpuType).

## Acceptance

- Mapping functions preserve behavior.
- Regression tests cover supported CpuType mappings.
- DebugUtilities code has lower duplication and clearer mapping ownership.
