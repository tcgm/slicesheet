using OpenCvSharp;
using ImageMagick;
using System;
using System.IO;
using System.Linq;
using System.Drawing;
using Point = OpenCvSharp.Point;

class Program
{
    static void Main(string[] args)
    {
        string[] supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff" };

        string directory;
        string[] imageFiles;

        // If a filename is provided and it exists as a file
        if (args.Length >= 1 && File.Exists(args[0]))
        {
            imageFiles = new[] { args[0] };
            directory = Path.GetDirectoryName(args[0]) ?? Directory.GetCurrentDirectory();
        }
        // If a directory is provided or implied, process all images in the folder
        else if (args.Length >= 1 && (Directory.Exists(args[0]) || args[0] == "."))
        {
            directory = args[0] == "." ? Directory.GetCurrentDirectory() : args[0];

            Console.WriteLine($"No specific filename provided. Searching for images in {directory}...");

            // Find all image files in the directory
            imageFiles = Directory.GetFiles(directory)
                                  .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                                  .ToArray();

            if (imageFiles.Length == 0)
            {
                Console.WriteLine("No images found in the specified directory.");
                return;
            }

            Console.WriteLine($"Found {imageFiles.Length} images in the directory.");
        }
        else
        {
            Console.WriteLine("Usage: Slicer <filename or folder> <widthpx heightpx> or <rows cols>");
            return;
        }

        // Process all found image files
        foreach (string filename in imageFiles)
        {
            ProcessImage(filename, args);
        }
    }

    static void ProcessImage(string filename, string[] args)
    {
        string directory = Path.GetDirectoryName(filename);
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
        string outputFolder = Path.Combine(directory, nameWithoutExtension);
        Directory.CreateDirectory(outputFolder);

        // If the user provides pixel size or row/column info, use ImageMagick
        if (args.Length >= 3)
        {
            ProcessWithImageMagick(filename, args, outputFolder);
        }
        else
        {
            // Otherwise, try OpenCVSharp for automatic detection
            bool success = ProcessWithOpenCV(filename, outputFolder);

            if (!success)
            {
                Console.WriteLine($"Failed to auto-detect sprites in {filename}. Skipping.");
            }
        }
    }

    static void ProcessWithImageMagick(string filename, string[] args, string outputFolder)
    {
        using (MagickImage image = new MagickImage(filename))
        {
            int imgWidth = (int)image.Width;
            int imgHeight = (int)image.Height;

            int sliceWidth = 0;
            int sliceHeight = 0;

            // Pixel size input
            if (args[1].EndsWith("px") && args[2].EndsWith("px"))
            {
                sliceWidth = int.Parse(args[1].Replace("px", ""));
                sliceHeight = int.Parse(args[2].Replace("px", ""));
                Console.WriteLine($"Slicing {filename} using pixel dimensions: {sliceWidth}x{sliceHeight}");
            }
            // Rows/columns input
            else
            {
                int rows = int.Parse(args[1]);
                int cols = int.Parse(args[2]);

                sliceWidth = imgWidth / cols;
                sliceHeight = imgHeight / rows;
                Console.WriteLine($"Slicing {filename} using rows/columns: {rows} rows and {cols} columns");
            }

            // Slice the image into tiles
            int count = 0;
            for (int y = 0; y < imgHeight; y += sliceHeight)
            {
                for (int x = 0; x < imgWidth; x += sliceWidth)
                {
                    uint sliceWidthUint = (uint)Math.Min(sliceWidth, imgWidth - x);
                    uint sliceHeightUint = (uint)Math.Min(sliceHeight, imgHeight - y);
                    MagickGeometry geometry = new MagickGeometry(x, y, sliceWidthUint, sliceHeightUint);

                    using (MagickImage slice = (MagickImage)image.Clone())
                    {
                        slice.Crop(geometry);
                        string outputFileName = Path.Combine(outputFolder, $"sprite_{count}.png");
                        slice.Write(outputFileName);
                        Console.WriteLine($"Saved: {outputFileName}");
                    }
                    count++;
                }
            }
            Console.WriteLine($"Processed {filename} with ImageMagick: {count} sprites saved in {outputFolder}");
        }
    }

