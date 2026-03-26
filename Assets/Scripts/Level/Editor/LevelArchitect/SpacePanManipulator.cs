using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectArk.Level.Editor
{
    /// <summary>
    /// A Figma-like "hold Space + LMB drag" pan manipulator for GraphView.
    /// Also supports middle-mouse-button drag for panning (no Space needed).
    /// 
    /// Usage: graphView.AddManipulator(new SpacePanManipulator());
    /// 
    /// Behavior:
    /// - Hold Space + LMB drag: pans the canvas.
    /// - Middle mouse button drag: always pans the canvas.
    /// - Release Space or mouse: stop panning.
    /// </summary>
    public class SpacePanManipulator : Manipulator
    {
        private bool _spaceHeld;
        private bool _isPanning;
        private Vector2 _lastMousePos;

        private UnityEditor.Experimental.GraphView.GraphView GraphView =>
            target as UnityEditor.Experimental.GraphView.GraphView;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            target.RegisterCallback<KeyUpEvent>(OnKeyUp, TrickleDown.TrickleDown);
            target.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            target.RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<KeyUpEvent>(OnKeyUp, TrickleDown.TrickleDown);
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            target.UnregisterCallback<FocusOutEvent>(OnFocusOut);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Space && !_spaceHeld)
            {
                _spaceHeld = true;
                evt.StopPropagation();
            }
        }

        private void OnKeyUp(KeyUpEvent evt)
        {
            if (evt.keyCode == KeyCode.Space)
            {
                _spaceHeld = false;

                if (_isPanning)
                {
                    StopPanning();
                }

                evt.StopPropagation();
            }
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            // Space + LMB, or MMB (middle mouse button)
            bool spaceLeftClick = _spaceHeld && evt.button == 0;
            bool middleClick = evt.button == 2;

            if (spaceLeftClick || middleClick)
            {
                _isPanning = true;
                _lastMousePos = evt.mousePosition;
                target.CaptureMouse();
                evt.StopImmediatePropagation();
            }
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!_isPanning) return;

            var gv = GraphView;
            if (gv == null) return;

            // Calculate delta and apply to viewTransform (the correct GraphView API)
            Vector2 delta = evt.mousePosition - _lastMousePos;
#pragma warning disable CS0618 // GraphView.viewTransform.position is the standard API; Unity hasn't migrated it yet
            Vector3 currentPos = gv.viewTransform.position;
            gv.viewTransform.position = currentPos + (Vector3)delta;
#pragma warning restore CS0618

            _lastMousePos = evt.mousePosition;
            evt.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (!_isPanning) return;

            bool releaseMatches = evt.button == 0 || evt.button == 2;
            if (releaseMatches)
            {
                StopPanning();
                evt.StopImmediatePropagation();
            }
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            _spaceHeld = false;
            if (_isPanning)
            {
                StopPanning();
            }
        }

        private void StopPanning()
        {
            _isPanning = false;
            target.ReleaseMouse();
        }
    }
}
