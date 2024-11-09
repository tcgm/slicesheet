# Sprite Sheet Slicer

A simple command-line tool that slices sprite sheets into individual sprites using **OpenCVSharp** for sprite detection and **ImageMagick** for image manipulation. The program supports sprite sheets with transparent backgrounds and automatically detects and slices sprites, snapping to the closest common sizes or powers of two.

## Features
- Automatically detects individual sprites on sprite sheets.
- Handles sprite sheets with transparent backgrounds.
- Resizes sprites to the nearest common size or power of two.
- Supports custom slicing based on pixel sizes or rows and columns.

## Requirements
- .NET 8.0 or higher
- OpenCVSharp4
- ImageMagick.NET (Magick.NET)
- ImageMagick

## Installation

### Step 1: Clone the Repository
To get started, clone this repository to your local machine:

```
git clone https://github.com/tcgm/slicesheet.git
cd slicesheet
```

### Step 2: Install Dependencies

The project requires **OpenCVSharp** and **Magick.NET** (ImageMagick.NET bindings). You can install these via **NuGet**.

#### Option 1: Visual Studio
1. Open the project in **Visual Studio**.
2. Right-click the project in **Solution Explorer**.
3. Select **Manage NuGet Packages**.
4. Search for and install the following packages:
   - `OpenCvSharp4.Windows`
   - `Magick.NET-Q16-AnyCPU`

#### Option 2: Using NuGet Command Line

Alternatively, you can use the **NuGet Package Manager Console** or **dotnet CLI** to install the required packages:

```
dotnet add package OpenCvSharp4.Windows
dotnet add package Magick.NET-Q16-AnyCPU
```

### Step 3: Build the Project

After installing the dependencies, build the project to create the executable:

```
dotnet build -c Release
```

### Step 4: Publish the Project (Optional)

To create a standalone executable, use the **dotnet publish** command:

```
dotnet publish -c Release -r win-x64 --self-contained
```

This will generate a single executable that can be run on any machine without needing to install .NET or the dependencies.

## Usage

### Basic Command-Line Usage

Once built, you can run the executable from the command line to slice sprite sheets. The tool supports two main modes:

1. **Slicing by Pixel Size:**

   Specify the width and height in pixels for slicing the sprite sheet.

   ```
   slicesheet <filename.png> 32px 32px
   ```

2. **Slicing by Rows and Columns:**

   Specify the number of rows and columns.

   ```
   slicesheet <filename.png> 10 5
   ```

3. **Automatic Mode (without filename):**

   The tool will detect and process all images in the current folder.

   ```
   slicesheet .
   ```

### Example:

```
slicesheet spritesheet.png 32px 32px
```

This command slices the `spritesheet.png` into 32x32 pixel sprites and saves them in the same folder.

### Parameters:
- **filename.png**: The sprite sheet to process.
- **32px 32px**: The width and height in pixels of each sprite.
- **10 5**: Number of rows and columns to slice the sprite sheet.

## Output

The output files will be saved in the same directory as the input file, in a folder named after the sprite sheet (without the file extension).

For example, if the input file is `spritesheet.png`, the individual sprites will be saved as `sprite_0.png`, `sprite_1.png`, and so on in a folder named `spritesheet`.

## Troubleshooting

### Common Issues:

1. **No sprites detected:**
   - Make sure the sprite sheet has a transparent background. The program relies on the alpha channel for sprite detection.
   - Increase the dilation size in the code if sprites are too close together.

2. **Memory issues with large images:**
   - If processing large sprite sheets, consider using a machine with more RAM or reducing the sprite sheet size.

3. **Performance issues:**
   - For large sprite sheets, slicing may take some time. Consider reducing the number of sprites or simplifying the sprite sheet layout.

### Logs:

The program provides basic logging in the command line. If sprites are not being detected, review the output to check image dimensions and other logged details.

## License

This project is licensed under the MIT License.
