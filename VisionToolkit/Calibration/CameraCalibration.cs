using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionToolkit.Utilities;
using System.Text.Json;

namespace VisionToolkit.Calibration
{
    /// <summary>
    /// Camera Calibration Module
    ///
    /// Mục đích:
    ///     - Hiệu chuẩn camera bằng chessboard
    ///     - Tính intrinsic, distortion, scale
    ///
    /// Trách nhiệm:
    ///     - Detect chessboard corners
    ///     - Refine sub-pixel accuracy
    ///     - Optimize camera parameters
    ///     - Evaluate reprojection error
    ///
    /// Output:
    ///     - CalibrationResult
    ///
    /// Ghi chú:
    ///     - Chỉ cần calibrate một lần cho mỗi setup camera
    ///     - Có thể lưu và load lại kết quả
    /// </summary>
    public class CameraCalibration
    {
        private CalibrationResult _result;

        /// <summary>
        /// Hiệu chuẩn camera từ tập ảnh chessboard.
        ///
        /// Mục đích:
        /// - Detect chessboard corners
        /// - Tính camera intrinsic matrix
        /// - Tính distortion coefficients
        /// - Tính pixel/mm ratio
        ///
        /// Input:
        /// - imagePaths     : danh sách ảnh chessboard
        /// - patternWidth   : số corner theo chiều ngang
        /// - patternHeight  : số corner theo chiều dọc
        /// - squareSizeMm   : kích thước ô vuông (mm)
        ///
        /// Output:
        /// - CalibrationResult
        ///
        /// Ghi chú:
        /// - Cần ít nhất 3 ảnh hợp lệ
        /// - 10~20 ảnh thường cho kết quả tốt
        /// </summary>
        public CalibrationResult Calibrate(
            string[] imagePaths,
            int patternWidth,
            int patternHeight,
            float squareSizeMm)
        {
            // Validate input
            if (imagePaths == null || imagePaths.Length == 0)
            {
                Console.WriteLine("[ERROR] Image list is empty.");

                return new CalibrationResult
                {
                    IsValid = false
                };
            }

            if (squareSizeMm <= 0)
            {
                Console.WriteLine("[ERROR] squareSizeMm must be > 0.");

                return new CalibrationResult
                {
                    IsValid = false
                };
            }

            // Tạo object point template
            var objectTemplate = CreateObjectPointTemplate(
                patternWidth,
                patternHeight,
                squareSizeMm);

            // Dataset cho calibration
            var objectPointsList = new List<IEnumerable<Point3f>>();
            var imagePointsList = new List<IEnumerable<Point2f>>();

            Size imageSize = new();

            Console.WriteLine($"Processing {imagePaths.Length} chessboard images...");

            int successCount = 0;

            // Process từng ảnh
            foreach (string path in imagePaths)
            {
                bool success = ProcessChessboardImage(
                    path,
                    patternWidth,
                    patternHeight,
                    objectTemplate,
                    ref imageSize,
                    objectPointsList,
                    imagePointsList);

                if (success)
                    successCount++;
            }

            if (successCount < 10)
            {
                Console.WriteLine($"[WARN] Only {successCount} valid images found.");
            }
            else
            {
                Console.WriteLine($"[OK] Success: {successCount}/{imagePaths.Length}");
            }

            // Tối thiểu 3 ảnh
            if (imagePointsList.Count < 3)
            {
                Console.WriteLine( "[ERROR] Need at least 3 valid images.");

                return new CalibrationResult
                {
                    IsValid = false
                };
            }

            // Calibrate thật ở bước này
            _result = PerformCalibration(
                        objectPointsList,
                        imagePointsList,
                        imageSize,
                        squareSizeMm,
                        patternWidth,
                        patternHeight);

            return _result;
        }

        public CalibrationResult GetResult()
        {
            return _result;
        }

