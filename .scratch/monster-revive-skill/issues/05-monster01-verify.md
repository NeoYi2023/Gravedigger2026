# 05-monster01-verify

spec_refs: SPEC_03 §3.14 Demo 怪物死亡复活边界; SPEC_04 §9.21c

## 配置

- `Defend_MonsterConfig`: `Monster_01` → `Skills=MSkill_SelfRevive;0`
- `Combat_MonsterSkillEffectConfig`: `MSkill_SelfRevive_99`（MaxReviveCount=99，`AlertRadius=6`）

## PlayMode 验收清单

1. PushMap 击杀 `Monster_01`：尸体留于击飞终点；约 10s 后以 75% HP 复活；复活过程约 1.5s 倒放死亡动画。
2. **变暗延续：** 自死亡 latch 至 Delay、倒放 1.5s、复活后无敌 1s 全程保持 RGB×0.4；无敌结束后恢复亮色。
3. **Die2 优先：** 有 `Die2` Trigger 的模型（如 `MonsterModel_05`，须先运行 `Tools/Gravedigger/Art/Wire Monster Die2 Animators`）倒放 `Die2_*`；无 Die2 则倒放 `Die_*`。
4. 假死 / CD / 倒放 / 复活后 1s：士兵不选该怪、HitConfirm 不结算。
5. 连续击杀 3 次：前 2 次复活，第 3 次计击杀；击杀数 +1。
6. 带 `IsBoss` 的复活怪：假死不触发 BOSS 通关，彻底死亡才触发。
7. **回归：** 无复活技能怪/士兵普通死亡变暗逻辑不变。
8. **首次复活警戒半径：** `Monster_01` 表内 `AlertRadius=3`，复活后实例改为 EffectParams `6`（怪发现士兵 + 士兵遇敌检测均变大）；第二次复活仍为 6，不再改。

## 给其他怪物启用

`MonsterConfig.Skills` 增加 `MSkill_SelfRevive;0`（或新建 MonsterSkillId + EffectParams 行）。
