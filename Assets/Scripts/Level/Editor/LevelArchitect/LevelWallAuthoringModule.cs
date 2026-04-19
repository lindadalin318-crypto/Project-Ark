using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// Single authoring module for all wall-related room tools.
    /// Groups static wall canvases and semantic breakable wall starters in one editor entry,
    /// while preserving their runtime ownership and hierarchy roots.
    /// </summary>
    public static class LevelWallAuthoringModule
    {
        public enum WallToolKind
        {
            OuterWallCanvas,
            InnerWallCanvas,
            BreakableWallStarter
        }

        public readonly struct WallToolDefinition
        {
            public WallToolDefinition(WallToolKind kind, string displayName)
            {
                Kind = kind;
                DisplayName = displayName;
            }

            public WallToolKind Kind { get; }
            public string DisplayName { get; }
        }

        private static readonly WallToolDefinition[] TOOLS =
        {
            new(WallToolKind.OuterWallCanvas, RoomGeometryCanvasFactory.GetDisplayName(RoomGeometryCanvasFactory.WallCanvasKind.OuterWalls)),
            new(WallToolKind.InnerWallCanvas, RoomGeometryCanvasFactory.GetDisplayName(RoomGeometryCanvasFactory.WallCanvasKind.InnerWalls)),
            new(WallToolKind.BreakableWallStarter, LevelRuntimeAssistFactory.GetDisplayName(LevelRuntimeAssistFactory.RoomAssistType.BreakableWall))
        };

        public static IReadOnlyList<WallToolDefinition> GetTools()
        {
            return TOOLS;
        }

        public static string GetDisplayName(WallToolKind kind)
        {
            foreach (var tool in TOOLS)
            {
                if (tool.Kind == kind)
                {
                    return tool.DisplayName;
                }
            }

            return kind.ToString();
        }

        public static GameObject Create(Room room, WallToolKind kind)
        {
            if (room == null)
            {
                Debug.LogWarning("[LevelWallAuthoringModule] Cannot create wall tool: room is null.");
                return null;
            }

            return kind switch
            {
                WallToolKind.OuterWallCanvas => RoomGeometryCanvasFactory.CreateCanvas(room, RoomGeometryCanvasFactory.WallCanvasKind.OuterWalls),
                WallToolKind.InnerWallCanvas => RoomGeometryCanvasFactory.CreateCanvas(room, RoomGeometryCanvasFactory.WallCanvasKind.InnerWalls),
                WallToolKind.BreakableWallStarter => LevelRuntimeAssistFactory.CreateRoomAssist(room, LevelRuntimeAssistFactory.RoomAssistType.BreakableWall),
                _ => null
            };
        }
    }
}
