using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionToolkit.Calibration
{
    internal class CalibrationConfig
    {
        public double[] CameraMatrix { get; set; }

        public double[] DistortionCoeffs { get; set; }

        public double PixelsPerMm { get; set; }

        public double StdDev { get; set; }

        public int ImageWidth { get; set; }

        public int ImageHeight { get; set; }

        public double RmsError { get; set; }

        public bool IsValid { get; set; }
    }
}
