using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// 世界图谱中的一条连接边。
    /// 每条边描述从一个房间的某个 Gate 到另一个房间的某个 Gate 的单向连接。
    /// 双向门应建模为两条方向相反的边。
    /// </summary>
    [System.Serializable]
    public struct ConnectionEdge
    {
        [Tooltip("起点房间 ID")]
        public string FromRoomID;

        [Tooltip("起点 Gate ID（如 'left_1', 'door_boss', 'shortcut_south'）")]
        public string FromGateID;

        [Tooltip("终点房间 ID")]
        public string ToRoomID;

        [Tooltip("终点 Gate ID")]
        public string ToGateID;

        [Tooltip("连接的语义类型")]
        public ConnectionType Type;

        [Tooltip("是否为层间连接（不同 FloorLevel）")]
        public bool IsLayerTransition;

        [Tooltip("Editor 备注")]
        public string DesignerNote;
    }
}
