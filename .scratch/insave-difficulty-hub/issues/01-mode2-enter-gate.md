---
title: Skip CampaignModeSelect enter as Mode2
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.2 / §3.4 / §3.8 D-045
  - SPEC_04 §6
---

## Goal

`HandleCreate` / `HandleEnter` skip `PromptCampaignMode`; always `EnterShell(slot, Mode2, isCreate)`.

## Done when

- [x] New save / enter occupied → no mode popup → Mode2 CSV root
- [x] CampaignModeSelectView retained unused on this path
