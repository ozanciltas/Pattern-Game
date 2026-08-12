using System;
using UnityEngine;

namespace PatternGame.Presentation
{
    [Serializable]
    public struct ColorPair
    {
        [SerializeField]
        Color wallColor;

        [SerializeField]
        Color keyPieceColor;

        public ColorPair(Color wallColor, Color keyPieceColor)
        {
            this.wallColor = wallColor;
            this.keyPieceColor = keyPieceColor;
        }

        public Color WallColor => wallColor;

        public Color KeyPieceColor => keyPieceColor;
    }

    [CreateAssetMenu(fileName = "ColorPalette", menuName = "Pattern Game/Color Palette")]
    public sealed class ColorPalette : ScriptableObject
    {
        [SerializeField]
        ColorPair[] pairs =
        {
            new ColorPair(new Color(0.16f, 0.18f, 0.23f), new Color(0.95f, 0.63f, 0.24f)),
            new ColorPair(new Color(0.14f, 0.20f, 0.23f), new Color(0.30f, 0.82f, 0.75f)),
            new ColorPair(new Color(0.20f, 0.16f, 0.23f), new Color(0.78f, 0.49f, 1.00f)),
            new ColorPair(new Color(0.23f, 0.17f, 0.17f), new Color(1.00f, 0.42f, 0.42f))
        };

        public int PairCount => pairs == null ? 0 : pairs.Length;

        public bool TryGetPair(int index, out ColorPair pair)
        {
            int count = PairCount;

            if (count == 0)
            {
                pair = default;
                return false;
            }

            pair = pairs[((index % count) + count) % count];
            return true;
        }
    }
}
