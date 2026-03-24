using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// GraphView edge representing a ConnectionEdge in the WorldGraphSO.
    /// Color-coded by ConnectionType, with visual indicators for layer transitions.
    /// </summary>
    public class ConnectionGraphEdge : Edge
    {
        /// <summary> Index into WorldGraphSO._connections array. -1 if newly created. </summary>
        public int EdgeIndex { get; set; }

        /// <summary> The ConnectionType for visual styling. </summary>
        public ConnectionType ConnType { get; set; }

        /// <summary> Whether this is a layer transition edge. </summary>
        public bool IsLayerTransition { get; set; }

        /// <summary> FromGateID for serialization back to WorldGraphSO. </summary>
        public string FromGateID { get; set; }

        /// <summary> ToGateID for serialization back to WorldGraphSO. </summary>
        public string ToGateID { get; set; }

        /// <summary> Designer note. </summary>
        public string DesignerNote { get; set; }

        public ConnectionGraphEdge()
        {
            EdgeIndex = -1;
        }

        public void ApplyConnectionStyle()
        {
            var color = GetConnectionTypeColor(ConnType);

            // Edge color
            edgeControl.inputColor = color;
            edgeControl.outputColor = color;

            // Layer transition: use dashed style via increased width
            if (IsLayerTransition)
            {
                edgeControl.edgeWidth = 3;
            }
            else
            {
                edgeControl.edgeWidth = 2;
            }
        }

        /// <summary>
        /// Returns the display color for a ConnectionType.
        /// </summary>
        public static Color GetConnectionTypeColor(ConnectionType type)
        {
            switch (type)
            {
                case ConnectionType.Progression: return new Color(0.7f, 0.7f, 0.7f);   // Light gray
                case ConnectionType.Return:      return new Color(0.5f, 0.3f, 0.8f);   // Purple
                case ConnectionType.Ability:     return new Color(0.2f, 0.7f, 0.9f);   // Cyan
                case ConnectionType.Challenge:   return new Color(0.9f, 0.3f, 0.2f);   // Red
                case ConnectionType.Identity:    return new Color(0.9f, 0.7f, 0.2f);   // Gold
                case ConnectionType.Scheduled:   return new Color(0.3f, 0.9f, 0.5f);   // Green
                default:                         return new Color(0.5f, 0.5f, 0.5f);   // Gray
            }
        }
    }
}
