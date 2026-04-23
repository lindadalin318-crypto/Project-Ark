using UnityEditor;
using UnityEngine;
using ProjectArk.SpaceLife.Data;

namespace ProjectArk.SpaceLife.Editor
{
    /// <summary>
    /// Shared editor utilities for the SpaceLife module.
    /// Scene bootstrap has been fully consolidated into <see cref="SpaceLifeSetupWindow"/>.
    /// This class intentionally keeps only:
    ///   - Data-asset MenuItems (surface ScriptableObject creation under ProjectArk/Space Life/Data)
    ///   - Small helpers shared with the setup window (sprite generation, input action lookup)
    /// Do NOT add scene-creation entry points here — route them through the Setup Wizard.
    /// </summary>
    public static class SpaceLifeMenuItems
    {
        // ------------------------------------------------------------
        // Data-asset MenuItems (authored-data surface area)
        // ------------------------------------------------------------

        [MenuItem("ProjectArk/Space Life/Data/Create NPC Data", priority = 60)]
        public static void CreateNPCData()
        {
            CreateScriptableObject<NPCDataSO>("NewNPCData");
        }

        [MenuItem("ProjectArk/Space Life/Data/Create Item Data", priority = 61)]
        public static void CreateItemData()
        {
            CreateScriptableObject<ItemSO>("NewItem");
        }

        // ------------------------------------------------------------
        // Helpers shared with SpaceLifeSetupWindow
        // ------------------------------------------------------------

        /// <summary>
        /// Locates the <c>ShipActions</c> InputActionAsset used by SpaceLife input handlers.
        /// Returns <c>null</c> if not found. Delegates to
        /// <see cref="ProjectArk.SpaceLife.EditorSupport.ShipInputActionLocator"/>
        /// (single source of truth for <c>ShipActions</c> lookup).
        /// </summary>
        internal static UnityEngine.Object FindInputActionAsset()
        {
            return ProjectArk.SpaceLife.EditorSupport.ShipInputActionLocator.FindAsObject(out _);
        }

        /// <summary>
        /// Creates a 64x64 solid-color sprite used as a procedural placeholder (background, NPC body).
        /// </summary>
        internal static Sprite CreateSquareSprite(Color color)
        {
            Texture2D texture = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            Rect rect = new Rect(0, 0, 64, 64);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            return Sprite.Create(texture, rect, pivot, 64);
        }

        /// <summary>
        /// Creates a procedural capsule-shaped sprite (32x64 rounded rectangle) for the Player2D placeholder.
        /// </summary>
        internal static Sprite CreateCapsuleSprite(Color color)
        {
            const int width = 32;
            const int height = 64;
            const int radius = width / 2;

            Texture2D texture = new Texture2D(width, height);
            Color transparent = new Color(0, 0, 0, 0);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float cx = x - (width * 0.5f - 0.5f);
                    float cy;
                    bool inside;

                    if (y < radius)
                    {
                        cy = y - (radius - 0.5f);
                        inside = (cx * cx + cy * cy) <= (radius * radius);
                    }
                    else if (y >= height - radius)
                    {
                        cy = y - (height - radius - 0.5f);
                        inside = (cx * cx + cy * cy) <= (radius * radius);
                    }
                    else
                    {
                        inside = true;
                    }

                    pixels[y * width + x] = inside ? color : transparent;
                }
            }

            texture.SetPixels(pixels);
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply();

            Rect rect = new Rect(0, 0, width, height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            return Sprite.Create(texture, rect, pivot, 64);
        }

        // ------------------------------------------------------------
        // Internal utilities
        // ------------------------------------------------------------

        private static void CreateScriptableObject<T>(string defaultName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            string path = EditorUtility.SaveFilePanelInProject(
                $"Save {typeof(T).Name}",
                defaultName,
                "asset",
                $"Please enter a name for the {typeof(T).Name}");

            if (string.IsNullOrEmpty(path)) return;

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = asset;
            Debug.Log($"[SpaceLife] {typeof(T).Name} created at {path}");
        }
    }
}
