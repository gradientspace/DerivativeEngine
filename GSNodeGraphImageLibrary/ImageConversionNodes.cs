using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Image
{

    [NodeFunctionLibrary("Image")]
    [MappedFunctionLibraryName("Geometry3.Image")]
    public static class ImageConversionFunctions
    {
        [NodeFunction]
        public static void GetImageBytes(ref PixelImage Image, out int Width, out int Height, out byte[] Bytes, bool bSRGB = true)
        {
            Width = Image.Width;
            Height = Image.Height;
            Bytes = ImageUtil.GetDecompressedRGBA8Bytes(Image, bSRGB);
        }

        [NodeFunction]
        public static void SetImageBytes(ref PixelImage Image, byte[] RGBAData)
        {
            Image.UpdateFromBytes(RGBAData);
        }

    }

}
