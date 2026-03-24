using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// [Authority: Level CanonicalSpec §9.2]
    /// GraphView node representing a single room in the WorldGraphSO.
    /// Displays RoomID, RoomNodeType, GateIDs as ports, and designer notes.
    /// Color-coded by RoomNodeType for pacing visualization.
    /// </summary>
    public class RoomGraphNode : Node
    {
        // ──────────────────── Public Data ────────────────────

        /// <summary> Index into WorldGraphSO._rooms array. </summary>
        public int RoomIndex { get; private set; }

        /// <summary> The RoomID for this node. </summary>
        public string RoomID { get; private set; }

        /// <summary> The current RoomNodeType. </summary>
        public RoomNodeType NodeType { get; set; }

        /// <summary> Designer note text. </summary>
        public string DesignerNote { get; set; }

        /// <summary> Mapping of GateID → Port for connection wiring. </summary>
        public Dictionary<string, Port> GatePorts { get; } = new Dictionary<string, Port>();

        // ──────────────────── Private UI ────────────────────

        private Label _nodeTypeLabel;
        private Label _noteLabel;
        private VisualElement _headerStripe;

        // ──────────────────── Construction ────────────────────

        public RoomGraphNode(RoomNodeData data, int roomIndex)
        {
            RoomIndex = roomIndex;
            RoomID = data.RoomID;
            NodeType = data.NodeType;
            DesignerNote = data.DesignerNote;

            title = data.RoomID;

            // Set position from editor data
            SetPosition(new Rect(data.EditorPosition.x, data.EditorPosition.y, 200, 150));

            BuildUI(data);
            RefreshExpandedState();
            RefreshPorts();
        }

        // ──────────────────── UI Building ────────────────────

        private void BuildUI(RoomNodeData data)
        {
            // ── Header stripe (color-coded by NodeType) ──
            _headerStripe = new VisualElement();
            _headerStripe.style.height = 4;
            _headerStripe.style.backgroundColor = GetNodeTypeColor(data.NodeType);
            titleContainer.Insert(0, _headerStripe);

            // ── Node type badge ──
            _nodeTypeLabel = new Label(data.NodeType.ToString());
            _nodeTypeLabel.style.fontSize = 10;
            _nodeTypeLabel.style.color = GetNodeTypeColor(data.NodeType);
            _nodeTypeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _nodeTypeLabel.style.marginLeft = 4;
            _nodeTypeLabel.style.marginBottom = 2;
            titleContainer.Add(_nodeTypeLabel);

            // ── Content area ──
            var contentBox = new VisualElement();
            contentBox.style.paddingLeft = 8;
            contentBox.style.paddingRight = 8;
            contentBox.style.paddingTop = 4;
            contentBox.style.paddingBottom = 4;

            // Designer note
            if (!string.IsNullOrEmpty(data.DesignerNote))
            {
                _noteLabel = new Label(data.DesignerNote);
                _noteLabel.style.fontSize = 10;
                _noteLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                _noteLabel.style.whiteSpace = WhiteSpace.Normal;
                _noteLabel.style.maxWidth = 180;
                contentBox.Add(_noteLabel);
            }

            // Gate count info
            int gateCount = data.GateIDs?.Length ?? 0;
            var gateInfo = new Label($"Gates: {gateCount}");
            gateInfo.style.fontSize = 9;
            gateInfo.style.color = new Color(0.5f, 0.5f, 0.5f);
            gateInfo.style.marginTop = 2;
            contentBox.Add(gateInfo);

            extensionContainer.Add(contentBox);

            // ── Gate Ports ──
            BuildGatePorts(data);

            // ── Style tweaks ──
            style.minWidth = 160;
            style.maxWidth = 240;

            // Node background tint
            var border = this.Q("node-border");
            if (border != null)
            {
                Color tint = GetNodeTypeColor(data.NodeType);
                tint.a = 0.08f;
                border.style.backgroundColor = tint;
            }
        }

        private void BuildGatePorts(RoomNodeData data)
        {
            if (data.GateIDs == null || data.GateIDs.Length == 0)
            {
                // 没有 Gate 时，至少创建一对通用端口
                var inPort = CreatePort(Direction.Input, Port.Capacity.Multi);
                inPort.portName = "In";
                inPort.portColor = new Color(0.6f, 0.8f, 1f);
                inputContainer.Add(inPort);
                GatePorts["_in"] = inPort;

                var outPort = CreatePort(Direction.Output, Port.Capacity.Multi);
                outPort.portName = "Out";
                outPort.portColor = new Color(1f, 0.8f, 0.6f);
                outputContainer.Add(outPort);
                GatePorts["_out"] = outPort;
                return;
            }

            // 为每个 GateID 创建双向端口
            // 分析 GateID 的方向前缀来决定放左还是右
            foreach (var gateID in data.GateIDs)
            {
                if (string.IsNullOrEmpty(gateID)) continue;

                bool isLeftSide = IsLeftSideGate(gateID);

                if (isLeftSide)
                {
                    var port = CreatePort(Direction.Input, Port.Capacity.Multi);
                    port.portName = ShortenGateID(gateID);
                    port.portColor = new Color(0.6f, 0.8f, 1f);
                    inputContainer.Add(port);
                    GatePorts[gateID] = port;
                }
                else
                {
                    var port = CreatePort(Direction.Output, Port.Capacity.Multi);
                    port.portName = ShortenGateID(gateID);
                    port.portColor = new Color(1f, 0.8f, 0.6f);
                    outputContainer.Add(port);
                    GatePorts[gateID] = port;
                }
            }

            // 如果所有端口都在一侧，确保另一侧也有一个通用端口
            if (inputContainer.childCount == 0)
            {
                var inPort = CreatePort(Direction.Input, Port.Capacity.Multi);
                inPort.portName = "In";
                inPort.portColor = new Color(0.6f, 0.8f, 1f);
                inputContainer.Add(inPort);
                GatePorts["_in"] = inPort;
            }

            if (outputContainer.childCount == 0)
            {
                var outPort = CreatePort(Direction.Output, Port.Capacity.Multi);
                outPort.portName = "Out";
                outPort.portColor = new Color(1f, 0.8f, 0.6f);
                outputContainer.Add(outPort);
                GatePorts["_out"] = outPort;
            }
        }

        private Port CreatePort(Direction direction, Port.Capacity capacity)
        {
            return InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(bool));
        }

        // ──────────────────── Lookup Helpers ────────────────────

        /// <summary>
        /// Finds the best port for a given GateID. Falls back to generic In/Out port.
        /// </summary>
        public Port GetPortForGate(string gateID, bool isOutput)
        {
            if (!string.IsNullOrEmpty(gateID) && GatePorts.TryGetValue(gateID, out var port))
                return port;

            // Fallback to generic port
            string fallbackKey = isOutput ? "_out" : "_in";
            if (GatePorts.TryGetValue(fallbackKey, out var fallback))
                return fallback;

            // Last resort: first port on the correct side
            if (isOutput && outputContainer.childCount > 0)
                return outputContainer[0] as Port;
            if (!isOutput && inputContainer.childCount > 0)
                return inputContainer[0] as Port;

            return null;
        }

        // ──────────────────── Visual Updates ────────────────────

        /// <summary>
        /// Updates the visual appearance after NodeType changes.
        /// </summary>
        public void RefreshNodeTypeVisuals()
        {
            Color color = GetNodeTypeColor(NodeType);

            if (_headerStripe != null)
                _headerStripe.style.backgroundColor = color;

            if (_nodeTypeLabel != null)
            {
                _nodeTypeLabel.text = NodeType.ToString();
                _nodeTypeLabel.style.color = color;
            }

            var border = this.Q("node-border");
            if (border != null)
            {
                Color tint = color;
                tint.a = 0.08f;
                border.style.backgroundColor = tint;
            }
        }

        // ──────────────────── Color Mapping ────────────────────

        /// <summary>
        /// Returns the display color for a RoomNodeType.
        /// Aligned with PacingOverlayRenderer's color philosophy.
        /// </summary>
        public static Color GetNodeTypeColor(RoomNodeType nodeType)
        {
            switch (nodeType)
            {
                case RoomNodeType.Transit:    return new Color(0.5f, 0.5f, 0.5f);    // Gray
                case RoomNodeType.Pressure:   return new Color(0.9f, 0.7f, 0.2f);    // Amber
                case RoomNodeType.Resolution: return new Color(0.9f, 0.3f, 0.2f);    // Red
                case RoomNodeType.Reward:     return new Color(0.2f, 0.8f, 0.4f);    // Green
                case RoomNodeType.Anchor:     return new Color(0.2f, 0.6f, 0.9f);    // Blue
                case RoomNodeType.Loop:       return new Color(0.7f, 0.4f, 0.9f);    // Purple
                case RoomNodeType.Hub:        return new Color(0.9f, 0.9f, 0.3f);    // Yellow
                case RoomNodeType.Threshold:  return new Color(0.9f, 0.5f, 0.1f);    // Orange
                case RoomNodeType.Safe:       return new Color(0.3f, 0.9f, 0.6f);    // Teal
                case RoomNodeType.Boss:       return new Color(0.9f, 0.1f, 0.1f);    // Bright Red
                default:                      return new Color(0.5f, 0.5f, 0.5f);    // Gray
            }
        }

        // ──────────────────── Utility ────────────────────

        private static bool IsLeftSideGate(string gateID)
        {
            string lower = gateID.ToLowerInvariant();
            // west/south/return 类型的 gate 放左侧（Input）
            return lower.Contains("west") || lower.Contains("south") || lower.Contains("return") || lower.Contains("_in");
        }

        private static string ShortenGateID(string gateID)
        {
            // "gate_east_SH-SLICE-R02" → "→ R02"
            if (gateID.StartsWith("gate_"))
            {
                string[] parts = gateID.Split('_');
                if (parts.Length >= 3)
                {
                    string dir = parts[1];
                    string target = string.Join("_", parts, 2, parts.Length - 2);

                    // 进一步缩短目标ID
                    if (target.Contains("-"))
                    {
                        string[] idParts = target.Split('-');
                        target = idParts[idParts.Length - 1]; // 取最后一段，如 "R02"
                    }

                    string arrow = dir switch
                    {
                        "east" => "→",
                        "west" => "←",
                        "north" => "↑",
                        "south" => "↓",
                        _ => "⇔"
                    };

                    return $"{arrow} {target}";
                }
            }

            return gateID;
        }
    }
}
