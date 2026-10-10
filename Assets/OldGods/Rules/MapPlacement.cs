using System;
using System.Collections.Generic;

namespace OldGods.Rules
{
    public enum FeatureKind { BossGate, Chest, Shrine, Merchant, Duplicator, LorePickup, OfferingShrine }

    public struct FeatureRequest
    {
        public FeatureKind Kind;
        public int Count;
        /// <summary>Minimum distance to any other feature of the same kind.</summary>
        public float SpacingSameKind;

        public FeatureRequest(FeatureKind kind, int count, float spacingSameKind)
        {
            Kind = kind;
            Count = count;
            SpacingSameKind = spacingSameKind;
        }
    }

    public struct Placement
    {
        public FeatureKind Kind;
        public float X, Z;
        public int Index; // nth of its kind

        public override string ToString() => $"{Kind}#{Index} ({X:0.#}, {Z:0.#})";
    }

    /// <summary>Global spacing rules for map features. PLACEHOLDER numbers.</summary>
    [Serializable]
    public sealed class PlacementRules
    {
        /// <summary>Minimum distance between any two features.</summary>
        public float MinSpacing = 9f;
        /// <summary>Features stay at least this far from the player's start.</summary>
        public float MinFromSpawn = 18f;
        /// <summary>The boss gate is at least this far from the start, so it must be found.</summary>
        public float BossGateMinFromSpawn = 70f;
        /// <summary>Features need ground flatter than this, in degrees.</summary>
        public float MaxSlope = 22f;
        /// <summary>Extra distance kept from the rim.</summary>
        public float EdgeMargin = 6f;
        public int AttemptsPerFeature = 400;
    }

    /// <summary>
    /// Scatters features over a map. With a layout, the boss gate goes where the layout put it
    /// and features first take the layout's detour spots (a chest on the hill fort, a shrine in
    /// the stone circle); the rest are dart-thrown under spacing and slope rules, clear of dressing.
    /// </summary>
    public static class MapPlacement
    {
        /// <summary>Features keep this far from walls, columns and other solid dressing.</summary>
        public const float DressingClearance = 2.5f;
        /// <summary>A detour spot sits inside its landmark, so it needs less room.</summary>
        public const float SpotClearance = 1.2f;

        public static List<Placement> Place(HeightField field, float rimWidth, PlacementRules rules, IList<FeatureRequest> requests, Rng rng, LevelLayout layout = null)
        {
            var placed = new List<Placement>();
            float minX = field.MinX + rimWidth + rules.EdgeMargin, maxX = field.MaxX - rimWidth - rules.EdgeMargin;
            float minZ = field.MinZ + rimWidth + rules.EdgeMargin, maxZ = field.MaxZ - rimWidth - rules.EdgeMargin;
            if (maxX <= minX || maxZ <= minZ) return placed;

            var spots = new List<Spot>();
            if (layout != null)
            {
                spots.AddRange(layout.Spots);
                rng.Shuffle(spots);
            }

            foreach (var req in requests)
            {
                int made = 0;
                if (req.Kind == FeatureKind.BossGate && layout != null && layout.HasGate)
                {
                    for (int i = 0; i < req.Count && i < 1; i++)
                        placed.Add(new Placement { Kind = req.Kind, X = layout.GateX, Z = layout.GateZ, Index = made++ });
                    continue;
                }
                for (int i = 0; i < spots.Count && made < req.Count; i++)
                {
                    var s = spots[i];
                    if (s.Prefer != req.Kind || !Fits(field, s.X, s.Z, req, rules, placed)) continue;
                    if (layout.Obstacles.Overlaps(s.X, s.Z, SpotClearance)) continue;
                    placed.Add(new Placement { Kind = req.Kind, X = s.X, Z = s.Z, Index = made });
                    made++;
                    spots.RemoveAt(i--);
                }
                for (int attempt = 0; attempt < rules.AttemptsPerFeature * Math.Max(1, req.Count) && made < req.Count; attempt++)
                {
                    float x = rng.Range(minX, maxX), z = rng.Range(minZ, maxZ);
                    if (!Fits(field, x, z, req, rules, placed)) continue;
                    if (layout != null && layout.Obstacles.Overlaps(x, z, DressingClearance)) continue;
                    placed.Add(new Placement { Kind = req.Kind, X = x, Z = z, Index = made });
                    made++;
                }
            }
            return placed;
        }

        public static bool Fits(HeightField field, float x, float z, FeatureRequest req, PlacementRules rules, List<Placement> placed)
        {
            float fromSpawn = (float)Math.Sqrt(x * x + z * z);
            float minSpawn = req.Kind == FeatureKind.BossGate ? rules.BossGateMinFromSpawn : rules.MinFromSpawn;
            if (fromSpawn < minSpawn) return false;
            if (field.SlopeDegrees(x, z) > rules.MaxSlope) return false;
            // Check the footprint too, so a feature does not straddle a cliff edge.
            const float r = 2f;
            if (field.SlopeDegrees(x + r, z) > rules.MaxSlope || field.SlopeDegrees(x - r, z) > rules.MaxSlope ||
                field.SlopeDegrees(x, z + r) > rules.MaxSlope || field.SlopeDegrees(x, z - r) > rules.MaxSlope) return false;
            foreach (var p in placed)
            {
                float dx = p.X - x, dz = p.Z - z;
                float d2 = dx * dx + dz * dz;
                float need = p.Kind == req.Kind ? Math.Max(rules.MinSpacing, req.SpacingSameKind) : rules.MinSpacing;
                if (d2 < need * need) return false;
            }
            return true;
        }
    }
}
