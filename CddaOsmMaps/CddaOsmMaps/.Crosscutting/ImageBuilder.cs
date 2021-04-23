using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace CddaOsmMaps.Crosscutting
{
    internal class ImageBuilder : IDisposable
    {
        public (int width, int height) Size { get; private set; }

        private readonly SKSurface Surface;
        private readonly SKCanvas Canvas;
        private readonly SKPaint Paint;
        private SKBitmap Bitmap;

        public ImageBuilder((int width, int height) size)
        {
            Size = size;
            var info = new SKImageInfo(size.width, size.height);

            Surface = SKSurface.Create(info);
            Canvas = Surface.Canvas;
            Canvas.Clear(SKColors.White);
            Canvas.Scale(1, -1, size.width / 2, size.height / 2); // flip vert

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

        public (byte r, byte g, byte b) GetPixelColor((int x, int y) pixelPos)
        {
            var pixelColor = Bitmap.GetPixel(pixelPos.x, pixelPos.y);

            return (pixelColor.Red, pixelColor.Green, pixelColor.Blue);
        }

        public void DrawPath(
            List<(float x, float y)> points,
            (byte r, byte g, byte b) color,
            float width
        )
        {
            Paint.Style = SKPaintStyle.Stroke;
            Paint.Color = new SKColor(color.r, color.g, color.b);
            Paint.StrokeWidth = width;

            DrawPoints(points);
        }

        public void DrawArea(
            List<(float x, float y)> points,
            (byte r, byte g, byte b) fillColor,
            (byte r, byte g, byte b) strokeColor,
            float strokeWidth
        )
        {
            Paint.Style = SKPaintStyle.Fill;
            Paint.Color = new SKColor(fillColor.r, fillColor.g, fillColor.b);
            DrawPoints(points);

            Paint.Style = SKPaintStyle.Stroke;
            Paint.Color = new SKColor(strokeColor.r, strokeColor.g, strokeColor.b);
            Paint.StrokeWidth = strokeWidth;
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
