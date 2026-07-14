# Network Loopback Multi-Run Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a repeatable multi-run local Mirror/KCP loopback gate with an atomic aggregate summary and non-zero failure behavior.

**Architecture:** Keep the existing player bootstrap and per-role result contract unchanged. Add pure summary helpers to `NetworkLoopback.psm1`, refactor the coordinator into build-once and run-once boundaries, then loop over independent run IDs and artifact directories from the entry script.

**Tech Stack:** Windows PowerShell 5.1, Pester 3.4, Unity 2022.3, Mirror/KCP, JSON/NDJSON artifacts.

---

### Task 1: Summary helpers

**Files:**
- Modify: `scripts/NetworkLoopback.psm1`
- Test: `scripts/tests/NetworkLoopback.Tests.ps1`

- [ ] Add a failing Pester test that calls `Write-NetworkLoopbackSummary` with two run records and asserts the JSON has `requestedIterations`, `completedIterations`, `passedIterations`, `failedIterations`, `success`, and both run IDs.
- [ ] Run `Invoke-Pester -Script .\scripts\tests\NetworkLoopback.Tests.ps1 -PassThru` and confirm failure because the helper is undefined.
- [ ] Implement `Write-NetworkLoopbackSummary` using `ConvertTo-Json -Depth 8`, a `.tmp` sibling, and `Move-Item` for atomic replacement; export the function.
- [ ] Re-run the Pester file and confirm all tests pass.

### Task 2: Multi-run coordinator

**Files:**
- Modify: `scripts/Run-NetworkLoopback.ps1`
- Test: `scripts/tests/NetworkLoopback.Tests.ps1`

- [ ] Add failing source-contract tests that require an `Iterations` parameter with `ValidateRange(1, 100)`, reject explicit multi-run ports, and require the coordinator to call `Write-NetworkLoopbackSummary`.
- [ ] Run the Pester file and confirm the new tests fail for the missing behavior.
- [ ] Add `-Iterations`; resolve/build the executable once; extract the current player orchestration into `Invoke-NetworkLoopbackRun`; loop through per-run IDs and directories; collect duration and results; stop on first failure; always write the batch summary.
- [ ] Preserve the current one-run command and output, while printing batch progress and the final summary path for multi-run execution.
- [ ] Re-run both Pester suites and confirm all tests pass.

### Task 3: Documentation and real verification

**Files:**
- Modify: `docs/network-loopback-baseline.md`
- Modify: `docs/local-network-test-plan.md`

- [ ] Document `-Iterations`, the batch directory layout, `summary.json`, stop-on-first-failure behavior, and the distinction between a 3-run engineering check and the 20-run L1 acceptance threshold.
- [ ] Locate the newest `AoyiLoopback.exe` and run `Run-NetworkLoopback.ps1 -SkipBuild -BuildPath <path> -Iterations 3 -TimeoutSeconds 90`.
- [ ] Read the generated summary and verify requested/completed/passed are `3`, failed is `0`, success is true, ports are distinct, and all scenes are `dantiao_map`.
- [ ] Run both Pester files, Unity Coverage, scoped `git diff --check`, Markdown link checks, and confirm no Unity or AoyiLoopback processes remain.
