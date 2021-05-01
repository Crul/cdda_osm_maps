using CddaOsmMaps.MapGen.Entities;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CddaOsmMaps.Crosscutting
{
    internal class ImageBuilder : IDisposable
    {
        public Size Size { get; private set; }

        private readonly SKSurface Surface;
        private readonly SKCanvas Canvas;
        private readonly SKPaint Paint;
        private SKBitmap Bitmap;

        public ImageBuilder(Size size, SKColor? bgrColor = null, bool log = false)
        {
            Size = size;
            var info = new SKImageInfo(size.Width, size.Height);
            if (log)
                Console.WriteLine($"Image Size: {info.Size}");

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
            => Canvas.Scale(1, -1, Size.Width / 2, Size.Height / 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SKColor GetPixelSKColor(Point pixelPos)
            => Bitmap.GetPixel(pixelPos.X, pixelPos.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color GetPixelColor(Point pixelPos)
            => GetPixelSKColor(pixelPos).ToColor();

        public void DrawPixel(Vector2 point, SKColor fillColor)
        {
            Paint.Style = SKPaintStyle.Fill;
            Paint.Color = fillColor;
            Canvas.DrawRect(point.X, point.Y, 1, 1, Paint);
        }

        public void DrawComplexPath(
            List<Polygon> polygons,
            Color color,
            float width
        ) => DrawComplexPath(polygons, color.ToSKColor(), width);

        public void DrawComplexPath(
            List<Polygon> polygons,
            SKColor color,
            float width
        )
        {
            SetStrokePaint(color, width);
            DrawPolygons(polygons);
        }

        public void DrawComplexPath(
            List<Polygon> polygons,
            Color color,
            List<float[]> polygonSegmentsWidth
        )
        {
            SetStrokePaint(color.ToSKColor());
            for (var i = 0; i < polygons.Count; i++)
                DrawPolygonSegment(polygons[i], polygonSegmentsWidth[i]);
        }

        private void DrawPolygonSegment(
            Polygon polygon,
            float[] segmentsWidth
        )
        {
            for (var i = 0; i < polygon.Count - 1; i++)
            {
                var segment = polygon.Skip(i).Take(2).ToList();
                var path = GetPath(segment);

                Paint.StrokeWidth = segmentsWidth[i];
                Canvas.DrawPath(path, Paint);
            }
        }

        public void SetStrokePaint(SKColor color, float? width = null)
        {
            Paint.Style = SKPaintStyle.Stroke;
            // TODO variable width paths (for roads) require rounded ends for the joins
            Paint.StrokeCap = SKStrokeCap.Round;
            Paint.Color = color;
            if (width.HasValue)
                Paint.StrokeWidth = width.Value;
        }

        public void DrawComplexArea(
            List<Polygon> polygons,
            Color fillColor
        ) => DrawComplexArea(polygons, fillColor.ToSKColor());

        public void DrawComplexArea(
            List<Polygon> polygons,
            SKColor fillColor
        )
        {
            Paint.Style = SKPaintStyle.Fill;
            Paint.Color = fillColor;
            DrawPolygons(polygons);
        }

        private void DrawPolygons(List<Polygon> polygons)
            => Canvas.DrawPath(GetPaths(polygons), Paint);

        private static SKPath GetPaths(List<Polygon> polygons)
        {
            var mainPath = GetPath(polygons[0]);
            if (polygons.Count == 1)
                return mainPath;

            polygons
                .Skip(1)
                .ToList()
                .ForEach(polygon =>
                {
                    mainPath = mainPath.Op(
                        GetPath(polygon),
                        polygon.IsOuterPolygon ? SKPathOp.Union : SKPathOp.Difference
                    );
                });

            return mainPath;
        }

        private static SKPath GetPath(List<Vector2> polygon)
        {
            var path = new SKPath();
            path.MoveTo(ToSKPoint(polygon[0]));
            for (var i = 1; i < polygon.Count; i++)
                path.LineTo(ToSKPoint(polygon[i]));

            return path;
        }

        private static SKPoint ToSKPoint(Vector2 point)
            => new SKPoint(point.Y, point.X);  // reversed x <-> y

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
