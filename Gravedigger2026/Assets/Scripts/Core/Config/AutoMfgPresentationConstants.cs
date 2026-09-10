using UnityEngine;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// UI-016 AutoManufacture body-rain / revive tunables from CombatConstantConfig (SPEC_04 §9.20b).
    /// </summary>
    public readonly struct AutoMfgPresentationConstants
    {
        public const float PixelsPerUnit = 100f;
        public const float OrthoSize = 5.4f;

        public float BodyDropIntervalSeconds { get; }
        public int DropCountMin { get; }
        public int DropCountMax { get; }
        public float GravityScale { get; }
        public float Bounciness { get; }
        public float Friction { get; }
        public float ColliderInset { get; }
        public float SpawnAngleMaxDeg { get; }
        public float SpawnJitterXPx { get; }
        public float MagicCircleFlashHz { get; }
        public float MagicCircleYPx { get; }
        public float BodyRainLayerYPx { get; }
        public float ReviveSpawnOffsetYPx { get; }
        public float PileFloorYPx { get; }
        public float SoldierLandYPx { get; }
        public float BodyMaxEdgePx { get; }
        public float SoldierMaxEdgePx { get; }
        public float SoldierVisualScale { get; }
        public float SoldierPitchFactor { get; }
        public float SoldierShadowWidthPx { get; }
        public float SoldierShadowHeightPx { get; }
        public float SoldierShadowAlpha { get; }
        public float SoldierShadowOffsetYPx { get; }
        public float BodyAngleMaxDeg { get; }
        public float UseLegacySoldierRow { get; }

        public AutoMfgPresentationConstants(
            float bodyDropIntervalSeconds,
            int dropCountMin,
            int dropCountMax,
            float gravityScale,
            float bounciness,
            float friction,
            float colliderInset,
            float spawnAngleMaxDeg,
            float spawnJitterXPx,
            float magicCircleFlashHz,
            float magicCircleYPx,
            float bodyRainLayerYPx,
            float reviveSpawnOffsetYPx,
            float pileFloorYPx,
            float soldierLandYPx,
            float bodyMaxEdgePx,
            float soldierMaxEdgePx,
            float soldierVisualScale,
            float soldierPitchFactor,
            float soldierShadowWidthPx,
            float soldierShadowHeightPx,
            float soldierShadowAlpha,
            float soldierShadowOffsetYPx,
            float bodyAngleMaxDeg,
            float useLegacySoldierRow)
        {
            BodyDropIntervalSeconds = Mathf.Max(0.01f, bodyDropIntervalSeconds);
            DropCountMin = Mathf.Max(1, dropCountMin);
            DropCountMax = Mathf.Max(DropCountMin, dropCountMax);
            GravityScale = Mathf.Max(0.01f, gravityScale);
            Bounciness = Mathf.Clamp01(bounciness);
            Friction = Mathf.Clamp01(friction);
            ColliderInset = Mathf.Clamp(colliderInset, 0.35f, 1f);
            SpawnAngleMaxDeg = Mathf.Max(0f, spawnAngleMaxDeg);
            SpawnJitterXPx = Mathf.Max(0f, spawnJitterXPx);
            MagicCircleFlashHz = Mathf.Max(0f, magicCircleFlashHz);
            MagicCircleYPx = magicCircleYPx;
            BodyRainLayerYPx = bodyRainLayerYPx;
            ReviveSpawnOffsetYPx = reviveSpawnOffsetYPx;
            PileFloorYPx = pileFloorYPx;
            SoldierLandYPx = soldierLandYPx;
            BodyMaxEdgePx = Mathf.Max(8f, bodyMaxEdgePx);
            SoldierMaxEdgePx = Mathf.Max(8f, soldierMaxEdgePx);
            SoldierVisualScale = Mathf.Max(0.01f, soldierVisualScale);
            SoldierPitchFactor = Mathf.Max(0.01f, soldierPitchFactor);
            SoldierShadowWidthPx = Mathf.Max(1f, soldierShadowWidthPx);
            SoldierShadowHeightPx = Mathf.Max(1f, soldierShadowHeightPx);
            SoldierShadowAlpha = Mathf.Clamp01(soldierShadowAlpha);
            SoldierShadowOffsetYPx = soldierShadowOffsetYPx;
            BodyAngleMaxDeg = Mathf.Max(0f, bodyAngleMaxDeg);
            UseLegacySoldierRow = useLegacySoldierRow;
        }

        public static AutoMfgPresentationConstants FromRepository(ConfigCsvRepository configs)
        {
            if (configs == null)
            {
                return SafetyDefaults;
            }

            return new AutoMfgPresentationConstants(
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgBodyDropIntervalSeconds,
                    CombatConstantKeys.Safety.AutoMfgBodyDropIntervalSeconds),
                Mathf.RoundToInt(configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgDropCountMin,
                    CombatConstantKeys.Safety.AutoMfgDropCountMin)),
                Mathf.RoundToInt(configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgDropCountMax,
                    CombatConstantKeys.Safety.AutoMfgDropCountMax)),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgGravityScale,
                    CombatConstantKeys.Safety.AutoMfgGravityScale),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgBounciness,
                    CombatConstantKeys.Safety.AutoMfgBounciness),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgFriction,
                    CombatConstantKeys.Safety.AutoMfgFriction),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgColliderInset,
                    CombatConstantKeys.Safety.AutoMfgColliderInset),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgSpawnAngleMaxDeg,
                    CombatConstantKeys.Safety.AutoMfgSpawnAngleMaxDeg),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgSpawnJitterXPx,
                    CombatConstantKeys.Safety.AutoMfgSpawnJitterXPx),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgMagicCircleFlashHz,
                    CombatConstantKeys.Safety.AutoMfgMagicCircleFlashHz),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgMagicCircleYPx,
                    CombatConstantKeys.Safety.AutoMfgMagicCircleYPx),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgBodyRainLayerYPx,
                    CombatConstantKeys.Safety.AutoMfgBodyRainLayerYPx),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgReviveSpawnOffsetYPx,
                    CombatConstantKeys.Safety.AutoMfgReviveSpawnOffsetYPx),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgPileFloorYPx,
                    CombatConstantKeys.Safety.AutoMfgPileFloorYPx),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgSoldierLandYPx,
                    CombatConstantKeys.Safety.AutoMfgSoldierLandYPx),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgBodyMaxEdgePx,
                    CombatConstantKeys.Safety.AutoMfgBodyMaxEdgePx),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgSoldierMaxEdgePx,
                    CombatConstantKeys.Safety.AutoMfgSoldierMaxEdgePx),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgSoldierVisualScale,
                    CombatConstantKeys.Safety.AutoMfgSoldierVisualScale),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgSoldierPitchFactor,
                    CombatConstantKeys.Safety.AutoMfgSoldierPitchFactor),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgSoldierShadowWidthPx,
                    CombatConstantKeys.Safety.AutoMfgSoldierShadowWidthPx),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgSoldierShadowHeightPx,
                    CombatConstantKeys.Safety.AutoMfgSoldierShadowHeightPx),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgSoldierShadowAlpha,
                    CombatConstantKeys.Safety.AutoMfgSoldierShadowAlpha),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgSoldierShadowOffsetYPx,
                    CombatConstantKeys.Safety.AutoMfgSoldierShadowOffsetYPx),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgBodyAngleMaxDeg,
                    CombatConstantKeys.Safety.AutoMfgBodyAngleMaxDeg),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.AutoMfgUseLegacySoldierRow,
                    CombatConstantKeys.Safety.AutoMfgUseLegacySoldierRow));
        }

        public static AutoMfgPresentationConstants SafetyDefaults => new AutoMfgPresentationConstants(
            CombatConstantKeys.Safety.AutoMfgBodyDropIntervalSeconds,
            Mathf.RoundToInt(CombatConstantKeys.Safety.AutoMfgDropCountMin),
            Mathf.RoundToInt(CombatConstantKeys.Safety.AutoMfgDropCountMax),
            CombatConstantKeys.Safety.AutoMfgGravityScale,
            CombatConstantKeys.Safety.AutoMfgBounciness,
            CombatConstantKeys.Safety.AutoMfgFriction,
            CombatConstantKeys.Safety.AutoMfgColliderInset,
            CombatConstantKeys.Safety.AutoMfgSpawnAngleMaxDeg,
            CombatConstantKeys.Safety.AutoMfgSpawnJitterXPx,
            CombatConstantKeys.Safety.AutoMfgMagicCircleFlashHz,
            CombatConstantKeys.Safety.AutoMfgMagicCircleYPx,
            CombatConstantKeys.Safety.AutoMfgBodyRainLayerYPx,
            CombatConstantKeys.Safety.AutoMfgReviveSpawnOffsetYPx,
            CombatConstantKeys.Safety.AutoMfgPileFloorYPx,
            CombatConstantKeys.Safety.AutoMfgSoldierLandYPx,
            CombatConstantKeys.Safety.AutoMfgBodyMaxEdgePx,
            CombatConstantKeys.Safety.AutoMfgSoldierMaxEdgePx,
            CombatConstantKeys.Safety.AutoMfgSoldierVisualScale,
            CombatConstantKeys.Safety.AutoMfgSoldierPitchFactor,
            CombatConstantKeys.Safety.AutoMfgSoldierShadowWidthPx,
            CombatConstantKeys.Safety.AutoMfgSoldierShadowHeightPx,
            CombatConstantKeys.Safety.AutoMfgSoldierShadowAlpha,
            CombatConstantKeys.Safety.AutoMfgSoldierShadowOffsetYPx,
            CombatConstantKeys.Safety.AutoMfgBodyAngleMaxDeg,
            CombatConstantKeys.Safety.AutoMfgUseLegacySoldierRow);

        public Vector2 PixelsToWorld(float xPx, float yPx)
        {
            return new Vector2(xPx / PixelsPerUnit, yPx / PixelsPerUnit);
        }

        public float PileFloorWorldY => PileFloorYPx / PixelsPerUnit;
        public float SoldierLandWorldY => SoldierLandYPx / PixelsPerUnit;
    }
}

