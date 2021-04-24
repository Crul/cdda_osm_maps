using SkiaSharp;
using System.Drawing;

namespace CddaOsmMaps.Crosscutting
{
    internal static class ColorExt
    {
        public static SKColor ToSKColor(this Color color)
            => new SKColor(color.R, color.G, color.B);

        public static Color ToColor(this SKColor color)
            => Color.FromArgb(color.Red, color.Green, color.Blue);
    }
}
