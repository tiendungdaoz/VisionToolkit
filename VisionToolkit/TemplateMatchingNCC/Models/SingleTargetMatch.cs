using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionToolkit.TemplateMatchingNCC.Models
{
    public class SingleTargetMatch
    {
        /// <summary>
        /// Single Target Match Result
        ///
        /// Purpose:
        ///     - Lưu kết quả của một template match
        ///     - Đại diện cho 1 object detect được
        ///
        /// Responsibilities:
        ///     - Chứa thông tin hình học của object
        ///     - Chứa score matching và góc xoay
        ///
        /// Contains:
        ///     - 4 corner points
        ///     - Center point
        ///     - Match angle
        ///     - Match score
        ///
        /// Notes:
        ///     - Đây là data model (DTO)
        ///     - Không chứa image processing logic
        /// </summary>
        public int Index { get; set; }
        public Point2d LeftTop { get; set; }
        public Point2d RightTop { get; set; }
        public Point2d LeftBottom { get; set; }
        public Point2d RightBottom { get; set; }
        public Point2d Center { get; set; }
        public double MatchedAngle { get; set; }
        public double MatchScore { get; set; }

        public List<Point2d> GetCorners() =>
            new List<Point2d> { LeftTop, RightTop, RightBottom, LeftBottom };
    }
}
