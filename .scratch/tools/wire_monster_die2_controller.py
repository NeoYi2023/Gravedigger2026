#!/usr/bin/env python3
"""Patch MonsterModel_05 animator controller with Die2 trigger + states (offline, no Unity)."""
from __future__ import annotations

import re
import secrets
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
CONTROLLER = (
    REPO
    / "Gravedigger2026/Assets/Art/Characters/Monsters/MonsterModel_05"
    / "Animation Clips/MonsterModel_05_20260808_164641_animator.controller"
)
DIE2_DIR = CONTROLLER.parent / "Die2"
IDLE_STATE = -5916796618116603871
DIE_SM_POS = (800, 20, 0)

DIR_TO_SUFFIX = {
    0: "E",
    1: "W",
    2: "S",
    3: "N",
    4: "NE",
    5: "NW",
    6: "SE",
    7: "SW",
}


def new_id(existing: set[int]) -> int:
    while True:
        # Unity uses signed 64-bit; stay in safe int range.
        value = secrets.randbits(63)
        if value > (1 << 62):
            value -= (1 << 63)
        if value not in existing and value != 0:
            existing.add(value)
            return value


def read_guid(meta_path: Path) -> str:
    text = meta_path.read_text(encoding="utf-8")
    match = re.search(r"^guid: ([0-9a-f]{32})$", text, re.MULTILINE)
    if not match:
        raise ValueError(f"No guid in {meta_path}")
    return match.group(1)


def load_existing_ids(text: str) -> set[int]:
    return {int(m.group(1)) for m in re.finditer(r"^--- !u!\d+ &(-?\d+)$", text, re.MULTILINE)}


def die2_clip_guids() -> dict[str, str]:
    guids: dict[str, str] = {}
    for meta in DIE2_DIR.glob("*.anim.meta"):
        name = meta.name.replace(".anim.meta", "")
        guids[name] = read_guid(meta)
    return guids


def state_yaml(state_id: int, name: str, clip_guid: str, transition_id: int, pos_y: float) -> str:
    return f"""--- !u!1102 &{state_id}
AnimatorState:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Speed: 1
  m_CycleOffset: 0
  m_Transitions:
  - {{fileID: {transition_id}}}
  m_StateMachineBehaviours: []
  m_Position: {{x: 50, y: {pos_y:.0f}, z: 0}}
  m_IKOnFeet: 0
  m_WriteDefaultValues: 1
  m_Mirror: 0
  m_SpeedParameterActive: 0
  m_MirrorParameterActive: 0
  m_CycleOffsetParameterActive: 0
  m_TimeParameterActive: 0
  m_Motion: {{fileID: 7400000, guid: {clip_guid}, type: 2}}
  m_Tag: 
  m_SpeedParameter: 
  m_MirrorParameter: 
  m_CycleOffsetParameter: 
  m_TimeParameter: 
"""


def exit_transition_yaml(trans_id: int) -> str:
    return f"""--- !u!1101 &{trans_id}
AnimatorStateTransition:
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: 
  m_Conditions: []
  m_DstStateMachine: {{fileID: 0}}
  m_DstState: {{fileID: {IDLE_STATE}}}
  m_Solo: 0
  m_Mute: 0
  m_IsExit: 0
  serializedVersion: 3
  m_TransitionDuration: 0
  m_TransitionOffset: 0
  m_ExitTime: 1
  m_HasExitTime: 1
  m_HasFixedDuration: 1
  m_InterruptionSource: 0
  m_OrderedInterruption: 1
  m_CanTransitionToSelf: 1
"""


def anystate_transition_yaml(trans_id: int, dir_index: int, dst_state_id: int) -> str:
    return f"""--- !u!1101 &{trans_id}
AnimatorStateTransition:
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: 
  m_Conditions:
  - m_ConditionMode: 1
    m_ConditionEvent: Die2
    m_EventTreshold: 0
  - m_ConditionMode: 6
    m_ConditionEvent: DirIndex
    m_EventTreshold: {dir_index}
  m_DstStateMachine: {{fileID: 0}}
  m_DstState: {{fileID: {dst_state_id}}}
  m_Solo: 0
  m_Mute: 0
  m_IsExit: 0
  serializedVersion: 3
  m_TransitionDuration: 0
  m_TransitionOffset: 0
  m_ExitTime: 0.75
  m_HasExitTime: 0
  m_HasFixedDuration: 1
  m_InterruptionSource: 0
  m_OrderedInterruption: 1
  m_CanTransitionToSelf: 1
"""


