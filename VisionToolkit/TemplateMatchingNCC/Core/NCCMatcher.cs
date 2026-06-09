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
    /// NCC Template Matcher
    ///
    /// Purpose:
    ///     - Thực hiện NCC template matching
    ///     - Calculate normalized correlation score
    ///
    /// Responsibilities:
    ///     - Run template correlation
    ///     - Normalize score
    ///     - Improve matching robustness
    ///
    /// Method:
    ///     - OpenCV MatchTemplate (CCorr)
    ///     - Custom NCC denominator normalization
    ///
    /// Notes:
    ///     - Performance critical section
    ///     - Unsafe pointer optimization
    /// </summary>
    internal static class NCCMatcher
    {
        public static void Match(Mat src, TemplData td, Mat result, int layer)
        {
            Cv2.MatchTemplate(src, td.VecPyramid[layer], result, TemplateMatchModes.CCorr);
            Normalize(src, td, result, layer);
        }

        private static unsafe void Normalize(Mat src, TemplData td, Mat result, int layer)
        {
            if (td.VecResultEqual1[layer]) { result.SetTo(Scalar.All(1)); return; }

            var sum = new Mat();
            var sqsum = new Mat();
            Cv2.Integral(src, sum, sqsum, MatType.CV_64F);

            double tMean = td.VecTemplMean[layer].Val0;
            double tNorm = td.VecTemplNorm[layer];
            double invArea = td.VecInvArea[layer];
            int tR = td.VecPyramid[layer].Rows;
            int tC = td.VecPyramid[layer].Cols;

            byte* sumPtr = (byte*)sum.Data.ToPointer();
            byte* sqsumPtr = (byte*)sqsum.Data.ToPointer();
            int sumStep = (int)(sum.Step() / sizeof(double));
            int sqStep = (int)(sqsum.Step() / sizeof(double));

            for (int i = 0; i < result.Rows; i++)
            {
                float* rrow = (float*)result.Ptr(i).ToPointer();
                int idx = i * sumStep, idx2 = i * sqStep;

                for (int j = 0; j < result.Cols; j++, idx++, idx2++)
                {
                    double num = rrow[j];

                    double* q0 = ((double*)sqsumPtr) + idx2;
                    double* q1 = q0 + tC;
                    double* q2 = ((double*)sqsumPtr) + (i + tR) * sqStep + j;
                    double* q3 = q2 + tC;

                    double* p0 = ((double*)sumPtr) + idx;
                    double* p1 = p0 + tC;
                    double* p2 = ((double*)sumPtr) + (i + tR) * sumStep + j;
                    double* p3 = p2 + tC;

                    double t = *p0 - *p1 - *p2 + *p3;
                    double wndMean2 = t * t;
                    num -= t * tMean;
                    wndMean2 *= invArea;

                    t = *q0 - *q1 - *q2 + *q3;
                    double diff2 = Math.Max(t - wndMean2, 0);

                    if (diff2 <= Math.Min(0.5, 10 * float.Epsilon * t))
                        t = 0;
                    else
                        t = Math.Sqrt(diff2) * tNorm;

                    if (Math.Abs(num) < t)
                        num /= t;
                    else if (Math.Abs(num) < t * 1.125)
                        num = num > 0 ? 1 : -1;
                    else
                        num = 0;

                    rrow[j] = (float)num;
                }
            }
            sum.Dispose(); sqsum.Dispose();
        }
    }
}
