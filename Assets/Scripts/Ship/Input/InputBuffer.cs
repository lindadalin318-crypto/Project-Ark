using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Generic input buffer utility. Records action timestamps and allows
    /// consumers to check if an action was pressed within a time window.
    /// Attach to the same GameObject as InputHandler, or use as a standalone helper.
    /// </summary>
    public class InputBuffer
    {
        private readonly Dictionary<string, float> _bufferTimestamps = new();

        /// <summary>
        /// Records the current time for the given action name.
        /// Call this when an input event fires.
        /// </summary>
        public void Record(string actionName)
        {
            _bufferTimestamps[actionName] = Time.unscaledTime;
        }

        /// <summary>
        /// Attempts to consume a buffered input. Returns true if the action
        /// was recorded within the specified window (seconds) from now.
        /// Consumes (clears) the buffer entry on success.
        /// </summary>
        public bool Consume(string actionName, float windowSeconds)
        {
            if (!_bufferTimestamps.TryGetValue(actionName, out float recordedTime))
                return false;

            if (Time.unscaledTime - recordedTime <= windowSeconds)
            {
                _bufferTimestamps.Remove(actionName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a buffered input exists within the window WITHOUT consuming it.
        /// </summary>
        public bool Peek(string actionName, float windowSeconds)
        {
            if (!_bufferTimestamps.TryGetValue(actionName, out float recordedTime))
                return false;

            return Time.unscaledTime - recordedTime <= windowSeconds;
        }

        /// <summary>
        /// Clears all buffered inputs.
        /// </summary>
        public void Clear()
        {
            _bufferTimestamps.Clear();
        }

        /// <summary>
        /// Clears a specific action's buffer.
        /// </summary>
        public void Clear(string actionName)
        {
            _bufferTimestamps.Remove(actionName);
        }
    }
}
