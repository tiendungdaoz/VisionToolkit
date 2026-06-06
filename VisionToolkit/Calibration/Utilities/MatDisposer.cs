using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionToolkit.Calibration.Utilities
{
    /// <summary>
    /// Mat Resource Manager
    ///
    /// Mục đích:
    ///     - Quản lý vòng đời của OpenCV Mat
    ///     - Tránh memory leak do Mat sử dụng unmanaged memory
    ///
    /// Trách nhiệm:
    ///     - Track Mat được tạo ra
    ///     - Dispose toàn bộ khi kết thúc scope
    ///
    /// Ghi chú:
    ///     - Dùng với 'using'
    ///     - Hoạt động giống mini garbage collector cho Mat
    /// </summary>
    public class MatDisposer : IDisposable
    {
        private readonly List<Mat> _mats = new();

        public Mat Track(Mat mat)
        {
            if (mat != null)
                _mats.Add(mat);

            return mat;
        }

        public void Dispose()
        {
            foreach (var mat in _mats)
                mat?.Dispose();

            _mats.Clear();
        }
    }
}
