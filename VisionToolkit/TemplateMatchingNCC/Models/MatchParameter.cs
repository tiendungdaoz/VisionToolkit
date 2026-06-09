using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionToolkit.TemplateMatchingNCC.Models
{
    /// <summary>
    /// Internal Match Candidate
    ///
    /// Purpose:
    ///     - Lưu candidate result trong quá trình matching
    ///     - Dùng nội bộ cho pyramid refinement
    ///
    /// Responsibilities:
    ///     - Lưu vị trí match tạm thời
    ///     - Lưu score và góc detect
    ///     - Hỗ trợ overlap filtering
    ///     - Hỗ trợ sub-pixel estimation
    ///
    /// Contains:
    ///     - Match position
    ///     - Match score
    ///     - Match angle
    ///     - Search angle range
    ///     - Rotated rectangle
    ///     - Local result matrix for sub-pixel fitting
    ///
    /// Workflow:
    ///     - Created after coarse matching
    ///     - Refined through pyramid layers
    ///     - Filtered by overlap and score
    ///     - Converted to SingleTargetMatch
    ///
    /// Notes:
    ///     - Internal algorithm object
    ///     - Không phải final output model
    ///     - Chỉ dùng bên trong matching engine
    /// </summary>
    internal class MatchParameter : IComparable<MatchParameter>
    {
        public Point2d Point { get; set; }
        public double MatchScore { get; set; }
        public double MatchAngle { get; set; }
        public double AngleStart { get; set; }
        public double AngleEnd { get; set; }
        public bool PosOnBorder { get; set; }
        public bool Delete { get; set; }
        public RotatedRect RectR { get; set; }
        public float[,] VecResult { get; set; } = new float[3, 3];

        public MatchParameter(Point2f pt, double score, double angle)
        {
            Point = new Point2d(pt.X, pt.Y);
            MatchScore = score;
            MatchAngle = angle;
        }

        // Sort descending by score
        public int CompareTo(MatchParameter other) =>
            other.MatchScore.CompareTo(MatchScore);
    }
}
