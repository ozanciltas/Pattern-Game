using UnityEngine;
using UnityEngine.InputSystem;
using PatternGame.Core.Input;

namespace PatternGame.Input
{
    public sealed class PointerInputService : IPointerInput
    {
        public bool IsAvailable => Pointer.current != null;

        public bool IsPressed
        {
            get
            {
                Pointer pointer = Pointer.current;
                return pointer != null && pointer.press.isPressed;
            }
        }

        public bool PressedThisFrame
        {
            get
            {
                Pointer pointer = Pointer.current;
                return pointer != null && pointer.press.wasPressedThisFrame;
            }
        }

        public bool ReleasedThisFrame
        {
            get
            {
                Pointer pointer = Pointer.current;
                return pointer != null && pointer.press.wasReleasedThisFrame;
            }
        }

        public Vector2 ScreenPosition
        {
            get
            {
                Pointer pointer = Pointer.current;
                return pointer != null ? pointer.position.ReadValue() : Vector2.zero;
            }
        }
    }
}
