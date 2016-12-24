namespace CreateMosaicImage
{
    using MoreLinq;
    using Nine.Imaging;
    using Nine.Imaging.Filtering;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Color Value, this makes it easier to compare cell and 
    /// image average color.
    /// </summary>
    struct ColorValue : IComparable<ColorValue>
    {
        public readonly double R, G, B;

        public ColorValue(double r, double g, double b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        public int CompareTo(ColorValue other)
        {
            return (this.Length() > other.Length()) ? 1 : -1;
        }

        public double Length()
        {
            return (this.R * this.R) + (this.G * this.G) + (this.B * this.B);
        }

        public static ColorValue operator -(ColorValue value1, ColorValue value2)
        {
            return new ColorValue(value1.R - value2.R, value1.G - value2.G, value1.B - value2.B);
        }

        public static bool operator <=(ColorValue value1, ColorValue value2) { return value1.Length() <= value2.Length(); }
        public static bool operator >=(ColorValue value1, ColorValue value2) { return value1.Length() >= value2.Length(); }
    }

    /// <summary>
    /// Saves the average color of a image while not keeping the image in memory.
    /// </summary>
    class ImageData : IComparable<ImageData>
    {
        public readonly ColorValue Color;
        public readonly string ImageFile;
        
        public ImageData(ColorValue color, string imageFile)
        {
            this.Color = color;
            this.ImageFile = imageFile;
        }

        public int CompareTo(ImageData other)
        {
            return Color.CompareTo(other.Color);
        }
    }

    class MosaicImage
    {
        private int desiredSize;
        private Image mosaicPattern;
        private ConcurrentBag<ImageData> images;
        private ColorValue[,] imageCells;
        private int imagesPerRow;
        private int avatarSize;
        
        public MosaicImage(Image mosaicPattern, int desiredSize)
        {
            this.images = new ConcurrentBag<ImageData>();
            this.mosaicPattern = mosaicPattern;
            this.desiredSize = desiredSize;

            // We could resize the pattern in here.
            System.Diagnostics.Debug.Assert(mosaicPattern.Width == desiredSize);
            System.Diagnostics.Debug.Assert(mosaicPattern.Height == desiredSize);
        }

        /// <summary>
        /// Add and analyze the image.
        /// </summary>
        public async Task<ImageData> AddImage(string filepath)
        {
            double aR = 0, aG = 0, aB = 0;
            double pixelCount = 0;

            Image image = null;

            // Try to load the image.
            try { image = Image.Load(filepath); }
            catch (Exception e) { }
            
            if (image != null)
            {
                // Forloop every pixel in the image
                for (int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        int offset = (y * image.Width + x) * 4;

                        byte r = image.Pixels[offset + 0];
                        byte g = image.Pixels[offset + 1];
                        byte b = image.Pixels[offset + 2];

                        aR += r / 255.0f;
                        aG += g / 255.0f;
                        aB += b / 255.0f;

                        pixelCount++;
                    }
                }
                
                var imageData = new ImageData(new ColorValue(aR / pixelCount, aG / pixelCount, aB / pixelCount), filepath);
                images.Add(imageData);
            }

            return null;
        }

        /// <summary>
        /// Mostly just for debugging.
        /// </summary>
        public void ShuffleImages()
        {
            var rnd = new Random();
            var result = images.OrderBy(item => rnd.Next());

            this.images = new ConcurrentBag<ImageData>(result);
        }
        
        /// <summary>
        /// Analyze the pattern cells.
        /// This should be called after all the images are added so we know
        /// how many cells there should be.
        /// 
        /// TODO: This should be made private, then we lose some of the diagnostics.
        /// </summary>
        public void AnalyzeCells()
        {
            imagesPerRow = (int)Math.Ceiling(Math.Sqrt(images.Count));
            avatarSize = (int)(desiredSize / imagesPerRow);

            this.imageCells = new ColorValue[imagesPerRow + 1, imagesPerRow + 1];

            double cellPixelCount = avatarSize * avatarSize;

            // loop all the cells
            for (int cellX = 0; cellX < imagesPerRow; cellX++)
            {
                for (int cellY = 0; cellY < imagesPerRow; cellY++)
                {
                    int cellOffsetX = cellX * avatarSize;
                    int cellOffsetY = cellY * avatarSize;

                    double aR = 0, aG = 0, aB = 0;

                    // loop the cell
                    for (int x = 0; x < avatarSize; x++)
                    {
                        for (int y = 0; y < avatarSize; y++)
                        {
                            int offset = ((cellOffsetY + y) * mosaicPattern.Width + (cellOffsetX + x)) * 4;

                            byte r = mosaicPattern.Pixels[offset + 0];
                            byte g = mosaicPattern.Pixels[offset + 1];
                            byte b = mosaicPattern.Pixels[offset + 2];

                            aR += r / 255.0f;
                            aG += g / 255.0f;
                            aB += b / 255.0f;
                        }
                    }

                    imageCells[cellX, cellY] = new ColorValue(aR / cellPixelCount, aG / cellPixelCount, aB / cellPixelCount);
                }
            }
        }
        
        /// <summary>
        /// Create the mosaic image.
        /// </summary>
        public Image CreateImage()
        {
            // Create a copy of all the imagedata structs.
            var allImages = new List<ImageData>(images);
            if (allImages.Count == 0)
            {
                throw new NotSupportedException("There are not inputted images.");
            }

            // Sort all the images.
            //allImages.Sort();

            // Create the image byte array.
            var imageBytes = new byte[desiredSize * desiredSize * 4];

            // loop all the cells

            LoopPattern pattern = new SpiralLoopPattern(imagesPerRow, imagesPerRow,
            (item) =>
            {
                var cellDesiredColor = imageCells[item.X, item.Y];

                int cellOffsetX = item.X * avatarSize;
                int cellOffsetY = item.Y * avatarSize;

                if (allImages.Count > 0)
                {
                    // Find the closest matching image to this cell
                    var closest = allImages.MinBy(n => Math.Abs((cellDesiredColor - n.Color).Length()));
                    if (closest != null)
                    {
                        // Remove it so we wont use it again.
                        allImages.Remove(closest); //< TODO: Optimize

                        // Add the image to the cell
                        var avatarImage = Image.Load(closest.ImageFile);
                        avatarImage = avatarImage.Resize(avatarSize);

                        // Insert the avatar into the image.
                        avatarImage.DrawImageToByteArray(cellOffsetX, cellOffsetY, mosaicPattern.Width, ref imageBytes);
                    }
                    else
                    {
                        // This should not happen.
                        System.Diagnostics.Debugger.Break();
                    }
                }
                else
                {
                    // This happends if we are out of images.

                    // Write the average cell color as image instead
                    ImageExtensions.DrawColorToByteArray(
                        (byte)(cellDesiredColor.B * 255), (byte)(cellDesiredColor.G * 255), (byte)(cellDesiredColor.R * 255),
                        avatarSize, avatarSize, cellOffsetX, cellOffsetY, mosaicPattern.Width, ref imageBytes);
                }
            });

            pattern.Execute();
            
            return new Image(desiredSize, desiredSize, imageBytes);
        }
    }
}
