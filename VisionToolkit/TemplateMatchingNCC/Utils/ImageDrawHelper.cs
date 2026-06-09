using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionToolkit.TemplateMatchingNCC.Models;

namespace VisionToolkit.TemplateMatchingNCC.Utils
{
    /// <summary>
    /// Match Visualization Helper
    ///
    /// Purpose:
    ///     - Draw template matching result lên image
    ///     - Hỗ trợ debug và visualization
    ///
    /// Responsibilities:
    ///     - Draw rotated rectangle
    ///     - Draw center point
    ///     - Draw score + angle label
    ///
    /// Input:
    ///     - Source image
    ///     - Match result list
    ///
    /// Output:
    ///     - Annotated image
    ///
    /// Notes:
    ///     - Không ảnh hưởng matching logic
    ///     - Chỉ dùng cho display/debug
    /// </summary>
    public static class ImageDrawHelper
    {
        /// <summary>
        /// Draw all template matches on image
        /// </summary>
        /// 
        public static Mat DrawMatchesOnImage(Mat image, List<SingleTargetMatch> matches)
        {
            Mat display = image.Channels() == 1
                ? image.CvtColor(ColorConversionCodes.GRAY2BGR)
                : image.Clone();

            Scalar[] colors =
            {
                new Scalar(0, 255, 0),   new Scalar(255, 0, 0),   new Scalar(0, 0, 255),
                new Scalar(255, 255, 0), new Scalar(255, 0, 255), new Scalar(0, 255, 255)
            };

            for (int ci = 0; ci < matches.Count; ci++)
            {
                var m = matches[ci];
                var color = colors[ci % colors.Length];
                var pts = m.GetCorners().Select(c => new Point((int)c.X, (int)c.Y)).ToArray();

                Cv2.Line(display, pts[0], pts[1], color, 2);
                Cv2.Line(display, pts[1], pts[2], color, 2);
                Cv2.Line(display, pts[2], pts[3], color, 2);
                Cv2.Line(display, pts[3], pts[0], color, 2);
                Cv2.Circle(display, new Point((int)m.Center.X, (int)m.Center.Y), 5, color, -1);

                string lbl = $"#{m.Index + 1} score={m.MatchScore:F3} angle={m.MatchedAngle:F1}";
                Cv2.PutText(display, lbl,
                    new Point((int)m.LeftTop.X, (int)m.LeftTop.Y - 10),
                    HersheyFonts.HersheySimplex, 0.5, color, 1);
            }
            return display;
        }
    }
}
