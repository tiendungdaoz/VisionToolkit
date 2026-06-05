using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionToolkit.Calibration;

namespace VisionToolkit.Demo.Test
{
    internal static class CalibrationDemo
    {
        public static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== CAMERA CALIBRATION TEST ===\n");

            var calibration = new CameraCalibration();

            //Select the path containing the chessboard images captured from the Camera.
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Chessboards");
            string jsonPath = "camera_config.json";

            // -----------------------------
            // Select mode
            // -----------------------------
            Console.WriteLine("1. New Calibration");
            Console.WriteLine("2. Load JSON");
            Console.Write("\nSelect Mode: ");

            string mode = Console.ReadLine();

            // ==================================================
            // MODE 1 : CALIBRATE
            // ==================================================
            if (mode == "1")
            {
                string[] imagePaths = Enumerable.Range(1, 20)
                    .Select(i => Path.Combine(dir, $"Im_R_{i}.png"))
                    .Where(File.Exists)
                    .ToArray();

                if (imagePaths.Length == 0)
                {
                    Console.WriteLine("[ERROR] No chessboard images found.");
                    Console.ReadKey();
                    return;
                }

                var result = calibration.Calibrate(imagePaths, 11, 7, 20f);

                if (!result.IsValid)
                {
                    Console.WriteLine("[ERROR] Calibration Failed.");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("\n[OK] Calibration Success!");
                Console.WriteLine($"RMS Error : {result.RmsError:F4} px");
                Console.WriteLine($"Pixel/mm  : {result.PixelsPerMm:F4}");
                Console.WriteLine($"mm/pixel  : {result.MmPerPixel:F6}");

                // Save JSON
                calibration.SaveResult(jsonPath);

                Console.WriteLine($"[OK] Saved JSON: {jsonPath}");
            }

            // ==================================================
            // MODE 2 : LOAD JSON
            // ==================================================
            else if (mode == "2")
            {
                try
                {
                    calibration.LoadResult(jsonPath);
                    Console.WriteLine("[OK] Calibration Loaded!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {ex.Message}");
                    Console.ReadKey();
                    return;
                }
            }
            else
            {
                Console.WriteLine("[ERROR] Invalid mode.");
                Console.ReadKey();
                return;
            }

            // ==================================================
            // UNDISTORT TEST
            // ==================================================
            Console.WriteLine("\n=== UNDISTORT TEST ===");

            string imagePath = Path.Combine(dir, $"Im_R_1.png");

            using Mat src = Cv2.ImRead(imagePath);
            using Mat corrected = calibration.Undistort(src);

            Cv2.ImShow("Original", src);
            Cv2.ImShow("Undistorted", corrected);

            Cv2.WaitKey();
            Cv2.DestroyAllWindows();

            // ==================================================
            // MEASURE DISTANCE
            // ==================================================
            Console.WriteLine("\n=== MEASURE DISTANCE ===");

            double distanceMm = calibration.MeasureDistance(
                new Point2f(100, 100),
                new Point2f(500, 300));

            Console.WriteLine($"Distance = {distanceMm:F3} mm");

            // ==================================================
            // MEASURE OBJECT
            // ==================================================
            Console.WriteLine("\n=== MEASURE OBJECT ===");

            var objectSize = calibration.MeasureObject(
                new Rect(100, 100, 320, 240));

            Console.WriteLine($"Width  : {objectSize.WidthMm:F2} mm");
            Console.WriteLine($"Height : {objectSize.HeightMm:F2} mm");

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
