# VisionToolkit

Machine Vision Toolkit built with C# and OpenCVSharp.

## Current Features

### Camera Calibration
- Chessboard corner detection
- Camera intrinsic matrix
- Distortion coefficients
- Pixel/mm calculation
- RMS reprojection error
- Image undistortion
- Distance measurement
- Object size measurement
- Save/Load JSON configuration

## Tech Stack
- C#
- .NET
- OpenCvSharp

## Workflow

```text
Calibrate
↓
Save JSON
↓
Load JSON
↓
Undistort
↓
Measurement
```

## Future Modules
- Find Line
- NCC Template Matching
- Blob Analysis
- OCR
- Barcode Reader