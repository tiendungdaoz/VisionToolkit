using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenCvSharp;
using VisionToolkit.TemplateMatchingNCC.Models;
using VisionToolkit.TemplateMatchingNCC.Utils;
using VisionToolkit.TemplateMatchingNCC.Processing;
using VisionToolkit.TemplateMatchingNCC.Filtering;

namespace VisionToolkit.TemplateMatchingNCC.Core
{
    // ─────────────────────────────────────────────────────────────────────────
    // Main algorithm class
    // ─────────────────────────────────────────────────────────────────────────
    public class TemplateMatchingEngine
    {
        private const double VISION_TOLERANCE = 1e-7;
        private const double D2R = Math.PI / 180.0;
        private const double R2D = 180.0 / Math.PI;

        public List<SingleTargetMatch> Match(Mat srcGray, Mat templGray, MatchParams p)
        {
            // Result list
            var result = new List<SingleTargetMatch>();

            // 1. Validate input images
            if (!IsValidInput(srcGray, templGray))
                return result;

            // 2. Prepare source image: clone and optional invert
            var sourceImage = srcGray.Clone();
            if (p.Invert)
                sourceImage = new Scalar(255) - sourceImage;

            // 3. Learn template pattern
            var templData = new TemplData();
            PatternLearner.Learn(templGray, p.MinReduceArea, templData);
            if (!templData.IsPatternLearned)
                return result;

            // 4. Build source pyramid
            int topLayer = PatternLearner.GetTopLayer(templGray, (int)Math.Sqrt(p.MinReduceArea));
            var sourcePyramid = new List<Mat>();
            PyramidBuilder.Build(sourceImage, sourcePyramid, topLayer);

            // 5. Generate search angles
            var searchAngles = GenerateAngles(templData, p, topLayer);

            // 6. Get center point of top pyramid layer
            var rotationCenter = GetLayerCenter(sourcePyramid, topLayer);

            // 7. Build score threshold
            var layerScores = BuildLayerScore(p, topLayer);

            // 8. Coarse matching
            var matchCandidates = SearchTopLayer(sourcePyramid,
                                                 templData,
                                                 searchAngles,
                                                 layerScores,
                                                 p,
                                                 topLayer,
                                                 rotationCenter);
            matchCandidates.Sort();

            // 9. Pyramid refinement
            var refinedMatches = ProcessPyramidLayers(matchCandidates,
                                                      templData,
                                                      sourcePyramid,
                                                      layerScores,
                                                      rotationCenter,
                                                      p,
                                                      topLayer);

            // 10. Score filtering
            ScoreFilter.Filter(refinedMatches, p.Score);

            // 11. Build rotated rectangle
            int templateWidth = templData.VecPyramid[0].Cols;
            int templateHeight = templData.VecPyramid[0].Rows;
            BuildRotatedRects(refinedMatches, templateWidth, templateHeight);

            // 12. Overlap filtering
            OverlapFilter.FilterWithRotatedRect(refinedMatches, p.MaxOverlap);
            refinedMatches.Sort();

            // 13. Convert final result
            result = ConvertToFinalResult(refinedMatches, p, templateWidth, templateHeight);

            // 14. Cleanup resources
            CleanupResources(templData, sourcePyramid, sourceImage);

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Internal helpers
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Validate Matching Input
        ///
        /// Purpose:
        ///     - Kiểm tra input image hợp lệ
        ///     - Ngăn invalid template matching
        ///
        /// Validation:
        ///     - Empty image
        ///     - Template larger than source
        ///     - Invalid image dimension
        ///
        /// Return:
        ///     - true  : valid
        ///     - false : invalid
        ///
        /// Notes:
        ///     - Early return để tránh crash
        /// </summary>
        private static bool IsValidInput(Mat source, Mat template)
        {
            if (source.Empty() || template.Empty())
            {
                return false;
            }

            bool invalidShape = template.Cols < source.Cols && template.Rows > source.Rows || template.Cols > source.Cols && template.Rows < source.Rows;

            if (invalidShape)
            {
                return false;
            }

            bool templateTooLarge = template.Cols * template.Rows > source.Cols * source.Rows;

            return !templateTooLarge;
        }

        /// <summary>
        /// Generate Search Angles
        ///
        /// Purpose:
        ///     - Sinh danh sách góc cần search
        ///     - Hỗ trợ rotation invariant matching
        ///
        /// Logic:
        ///     - Angle step dựa theo template size
        ///     - Search từ: -ToleranceAngle → +ToleranceAngle      
        ///
        /// Return:
        ///     - List of candidate angles
        ///
        /// Notes:
        ///     - Nếu tolerance ≈ 0 chỉ search angle = 0  
        /// </summary>
        private static List<double> GenerateAngles(TemplData templData, MatchParams p, int topLayer)
        {
            double angleStep = Math.Atan(2.0 / Math.Max(templData.VecPyramid[topLayer].Cols, templData.VecPyramid[topLayer].Rows)) * R2D;

            List<double> angles = new();

            if (p.ToleranceAngle < VISION_TOLERANCE)
            {
                angles.Add(0.0);
                return angles;
            }

            for (double a = 0; a < p.ToleranceAngle + angleStep; a += angleStep)
            {
                angles.Add(a);
            }

            for (double a = -angleStep; a > -p.ToleranceAngle - angleStep; a -= angleStep)
            {
                angles.Add(a);
            }

            return angles;
        }

        /// <summary>
        /// Build Pyramid Layer Score
        ///
        /// Purpose:
        ///     - Sinh score threshold cho từng pyramid layer
        ///       
        /// Logic:
        ///     - Top layer: dùng p.Score
        ///     - Lower layer: giảm dần 10%
        ///         
        /// Example: Score = 0.8
        ///          Layer0 = 0.8
        ///          Layer1 = 0.72
        ///          Layer2 = 0.648
        ///          
        /// Return:
        ///     - Layer score list
        ///
        /// Notes:
        ///     - Giúp coarse matching  tolerant hơn   
        /// </summary>
        private static List<double> BuildLayerScore(MatchParams p, int topLayer)
        {
            List<double> layerScore = new(topLayer + 1);

            for (int i = 0; i <= topLayer; i++)
            {
                layerScore.Add(p.Score);
            }

            for (int layer = 1; layer <= topLayer; layer++)
            {
                layerScore[layer] = layerScore[layer - 1] * 0.9;
            }

            return layerScore;
        }

        /// <summary>
        /// Search Match Candidates At Top Layer
        ///
        /// Purpose:
        ///     - Thực hiện coarse matching ở pyramid top layer
        ///      
        /// Workflow:
        ///     1. Rotate source image
        ///     2. NCC template matching
        ///     3. Find peak candidates
        ///     4. Collect match parameter
        ///
        /// Return: - Candidate match list  
        ///
        /// Notes:
        ///     - Đây là coarse search
        ///     - Refine ở lower layers
        /// </summary>
        private List<MatchParameter> SearchTopLayer(List<Mat> srcPyramid,
                                                    TemplData templData,
                                                    List<double> angles,
                                                    List<double> layerScore,
                                                    MatchParams p,
                                                    int topLayer,
                                                    Point2f ptCenter)
        {
            List<MatchParameter> matchCandidates = new();

            Size sizePat = templData.VecPyramid[topLayer].Size();

            bool calMaxByBlock = srcPyramid[topLayer].Width * srcPyramid[topLayer].Height / (sizePat.Width * sizePat.Height) > 500 && p.MaxPos > 10;

            foreach (double angle in angles)
            {
                Mat matR = Cv2.GetRotationMatrix2D(ptCenter, angle, 1);

                Size sizeBest = RotationHelper.GetBestRotationSize(srcPyramid[topLayer].Size(), templData.VecPyramid[topLayer].Size(), angle);

                float tx = (sizeBest.Width - 1) / 2.0f - ptCenter.X;

                float ty = (sizeBest.Height - 1) / 2.0f - ptCenter.Y;

                matR.Set(0, 2, matR.Get<double>(0, 2) + tx);

                matR.Set(1, 2, matR.Get<double>(1, 2) + ty);

                Mat matRotatedSrc = new();

                Cv2.WarpAffine(srcPyramid[topLayer], matRotatedSrc, matR, sizeBest, InterpolationFlags.Linear, BorderTypes.Constant, new Scalar(templData.BorderColor));

                Mat matResult = new();

                NCCMatcher.Match(matRotatedSrc, templData, matResult, topLayer);

                if (calMaxByBlock)
                {
                    var bm = new BlockMax(matResult, sizePat);

                    bm.GetMaxValueLoc(out double maxVal, out Point ptMaxLoc);

                    if (maxVal >= layerScore[topLayer])
                    {
                        matchCandidates.Add(new MatchParameter(new Point2f(ptMaxLoc.X - tx, ptMaxLoc.Y - ty), maxVal, angle));
                    }

                    for (int j = 0; j < p.MaxPos + 4; j++)
                    {
                        Point next = PeakFinder.GetNextMaxLoc(matResult, ptMaxLoc, sizePat, out double v, p.MaxOverlap, bm);

                        if (v < layerScore[topLayer]) break;


                        matchCandidates.Add(new MatchParameter(new Point2f(next.X - tx, next.Y - ty), v, angle));

                        ptMaxLoc = next;
                    }
                }
                else
                {
                    Cv2.MinMaxLoc(matResult, out _, out double maxVal, out _, out Point ptMaxLoc);

                    if (maxVal >= layerScore[topLayer])
                    {
                        matchCandidates.Add(new MatchParameter(new Point2f(ptMaxLoc.X - tx, ptMaxLoc.Y - ty), maxVal, angle));
                    }

                    for (int j = 0; j < p.MaxPos + 4; j++)
                    {
                        Point next = PeakFinder.GetNextMaxLoc(matResult, ptMaxLoc, sizePat, out double v, p.MaxOverlap);

                        if (v < layerScore[topLayer]) break;


                        matchCandidates.Add(new MatchParameter(new Point2f(next.X - tx, next.Y - ty), v, angle));

                        ptMaxLoc = next;
                    }
                }
                matR.Dispose();
                matRotatedSrc.Dispose();
                matResult.Dispose();
            }
            return matchCandidates;
        }

        /// <summary>
        /// Convert Internal Match Result
        ///
        /// Purpose:
        ///     - Convert internal candidate
        ///       sang public result model
        ///
        /// Workflow:
        ///     1. Calculate rotated corners
        ///     2. Calculate center point
        ///     3. Normalize angle
        ///     4. Build final result
        ///
        /// Return:
        ///     - Final matching result
        ///
        /// Notes:
        ///     - Chỉ lấy tối đa MaxPos
        ///     - Final public output
        /// </summary>
        private static List<SingleTargetMatch> ConvertToFinalResult(List<MatchParameter> matches,
                                                                    MatchParams p,
                                                                    int templateWidth,
                                                                    int templateHeight)
        {
            List<SingleTargetMatch> result = new();

            for (int i = 0; i < matches.Count && result.Count < p.MaxPos; i++)
            {
                MatchParameter m = matches[i];

                double rA = -m.MatchAngle * D2R;

                SingleTargetMatch sm = new()
                {
                    Index = i,
                    LeftTop = m.Point,
                    RightTop = new Point2d(m.Point.X + templateWidth * Math.Cos(rA), m.Point.Y - templateWidth * Math.Sin(rA)),
                    LeftBottom = new Point2d(m.Point.X + templateHeight * Math.Sin(rA), m.Point.Y + templateHeight * Math.Cos(rA)),
                    MatchedAngle = -m.MatchAngle,
                    MatchScore = m.MatchScore
                };

                sm.RightBottom = new Point2d(sm.RightTop.X + templateHeight * Math.Sin(rA), sm.RightTop.Y + templateHeight * Math.Cos(rA));

                sm.Center = new Point2d((sm.LeftTop.X + sm.RightTop.X + sm.RightBottom.X + sm.LeftBottom.X) / 4.0, (sm.LeftTop.Y + sm.RightTop.Y + sm.RightBottom.Y + sm.LeftBottom.Y) / 4.0);

                if (sm.MatchedAngle < -180)
                {
                    sm.MatchedAngle += 360;
                }

                if (sm.MatchedAngle > 180)
                {
                    sm.MatchedAngle -= 360;
                }
                result.Add(sm);
            }
            return result;
        }

        /// <summary>
        /// Cleanup Temporary Resources
        ///
        /// Purpose: - Giải phóng memory sau matching
        ///
        /// Cleanup:
        ///     - Template pyramid
        ///     - Source pyramid
        ///     - Temporary source image
        ///
        /// Notes:
        ///     - Tránh memory leak
        ///     - Quan trọng với OpenCV Mat
        /// </summary>
        private static void CleanupResources(TemplData templData, List<Mat> srcPyramid, Mat source)
        {
            templData.Clear();

            foreach (Mat mat in srcPyramid)
            {
                mat?.Dispose();
            }

            source?.Dispose();
        }

        /// <summary>
        /// Get Pyramid Layer Center
        ///
        /// Purpose: - Lấy center point của pyramid layer
        ///
        /// Return:  - Image center point
        ///    
        /// Notes:  - Dùng làm rotation center
        /// </summary>
        private static Point2f GetLayerCenter(List<Mat> pyramid, int layer)
        {
            int width = pyramid[layer].Cols;

            int height = pyramid[layer].Rows;

            return new Point2f((width - 1) / 2.0f, (height - 1) / 2.0f);
        }

        /// <summary>
        /// Build Rotated Rectangle
        ///
        /// Purpose: - Tạo rotated rectangle cho mỗi match candidate
        ///
        /// Workflow:
        ///     - Calculate 4 corners
        ///     - Create RotatedRect
        ///
        /// Notes:
        ///     - Dùng cho overlap filtering
        /// </summary>
        private static void BuildRotatedRects(List<MatchParameter> matches, int templateWidth, int templateHeight)
        {
            foreach (MatchParameter match in matches)
            {
                double rA = -match.MatchAngle * D2R;

                Point2f ptLT = new((float)match.Point.X, (float)match.Point.Y);

                Point2f ptRT = new(ptLT.X + templateWidth * (float)Math.Cos(rA), ptLT.Y - templateWidth * (float)Math.Sin(rA));

                Point2f ptLB = new(ptLT.X + templateHeight * (float)Math.Sin(rA), ptLT.Y + templateHeight * (float)Math.Cos(rA));

                Point2f ptRB = new(ptRT.X + templateHeight * (float)Math.Sin(rA), ptRT.Y + templateHeight * (float)Math.Cos(rA));

                match.RectR = new RotatedRect(ptLT, ptRT, ptRB);
            }
        }

        /// <summary>
        /// Refine Match Candidates Through Pyramid Layers
        ///
        /// Purpose:
        ///     - Refine coarse match candidates từ top pyramid xuống layer gốc
        ///
        /// Responsibilities:
        ///     - Refine match position
        ///     - Refine match angle
        ///     - Validate matching score
        ///     - Support sub-pixel estimation
        ///     - Convert coordinate giữa layers
        ///
        /// Workflow:
        ///     1. Start from top layer candidate
        ///     2. Rotate candidate position
        ///     3. Generate local search angles
        ///     4. Extract rotated ROI
        ///     5. Run NCC matching
        ///     6. Select best candidate
        ///     7. Refine position & angle
        ///     8. Repeat until layer 0
        ///
        /// Parameters:
        ///     - vecMP:  coarse candidates
        ///     - td:  learned template data
        ///     - srcPyr:  source pyramid images
        ///     - layerScore:threshold for each layer
        ///     - ptCenter:  rotation center
        ///     - p: matching parameters
        ///     - topLayer:   highest pyramid level
        ///     
        /// Return: - Refined match result
        ///     
        /// Notes:
        ///     - Đây là core refinement logic
        /// </summary>
        private List<MatchParameter> ProcessPyramidLayers(
            List<MatchParameter> vecMP, TemplData td,
            List<Mat> srcPyr, List<double> layerScore,
            Point2f ptCenter, MatchParams p, int topLayer)
        {
            var allResult = new List<MatchParameter>();
            int iDstW = td.VecPyramid[topLayer].Cols;
            int iDstH = td.VecPyramid[topLayer].Rows;

            foreach (var mp in vecMP)
            {
                double rA = -mp.MatchAngle * D2R;
                var ptLT = RotationHelper.RotatePoint(new Point2f((float)mp.Point.X, (float)mp.Point.Y), ptCenter, rA);

                double dStep = Math.Atan(2.0 / Math.Max(iDstW, iDstH)) * R2D;
                mp.AngleStart = mp.MatchAngle - dStep;
                mp.AngleEnd = mp.MatchAngle + dStep;

                if (topLayer <= 0)
                {
                    mp.Point = new Point2d(ptLT.X, ptLT.Y);
                    allResult.Add(mp);
                }
                else
                {
                    var curPt = ptLT;
                    var curAngle = mp.MatchAngle;
                    bool failed = false;

                    for (int layer = topLayer - 1; layer >= 0; layer--)
                    {
                        double astep = Math.Atan(2.0 / Math.Max(
                            td.VecPyramid[layer].Cols, td.VecPyramid[layer].Rows)) * R2D;

                        var angles = new List<double>();
                        if (p.ToleranceAngle < VISION_TOLERANCE)
                            angles.Add(0.0);
                        else
                            for (int k = -1; k <= 1; k++) angles.Add(curAngle + astep * k);

                        var ptSrcCenter = new Point2f(
                            (srcPyr[layer].Cols - 1) / 2.0f,
                            (srcPyr[layer].Rows - 1) / 2.0f);
                        var newMPs = new List<MatchParameter>();
                        int bestIdx = 0; double bestVal = -1;

                        for (int j = 0; j < angles.Count; j++)
                        {
                            var roi = new Mat(); var res = new Mat();
                            RoiExtractor.GetRotatedROI(srcPyr[layer], td.VecPyramid[layer].Size(),
                                          new Point2f(curPt.X * 2, curPt.Y * 2), angles[j], roi);
                            NCCMatcher.Match(roi, td, res, layer);

                            Cv2.MinMaxLoc(res, out _, out double maxV, out _, out Point maxL);
                            var nm = new MatchParameter(new Point2f(maxL.X, maxL.Y), maxV, angles[j]);

                            if (maxV > bestVal) { bestVal = maxV; bestIdx = j; }

                            if (maxL.X == 0 || maxL.Y == 0 ||
                                maxL.X == res.Cols - 1 || maxL.Y == res.Rows - 1)
                                nm.PosOnBorder = true;

                            if (!nm.PosOnBorder)
                                for (int dy = -1; dy <= 1; dy++)
                                    for (int dx = -1; dx <= 1; dx++)
                                    {
                                        var cp = new Point(maxL.X + dx, maxL.Y + dy);
                                        if (cp.X >= 0 && cp.Y >= 0 && cp.X < res.Cols && cp.Y < res.Rows)
                                            nm.VecResult[dx + 1, dy + 1] = res.At<float>(cp.Y, cp.X);
                                    }

                            newMPs.Add(nm);
                            roi.Dispose(); res.Dispose();
                        }

                        if (newMPs[bestIdx].MatchScore < layerScore[layer]) { failed = true; break; }

                        if (p.SubPixel && layer == 0 && !newMPs[bestIdx].PosOnBorder &&
                            bestIdx != 0 && bestIdx != angles.Count - 1)
                        {
                            SubPixelEstimator.Estimate(newMPs, out double nx, out double ny, out double na, astep, bestIdx);
                            newMPs[bestIdx].Point = new Point2d(nx, ny);
                            newMPs[bestIdx].MatchAngle = na;
                        }

                        double newAngle = newMPs[bestIdx].MatchAngle;
                        var ptPad = RotationHelper.RotatePoint(
                            new Point2f(curPt.X * 2, curPt.Y * 2), ptSrcCenter, newAngle * D2R)
                            - new Point2f(3, 3);
                        var ptNew = new Point2f(
                            (float)newMPs[bestIdx].Point.X + ptPad.X,
                            (float)newMPs[bestIdx].Point.Y + ptPad.Y);
                        ptNew = RotationHelper.RotatePoint(ptNew, ptSrcCenter, -newAngle * D2R);

                        if (layer == 0)
                        {
                            newMPs[bestIdx].Point = new Point2d(ptNew.X, ptNew.Y);
                            allResult.Add(newMPs[bestIdx]);
                        }
                        else { curAngle = newAngle; curPt = ptNew; }
                    }
                }
            }
            return allResult;
        }
    }
}