using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionToolkit.TemplateMatchingNCC.Models;

namespace VisionToolkit.TemplateMatchingNCC.Processing
{
    /// <summary>
    /// Sub-pixel Estimator
    ///
    /// Purpose:
    ///     - Tăng độ chính xác matching
    ///     - Estimate vị trí dưới mức pixel
    ///
    /// Responsibilities:
    ///     - Fit quadratic surface
    ///     - Estimate best X/Y/Angle
    ///
    /// Method:
    ///     - Least square fitting
    ///     - 3D quadratic optimization
    ///
    /// Notes:
    ///     - Chỉ dùng final layer
    ///     - Có thể disable để tăng speed
    /// </summary>
    internal static class SubPixelEstimator
    {
        private const double VISION_TOLERANCE = 1e-7;
        private const double D2R = Math.PI / 180.0;
        private const double R2D = 180.0 / Math.PI;
        public static bool Estimate(List<MatchParameter> vec, out double newX, out double newY,
                                      out double newAngle, double angleStep, int bestIdx)
        {
            newX = newY = newAngle = 0;
            try
            {
                var matA = new Mat(27, 10, MatType.CV_64F);
                var matS = new Mat(27, 1, MatType.CV_64F);
                double x0 = vec[bestIdx].Point.X, y0 = vec[bestIdx].Point.Y, t0 = vec[bestIdx].MatchAngle;

                int row = 0;
                for (int theta = 0; theta <= 2; theta++)
                    for (int y = -1; y <= 1; y++)
                        for (int x = -1; x <= 1; x++)
                        {
                            double dX = x0 + x, dY = y0 + y;
                            double dT = (t0 + (theta - 1) * angleStep) * D2R;
                            matA.Set<double>(row, 0, dX * dX); matA.Set<double>(row, 1, dY * dY);
                            matA.Set<double>(row, 2, dT * dT); matA.Set<double>(row, 3, dX * dY);
                            matA.Set<double>(row, 4, dX * dT); matA.Set<double>(row, 5, dY * dT);
                            matA.Set<double>(row, 6, dX); matA.Set<double>(row, 7, dY);
                            matA.Set<double>(row, 8, dT); matA.Set<double>(row, 9, 1.0);
                            int vi = bestIdx + (theta - 1);
                            matS.Set<double>(row, 0, vi >= 0 && vi < vec.Count ? vec[vi].VecResult[x + 1, y + 1] : 0.0);
                            row++;
                        }

                var matAt = new Mat(); var matAtA = new Mat(); var matAtA_inv = new Mat();
                var matZ = new Mat();
                Cv2.Transpose(matA, matAt);
                Cv2.Gemm(matAt, matA, 1, new Mat(), 0, matAtA);
                Cv2.Invert(matAtA, matAtA_inv);
                Cv2.Gemm(matAtA_inv, matAt, 1, new Mat(), 0, matZ);
                Cv2.Gemm(matZ, matS, 1, new Mat(), 0, matZ);

                double[] z = Enumerable.Range(0, 10).Select(i => matZ.Get<double>(i, 0)).ToArray();

                var K1 = new Mat(3, 3, MatType.CV_64F);
                var K2 = new Mat(3, 1, MatType.CV_64F);
                K1.Set<double>(0, 0, 2 * z[0]); K1.Set<double>(0, 1, z[3]); K1.Set<double>(0, 2, z[4]);
                K1.Set<double>(1, 0, z[3]); K1.Set<double>(1, 1, 2 * z[1]); K1.Set<double>(1, 2, z[5]);
                K1.Set<double>(2, 0, z[4]); K1.Set<double>(2, 1, z[5]); K1.Set<double>(2, 2, 2 * z[2]);
                K2.Set<double>(0, 0, -z[6]); K2.Set<double>(1, 0, -z[7]); K2.Set<double>(2, 0, -z[8]);

                var K1_inv = new Mat(); var delta = new Mat();
                Cv2.Invert(K1, K1_inv);
                Cv2.Gemm(K1_inv, K2, 1, new Mat(), 0, delta);

                newX = delta.Get<double>(0, 0);
                newY = delta.Get<double>(1, 0);
                newAngle = delta.Get<double>(2, 0) * R2D;

                foreach (var m in new[] { matA, matS, matAt, matAtA, matAtA_inv, matZ, K1, K2, K1_inv, delta }) m.Dispose();
                return true;
            }
            catch { return false; }
        }
    }
}
