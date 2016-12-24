namespace CreateMosaicImage
{
    using Nine.Imaging;
    using System;

    static class ImageExtensions
    {
        /// <summary>
        /// Draws a image to a byte array.
        /// </summary>
        public static void DrawImageToByteArray(this Image image, int destOffsetX, int destOffsetY, int destWidth, ref byte[] destImageArray)
        {
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    int newX = destOffsetX + x;
                    int newY = destOffsetY + y;

                    var pixel = image[x, y];

                    int offset = (newY * destWidth + newX) * 4;
                    destImageArray[offset + 0] = pixel.B;
                    destImageArray[offset + 1] = pixel.G;
                    destImageArray[offset + 2] = pixel.R;
                    destImageArray[offset + 3] = pixel.A;
                }
            }
        }

        /// <summary>
        /// Draws a solid color to a byte array.
        /// </summary>
        public static void DrawColorToByteArray(byte r, byte g, byte b, int srcWidth, int srcHeight, int destOffsetX, int destOffsetY, int destWidth, ref byte[] destImageArray)
        {
            for (int x = 0; x < srcWidth; x++)
            {
                for (int y = 0; y < srcHeight; y++)
                {
                    int newX = destOffsetX + x;
                    int newY = destOffsetY + y;

                    int offset = (newY * destWidth + newX) * 4;
                    destImageArray[offset + 0] = r;
                    destImageArray[offset + 1] = g;
                    destImageArray[offset + 2] = b;
                    destImageArray[offset + 3] = 255;
                }
            }
        }

        public static Tuple<float, float, float> AnalyzeImage(this Image image)
        {
            throw new NotImplementedException();
        }
    }
}
