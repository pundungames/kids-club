using System;
using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    /// <summary>
    /// Hack-around to capture IMGUI event <see cref="Event.current"/> while not inside OnGUI loop.
    /// Otherwise <see cref="Event.current"/> is always will be null.
    /// This window opens and instantly closes on the first GUI frame.
    /// </summary>
    internal class IMGUIEventsCaptureWindow : EditorWindow
    {
        private Action<Event> _onGUIUpdate;
        private bool _captured;

        private void OnGUI()
        {
            if (_captured)
            {
                return;
            }
            
            _captured = true;
            Close();
            _onGUIUpdate?.Invoke(Event.current);
        }

        public static void CaptureEvent(Action<Event> onEventCaptured)
        {
            var window = GetWindow<IMGUIEventsCaptureWindow>();
            window._captured = false;
            window._onGUIUpdate = onEventCaptured;
            window.ShowAsDropDown(Rect.zero, Vector2.zero);
        }
    }
}