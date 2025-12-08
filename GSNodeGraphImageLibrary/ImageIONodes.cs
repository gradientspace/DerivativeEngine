// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using SkiaSharp;
using Gradientspace.NodeGraph.Nodes;

namespace Gradientspace.NodeGraph.Image
{
    [NodeFunctionLibrary("Geometry3.Image")]
    [MappedFunctionLibraryName("Geometry3.Image")]
    public static class ImageIOFunctions
    {
        [NodeFunction(ReturnName="Image")]
        public static PixelImage LoadImage(string Path = "gradient.png", bool Decompress = false)
        {
            byte[] imageBytes = File.ReadAllBytes(Path);

            SKImageInfo imageInfo = new();
            SKEncodedImageFormat format = SKEncodedImageFormat.Png;
            using (var stream = new MemoryStream(imageBytes)) {
                // Create an SKCodec from the stream
                using (var codec = SKCodec.Create(stream, out var result)) {
                    if (result != SKCodecResult.Success) {
                        throw new InvalidDataException($"[LoadImage] Failed to decode file - probably not a supported image type");
                    }
                    // Get the image info from the codec
                    format = codec.EncodedFormat;
                    imageInfo = codec.Info;
                }
            }
            
            PixelImage.ECompression compression = PixelImage.ECompression.UnspecifiedImageFormat;
            switch (format) {
                case SKEncodedImageFormat.Png:      compression = PixelImage.ECompression.PNG; break;
                case SKEncodedImageFormat.Jpeg:     compression = PixelImage.ECompression.JPEG; break;
                default:
                    throw new FormatException($"[LoadImage] Image format {format} is unsupported");
            }

            // todo can we figure out pixel format from SKImageInfo?

            PixelImage image = new PixelImage(imageInfo.Width, imageInfo.Height, imageBytes, PixelImage.EPixelFormat.Encoded, compression);

            if (Decompress) {
                image = ImageUtil.GetDecompressedRGBA8(image, bAsSRGB: true);
            }

            return image;
        }



        [NodeFunction(ReturnName = "Image")]
        public static void SaveImageToFile(ref PixelImage Image, string Path = "output.png")
        {
            SKImage skImage = ImageUtil.PixelImageToSKImage(Image);

            SKEncodedImageFormat Format = SKEncodedImageFormat.Png;
            if (Path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) {
                Format = SKEncodedImageFormat.Png;
            } else if (Path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || Path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)) {
                Format = SKEncodedImageFormat.Jpeg;
            } else {
                throw new FormatException($"[SaveImageToFile] Unsupported file extension in path: {Path}");
            }

            using (var encodedData = skImage.Encode(Format, 100)) {
                using (var fileStream = File.OpenWrite(Path)) {
                    encodedData.SaveTo(fileStream);
                }
            }
        }

    }

}
