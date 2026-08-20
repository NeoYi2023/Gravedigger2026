# Projectile

远程弹道源素材。Prefab：`Assets/Prefabs/Defend/Projectile.prefab`。

| 基础职业 BaseClass | 源图 | Catalog 字段 |
|-------------------|------|--------------|
| 射手 Archer | `JianShi_1.png` | `DefendPrefabCatalog._archerProjectileSprite` |
| 法师 Mage | `MoFa_1.png` | `DefendPrefabCatalog._mageProjectileSprite` |
| Dig 炸药桶 | `ZYT_1.png` | `DigPrefabCatalog._explosiveBarrelSprite` |

贴图弹头朝 **+Y（图片上方）**；运行时根节点 `LookRotation` 对齐飞行方向，`Visual` 子节点 `localEulerAngles = (90,0,0)`。
