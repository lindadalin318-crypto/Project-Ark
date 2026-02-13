using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Represents a door connection between two rooms on the map.
    /// </summary>
    public struct MapConnection
    {
        /// <summary> Room ID of the source room (where the door is). </summary>
        public string FromRoomID;

        /// <summary> Room ID of the destination room (where the door leads). </summary>
        public string ToRoomID;

        /// <summary> World midpoint of the connection (average of room centers). </summary>
        public Vector2 Midpoint;

        /// <summary> Whether this connection is a layer/floor transition. </summary>
        public bool IsLayerTransition;
    }
}
