using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionToolkit.TemplateMatchingNCC.Models;

namespace VisionToolkit.TemplateMatchingNCC.Core
{
    /// <summary>
    /// Template Pattern Learner
    ///
    /// Purpose:
    ///     - Tiền xử lý template trước matching
    ///     - Chuẩn bị dữ liệu cho NCC
    ///
    /// Responsibilities:
    ///     - Build template pyramid
    ///     - Calculate template statistics
    ///     - Cache normalization data
    ///
    /// Output:
    ///     - Filled TemplData
    ///
    /// Notes:
    ///     - Chỉ chạy 1 lần
    ///     - Dùng để tăng performance
    /// </summary>
    internal static class PatternLearner
    {
        public static void Learn(Mat templGray, int minReduceArea, TemplData td)
        {
            td.Clear();
            int topLayer = GetTopLayer(templGray, (int)Math.Sqrt(minReduceArea));
            PyramidBuilder.Build(templGray, td.VecPyramid, topLayer);

            var meanVal = Cv2.Mean(templGray);
            td.BorderColor = meanVal.Val0 < 128 ? 255 : 0;

            int size = td.VecPyramid.Count;
            td.Resize(size);

            for (int i = 0; i < size; i++)
            {
                double invArea = 1.0 / (td.VecPyramid[i].Rows * td.VecPyramid[i].Cols);
                Cv2.MeanStdDev(td.VecPyramid[i], out Scalar mean, out Scalar stdDev);

                double norm = stdDev.Val0 * stdDev.Val0 + stdDev.Val1 * stdDev.Val1 +
                              stdDev.Val2 * stdDev.Val2 + stdDev.Val3 * stdDev.Val3;
                if (norm < double.Epsilon) td.VecResultEqual1[i] = true;

                norm = Math.Sqrt(norm) / Math.Sqrt(invArea);
                td.VecInvArea[i] = invArea;
                td.VecTemplMean[i] = mean;
                td.VecTemplNorm[i] = norm;
            }
            td.IsPatternLearned = true;
        }

        public static int GetTopLayer(Mat m, int minLen)
        {
            int top = 0, area = m.Cols * m.Rows, minArea = minLen * minLen;
            while (area > minArea) { area /= 4; top++; }
            return top;
        }
    }
}
