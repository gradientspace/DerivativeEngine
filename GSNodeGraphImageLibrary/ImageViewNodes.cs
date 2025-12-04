using g3;
using Gradientspace.NodeGraph.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Image
{
    public class PixelImage
    {
        public byte[]? data;
    }


    [NodeFunctionLibrary("Geometry3.Image")]
    [MappedFunctionLibraryName("Geometry3.Image")]
    public static class ImageIOFunctions
    {
        [NodeFunction]
        public static PixelImage LoadImage(string Path = "gradient.png")
        {
            PixelImage image = new PixelImage();
            image.data = File.ReadAllBytes(Path);
            return image;
        }
    }


    public class ImageViewNode : NodeBase
    {
        public override string GetDefaultNodeName() { return "ViewImage"; }

        public const string ImageInputName = "Image";
        public const string ImageOutputName = "Image";

        public ImageViewNode()
        {
            StandardNodeInputBase ImageInput = new StandardNodeInputBase(typeof(PixelImage));
            AddInput(ImageInputName, ImageInput);
            ImageInput.Flags |= ENodeInputFlags.IsInOut;
            ImageInput.Flags |= ENodeInputFlags.HiddenLabel;

            StandardNodeOutputBase ImageOutput = new StandardNodeOutputBase(typeof(PixelImage));
            AddOutput(ImageOutputName, ImageOutput);
            ImageOutput.Flags |= ENodeOutputFlags.HiddenLabel;
        }

        public delegate void ImageViewUpdateEvent(ReadOnlySpan<byte> ImageBytes);
        public event ImageViewUpdateEvent? OnImageUpdate;

        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(ImageOutputName);
            if (OutputIndex == -1)
                throw new Exception($"{GetDefaultNodeName()}: output not found");

            object? foundData = DataIn.FindItemValue(ImageInputName);
            if (foundData is PixelImage image && image.data != null) {

                OnImageUpdate?.Invoke(image.data.AsSpan<byte>());
                RequestedDataOut.SetItem(OutputIndex, ImageOutputName, image);

            } else
                throw new Exception($"{GetDefaultNodeName()}: input image not found or invalid");
        }

    }

}