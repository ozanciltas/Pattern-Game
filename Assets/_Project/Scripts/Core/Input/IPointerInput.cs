using UnityEngine;

namespace PatternGame.Core.Input
{
    public interface IPointerInput
    {
        bool IsAvailable { get; }

        bool IsPressed { get; }

        bool PressedThisFrame { get; }

        bool ReleasedThisFrame { get; }

        Vector2 ScreenPosition { get; }
    }
}
