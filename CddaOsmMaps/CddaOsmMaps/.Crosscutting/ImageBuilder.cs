using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;

namespace CddaOsmMaps.Crosscutting
{
    internal class ImageBuilder : IDisposable
    {
        public (int width, int height) Size { get; private set; }

        private readonly SKSurface Surface;
        private readonly SKCanvas Canvas;
        private readonly SKPaint Paint;
        private SKBitmap Bitmap;

        public ImageBuilder((int width, int height) size, SKColor? bgrColor = null)
        {
            Size = size;
            var info = new SKImageInfo(size.width, size.height);
            Surface = SKSurface.Create(info);
            Canvas = Surface.Canvas;
            FlipCanvasVertical();
            Canvas.Clear(bgrColor ?? SKColors.White);

            Paint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = false
            };
        }

        public void Save(string filepath)
        {
            using var image = Surface.Snapshot();
            Bitmap = SKBitmap.FromImage(image);

            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(filepath);
            data.SaveTo(stream);

            DisposeBuldingProperties();
        }

        public void CacheBitmap()
            => Bitmap = SKBitmap.FromImage(Surface.Snapshot());

        private void FlipCanvasVertical()
            => Canvas.Scale(1, -1, Size.width / 2, Size.height / 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SKColor GetPixelSKColor((int x, int y) pixelPos)
            => Bitmap.GetPixel(pixelPos.x, pixelPos.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color GetPixelColor((int x, int y) pixelPos)
            => GetPixelSKColor(pixelPos).ToColor();

        public void DrawPixel(
            (float x, float y) point,
            SKColor fillColor
        )
        {
            Paint.Style = SKPaintStyle.Fill;
            Paint.Color = fillColor;
            Canvas.DrawRect(point.x, point.y, 1, 1, Paint);
        }

        public void DrawPath(
            List<(float x, float y)> points,
            Color color,
            float width
        ) => DrawPath(points, color.ToSKColor(), width);

        public void DrawPath(
            List<(float x, float y)> points,
            SKColor color,
            float width
        )
        {
            Paint.Style = SKPaintStyle.Stroke;
            Paint.Color = color;
            Paint.StrokeWidth = width;

            DrawPoints(points);
        }

        public void DrawArea(
            List<(float x, float y)> points,
            Color fillColor
        ) => DrawArea(points, fillColor.ToSKColor());

        public void DrawArea(
            List<(float x, float y)> points,
            SKColor fillColor
        )
        {
            Paint.Style = SKPaintStyle.Fill;
            Paint.Color = fillColor;
            DrawPoints(points);
        }

        private void DrawPoints(List<(float x, float y)> points)
        {
            var path = new SKPath();
            path.MoveTo(ToSKPoint(points[0]));
            for (var i = 1; i < points.Count; i++)
                path.LineTo(ToSKPoint(points[i]));

            Canvas.DrawPath(path, Paint);
        }

        private static SKPoint ToSKPoint((float x, float y) point)
            => new SKPoint(point.y, point.x);  // reversed x <-> y

        public void Dispose()
        {
            DisposeBuldingProperties();
            Bitmap?.Dispose();
        }

        public void DisposeBuldingProperties()
        {
            Surface?.Dispose();
            Canvas?.Dispose();
            Paint?.Dispose();
        }
    }
}
