using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionToolkit.TemplateMatchingNCC.Models
{
    /// <summary>
    /// Template Matching Parameters
    ///
    /// Purpose:
    ///     - Chứa toàn bộ cấu hình đầu vào cho thuật toán
    ///     - Điều khiển cách NCC template matching hoạt động
    ///
    /// Responsibilities:
    ///     - Thiết lập score threshold
    ///     - Thiết lập số lượng object cần detect
    ///     - Điều khiển overlap filtering
    ///     - Điều khiển search angle
    ///     - Bật/tắt sub-pixel refinement
    ///
    /// Contains:
    ///     - Matching score threshold
    ///     - Max number of targets
    ///     - Maximum overlap ratio
    ///     - Rotation search tolerance
    ///     - Image invert option
    ///     - Sub-pixel refinement option
    ///
    /// Notes:
    ///     - Đây là input configuration model
    ///     - Được truyền vào trước khi chạy matching
    ///     - Không chứa kết quả detect
    /// </summary>
    public class MatchParams
    {
        public int MaxPos { get; set; } = 10;
        public double Score { get; set; } = 0.9;
        public double MaxOverlap { get; set; } = 0.1;
        public double ToleranceAngle { get; set; } = 10.0;
        public bool SubPixel { get; set; } = true;
        public bool Invert { get; set; } = false;
        public int MinReduceArea { get; set; } = 256;
    }
}
