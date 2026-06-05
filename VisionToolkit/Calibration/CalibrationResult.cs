using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace VisionToolkit.Calibration
{
    /// <summary>
    /// Calibration Result
    ///
    /// Mục đích:
    ///     - Lưu toàn bộ kết quả sau khi camera calibration
    ///     - Được dùng lại cho measurement, undistort,
    ///       coordinate transform,...
    ///
    /// Chứa:
    ///     - Camera matrix (Intrinsic parameters)
    ///     - Distortion coefficients
    ///     - Pixel/mm ratio
    ///     - RMS reprojection error
    ///     - Image size
    ///
    /// Ghi chú:
    ///     - Đây là data model (DTO)
    ///     - Không chứa logic xử lý ảnh
    /// </summary>
    public class CalibrationResult
    {
        public double[,] CameraMatrix { get; set; }
        public double[] DistortionCoeffs { get; set; }

        public double PixelsPerMm { get; set; }
        public double StdDev { get; set; }

        public double MmPerPixel =>
            PixelsPerMm > 0
                ? 1.0 / PixelsPerMm
                : 0;

        public Size ImageSize { get; set; }

        public double RmsError { get; set; }

        public bool IsValid { get; set; }
    }
}
