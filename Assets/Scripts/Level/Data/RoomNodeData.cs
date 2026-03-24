using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// 世界图谱中的一个房间节点。
    /// 这是纯拓扑数据，不包含空间信息（空间信息由场景中的 Room MonoBehaviour 持有）。
    /// </summary>
    [System.Serializable]
    public struct RoomNodeData
    {
        [Tooltip("必须与场景中 Room 的 RoomSO.RoomID 一致")]
        public string RoomID;

        [Tooltip("房间在关卡节奏中的职责类型")]
        public RoomNodeType NodeType;

        [Tooltip("该房间暴露的所有命名入口（GateID 列表）")]
        public string[] GateIDs;

        [Tooltip("Editor 备注，不影响运行时")]
        public string DesignerNote;

        [Tooltip("节点在 WorldGraphEditor 中的布局位置（仅编辑器使用，不影响运行时）")]
        public Vector2 EditorPosition;
    }
}
