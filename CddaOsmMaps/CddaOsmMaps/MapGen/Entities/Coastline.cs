using CddaOsmMaps.Crosscutting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CddaOsmMaps.MapGen.Entities
{
    internal class Coastline : MapElement
    {
        public List<Polygon> SideIndicators { get; private set; }

        public const float BORDER_WIDTH = 2;
        private const float SIDE_INDICATOR_WIDTH = 5;

        public Coastline(List<Polygon> polygons)
            : base(polygons)
            => SideIndicators = polygons
                .Where(p => p.IsOuterPolygon) // inner ignored
                .Select(CoastlineToRightSidePolygon)
                .ToList();

        private static Polygon CoastlineToRightSidePolygon(Polygon polygon)
        {
            var extraPoints = new List<Vector2>();
            for (var i = 0; i < polygon.Count; i++)
            {
                if (i == 0)
                    extraPoints.Add(GetNextDisplacedPoint(polygon, i));

                else if (i == polygon.Count - 1)
                    extraPoints.Add(GetPrevDisplacedPoint(polygon, i));

                else
                {
                    var currentPoint = polygon[i];
                    var currentVector = new Vector2(currentPoint.X, currentPoint.Y);

                    var nextDisplacedPoint = GetNextDisplacedPoint(polygon, i);
                    var nextDisplacedPointVector = new Vector2(nextDisplacedPoint.X, nextDisplacedPoint.Y);

                    var prevDisplacedPoint = GetPrevDisplacedPoint(polygon, i);
                    var prevDisplacedPointVector = new Vector2(prevDisplacedPoint.X, prevDisplacedPoint.Y);

                    var finalDisplacement =
                        (nextDisplacedPointVector - currentVector)
                        + (prevDisplacedPointVector - currentVector);

                    var finalDisplacementNorm = Vector2.Normalize(finalDisplacement);

                    // TODO ? scale displacement based on angle defined by the 3 dots:
                    //    180º: 1x    factor
                    //     ~0º: 2x    factor (arbitrary "big but not too big" factor)
                    //   ~360º: 2x    factor (arbitrary "big but not too big" factor)
                    //  +/-90º: 1.14x factor (sqrt(2))

                    // var scale = 1 + ((((angle - 180) / 90) ^ 2) / (1 + Math.Sqrt(2)));
                    // finalDisplacementNorm *= scale;

                    var finalDisplacedPoint = currentVector + (SIDE_INDICATOR_WIDTH * finalDisplacementNorm);

                    extraPoints.Add(new Vector2(finalDisplacedPoint.X, finalDisplacedPoint.Y));
                }
            }

            extraPoints.Reverse();

            return new Polygon(polygon.Concat(extraPoints));
        }

        private static Vector2 GetNextDisplacedPoint(
            Polygon polygon,
            int idx
        ) => GetDisplacedPoint(
            polygon,
            idx,
            idxSecondPointDelta: 1,
            rotateAngle: -90
        );

        private static Vector2 GetPrevDisplacedPoint(
            Polygon polygon,
            int idx
        ) => GetDisplacedPoint(
            polygon,
            idx,
            idxSecondPointDelta: -1,
            rotateAngle: 90
        );

        private static Vector2 GetDisplacedPoint(
            Polygon polygon,
            int idx,
            int idxSecondPointDelta,
            int rotateAngle
        )
        {
            var currentPoint = polygon[idx];
            var prevPoint = polygon[idx + idxSecondPointDelta];
            var angle = MathExt.ToRadians(
                Geom.GetAngle(currentPoint, prevPoint) + rotateAngle
            );

            return new Vector2(
                (float)(currentPoint.X + SIDE_INDICATOR_WIDTH * Math.Cos(angle)),
                (float)(currentPoint.Y + SIDE_INDICATOR_WIDTH * Math.Sin(angle))
            );
        }
    }
}
