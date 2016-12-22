namespace CreateMosaicImage
{
    using Nine.Imaging;
    using Nine.Imaging.Filtering;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Color Value, this makes it easier to compare cell and 
    /// image average color.
    /// </summary>
    struct ColorValue
    {
        public readonly double R, G, B;

        public ColorValue(double r, double g, double b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        public float Value()
        {
            return (float)(R + G + B);
        }
    }

    /// <summary>
    /// Saves the average color of a image while not keeping the image in memory.
    /// </summary>
    struct ImageData
    {
        public readonly ColorValue Color;
        public readonly string ImageFile;
        
        public ImageData(ColorValue color, string imageFile)
        {
            this.Color = color;
            this.ImageFile = imageFile;
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
            Debug.Assert(mosaicPattern.Width == desiredSize);
            Debug.Assert(mosaicPattern.Height == desiredSize);
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

                images.Add(imageData);
            }

            // TODO: Error
            return new ImageData();
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

            this.imageCells = new ColorValue[imagesPerRow, imagesPerRow];

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
                throw new NotSupportedException("There are not inputed images.");
            }

            // Create the image byte array.
            var imageBytes = new byte[desiredSize * desiredSize * 4];

            // loop all the cells
            for (int cellX = 0; cellX < imagesPerRow; cellX++)
            {
                for (int cellY = 0; cellY < imagesPerRow; cellY++)
                {
                    var cellData = imageCells[cellX, cellY];
                    var desiredCellValue = cellData.Value();

                    int cellOffsetX = cellX * avatarSize;
                    int cellOffsetY = cellY * avatarSize;

                    if (allImages.Count > 0)
                    {
                        // find the closest matching image to this cell
                        var imageDataIndex = 0;
                        var imageData = allImages[imageDataIndex];
                        var imageDataValue = imageData.Color.Value();

                        var cellAvg = Math.Abs(desiredCellValue - imageDataValue);

                        for (int i = 0; i < allImages.Count; i++)
                        {
                            var currentCellValue = allImages[i].Color.Value();
                            // TODO: This could be improved to match the color better.
                            if (cellAvg > Math.Abs(desiredCellValue - currentCellValue))
                            {
                                imageDataIndex = i;
                                imageData = allImages[i];
                                imageDataValue = imageData.Color.Value();
                            }
                        }

                        allImages.RemoveAt(imageDataIndex);

                        // Add the image to the cell
                        var avatarImage = Image.Load(imageData.ImageFile);
                        avatarImage = avatarImage.Resize(avatarSize);

                        // TODO: Ensure that the avatar image is the same as avatarSize

                        for (int x = 0; x < avatarSize; x++)
                        {
                            for (int y = 0; y < avatarSize; y++)
                            {
                                var pixel = avatarImage[x, y];

                                int newX = cellOffsetX + x;
                                int newY = cellOffsetY + y;

                                int offset = ((cellOffsetY + y) * mosaicPattern.Width + (cellOffsetX + x)) * 4;
                                imageBytes[offset + 0] = pixel.B;
                                imageBytes[offset + 1] = pixel.G;
                                imageBytes[offset + 2] = pixel.R;
                                imageBytes[offset + 3] = pixel.A;
                            }
                        }
                    }
                    else
                    {
                        // This happends if we are out of images.

                        // Write the average cell color as image instead
                        for (int x = 0; x < avatarSize; x++)
                        {
                            for (int y = 0; y < avatarSize; y++)
                            {
                                int newX = cellOffsetX + x;
                                int newY = cellOffsetY + y;

                                int offset = ((cellOffsetY + y) * mosaicPattern.Width + (cellOffsetX + x)) * 4;
                                imageBytes[offset + 0] = (byte)(cellData.B * 255);
                                imageBytes[offset + 1] = (byte)(cellData.G * 255);
                                imageBytes[offset + 2] = (byte)(cellData.R * 255);
                                imageBytes[offset + 3] = 255;
                            }
                        }
                    }
                }
            }

            return new Image(desiredSize, desiredSize, imageBytes);
        }
    }
}
