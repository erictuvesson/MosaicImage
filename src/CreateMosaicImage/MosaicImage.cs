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

    struct ColorValue
    {
        public double R, G, B;

        public float Value()
        {
            return (float)(R + G + B);
        }
    }

    struct ImageData
    {
        // RGB Color values
        public double R, G, B;
        // Image Filepath
        public string ImageFile;

        public float Value()
        {
            return (float)(R + G + B);
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

            Debug.Assert(mosaicPattern.Width == desiredSize);
            Debug.Assert(mosaicPattern.Height == desiredSize);
        }

        /// <summary>
        /// Add and analyze the image.
        /// </summary>
        public async Task<ImageData> AddImage(string filepath)
        {
            var imageData = new ImageData();
            imageData.ImageFile = filepath;

            double aR = 0, aG = 0, aB = 0;
            double pixelCount = 0;

            Image image = null;

            try
            {
                image = Image.Load(filepath);
            }
            catch (Exception e) { }
            
            if (image != null)
            {
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

                imageData.R = aR / pixelCount;
                imageData.G = aG / pixelCount;
                imageData.B = aB / pixelCount;

                images.Add(imageData);
            }

            // TODO: Error
            return imageData;
        }

        public void MakeRandom()
        {
            var rnd = new Random();
            var result = images.OrderBy(item => rnd.Next());

            images = new ConcurrentBag<ImageData>(result);
        }

        public void AnalyzeCells()
        {
            imagesPerRow = (int)Math.Ceiling(Math.Sqrt(images.Count));
            avatarSize = (int)(desiredSize / imagesPerRow);

            this.imageCells = new ColorValue[imagesPerRow, imagesPerRow];

            // loop all the cells
            for (int cellX = 0; cellX < imagesPerRow; cellX++)
            {
                for (int cellY = 0; cellY < imagesPerRow; cellY++)
                {
                    // loop the cell

                    int cellOffsetX = cellX * avatarSize;
                    int cellOffsetY = cellY * avatarSize;

                    var colorValue = new ColorValue();
                    double aR = 0, aG = 0, aB = 0;

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

                    double pixelCount = avatarSize * avatarSize;
                    colorValue.R = aR / pixelCount;
                    colorValue.G = aG / pixelCount;
                    colorValue.B = aB / pixelCount;

                    imageCells[cellX, cellY] = colorValue;
                }
            }
        }
        
        public Image CreateImage()
        {
            var allImages = new List<ImageData>(images);
            if (allImages.Count == 0)
            {
                throw new Exception();
            }

            var imageCellData = new ImageData[imagesPerRow, imagesPerRow];
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
                        var imageDataValue = imageData.Value();

                        var cellAvg = Math.Abs(desiredCellValue - imageDataValue);

                        for (int i = 0; i < allImages.Count; i++)
                        {
                            var currentCellValue = allImages[i].Value();
                            // TODO: This could be improved to match the color better.
                            if (cellAvg > Math.Abs(desiredCellValue - currentCellValue))
                            {
                                imageDataIndex = i;
                                imageData = allImages[i];
                                imageDataValue = imageData.Value();
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
