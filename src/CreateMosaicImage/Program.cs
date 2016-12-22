namespace CreateMosaicImage
{
    using CommandLine;
    using Nine.Imaging;
    using Nine.Imaging.Filtering;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "Input directory.")]
        public string InputDirectory { get; set; }

        [Option("inputImage", Required = true, HelpText = "Input Mosaic Image.")]
        public string InputImage { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output file. (example.png)")]
        public string OutputFile { get; set; }

        [Option('s', "size", HelpText = "Desired output size.")]
        public int DesiredSize { get; set; }

        public Options()
        {
            this.OutputFile = "mosaic.png";
            this.InputImage = "input.png";
            this.DesiredSize = 4096;
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            if (result.Errors.Count() > 0)
            {
                return 1;
            }

            // Start program
            var failedFiles = new List<string>();
            var files = Directory.GetFiles(result.Value.InputDirectory);

            var mosaicPattern = Image.Load(result.Value.InputImage);
            mosaicPattern = mosaicPattern.Resize(result.Value.DesiredSize);

            // Create mosaic image
            var mosaicImage = new MosaicImage(mosaicPattern, result.Value.DesiredSize);

            var tasks = new List<Task>();
            foreach (var filepath in files)
            {
                tasks.Add(mosaicImage.AddImage(filepath));
            }

            Task.WaitAll(tasks.ToArray());

            mosaicImage.MakeRandom();
            mosaicImage.AnalyzeCells();

            // Save the mosaic image
            var image = mosaicImage.CreateImage();
            using (var stream = new FileStream(result.Value.OutputFile, FileMode.Create))
            {
                image.SaveAsPng(stream);
            }

            // Write out all the avatars we failed on.
            if (failedFiles.Count > 0)
            {
                using (var file = new StreamWriter(Path.Combine(result.Value.OutputFile, "FailedAvatars.txt")))
                    foreach (var item in failedFiles)
                        file.WriteLine(item);
            }

            //Console.WriteLine($"Added {success} of {files.Length} images.");
            
            return 0;
        }
    }
}

/*

            // Calculate how many images we are going to use per row and their size.
            var imagesPerRow = Math.Ceiling(Math.Sqrt(files.Length));
            var avatarSize = (int)(result.Value.DesiredSize / imagesPerRow);

            // Create mosaic image
            byte[] imageBytes = new byte[result.Value.DesiredSize * result.Value.DesiredSize * 4];

            // loop all the images
            int success = 0, offsetX = 0, offsetY = 0;
            foreach (var filepath in files)
            {
                Image avatar = null;

                try
                {
                    avatar = Image.Load(File.OpenRead(filepath));
                    avatar = avatar.Resize(avatarSize);
                }
                catch (Exception e)
                {
                    failedFiles.Add(Path.GetFileName(filepath) + " : " + e.Message);
                }

                if (avatar == null)
                {
                    for (int x = 0; x < avatar.Width; x++)
                    {
                        for (int y = 0; y < avatar.Height; y++)
                        {
                            var pixel = avatar[x, y];

                            int newX = offsetX + x;
                            int newY = offsetY + y;

                            int offset = (newY * result.Value.DesiredSize + newX) * 4;
                            imageBytes[offset + 0] = pixel.B;
                            imageBytes[offset + 1] = pixel.G;
                            imageBytes[offset + 2] = pixel.R;
                            imageBytes[offset + 3] = pixel.A;
                        }
                    }

                    success++;

                    offsetX += avatar.Width;
                    if (offsetX >= result.Value.DesiredSize) // this should always be equal and not go beyond.
                    {
                        offsetX = 0;
                        offsetY += avatar.Height;
                    }
                }
            }

 */