def main() -> None:
    text = CONTROLLER.read_text(encoding="utf-8")
    if 'm_Name: Die2\n  m_ChildStates:' in text:
        print("Controller already contains Die2 state machine; skipping.")
        return

    clip_guids = die2_clip_guids()
    ids = load_existing_ids(text)

    die2_sm_id = new_id(ids)
    child_entries: list[str] = []
    state_blocks: list[str] = []
    anystate_ids: list[int] = []
    default_state_id: int | None = None

    for dir_index in range(8):
        suffix = DIR_TO_SUFFIX[dir_index]
        die2_name = f"Die2_{suffix}"
        if die2_name not in clip_guids:
            raise SystemExit(f"Missing clip {die2_name}")

        state_id = new_id(ids)
        if dir_index == 0:
            default_state_id = state_id
        exit_id = new_id(ids)
        any_id = new_id(ids)
        anystate_ids.append(any_id)

        child_entries.append(
            f"  - serializedVersion: 1\n"
            f"    m_State: {{fileID: {state_id}}}\n"
            f"    m_Position: {{x: 200, y: {dir_index * 65}, z: 0}}"
        )
        state_blocks.append(exit_transition_yaml(exit_id))
        state_blocks.append(state_yaml(state_id, die2_name, clip_guids[die2_name], exit_id, dir_index * 65))
        state_blocks.append(anystate_transition_yaml(any_id, dir_index, state_id))

    assert default_state_id is not None

    die2_sm_yaml = f"""--- !u!1107 &{die2_sm_id}
AnimatorStateMachine:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: Die2
  m_ChildStates:
{chr(10).join(child_entries)}
  m_ChildStateMachines: []
  m_AnyStateTransitions: []
  m_EntryTransitions: []
  m_StateMachineTransitions: {{}}
  m_StateMachineBehaviours: []
  m_AnyStatePosition: {{x: 50, y: 20, z: 0}}
  m_EntryPosition: {{x: 50, y: 120, z: 0}}
  m_ExitPosition: {{x: 800, y: 120, z: 0}}
  m_ParentStateMachinePosition: {{x: {DIE_SM_POS[0]}, y: {DIE_SM_POS[1]}, z: {DIE_SM_POS[2]}}}
  m_DefaultState: {{fileID: {default_state_id}}}
"""

    die2_param = """  - m_Name: Die2
    m_Type: 9
    m_DefaultFloat: 0
    m_DefaultInt: 0
    m_DefaultBool: 0
    m_Controller: {fileID: 9100000}
"""
    if "  - m_Name: Die2\n" in text:
        raise SystemExit("Die2 parameter already present")

    text = text.replace(
        "  - m_Name: Die\n    m_Type: 9\n    m_DefaultFloat: 0\n    m_DefaultInt: 0\n    m_DefaultBool: 0\n    m_Controller: {fileID: 9100000}\n",
        "  - m_Name: Die\n    m_Type: 9\n    m_DefaultFloat: 0\n    m_DefaultInt: 0\n    m_DefaultBool: 0\n    m_Controller: {fileID: 9100000}\n"
        + die2_param,
        1,
    )

    child_sm_entry = (
        f"  - serializedVersion: 1\n"
        f"    m_StateMachine: {{fileID: {die2_sm_id}}}\n"
        f"    m_Position: {{x: {DIE_SM_POS[0]}, y: {DIE_SM_POS[1]}, z: 0}}"
    )
    text = text.replace(
        "    m_StateMachine: {fileID: -7754213689448372144}\n    m_Position: {x: 0, y: 0, z: 0}",
        "    m_StateMachine: {fileID: -7754213689448372144}\n    m_Position: {x: 0, y: 0, z: 0}\n"
        + child_sm_entry,
        1,
    )

    anystate_block = "".join(f"  - {{fileID: {tid}}}\n" for tid in anystate_ids)
    anchor = "  - {fileID: -4917091705456921549}\n  m_EntryTransitions: []"
    if anchor not in text:
        raise SystemExit("Root AnyState anchor not found; controller layout changed.")
    text = text.replace(
        anchor,
        "  - {fileID: -4917091705456921549}\n" + anystate_block + "  m_EntryTransitions: []",
        1,
    )

    append = die2_sm_yaml + "\n" + "\n".join(state_blocks) + "\n"
    if not text.endswith("\n"):
        text += "\n"
    text += append

    CONTROLLER.write_text(text, encoding="utf-8")
    print(f"Patched {CONTROLLER.name}: Die2 SM={die2_sm_id}, transitions={len(anystate_ids)}")


if __name__ == "__main__":
    main()
