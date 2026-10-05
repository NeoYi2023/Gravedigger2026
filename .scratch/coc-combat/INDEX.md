# COC 战斗

规则已写入 SPEC，本目录只拆后续编码切片。权威：[SPEC_03 §3.21](../../../SPEC_03_GameRules.md) / D-099，表 [SPEC_04 §9.35](../../../SPEC_04_Technical.md)–[§9.38](../../../SPEC_04_Technical.md)，存档 [SPEC_04 §6](../../../SPEC_04_Technical.md) `SandboxProgress`。

每片编码前再确认难度。迷雾片（04）已落地（方案 A 格子盖章；04b 同格三态就地更新）。占领（05）与长按展开（06）已落地。一次会话最多实现一个未阻塞切片。

| Issue | 内容 | 状态 |
|-------|------|------|
| [01-enter-counts](issues/01-enter-counts.md) | 沙盘剩余次数存档；挖坟/商店/自动造兵进入扣次 | done |
| [02-map-enter](issues/02-map-enter.md) | 复制搜打撤地图为 COC 预制体，从沙盘进入空地图 | done |
| [03-deploy-and-boss](issues/03-deploy-and-boss.md) | 洒兵、走向最终 BOSS、胜负与扣次（已拆成 03a–03d） | done |
| [03a-spawn-idle](issues/03a-spawn-idle.md) | 进图刷怪并停在刷怪点 | done |
| [03b-deploy-fight](issues/03b-deploy-fight.md) | 按职业洒兵并选敌战斗 | done |
| [03c-boss-march](issues/03c-boss-march.md) | 没有敌人时走向最终 BOSS | done |
| [03d-win-loss](issues/03d-win-loss.md) | 击杀最终 BOSS 通关，失败扣次 | done |
| [04-fog](issues/04-fog.md) | 三态黑雾与可洒区域（已拆成 04a–04b） | done |
| [04a-static-fog](issues/04a-static-fog.md) | 静态黑雾、藏普通怪、多边形内禁洒 | done |
| [04b-reveal-dark](issues/04b-reveal-dark.md) | 临时亮起、变暗、已发现与重进重置 | done |
| [05-capture](issues/05-capture.md) | 占领点激活、永久亮起、给其他玩法加次数 | done |
| [06-card-hold](issues/06-card-hold.md) | 长按卡牌展开组内士兵，只查看 | done |
