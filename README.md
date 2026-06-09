# VisionToolkit

Machine Vision Toolkit built with **C#** and **OpenCvSharp**.

A modular computer vision toolkit for industrial machine vision applications, including camera calibration, template matching, measurement, and future inspection tools.

---

## Current Features

### 1. Camera Calibration
Industrial camera calibration module.

#### Features
- Chessboard corner detection
- Camera intrinsic matrix estimation
- Distortion coefficients calculation
- RMS reprojection error
- Pixel/mm ratio calculation
- Image undistortion
- Coordinate correction
- Distance measurement
- Object size measurement
- Save/Load calibration result via JSON

#### Workflow

```text
Calibration
↓
Save JSON
↓
Load JSON
↓
Undistort
↓
Measurement
```

---

### 2. NCC Template Matching
Rotation-tolerant template matching using pyramid search and NCC normalization.

#### Features
- Multi-angle template matching
- Pyramid coarse-to-fine search
- Sub-pixel refinement
- Overlap filtering
- Score threshold filtering
- Multiple object detection
- Rotation angle estimation
- Rotated bounding box output

#### Workflow

```text
Learn Template
↓
Build Pyramid
↓
Generate Search Angles
↓
Top Layer Matching
↓
Pyramid Refinement
↓
Score Filtering
↓
Overlap Filtering
↓
Final Match Result
```

---

## Tech Stack

- **C#**
- **.NET**
- **OpenCvSharp**
- **Machine Vision / Image Processing**

---

## Example Modules

### Camera Calibration
- Camera calibration
- Distortion correction
- Measurement

### Template Matching NCC
- Rotation matching
- Multi-object detection
- Angle estimation

---

## Future Modules
- Find Line
- Blob Analysis
- OCR
- Barcode Reader
- Deep Learning Inference (ONNX)

---

## Goal

Build a reusable **industrial machine vision toolkit** for learning and real-world automation projects.
