using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionToolkit.TemplateMatchingNCC.Models
{
    /// <summary>
    /// Template Pyramid Data
    ///
    /// Purpose:
    ///     - Lưu dữ liệu template sau khi learning
    ///     - Dùng lại cho pyramid matching
    ///
    /// Responsibilities:
    ///     - Quản lý template pyramid
    ///     - Cache statistical information
    ///     - Giảm tính toán lặp lại
    ///
    /// Contains:
    ///     - Pyramid images
    ///     - Template mean
    ///     - Template normalization factor
    ///     - Inverse area
    ///     - Border color
    ///
    /// Notes:
    ///     - Internal data container
    ///     - Chỉ dùng trong matching engine
    /// </summary>
    internal class TemplData
    {
        public List<Mat> VecPyramid { get; } = new List<Mat>();
        public List<bool> VecResultEqual1 { get; private set; } = new List<bool>();
        public List<double> VecInvArea { get; private set; } = new List<double>();
        public List<Scalar> VecTemplMean { get; private set; } = new List<Scalar>();
        public List<double> VecTemplNorm { get; private set; } = new List<double>();
        public double BorderColor { get; set; }
        public bool IsPatternLearned { get; set; }

        public void Resize(int size)
        {
            VecResultEqual1 = Enumerable.Repeat(false, size).ToList();
            VecInvArea = Enumerable.Repeat(0.0, size).ToList();
            VecTemplMean = Enumerable.Repeat(Scalar.All(0), size).ToList();
            VecTemplNorm = Enumerable.Repeat(0.0, size).ToList();
        }

        public void Clear()
        {
            foreach (var m in VecPyramid) m?.Dispose();
            VecPyramid.Clear();
            IsPatternLearned = false;
        }
    }
}
