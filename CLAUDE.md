# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BrutalCStore is a C# console application that functions as a DICOM C-Store SCU (Service Class User) client. It generates synthetic DICOM images with bitmaps and sends them to a DICOM SCP (Service Class Provider) server for testing and stress testing purposes.

## Common Development Commands

### Build and Run
```bash
# Build the project
dotnet build

# Run the application  
dotnet run --project BrutalCStore

# Build for release
dotnet build --configuration Release
```

### Package Management
```bash
# Restore NuGet packages
dotnet restore

# Add new package (example)
dotnet add BrutalCStore package [PackageName]
```

## Architecture and Key Components

### Core Application Flow
1. **Initialization** (`InitialEnvironment()`) - Sets up directories and loads configuration
2. **DICOM Generation** (`CreateDicomFromBitmap()`, `GetDicom()`) - Creates synthetic DICOM files from bitmaps
3. **Network Transmission** (`SendDcm()`) - Sends DICOM files via C-Store requests
4. **Parallel Processing** - Uses `Parallel.For` and `ConcurrentBag` for concurrent DICOM generation

### Key Classes and Files
- **Program.cs** - Main application logic with DICOM generation and network communication
- **StoreConfig.cs** - Configuration model classes (`StoreConfig`, `DcmTagPair`)
- **setting.json** - DICOM server connection settings and test parameters
- **dcmTagValuePair.json** - Additional DICOM tags to inject into generated files

### Dependencies
- **fo-dicom (4.0.8)** - Primary DICOM library for file manipulation and network operations
- **System.Drawing.Common (7.0.0)** - For bitmap generation and image processing
- **.NET 6.0** - Target framework with nullable reference types enabled

### Directory Structure
- **dcm/** - Generated DICOM files with unique study/series UIDs (created at runtime)
- **temp/** - Template DICOM files created from bitmaps (reused across runs)

## Configuration

### DICOM Server Settings (setting.json)
- **ip/port** - Target DICOM SCP server address
- **calledAE/callingAE** - DICOM Application Entity titles
- **countOfDcm** - Number of DICOM instances per series (default: 200)
- **interval** - Number of study iterations to perform (default: 100)

### Custom DICOM Tags (dcmTagValuePair.json)
Array of additional DICOM tags to inject into generated files. Each entry requires:
- Name, Group (hex), Element (hex), Value

## Key Implementation Details

### DICOM Image Generation
- Creates 1024x1024 grayscale images with red borders and text
- Converts bitmap pixels to 8-bit grayscale using standard luminance formula
- Generates unique SOPInstanceUIDs for each DICOM file

### Network Operations
- Uses asynchronous DICOM client operations (`DicomClient.SendAsync()`)
- Implements C-Store response handling with automatic file cleanup
- Supports parallel DICOM generation but sequential network transmission per study

### File Management
- Template files in temp/ directory are reused across studies for efficiency  
- Generated DICOM files in dcm/ directory are deleted after successful transmission
- Uses GUID-based directory names to avoid conflicts