using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionToolkit.TemplateMatchingNCC.Core
{
    /// <summary>
    /// Image Pyramid Builder
    ///
    /// Purpose:
    ///     - Xây dựng image pyramid
    ///     - Hỗ trợ coarse-to-fine matching
    ///
    /// Responsibilities:
    ///     - Generate multi-resolution image
    ///     - Down sample image level by level
    ///
    /// Workflow:
    ///     - Level 0 = original image
    ///     - Higher level = lower resolution
    ///
    /// Notes:
    ///     - Dùng cho source và template
    ///     - Pyramid scale = 2
    /// </summary>
    internal static class PyramidBuilder
    {
        /// <summary>
        /// Build image pyramid
        /// </summary>
        /// 
        public static void Build(Mat src, List<Mat> pyramid, int maxLevel)
        {
            pyramid.Clear();

            pyramid.Add(src.Clone());

            for (int level = 1; level <= maxLevel; level++)
            {
                var downSample = new Mat();

                Cv2.PyrDown(pyramid[level - 1], downSample);

                pyramid.Add(downSample);
            }
        }
    }
}