        /// <summary>
        /// Tạo tọa độ 3D mẫu của chessboard.
        ///
        /// Mục đích:
        ///     - Sinh object points (tọa độ thật)
        ///     - Dùng chung cho tất cả ảnh chessboard
        ///
        /// Ví dụ:
        ///     squareSize = 20 mm
        ///
        ///     (0,0,0)
        ///     (20,0,0)
        ///     (40,0,0)
        ///     ...
        ///
        /// Ghi chú:
        ///     - Z = 0 vì chessboard nằm trên mặt phẳng
        /// </summary>
        private List<Point3f> CreateObjectPointTemplate(int width, int height, float squareSizeMm)
        {
            var points = new List<Point3f>(width * height);

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    points.Add(
                        new Point3f(
                            col * squareSizeMm,
                            row * squareSizeMm,
                            0f));
                }
            }
            return points;
        }

        /// <summary>
        /// Xử lý một ảnh chessboard.
        ///
        /// Mục đích:
        /// - Detect các corner của chessboard
        /// - Refine độ chính xác sub-pixel
        /// - Thêm dữ liệu vào calibration dataset
        ///
        /// Pipeline:
        /// Load image → Gray → Find corners
        /// → Sub-pixel refine → Save points
        ///
        /// Output:
        /// - true  : detect thành công
        /// - false : detect thất bại
        ///
        /// Ghi chú:
        /// - Mỗi ảnh thành công sẽ đóng góp
        ///   một bộ object points + image points
        /// </summary>
        private bool ProcessChessboardImage(
            string imagePath,
            int patternWidth,
            int patternHeight,
            List<Point3f> objectTemplate,
            ref Size imageSize,
            List<IEnumerable<Point3f>> objectPointsList,
            List<IEnumerable<Point2f>> imagePointsList)
        {
            using var disposer = new MatDisposer();

            // 1. Load image
            var image = ImageHelper.LoadImageSafe(imagePath, out string error);

            if (image == null)
            {
                Console.WriteLine($"[ERROR] {error}");
                return false;
            }

            disposer.Track(image);

            // Lưu kích thước ảnh đầu tiên
            if (imageSize.Width == 0)
                imageSize = image.Size();

            // 2. Convert to grayscale
            var gray = disposer.Track(new Mat());

            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            // 3. Detect chessboard corners
            bool found = Cv2.FindChessboardCorners(
                gray,
                new Size(patternWidth, patternHeight),
                out Point2f[] corners,
                ChessboardFlags.AdaptiveThresh |
                ChessboardFlags.NormalizeImage |
                ChessboardFlags.FastCheck);

            if (!found)
            {
                Console.WriteLine($"[ERROR] {Path.GetFileName(imagePath)} : Corner not found");
                return false;
            }

            // 4. Refine sub-pixel accuracy
            var criteria = new TermCriteria(
                CriteriaTypes.Eps | CriteriaTypes.MaxIter,
                30,
                0.001);

            Cv2.CornerSubPix(
                gray,
                corners,
                new Size(11, 11),
                new Size(-1, -1),
                criteria);

            // 5. Save calibration data
            objectPointsList.Add(objectTemplate.ToArray());
            imagePointsList.Add(corners);

            Console.WriteLine($"[OK] {Path.GetFileName(imagePath)} ({corners.Length} corners)");

            return true;
        }

        /// <summary>
        /// Thực hiện camera calibration.
        ///
        /// Mục đích:
        /// - Tính camera intrinsic matrix
        /// - Tính distortion coefficients
        /// - Đánh giá RMS reprojection error
        ///
        /// Pipeline:
        /// Object points + Image points
        ///             ↓
        ///     Cv2.CalibrateCamera()
        ///             ↓
        ///      Camera parameters
        ///
        /// Output:
        /// - CalibrationResult
        ///
        /// Ghi chú:
        /// - RMS càng nhỏ càng tốt
        /// - < 0.5 px : rất tốt
        /// - 0.5~1 px : ổn
        /// - > 1 px : nên chụp lại ảnh
        /// </summary>
        private CalibrationResult PerformCalibration(
              List<IEnumerable<Point3f>> objectPointsList,
              List<IEnumerable<Point2f>> imagePointsList,
              Size imageSize,
              float squareSizeMm,
              int patternWidth,
              int patternHeight)
        {
            // Camera matrix (3x3)
            double[,] cameraMatrix = new double[3, 3];

            // Distortion coefficients
            double[] distCoeffs = new double[5];

            Console.WriteLine("\nPerforming calibration...");

            // OpenCV calibration
            double rms = Cv2.CalibrateCamera(
                objectPointsList,
                imagePointsList,
                imageSize,
                cameraMatrix,
                distCoeffs,
                out Vec3d[] rvecs,
                out Vec3d[] tvecs);

            // Tính pixel/mm của từng ảnh
            var ratios =
                CalculatePixelsPerMmRatios(
                    imagePointsList,
                    squareSizeMm,
                    patternWidth,
                    patternHeight);

            // Mean pixel/mm
            double pixelsPerMm = ratios.Average();

            // Stability
            double stdDev = CalculateStdDev(ratios, pixelsPerMm);

            if (rms < 0.5)
            {
                Console.WriteLine( $"[OK] RMS Reprojection Error: {rms:F4} px");
            }
            else if (rms < 1.0)
            {
                Console.WriteLine( $"[WARN] RMS Reprojection Error: {rms:F4} px (acceptable)");
            }
            else
            {
                Console.WriteLine( $"[WARN] RMS Reprojection Error: {rms:F4} px (consider recalibration)");
            }

            Console.WriteLine($"[OK] Pixel/mm = {pixelsPerMm:F4}");

            Console.WriteLine($"[OK] mm/pixel = {1.0 / pixelsPerMm:F6}");

            Console.WriteLine($"[OK] StdDev = {stdDev:F4}");
            return new CalibrationResult
            {
                CameraMatrix = cameraMatrix,
                DistortionCoeffs = distCoeffs,
                PixelsPerMm = pixelsPerMm,
                StdDev = stdDev,
                ImageSize = imageSize,
                RmsError = rms,
                IsValid = true
            };
        }

        /// <summary>
        /// Sửa méo ảnh bằng calibration result.
        ///
        /// Mục đích:
        /// - Loại bỏ méo lens
        /// - Tạo ảnh chuẩn để đo kích thước
        ///
        /// Input:
        /// - src : ảnh gốc
        ///
        /// Output:
        /// - ảnh đã sửa méo
        ///
        /// Ghi chú:
        /// - Không modify ảnh gốc
        /// - Trả về Mat mới
        /// </summary>
        public Mat Undistort(Mat src)
        {
            if (_result == null || !_result.IsValid)
                throw new InvalidOperationException("Calibration result is invalid.");

            if (src == null || src.Empty())
                throw new ArgumentException("Input image is null or empty.");

            using var disposer = new MatDisposer();

            var camMat = disposer.Track(MatFromArray(_result.CameraMatrix));

            var distMat = disposer.Track(MatFromArray(_result.DistortionCoeffs));

            var output = new Mat();
            Cv2.Undistort(
                src,
                output,
                camMat,
                distMat);

            return output;
        }

        /// <summary>
        /// Tính pixel/mm ratio của từng ảnh chessboard.
        ///
        /// Ý tưởng:
        ///     Mỗi ảnh → tính average khoảng cách
        ///     giữa các corner kề nhau
        ///     → sinh ra 1 ratio.
        ///
        /// Output:
        ///     - List<double>
        ///     - Mỗi ảnh = 1 ratio
        ///
        /// Ghi chú:
        ///     - Dùng để đánh giá độ ổn định
        ///       giữa các lần capture
        /// </summary>
        private List<double> CalculatePixelsPerMmRatios(
            List<IEnumerable<Point2f>> imagePointsList,
            float squareSizeMm,
            int patternWidth,
            int patternHeight)
        {
            var ratios = new List<double>();

            foreach (var imageCorners in imagePointsList)
            {
                Point2f[] corners = imageCorners.ToArray();

                double totalDist = 0;
                int count = 0;

                // Horizontal
                for (int row = 0; row < patternHeight; row++)
                {
                    for (int col = 0; col < patternWidth - 1; col++)
                    {
                        int i1 = row * patternWidth + col;

                        int i2 = i1 + 1;

                        totalDist += Distance(corners[i1], corners[i2]);

                        count++;
                    }
                }

                // Vertical
                for (int row = 0; row < patternHeight - 1; row++)
                {
                    for (int col = 0; col < patternWidth; col++)
                    {
                        int i1 = row * patternWidth + col;

                        int i2 = i1 + patternWidth;

                        totalDist += Distance(corners[i1], corners[i2]);

                        count++;
                    }
                }

                if (count > 0)
                {
                    double avgPixelDist = totalDist / count;

                    double pixelPerMm = avgPixelDist / squareSizeMm;

                    ratios.Add(pixelPerMm);
                }
            }

            return ratios;
        }

        /// <summary>
        /// Tính độ lệch chuẩn (StdDev).
        ///
        /// Mục đích:
        /// - Đánh giá độ ổn định của pixel/mm
        /// - StdDev càng nhỏ càng tốt
        ///
        /// Output:
        /// - Độ lệch chuẩn
        /// </summary>
        private double CalculateStdDev(List<double> values, double mean)
        {
            if (values == null || values.Count <= 1)
                return 0;

            double variance = values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1);

            return Math.Sqrt(variance);
        }

        /// <summary>
        /// Đo khoảng cách giữa 2 điểm pixel.
        ///
        /// Mục đích:
        /// - Convert pixel → mm
        /// - Dùng cho dimension measurement
        ///
        /// Input:
        /// - p1 : điểm đầu
        /// - p2 : điểm cuối
        ///
        /// Output:
        /// - khoảng cách (mm)
        ///
        /// Ghi chú:
        /// - Nên dùng ảnh đã undistort
        /// </summary>
        public double MeasureDistance(Point2f p1, Point2f p2)
        {
            if (_result == null || !_result.IsValid)
                throw new InvalidOperationException("Calibration result is invalid.");

            if (_result.PixelsPerMm <= 0)
                throw new InvalidOperationException("PixelsPerMm is invalid.");

            // Distance in pixel
            double distPx = Distance(p1, p2);

            // Convert to mm
            double distMm = distPx / _result.PixelsPerMm;

            return distMm;
        }

        /// <summary>
        /// Đo kích thước vật thể từ bounding rectangle.
        ///
        /// Mục đích:
        /// - Convert width/height pixel → mm
        ///
        /// Input:
        /// - boundingRect : ROI của object
        ///
        /// Output:
        /// - (widthMm, heightMm)
        /// </summary>
        public (double WidthMm, double HeightMm) MeasureObject(Rect boundingRect)
        {
            if (_result == null || !_result.IsValid)
                throw new InvalidOperationException("Calibration result is invalid.");

            double widthMm = boundingRect.Width / _result.PixelsPerMm;

            double heightMm = boundingRect.Height / _result.PixelsPerMm;

            return (widthMm, heightMm);
        }

        /// <summary>
        /// Lưu calibration result ra file JSON.
        /// Dùng để reuse calibration mà không cần calibrate lại.
        /// </summary>
        public void SaveResult(string filePath)
        {
            if (_result == null || !_result.IsValid)
                throw new InvalidOperationException("Calibration result is invalid.");

            var config = new CalibrationConfig
            {
                CameraMatrix = new double[]
                {
                    _result.CameraMatrix[0,0],
                    _result.CameraMatrix[0,1],
                    _result.CameraMatrix[0,2],
                    _result.CameraMatrix[1,0],
                    _result.CameraMatrix[1,1],
                    _result.CameraMatrix[1,2],
                    _result.CameraMatrix[2,0],
                    _result.CameraMatrix[2,1],
                    _result.CameraMatrix[2,2]
                },

                DistortionCoeffs = _result.DistortionCoeffs,
                PixelsPerMm = _result.PixelsPerMm,
                StdDev = _result.StdDev,
                ImageWidth = _result.ImageSize.Width,
                ImageHeight = _result.ImageSize.Height,
                RmsError = _result.RmsError,
                IsValid = _result.IsValid
            };

            string json = JsonSerializer.Serialize(
                config,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Load calibration result từ file JSON.
        /// Dùng lại calibration cũ mà không cần chạy chessboard.
        /// </summary>
        public void LoadResult(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            string json = File.ReadAllText(filePath);

            var config =
                JsonSerializer.Deserialize<CalibrationConfig>(json);

            if (config == null)
                throw new Exception("Failed to load calibration.");

            _result = new CalibrationResult
            {
                CameraMatrix = new double[,]
                {
            { config.CameraMatrix[0], config.CameraMatrix[1], config.CameraMatrix[2] },
            { config.CameraMatrix[3], config.CameraMatrix[4], config.CameraMatrix[5] },
            { config.CameraMatrix[6], config.CameraMatrix[7], config.CameraMatrix[8] }
                },

                DistortionCoeffs = config.DistortionCoeffs,
                PixelsPerMm = config.PixelsPerMm,
                StdDev = config.StdDev,
                ImageSize = new Size(config.ImageWidth, config.ImageHeight),
                RmsError = config.RmsError,
                IsValid = config.IsValid
            };
        }

        /// <summary>
        /// Convert double[,] → OpenCV Mat.
        ///
        /// Dùng cho:
        /// - Camera matrix
        /// </summary>
        private Mat MatFromArray(double[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);

            var mat = new Mat(rows, cols, MatType.CV_64F);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    mat.Set(r, c, array[r, c]);
                }
            }

            return mat;
        }

        /// <summary>
        /// Convert double[] → OpenCV Mat.
        ///
        /// Dùng cho:
        /// - Distortion coefficients
        /// </summary>
        private Mat MatFromArray(double[] array)
        {
            var mat = new Mat(1, array.Length, MatType.CV_64F);

            for (int i = 0; i < array.Length; i++)
            {
                mat.Set(0, i, array[i]);
            }

            return mat;
        }

        /// <summary>
        /// Tính khoảng cách Euclidean giữa 2 điểm pixel.
        /// </summary>
        private double Distance(Point2f p1, Point2f p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
