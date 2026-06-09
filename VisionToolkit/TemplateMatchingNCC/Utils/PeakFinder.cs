using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionToolkit.TemplateMatchingNCC.Utils
{
    /// <summary>
    /// Peak Finder
    ///
    /// Purpose:
    ///     - Tìm local maximum tiếp theo
    ///     - Hỗ trợ multi-object detection
    ///
    /// Responsibilities:
    ///     - Remove overlap region
    ///     - Find next peak score
    ///     - Avoid duplicated match
    ///
    /// Notes:
    ///     - Used in top-layer matching
    ///     - Critical for multi-position search
    /// </summary>
    internal class PeakFinder
    {
        public static Point GetNextMaxLoc(Mat result, Point cur, Size sz, out double maxVal, double overlap)
        {
            int sx = (int)(cur.X - sz.Width * (1 - overlap));
            int sy = (int)(cur.Y - sz.Height * (1 - overlap));
            var rect = new Rect(sx, sy,
                (int)(2 * sz.Width * (1 - overlap)),
                (int)(2 * sz.Height * (1 - overlap)));
            Cv2.Rectangle(result, rect, Scalar.All(-1), -1);
            Cv2.MinMaxLoc(result, out _, out maxVal, out _, out Point next);
            return next;
        }

        public static Point GetNextMaxLoc(Mat result, Point cur, Size sz, out double maxVal, double overlap, BlockMax bm)
        {
            int sx = (int)(cur.X - sz.Width * (1 - overlap));
            int sy = (int)(cur.Y - sz.Height * (1 - overlap));
            var rect = new Rect(sx, sy,
                (int)(2 * sz.Width * (1 - overlap)),
                (int)(2 * sz.Height * (1 - overlap)));
            Cv2.Rectangle(result, rect, Scalar.All(-1), -1);
            bm.UpdateMax(rect);
            bm.GetMaxValueLoc(out maxVal, out Point next);
            return next;
        }
    }
}
