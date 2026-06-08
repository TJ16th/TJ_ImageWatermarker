# WatermarkApp (Windows, GUI-less)

JPEG/PNG files can be processed by dragging and dropping them onto the executable icon.

## Features

- Reads JPEG and PNG inputs.
- Watermark text includes:
  - file creation time (Windows file system value)
  - Windows user name
  - original file name
- Adds two watermark layers:
  - bottom-right small low-opacity readable text
  - very light repeated pattern across the image
- Writes PNG output to the same folder as the executable.
- Uses non-overwrite naming:
  - `name_watermarked.png`
  - `name_watermarked_001.png`, ...

## Watermark spec

- Bottom-right text size: 2.2% of short side (min 12px, max 36px)
- Bottom-right text color: auto black/white from local background luminance
- Bottom-right opacity: 18%
- Bottom-right margin: 2.0% of short side (min 8px, max 32px)
- Pattern opacity: 5%
- Pattern angle: -28 degrees
- Pattern text size: 3.0% of short side (min 14px, max 48px)
- Pattern spacing: x = 1.8x text width, y = 2.2x text height

### Tiny-image rule

For images with short side < 300px:
- Bottom-right text uses 1.8% size (min 10px, max 18px)
- Bottom-right opacity is reduced to 14%
- Repeated pattern is disabled

## Build and publish

Requires .NET SDK 8.

If `dotnet` is already available, run one command:

```powershell
.\build-and-publish.ps1
```

```powershell
dotnet publish .\WatermarkApp\WatermarkApp.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Published executable will be in:

- `WatermarkApp\bin\Release\net8.0-windows\win-x64\publish\`

## Usage

1. Build the executable.
2. Drag one or more image files onto `WaterMarker.exe`.
3. Output PNG files are saved next to `WaterMarker.exe`.

If an error occurs, `watermark_error.log` is written next to the executable.
