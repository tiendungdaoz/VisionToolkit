using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionToolkit.TemplateMatchingNCC.Processing
{
    /// <summary>
    /// Rotation Geometry Helper
    ///
    /// Purpose:
    ///     - Cung cấp geometry utility cho image rotation
    ///     - Hỗ trợ template rotation matching
    ///
    /// Responsibilities:
    ///     - Rotate point quanh center
    ///     - Estimate best rotated image size
    ///
    /// Used By:
    ///     - Pyramid matching
    ///     - ROI extraction
    ///     - Rotated template search
    ///
    /// Notes:
    ///     - Chỉ chứa geometry calculation
    ///     - Không xử lý image matching
    /// </summary>
    public static class RotationHelper
    {
        private const double VISION_TOLERANCE = 1e-7;
        private const double D2R = Math.PI / 180.0;
        private const double R2D = 180.0 / Math.PI;

        public static Size GetBestRotationSize(Size sizeSrc, Size sizeDst, double angle)
        {
            double rad = angle * D2R;
            var c = new Point2f((sizeSrc.Width - 1) / 2.0f, (sizeSrc.Height - 1) / 2.0f);

            var corners = new[]
            {
                RotatePoint(new Point2f(0, 0), c, rad),
                RotatePoint(new Point2f(0, sizeSrc.Height - 1), c, rad),
                RotatePoint(new Point2f(sizeSrc.Width - 1, sizeSrc.Height - 1), c, rad),
                RotatePoint(new Point2f(sizeSrc.Width - 1, 0), c, rad)
            };

            float topY = corners.Max(p => p.Y), btmY = corners.Min(p => p.Y);
            float rgtX = corners.Max(p => p.X), lftX = corners.Min(p => p.X);

            double a = angle < 0 ? angle + 360 : angle;
            if (Math.Abs(Math.Abs(a) - 90) < VISION_TOLERANCE || Math.Abs(Math.Abs(a) - 270) < VISION_TOLERANCE)
                return new Size(sizeSrc.Height, sizeSrc.Width);
            if (Math.Abs(a) < VISION_TOLERANCE || Math.Abs(Math.Abs(a) - 180) < VISION_TOLERANCE)
                return sizeSrc;

            double da = a;
            if (da > 90 && da < 180) da -= 90;
            else if (da > 180 && da < 270) da -= 180;
            else if (da > 270 && da < 360) da -= 270;

            float h1 = sizeDst.Width * (float)Math.Sin(da * D2R) * (float)Math.Cos(da * D2R);
            float h2 = sizeDst.Height * (float)Math.Sin(da * D2R) * (float)Math.Cos(da * D2R);
            int halfH = (int)Math.Ceiling(topY - c.Y - h1);
            int halfW = (int)Math.Ceiling(rgtX - c.X - h2);

            var sizeRet = new Size(halfW * 2, halfH * 2);
            bool wrong = (sizeDst.Width < sizeRet.Width && sizeDst.Height > sizeRet.Height) ||
                         (sizeDst.Width > sizeRet.Width && sizeDst.Height < sizeRet.Height) ||
                         (sizeDst.Width * sizeDst.Height > sizeRet.Width * sizeRet.Height);

            return wrong ? new Size((int)(rgtX - lftX + 0.5), (int)(topY - btmY + 0.5)) : sizeRet;
        }

        public static Point2f RotatePoint(Point2f pt, Point2f org, double angle)
        {
            double h = org.Y * 2;
            double y1 = h - pt.Y, y2 = h - org.Y;
            double dx = pt.X - org.X, dy = y1 - org.Y;
            double rx = dx * Math.Cos(angle) - dy * Math.Sin(angle) + org.X;
            double ry = dx * Math.Sin(angle) + dy * Math.Cos(angle) + y2;
            return new Point2f((float)rx, (float)(-ry + h));
        }


    }
}
