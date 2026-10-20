using System.Globalization;
using UnityEngine;

namespace BlueMarble.Utility
{
    public static class ColorExtensions
    {
        public static Color HexToColor(this string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                Debug.LogWarning($"[HexToColor] {raw} is empty.");
                return Color.white;
            }
            
            string hex = raw.Replace("#", "");
            
            byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);

            return new Color32(r, g, b, 255);
        }
    }
}