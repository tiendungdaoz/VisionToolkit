using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VisionToolkit.TemplateMatchingNCC.Core;
using VisionToolkit.TemplateMatchingNCC.Models;
using VisionToolkit.TemplateMatchingNCC.Utils;

namespace VisionToolkit.Demo.Test
{
    internal class TemplateMatchingDemo
    {
        public static void Run()
        {
            // ──────────────────────────────────────────────
            // 1. Image path
            // ──────────────────────────────────────────────
            string imageFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "TemplateMatching");
            string sourcePath = Path.Combine(imageFolder, "Src1.bmp");
            string templatePath = Path.Combine(imageFolder, "Temp1.bmp");
            string outputPath = Path.Combine(imageFolder, "Result.bmp");

            // ──────────────────────────────────────────────
            // 2. Matching parameters
            // ──────────────────────────────────────────────
            MatchParams param = new()
            {
                Score = 0.9,
                MaxPos = 10,
                MaxOverlap = 0.1,
                ToleranceAngle = 180,
                Invert = false,
                SubPixel = true
            };

            // ──────────────────────────────────────────────
            // 3. Load images
            // ──────────────────────────────────────────────
            Console.WriteLine("[INFO] Loading images...");
            using Mat srcRaw = Cv2.ImRead(sourcePath);
            using Mat templateRaw = Cv2.ImRead(templatePath);

            if (srcRaw.Empty())
            {
                Console.WriteLine($"[ERROR] Cannot load source image:\n{sourcePath}");
                return;
            }

            if (templateRaw.Empty())
            {
                Console.WriteLine($"[ERROR] Cannot load template image:\n{templatePath}");
                return;
            }

            // ──────────────────────────────────────────────
            // 4. Convert to grayscale
            // ──────────────────────────────────────────────
            using Mat srcGray = srcRaw.Channels() > 1 ? srcRaw.CvtColor(ColorConversionCodes.BGR2GRAY) : srcRaw.Clone();
            using Mat templateGray = templateRaw.Channels() > 1 ? templateRaw.CvtColor(ColorConversionCodes.BGR2GRAY) : templateRaw.Clone();

            // ──────────────────────────────────────────────
            // 5. Print info
            // ──────────────────────────────────────────────
            Console.WriteLine();
            Console.WriteLine("========== TEMPLATE MATCHING ==========");
            Console.WriteLine($"Source   : {srcRaw.Width} x {srcRaw.Height}");
            Console.WriteLine($"Template : {templateRaw.Width} x {templateRaw.Height}");
            Console.WriteLine();
            Console.WriteLine("[PARAMETERS]");
            Console.WriteLine($"Score       : {param.Score}");
            Console.WriteLine($"MaxPos      : {param.MaxPos}");
            Console.WriteLine($"Overlap     : {param.MaxOverlap}");
            Console.WriteLine($"Angle Range : ±{param.ToleranceAngle}°");
            Console.WriteLine($"SubPixel    : {param.SubPixel}");
            Console.WriteLine($"Invert      : {param.Invert}");
            Console.WriteLine();

            // ──────────────────────────────────────────────
            // 6. Run matching
            // ──────────────────────────────────────────────
            Console.WriteLine("[INFO] Running template matching...");

            TemplateMatchingEngine engine = new TemplateMatchingEngine();
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<SingleTargetMatch> matches;
            try
            {
                matches = engine.Match(srcGray, templateGray, param);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("[ERROR] Matching failed");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return;
            }
            stopwatch.Stop();

            // ──────────────────────────────────────────────
            // 7. Print result
            // ──────────────────────────────────────────────
            Console.WriteLine();
            Console.WriteLine("========== RESULT ==========");
            Console.WriteLine($"Elapsed Time : {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Match Count  : {matches.Count}");
            Console.WriteLine();

            if (matches.Count == 0)
            {
                Console.WriteLine("No match found.");
            }
            else
            {
                Console.WriteLine($"{"#",-4} {"Score",8} {"Angle",10} {"CenterX",10} {"CenterY",10}");
                Console.WriteLine(new string('-', 50));
                foreach (var match in matches)
                {
                    Console.WriteLine(
                        $"{match.Index + 1,-4}" +
                        $" {match.MatchScore,8:F4}" +
                        $" {match.MatchedAngle,10:F2}" +
                        $" {match.Center.X,10:F2}" +
                        $" {match.Center.Y,10:F2}");
                }
            }

            // ──────────────────────────────────────────────
            // 8. Draw result image
            // ──────────────────────────────────────────────
            try
            {
                using Mat display = ImageDrawHelper.DrawMatchesOnImage(srcRaw, matches);
                using Mat resized = new Mat();

                Cv2.Resize(display, resized, new Size(), 0.5, 0.5);

                Cv2.ImShow("Template Matching Result", resized);

                Cv2.ImWrite(outputPath, display);

                Console.WriteLine();
                Console.WriteLine($"[INFO] Result saved:\n{outputPath}");
                Cv2.WaitKey();
                Cv2.DestroyAllWindows();
            }
            catch (Exception ex)
            {
                Console.WriteLine( $"[WARNING] Draw result failed: {ex.Message}");
            }
        }
    }
}