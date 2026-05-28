using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionToolkit.Utilities
{
    /// <summary>
    /// Image Helper
    ///
    /// Mục đích:
    ///     - Chứa các hàm tiện ích xử lý I/O ảnh
    ///     - Giảm code lặp khi load/save ảnh
    ///
    /// Trách nhiệm:
    ///     - Load ảnh an toàn
    ///     - Validate ảnh tồn tại
    ///     - Kiểm tra lỗi khi đọc ảnh
    ///
    /// Ghi chú:
    ///     - Đây là utility class (helper)
    ///     - Chỉ chứa static methods
    ///     - Không chứa xử lý vision algorithm
    ///
    /// Ví dụ sử dụng:
    ///     var image =
    ///         ImageHelper.LoadImageSafe(
    ///             path,
    ///             out string error);
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// Load ảnh an toàn từ đường dẫn.
        ///
        /// Chức năng:
        ///     - Kiểm tra file tồn tại
        ///     - Load ảnh bằng OpenCV
        ///     - Validate ảnh hợp lệ
        ///
        /// Input:
        ///     - path: đường dẫn ảnh
        ///
        /// Output:
        ///     - Mat hợp lệ nếu load thành công
        ///     - null nếu lỗi
        ///
        /// Ghi chú:
        ///     - error sẽ chứa thông tin lỗi
        /// </summary>
        public static Mat LoadImageSafe(
            string path,
            out string error)
        {
            error = null;

            if (!File.Exists(path))
            {
                error = $"Image not found: {path}";
                return null;
            }

            var img = Cv2.ImRead(path);

            if (img == null || img.Empty())
            {
                error = $"Cannot load image: {path}";
                img?.Dispose();
                return null;
            }

            return img;
        }
    }
}
