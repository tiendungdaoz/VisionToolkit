using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionToolkit.TemplateMatchingNCC.Models;

namespace VisionToolkit.TemplateMatchingNCC.Filtering
{
    /// <summary>
    /// Match Overlap Filter
    ///
    /// Purpose:
    ///     - Loại bỏ duplicated match
    ///     - Giảm multiple detection cùng object
    ///
    /// Responsibilities:
    ///     - Check rotated rectangle intersection
    ///     - Remove excessive overlap
    ///     - Keep highest score result
    ///
    /// Workflow:
    ///     - Compare all candidates
    ///     - Calculate overlap ratio
    ///     - Remove lower score match
    ///
    /// Notes:
    ///     - Dùng sau score filtering
    ///     - Rotation-aware overlap check
    /// </summary>
    internal static class OverlapFilter
    {
        private const double R2D = 180.0 / Math.PI;

        public static void FilterWithRotatedRect(List<MatchParameter> v, double maxOverlap)
        {
            for (int i = 0; i < v.Count - 1; i++)
            {
                if (v[i].Delete) continue;
                for (int j = i + 1; j < v.Count; j++)
                {
                    if (v[j].Delete) continue;
                    var it = Cv2.RotatedRectangleIntersection(v[i].RectR, v[j].RectR, out Point2f[] pts);
                    if (it == RectanglesIntersectTypes.None) continue;
                    if (it == RectanglesIntersectTypes.Full)
                    { v[v[i].MatchScore >= v[j].MatchScore ? j : i].Delete = true; }
                    else
                    {
                        if (pts.Length < 3) continue;
                        var list = pts.ToList(); SortPointsWithCenter(list);
                        double area = Cv2.ContourArea(list.ToArray());
                        double ratio = area / (v[i].RectR.Size.Width * v[i].RectR.Size.Height);
                        if (ratio > maxOverlap)
                            v[v[i].MatchScore >= v[j].MatchScore ? j : i].Delete = true;
                    }
                }
            }
            v.RemoveAll(m => m.Delete);
        }

        public static void SortPointsWithCenter(List<Point2f> pts)
        {
            float cx = pts.Average(p => p.X), cy = pts.Average(p => p.Y);
            var center = new Point2f(cx, cy);
            var withAngle = pts.Select(p =>
            {
                var v = new Point2f(p.X - cx, p.Y - cy);
                float norm = v.X * v.X + v.Y * v.Y;
                double angle = v.Y < 0 ? Math.Acos(v.X / norm) * R2D
                             : v.Y > 0 ? 360 - Math.Acos(v.X / norm) * R2D
                             : v.X > 0 ? 0 : 180;
                return (p, angle);
            }).OrderBy(x => x.angle).ToList();
            for (int i = 0; i < pts.Count; i++) pts[i] = withAngle[i].p;
        }
    }
}
