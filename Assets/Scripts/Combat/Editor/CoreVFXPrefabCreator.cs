using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectArk.Combat;

namespace ProjectArk.Combat.Editor
{
    /// <summary>
    /// Editor tool that procedurally generates placeholder VFX prefabs for the
    /// four Core families (Matter / Light / Echo / Anomaly), so that the data-driven
    /// MuzzleFlash / ImpactVFX pipeline has real assets to bind to.
    ///
    /// Rationale: per Docs/3_WorkflowsAndRules/Project/ProceduralPresentation_WorkflowSpec.md
    /// and the StarChart Governance Plan, every Core family must have visually
    /// distinct muzzle + impact feedback. Sourcing authored art for all four families
    /// is out of scope for this governance batch — this tool emits programmatic
    /// placeholders that are clearly family-coded and good enough to verify the
    /// replacement seam end-to-end.
    ///
    /// Output: 8 prefabs in Assets/_Prefabs/VFX/Core/
    ///   MuzzleFlash_Matter.prefab   ImpactVFX_Matter.prefab
    ///   MuzzleFlash_Light.prefab    ImpactVFX_Light.prefab
    ///   MuzzleFlash_Echo.prefab     ImpactVFX_Echo.prefab
    ///   MuzzleFlash_Anomaly.prefab  ImpactVFX_Anomaly.prefab
    ///
    /// Each prefab contains:
    ///   - ParticleSystem (family-tuned color / shape / speed)
    ///   - PooledVFX (auto-return on playback complete)
    ///
    /// Run from: ProjectArk &gt; Create Core VFX Placeholder Prefabs
    /// </summary>
    public static class CoreVFXPrefabCreator
    {
        private const string OUTPUT_FOLDER = "Assets/_Prefabs/VFX/Core";
        private const string MENU_PATH = "ProjectArk/Create Core VFX Placeholder Prefabs";

