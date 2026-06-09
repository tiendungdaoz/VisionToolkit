using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionToolkit.TemplateMatchingNCC.Utils
{
    /// <summary>
    /// Block Maximum Finder
    ///
    /// Purpose:
    ///     - Tìm giá trị maximum trong result map
    ///     - Hỗ trợ multi-object template matching
    ///
    /// Responsibilities:
    ///     - Cache current maximum value
    ///     - Update maximum sau khi vùng overlap bị remove
    ///     - Giảm số lần MinMaxLoc() toàn ảnh
    ///
    /// Contains:
    ///     - Result matrix
    ///     - Current max value
    ///     - Current max location
    ///
    /// Workflow:
    ///     - Initialize with result image
    ///     - Find first maximum
    ///     - Update after overlap suppression
    ///
    /// Notes:
    ///     - Internal utility class
    ///     - Chỉ dùng khi search nhiều object
    ///     - Dùng để tăng performance
    /// </summary>
    internal class BlockMax
    {
        private readonly Mat _matResult;
        private readonly Size _blockSize;

        private double _maxVal;
        private Point _maxLoc;

        public BlockMax(Mat matResult, Size blockSize)
        {
            _matResult = matResult;
            _blockSize = blockSize;

            Cv2.MinMaxLoc(matResult, out _, out _maxVal, out _, out _maxLoc);
        }

        /// <summary>
        /// Get current maximum value and location
        /// </summary>
        public void GetMaxValueLoc(out double maxVal, out Point maxLoc)
        {
            maxVal = _maxVal;
            maxLoc = _maxLoc;
        }

        /// <summary>
        /// Recompute maximum after overlap region removed
        /// </summary>
        public void UpdateMax(Rect ignoredRect)
        {
            Cv2.MinMaxLoc(_matResult, out _, out _maxVal, out _, out _maxLoc);
        }
    }
}
