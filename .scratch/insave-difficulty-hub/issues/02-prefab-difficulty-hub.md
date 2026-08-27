---
title: InSaveShellPanel Prefab + DifficultySelectHost
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.5 / §3.6 UI-008 / UI-029 / §3.8 D-081
  - SPEC_04 §6
---

## Goal

Standalone `InSaveShellPanel.prefab`; DifficultySelectHost default expanded Normal; embed LevelSelect with auto-select last + Enter; Hard/Hell Toast; chrome buttons unchanged.

## Done when

- [x] Prefab Ensure menu + runtime EnsureDifficultySelectHost
- [x] EnterShell shows Hub + LevelSelect
- [x] Enter button → TryEnterLevel Stage 1
- [x] Hard/Hell → Toast「还未制作」
