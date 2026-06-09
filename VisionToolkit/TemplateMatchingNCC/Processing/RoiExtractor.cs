using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionToolkit.TemplateMatchingNCC.Processing
{
    /// <summary>
    /// Rotated ROI Extractor
    ///
    /// Purpose:
    ///     - Crop rotated ROI từ source image
    ///     - Chuẩn bị image cho local template matching
    ///
    /// Responsibilities:
    ///     - Rotate image
    ///     - Translate ROI region
    ///     - Extract search region
    ///
    /// Used By:
    ///     - Pyramid refinement
    ///     - Angle search
    ///
    /// Notes:
    ///     - Có padding 3 pixel
    ///     - Border filled with constant value
    /// </summary>
    internal static class RoiExtractor
    {
        private const double VISION_TOLERANCE = 1e-7;
        private const double D2R = Math.PI / 180.0;
        private const double R2D = 180.0 / Math.PI;

        public static void GetRotatedROI(Mat src, Size size, Point2f ptLT, double angle, Mat roi)
        {
            var c = new Point2f((src.Cols - 1) / 2.0f, (src.Rows - 1) / 2.0f);
            var ptR = RotationHelper.RotatePoint(ptLT, c, angle * D2R);
            var sz = new Size(size.Width + 6, size.Height + 6);
            var rM = Cv2.GetRotationMatrix2D(c, angle, 1);
            rM.Set<double>(0, 2, rM.Get<double>(0, 2) - ptR.X + 3);
            rM.Set<double>(1, 2, rM.Get<double>(1, 2) - ptR.Y + 3);
            Cv2.WarpAffine(src, roi, rM, sz, InterpolationFlags.Linear,
                           BorderTypes.Constant, new Scalar(128));
            rM.Dispose();
        }
    }
}
