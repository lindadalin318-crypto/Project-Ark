#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Copies a curated subset of Galactic Glitch reference assets into the isolated
    /// GGReplica folder and applies deterministic Unity importer settings.
    /// Never copies external .meta files.
    /// </summary>
    public static class GGReplicaAssetImporter
    {
        private const string SourceRoot = "/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch";
        private const string TargetRoot = "Assets/_Art/Ship/GGReplica";
        private const int DefaultShipPPU = 320;

        private readonly struct CopyEntry
        {
            public readonly string SourceRelative;
            public readonly string TargetRelative;
            public readonly int PixelsPerUnit;
            public readonly Vector2 Pivot;

            public CopyEntry(string sourceRelative, string targetRelative, int pixelsPerUnit, Vector2 pivot)
            {
                SourceRelative = sourceRelative;
                TargetRelative = targetRelative;
                PixelsPerUnit = pixelsPerUnit;
                Pivot = pivot;
            }
        }

        private static readonly CopyEntry[] SpriteEntries =
        {
            new CopyEntry("DevXUnity/Sprite/Movement_d10.png", "Sprites/Movement_10.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/Movement_d3.png", "Sprites/Movement_3.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/Movement_d21.png", "Sprites/Movement_21.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/Boost_d2.png", "Sprites/Boost_2.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/Boost_d16.png", "Sprites/Boost_16.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/Boost_d8.png", "Sprites/Boost_8.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/Primary_d4.png", "Sprites/Primary_4.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/Primary.png", "Sprites/Primary.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/Primary_d6.png", "Sprites/Primary_6.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/GrabGun_back_d3.png", "Sprites/GrabGun_Back_3.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/reactor.png", "Sprites/reactor.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/Secondary_d8.png", "Sprites/Secondary_8.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/Secondary.png", "Sprites/Secondary_0.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/Secondary_d17.png", "Sprites/Secondary_17.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/Healing_d1.png", "Sprites/Healing_0.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/Healing.png", "Sprites/Healing.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/vfx_dot_001.png", "Sprites/vfx_dot_001.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/GrabGun_Base_d9.png", "Sprites/GrabGun_Base_9.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/GrabGun_Base_d8.png", "Sprites/GrabGun_Base_8.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/GrabGun_Hand_d7.png", "Sprites/GrabGun_Hand_7.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/scheme3_tp.png", "Sprites/scheme3_tp.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new CopyEntry("DevXUnity/Sprite/SHIP_PLAYER_DODGE_HALF.png", "Sprites/SHIP_PLAYER_DODGE_HALF.png", DefaultShipPPU, new Vector2(0.5f, 0.5f))
        };

        private static readonly CopyEntry[] AudioEntries =
        {
            new CopyEntry("DevXUnity/AudioClip/SND_PLAYER_BOOST.wav", "Audio/SND_PLAYER_BOOST.wav", 0, Vector2.zero),
            new CopyEntry("DevXUnity/AudioClip/SND_PLAYER_BOOST_IGNITE.wav", "Audio/SND_PLAYER_BOOST_IGNITE.wav", 0, Vector2.zero),
            new CopyEntry("DevXUnity/AudioClip/PLAYER_DODGE.wav", "Audio/PLAYER_DODGE.wav", 0, Vector2.zero),
            new CopyEntry("DevXUnity/AudioClip/PLAYER_NORMAL_SHOT.wav", "Audio/PLAYER_NORMAL_SHOT.wav", 0, Vector2.zero),
            new CopyEntry("DevXUnity/AudioClip/PlayerHealingProgress.wav", "Audio/PlayerHealingProgress.wav", 0, Vector2.zero),
            new CopyEntry("DevXUnity/AudioClip/PLAYER_FIRST_SHOT.wav", "Audio/PLAYER_FIRST_SHOT.wav", 0, Vector2.zero),
            new CopyEntry("DevXUnity/AudioClip/PLAYER_LAST_SHOT.wav", "Audio/PLAYER_LAST_SHOT.wav", 0, Vector2.zero),
            new CopyEntry("DevXUnity/AudioClip/PLAYER_DEATH.wav", "Audio/PLAYER_DEATH.wav", 0, Vector2.zero)
        };

        private static readonly CopyEntry[] ShaderTrialEntries =
        {
            new CopyEntry("DevXUnity/CLG/CLG_PlayerShipHighlight.shader", "Shaders/DevX_Trial/CLG_PlayerShipHighlight.shader", 0, Vector2.zero),
            new CopyEntry("DevXUnity/Sprite Shaders URP/Lit Sprite/Color/Sprite Shaders URP_Lit Sprite_Color_Strong Tint (Override) Lit PlayerLightSourceColored.shader", "Shaders/DevX_Trial/Lit_PlayerLightSourceColored.shader", 0, Vector2.zero),
            new CopyEntry("DevXUnity/Shader Graphs/Shader Graphs_PlayerLQTrail.shader", "Shaders/DevX_Trial/PlayerLQTrail.shader", 0, Vector2.zero)
        };

        [MenuItem("ProjectArk/Ship/GG Replica/Import Curated Assets")]
        public static void ImportCuratedAssets()
        {
            var missing = new List<string>();
            EnsureTargetFolders();

            CopyEntries(SpriteEntries, missing);
            CopyEntries(AudioEntries, missing);
            CopyEntries(ShaderTrialEntries, missing);
            AssetDatabase.Refresh();

            ApplySpriteSettings(SpriteEntries);
            ApplyAudioSettings(AudioEntries);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (missing.Count > 0)
            {
                Debug.LogError("[GGReplicaAssetImporter] Missing source files:\n" + string.Join("\n", missing));
                return;
            }

            Debug.Log("[GGReplicaAssetImporter] Curated GGReplica assets imported successfully. Imported curated sprites, audio, and shader trial files into Assets/_Art/Ship/GGReplica.");
        }

        private static void EnsureTargetFolders()
        {
            EnsureFolder("Assets", "_Art");
            EnsureFolder("Assets/_Art", "Ship");
            EnsureFolder("Assets/_Art/Ship", "GGReplica");
            EnsureFolder(TargetRoot, "Sprites");
            EnsureFolder(TargetRoot, "Audio");
            EnsureFolder(TargetRoot, "Shaders");
            EnsureFolder($"{TargetRoot}/Shaders", "DevX_Trial");
            EnsureFolder(TargetRoot, "Materials");
            EnsureFolder($"{TargetRoot}/Materials", "DevX_Trial");
        }

        private static void CopyEntries(IEnumerable<CopyEntry> entries, List<string> missing)
        {
            foreach (var entry in entries)
            {
                string source = Path.Combine(SourceRoot, entry.SourceRelative);
                string targetAssetPath = $"{TargetRoot}/{entry.TargetRelative}";
                string targetAbsolutePath = Path.Combine(ProjectRoot, targetAssetPath);
                string targetDirectory = Path.GetDirectoryName(targetAbsolutePath);

                if (!string.IsNullOrEmpty(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                if (!File.Exists(source))
                {
                    missing.Add(source);
                    continue;
                }

                File.Copy(source, targetAbsolutePath, overwrite: true);
            }
        }

        private static void ApplySpriteSettings(IEnumerable<CopyEntry> entries)
        {
            foreach (var entry in entries)
            {
                string assetPath = $"{TargetRoot}/{entry.TargetRelative}";
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"[GGReplicaAssetImporter] TextureImporter not found for {assetPath}.");
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = entry.PixelsPerUnit;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePivot = entry.Pivot;
                importer.SaveAndReimport();
            }
        }

        private static void ApplyAudioSettings(IEnumerable<CopyEntry> entries)
        {
            foreach (var entry in entries)
            {
                string assetPath = $"{TargetRoot}/{entry.TargetRelative}";
                var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"[GGReplicaAssetImporter] AudioImporter not found for {assetPath}.");
                    continue;
                }

                var sampleSettings = importer.defaultSampleSettings;
                sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
                sampleSettings.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = sampleSettings;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureFolder(string parent, string folder)
        {
            string path = $"{parent}/{folder}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }
}
#endif
