using UnityEngine;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    internal static class AmPresentationLayers
    {
        public const string Body = "AmBody";
        public const string Soldier = "AmSoldier";
        public const string Floor = "AmFloor";

        public static int BodyLayer => Resolve(Body);
        public static int SoldierLayer => Resolve(Soldier);
        public static int FloorLayer => Resolve(Floor);

        public static void ConfigureCollisionMatrix()
        {
            var body = BodyLayer;
            var soldier = SoldierLayer;
            var floor = FloorLayer;
            if (body >= 0 && soldier >= 0)
            {
                // Bodies pile among themselves; revived soldiers pass through the pile.
                Physics2D.IgnoreLayerCollision(body, soldier, true);
            }

            if (soldier >= 0 && floor >= 0)
            {
                // Pile floor is AmFloor; soldier land collider uses AmSoldier (same layer as pieces).
                Physics2D.IgnoreLayerCollision(soldier, floor, true);
            }
        }

        private static int Resolve(string name)
        {
            var layer = LayerMask.NameToLayer(name);
            if (layer < 0)
            {
                Debug.LogWarning($"[AmPresentation] Layer '{name}' missing — add via TagManager or AmAssetBuilder.");
            }

            return layer;
        }
    }
}