    static bool ProcessWithOpenCV(string filename, string outputFolder)
    {
        try
        {
            // Load the sprite sheet (with transparency)
            Mat image = Cv2.ImRead(filename, ImreadModes.Unchanged); // Use ImreadModes.Unchanged to load alpha channel

            // Check if the image has an alpha channel
            if (image.Channels() < 4)
            {
                Console.WriteLine("Image does not have an alpha channel.");
                return false;
            }

            // Split the image into channels (RGBA)
            Mat[] channels = Cv2.Split(image);
            Mat alphaChannel = channels[3]; // Extract the alpha channel (index 3)

            // Log image dimensions for debugging
            Console.WriteLine($"Image dimensions: {image.Width}x{image.Height}");

            // Invert the alpha channel to create a binary mask (white for sprites, black for background)
            Mat binary = new Mat();
            Cv2.Threshold(alphaChannel, binary, 1, 255, ThresholdTypes.Binary);

            // Increase dilation to help separate tightly packed sprites
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5)); // Larger kernel for more separation
            Cv2.Dilate(binary, binary, kernel);

            // Find contours (sprite boundaries) in the binary mask
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(binary, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0)
            {
                Console.WriteLine("No contours found.");
                return false;
            }

            // List to store detected sprite sizes
            List<OpenCvSharp.Size> spriteSizes = new List<OpenCvSharp.Size>();

            // Collect sprite sizes and save individual sprites temporarily
            int count = 0;
            foreach (var contour in contours)
            {
                // Get the bounding rectangle of each sprite
                Rect spriteBounds = Cv2.BoundingRect(contour);

                // Save the size of the sprite
                spriteSizes.Add(new OpenCvSharp.Size(spriteBounds.Width, spriteBounds.Height));

                // Crop the sprite from the original image
                Mat sprite = new Mat(image, spriteBounds);

                // Save each detected sprite as a new image
                string outputFilePath = Path.Combine(outputFolder, $"sprite_{count}.png");
                Cv2.ImWrite(outputFilePath, sprite);
                Console.WriteLine($"Saved: {outputFilePath}");
                count++;
            }

            Console.WriteLine($"Processed {filename} with OpenCV: {count} sprites saved in {outputFolder}");
            return true;
        }
        catch (Exception ex)
        {
            // Log the error and return false
            Console.WriteLine($"Error processing {filename} with OpenCV: {ex.Message}");
            return false;
        }
    }


    // Calculate the most common sprite width and height separately by averaging detected sizes,
    // and if they are far from common sizes, snap them to the nearest power of 2.
    static (int width, int height) CalculateCommonSpriteDimensions(List<OpenCvSharp.Size> spriteSizes)
    {
        // Calculate average width and height separately
        double avgWidth = spriteSizes.Average(s => s.Width);
        double avgHeight = spriteSizes.Average(s => s.Height);

        // Snap each dimension to the closest "common" size (e.g., 16x32, 32x64), or fallback to the nearest power of 2
        int commonWidth = SnapToCommonOrPowerOf2((int)avgWidth);
        int commonHeight = SnapToCommonOrPowerOf2((int)avgHeight);

        return (commonWidth, commonHeight);
    }

    // Snap to the nearest common sprite size (16, 32, 64, etc.) or the nearest power of 2 if it's not close enough
    static int SnapToCommonOrPowerOf2(int value)
    {
        int[] commonSizes = { 8, 16, 32, 48, 64, 128 }; // Add more common sizes if needed
        int closestCommonSize = commonSizes.OrderBy(s => Math.Abs(s - value)).First();

        // If the difference between the value and the closest common size is more than 10 pixels, use the nearest power of 2
        if (Math.Abs(closestCommonSize - value) > 10)
        {
            // Find the nearest power of 2
            return NearestPowerOf2(value);
        }

        // Otherwise, use the closest common size
        return closestCommonSize;
    }

    // Calculate the nearest power of 2 for a given value
    static int NearestPowerOf2(int value)
    {
        if (value <= 0)
            return 1;  // Powers of 2 start at 1 (2^0)

        int power = 1;

        // Shift power by multiplying by 2 until it's greater than or equal to the input value
        while (power < value)
        {
            power <<= 1; // Same as multiplying by 2 (e.g., 1, 2, 4, 8, 16, etc.)
        }

        // Check if the previous power of 2 (power / 2) is closer to the input value
        int previousPower = power >> 1; // Divide by 2 to get the previous power of 2

        // Return whichever is closer: the current power or the previous power
        return (Math.Abs(value - previousPower) < Math.Abs(value - power)) ? previousPower : power;
    }

}