        [MenuItem(MENU_PATH)]
        public static void CreateAll()
        {
            if (!EnsureFolder(OUTPUT_FOLDER))
            {
                Debug.LogError($"[CoreVFXPrefabCreator] Failed to create/verify output folder: {OUTPUT_FOLDER}");
                return;
            }

            int created = 0;
            int overwritten = 0;

            foreach (CoreFamily family in System.Enum.GetValues(typeof(CoreFamily)))
            {
                var profile = GetProfile(family);
                if (profile == null) continue;

                var muzzlePath = $"{OUTPUT_FOLDER}/MuzzleFlash_{family}.prefab";
                var impactPath = $"{OUTPUT_FOLDER}/ImpactVFX_{family}.prefab";

                var muzzleExisted = AssetDatabase.LoadAssetAtPath<GameObject>(muzzlePath) != null;
                var impactExisted = AssetDatabase.LoadAssetAtPath<GameObject>(impactPath) != null;

                BuildAndSave(BuildMuzzle(profile), muzzlePath);
                BuildAndSave(BuildImpact(profile), impactPath);

                if (muzzleExisted) overwritten++; else created++;
                if (impactExisted) overwritten++; else created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CoreVFXPrefabCreator] Done. Created {created}, overwrote {overwritten}. Output folder: {OUTPUT_FOLDER}");
            EditorUtility.DisplayDialog(
                "Core VFX Placeholders Generated",
                $"8 placeholder VFX prefabs written to:\n{OUTPUT_FOLDER}\n\n" +
                $"Created: {created}\nOverwritten: {overwritten}\n\n" +
                "Next step: open the 4 baseline Core SOs (MatterCore / LightCore / EchoCore / AnomalyCore) " +
                "and assign MuzzleFlashPrefab + ImpactVFXPrefab.\n\n" +
                "You can also run 'ProjectArk > Audit StarCore VFX' to verify coverage.",
                "OK");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Family profiles — visual DNA for each Core family.
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Visual profile for a Core family. Defines color palette and particle
        /// behavior DNA so Matter / Light / Echo / Anomaly feel distinct at a glance.
        /// </summary>
        private class FamilyProfile
        {
            public Color PrimaryColor;
            public Color SecondaryColor;
            public float MuzzleLifetime;
            public float ImpactLifetime;
            public float MuzzleSpeed;
            public float ImpactSpeed;
            public int MuzzleBurst;
            public int ImpactBurst;
            public float MuzzleSize;
            public float ImpactSize;
            public ParticleSystemShapeType MuzzleShape;
            public ParticleSystemShapeType ImpactShape;
            public float MuzzleShapeAngle;   // For cone
            public float MuzzleShapeRadius;
            public float ImpactShapeRadius;
        }

        private static FamilyProfile GetProfile(CoreFamily family)
        {
            switch (family)
            {
                case CoreFamily.Matter:
                    // 物理/橙黄：短促扇形火花 → 径向爆开
                    return new FamilyProfile
                    {
                        PrimaryColor = new Color(1f, 0.75f, 0.25f, 1f),
                        SecondaryColor = new Color(1f, 0.45f, 0.05f, 0f),
                        MuzzleLifetime = 0.12f,
                        ImpactLifetime = 0.25f,
                        MuzzleSpeed = 7f,
                        ImpactSpeed = 5f,
                        MuzzleBurst = 8,
                        ImpactBurst = 12,
                        MuzzleSize = 0.18f,
                        ImpactSize = 0.22f,
                        MuzzleShape = ParticleSystemShapeType.Cone,
                        ImpactShape = ParticleSystemShapeType.Circle,
                        MuzzleShapeAngle = 18f,
                        MuzzleShapeRadius = 0.05f,
                        ImpactShapeRadius = 0.1f,
                    };
                case CoreFamily.Light:
                    // 能量/青白：薄环脉冲 → 快速膨胀环
                    return new FamilyProfile
                    {
                        PrimaryColor = new Color(0.7f, 0.95f, 1f, 1f),
                        SecondaryColor = new Color(0.3f, 0.7f, 1f, 0f),
                        MuzzleLifetime = 0.15f,
                        ImpactLifetime = 0.3f,
                        MuzzleSpeed = 4f,
                        ImpactSpeed = 8f,
                        MuzzleBurst = 10,
                        ImpactBurst = 16,
                        MuzzleSize = 0.22f,
                        ImpactSize = 0.25f,
                        MuzzleShape = ParticleSystemShapeType.Circle,
                        ImpactShape = ParticleSystemShapeType.Circle,
                        MuzzleShapeAngle = 0f,
                        MuzzleShapeRadius = 0.08f,
                        ImpactShapeRadius = 0.15f,
                    };
                case CoreFamily.Echo:
                    // 音波/紫：同心圆扩散环 → 三环脉冲
                    return new FamilyProfile
                    {
                        PrimaryColor = new Color(0.85f, 0.55f, 1f, 1f),
                        SecondaryColor = new Color(0.6f, 0.25f, 0.9f, 0f),
                        MuzzleLifetime = 0.2f,
                        ImpactLifetime = 0.4f,
                        MuzzleSpeed = 3f,
                        ImpactSpeed = 6f,
                        MuzzleBurst = 6,
                        ImpactBurst = 10,
                        MuzzleSize = 0.28f,
                        ImpactSize = 0.3f,
                        MuzzleShape = ParticleSystemShapeType.Circle,
                        ImpactShape = ParticleSystemShapeType.Circle,
                        MuzzleShapeAngle = 0f,
                        MuzzleShapeRadius = 0.12f,
                        ImpactShapeRadius = 0.2f,
                    };
                case CoreFamily.Anomaly:
                    // 异常/洋红绿：混沌散射 → 爆炸 + 二次噪点
                    return new FamilyProfile
                    {
                        PrimaryColor = new Color(1f, 0.3f, 0.75f, 1f),
                        SecondaryColor = new Color(0.3f, 1f, 0.5f, 0f),
                        MuzzleLifetime = 0.18f,
                        ImpactLifetime = 0.35f,
                        MuzzleSpeed = 9f,
                        ImpactSpeed = 7f,
                        MuzzleBurst = 14,
                        ImpactBurst = 20,
                        MuzzleSize = 0.15f,
                        ImpactSize = 0.2f,
                        MuzzleShape = ParticleSystemShapeType.Sphere,
                        ImpactShape = ParticleSystemShapeType.Sphere,
                        MuzzleShapeAngle = 0f,
                        MuzzleShapeRadius = 0.1f,
                        ImpactShapeRadius = 0.15f,
                    };
                default:
                    return null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Builders — construct GameObject hierarchies for each VFX type.
        // ─────────────────────────────────────────────────────────────────────

        private static GameObject BuildMuzzle(FamilyProfile p)
        {
            var go = new GameObject("MuzzleFlash");
            var ps = go.AddComponent<ParticleSystem>();
            ConfigureMain(ps, p.MuzzleLifetime, p.MuzzleSpeed, p.MuzzleSize, p.PrimaryColor);
            ConfigureEmission(ps, p.MuzzleBurst);
            ConfigureShape(ps, p.MuzzleShape, p.MuzzleShapeAngle, p.MuzzleShapeRadius);
            ConfigureColorOverLifetime(ps, p.PrimaryColor, p.SecondaryColor);
            ConfigureSizeOverLifetime(ps, startScale: 1f, endScale: 0f);
            ConfigureRenderer(ps);

            // PooledVFX handles Play on spawn + auto-return.
            go.AddComponent<PooledVFX>();
            return go;
        }

        private static GameObject BuildImpact(FamilyProfile p)
        {
            var go = new GameObject("ImpactVFX");
            var ps = go.AddComponent<ParticleSystem>();
            ConfigureMain(ps, p.ImpactLifetime, p.ImpactSpeed, p.ImpactSize, p.PrimaryColor);
            ConfigureEmission(ps, p.ImpactBurst);
            ConfigureShape(ps, p.ImpactShape, 0f, p.ImpactShapeRadius);
            ConfigureColorOverLifetime(ps, p.PrimaryColor, p.SecondaryColor);
            ConfigureSizeOverLifetime(ps, startScale: 0.8f, endScale: 0f);
            ConfigureRenderer(ps);

            go.AddComponent<PooledVFX>();
            return go;
        }

        // ─────────────────────────────────────────────────────────────────────
        // ParticleSystem module configurators.
        // ─────────────────────────────────────────────────────────────────────

        private static void ConfigureMain(ParticleSystem ps, float lifetime, float speed, float size, Color color)
        {
            var main = ps.main;
            main.duration = 0.25f;
            main.loop = false;
            main.playOnAwake = false;          // PooledVFX calls Play() explicitly
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None; // PooledVFX handles return
            main.maxParticles = 100;
        }

        private static void ConfigureEmission(ParticleSystem ps, int burstCount)
        {
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, burstCount)
            });
        }

