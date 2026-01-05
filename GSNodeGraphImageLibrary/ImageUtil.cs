// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Image
{
    public static class ImageUtil
    {
        public static PixelImage GetDecompressedRGBA8(PixelImage fromImage, bool bAsSRGB)
        {
            if (fromImage.Compression == PixelImage.ECompression.Uncompressed) 
            {
                if (fromImage.Format == PixelImage.EPixelFormat.RGBA8)
                    return fromImage;
                else
                    throw new NotImplementedException($"ImageUtil.GetDecompressedRGBA8: uncompressed but unsupported pixel format {fromImage.Format}");
            }

            SKAlphaType alphaType = SKAlphaType.Unpremul;
            SKColorSpace? useColorSpace = bAsSRGB ? SKColorSpace.CreateSrgb() : null;
            SKImageInfo info = new SKImageInfo(
                fromImage.Width, fromImage.Height,
                SKColorType.Rgba8888, alphaType, useColorSpace);

            byte[]? uncompressedBytes = null;
            using (SKImage tmpImage = SKImage.FromEncodedData(fromImage.AccessDataUnsafe())) {
                using (var tmpBitmap = new SKBitmap(info)) {
                    if (tmpImage.ScalePixels(tmpBitmap.PeekPixels(), SKFilterQuality.None))
                        uncompressedBytes = tmpBitmap.Bytes;
                }
            }
            if (uncompressedBytes == null)
                throw new Exception("ImageUtil.GetDecompressedRGBA8: failed to decompress image data");

            return new PixelImage(fromImage.Width, fromImage.Height, PixelImage.EPixelFormat.RGBA8, uncompressedBytes);
        }

        public static byte[] GetDecompressedRGBA8Bytes(PixelImage fromImage, bool bAsSRGB)
        {
            if (fromImage.Compression == PixelImage.ECompression.Uncompressed &&
                fromImage.Format == PixelImage.EPixelFormat.RGBA8)
                    return fromImage.ExtractBytes();

            PixelImage tmpImage = ImageUtil.GetDecompressedRGBA8(fromImage, true);
            return tmpImage.ExtractBytes(false);
        }


        public static SKImage PixelImageToSKImage(PixelImage Image)
        {
            if (Image.Format != PixelImage.EPixelFormat.Encoded) {
                if (Image.Format != PixelImage.EPixelFormat.RGBA8)
                    throw new NotImplementedException($"ImageUtil.FromPixelImage: unsupported pixel format {Image.Format}");
                var info = new SKImageInfo(Image.Width, Image.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                return SKImage.FromPixelCopy(info, Image.AccessDataUnsafe());
            } else {
                return SKImage.FromEncodedData(Image.AccessDataUnsafe());
            }
        }

        public static byte[] PixelImageToJpeg(PixelImage Image, int Quality = 100)
        {
            if (Image.Compression == PixelImage.ECompression.JPEG)
                return Image.ExtractBytes();

            SKImage skImage = PixelImageToSKImage(Image);
            SKData imageData = skImage.Encode(SKEncodedImageFormat.Jpeg, Quality);
            return imageData.ToArray();
        }

        public static byte[] PixelImageToPng(PixelImage Image, int Quality = 100)
        {
            if (Image.Compression == PixelImage.ECompression.PNG)
                return Image.ExtractBytes();

            SKImage skImage = PixelImageToSKImage(Image);
            SKData imageData = skImage.Encode(SKEncodedImageFormat.Png, Quality);
            return imageData.ToArray();
        }

        public static PixelImage PNGBufferToPixelImage(ReadOnlySpan<byte> imageBytes)
        {
            // this is not efficient...
            SKImage image = SKImage.FromEncodedData(imageBytes);
            return new PixelImage(image.Width, image.Height, imageBytes, PixelImage.EPixelFormat.Encoded, PixelImage.ECompression.PNG);
        }
        public static PixelImage JPEGBufferToPixelImage(ReadOnlySpan<byte> imageBytes)
        {
            SKImage image = SKImage.FromEncodedData(imageBytes);
            return new PixelImage(image.Width, image.Height, imageBytes, PixelImage.EPixelFormat.Encoded, PixelImage.ECompression.JPEG);
        }

        public static PixelImage ImageBytesToPixelImage(in byte[] imageBytes)
        {
            using (Stream memStream = new MemoryStream(imageBytes)) {
                SKCodec codec = SKCodec.Create(memStream);
                int width = codec.Info.Width;
                int height = codec.Info.Height;
                SKEncodedImageFormat format = codec.EncodedFormat;
                if (format == SKEncodedImageFormat.Png) {
                    return new PixelImage(width, height, imageBytes, PixelImage.EPixelFormat.Encoded, PixelImage.ECompression.PNG);
                } else if (format == SKEncodedImageFormat.Jpeg) {
                    return new PixelImage(width, height, imageBytes, PixelImage.EPixelFormat.Encoded, PixelImage.ECompression.JPEG);
                } else
                    throw new NotSupportedException($"[ImageUtil.ImageBytesToPixelImage] image format {format} is not supported");
            }
        }


        public static byte[] PixelImageToMimeData(PixelImage Image, out string mimeType, int Quality = 100)
        {
            if (Image.Compression == PixelImage.ECompression.JPEG) {
                mimeType = "image/jpeg";
                return Image.ExtractBytes();
            }
            if (Image.Compression == PixelImage.ECompression.PNG) {
                mimeType = "image/png";
                return Image.ExtractBytes();
            }
            mimeType = "image/jpeg";
            return PixelImageToJpeg(Image, Quality);
        }
    }
}
