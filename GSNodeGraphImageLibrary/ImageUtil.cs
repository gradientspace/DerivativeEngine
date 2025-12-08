// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