        private static void ConfigureShape(ParticleSystem ps, ParticleSystemShapeType shape, float angle, float radius)
        {
            var s = ps.shape;
            s.enabled = true;
            s.shapeType = shape;
            s.angle = angle;
            s.radius = radius;
            s.radiusThickness = 1f;
        }

        private static void ConfigureColorOverLifetime(ParticleSystem ps, Color start, Color end)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.7f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(grad);
        }

        private static void ConfigureSizeOverLifetime(ParticleSystem ps, float startScale, float endScale)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            var curve = new AnimationCurve(
                new Keyframe(0f, startScale),
                new Keyframe(1f, endScale));
            sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private static void ConfigureRenderer(ParticleSystem ps)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortMode = ParticleSystemSortMode.Distance;
            r.sortingLayerName = "Default";
            r.sortingOrder = 10;

            // Use Unity's built-in default particle material (sprite alpha blended).
            // This avoids depending on authored art while still rendering correctly under URP 2D.
            var defaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            if (defaultMat != null)
                r.sharedMaterial = defaultMat;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Prefab I/O.
        // ─────────────────────────────────────────────────────────────────────

        private static void BuildAndSave(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path, out var success);
            Object.DestroyImmediate(root);
            if (!success)
                Debug.LogError($"[CoreVFXPrefabCreator] Failed to save prefab at {path}");
        }

        private static bool EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return true;

            // Recursively create parent folders as needed.
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                if (!EnsureFolder(parent))
                    return false;
            }

            var leaf = Path.GetFileName(path);
            var guid = AssetDatabase.CreateFolder(parent, leaf);
            return !string.IsNullOrEmpty(guid);
        }
    }
}
