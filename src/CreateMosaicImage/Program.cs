namespace CreateMosaicImage
{
    using CommandLine;
    using Nine.Imaging;
    using Nine.Imaging.Filtering;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Command Line Arguments
    /// </summary>
    public class MosaicOptions
    {
        [Option('i', "input", Required = true, HelpText = "Input directory.")]
        public string InputDirectory { get; set; }

        [Option("inputImage", Required = true, HelpText = "Input Mosaic Image.")]
        public string InputImage { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output file. (example.png)")]
        public string OutputFile { get; set; }

        [Option('s', "size", HelpText = "Desired output size.")]
        public int DesiredSize { get; set; }

        public MosaicOptions()
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
            // Parse arguments
            var result = Parser.Default.ParseArguments<MosaicOptions>(args);
            if (result.Errors.Count() > 0)
            {
                // Failed to parse arguments
                return 1;
            }

            // Start program
            var stopwatch = new Stopwatch();

            // Get all the files in the input directory.
            var files = Directory.GetFiles(result.Value.InputDirectory);

            // Check if pattern image exists
            if (!File.Exists(result.Value.InputImage))
            {
                // TODO: Ask to create without pattern.
                return 1;
            }

            // Load the pattern image.
            var mosaicPattern = Image.Load(result.Value.InputImage);

            // Resize the pattern image to the desired size, 
            // this could be _optimized_ to just resize to the cell numbers.
            mosaicPattern = mosaicPattern.Resize(result.Value.DesiredSize);

            // Create mosaic image
            var mosaicImage = new MosaicImage(mosaicPattern, result.Value.DesiredSize);

            Console.WriteLine("Analyzing all the avatars.");
            stopwatch.Start();

            var tasks = new List<Task>();
            foreach (var filepath in files)
            {
                tasks.Add(mosaicImage.AddImage(filepath));
            }

            Task.WaitAll(tasks.ToArray());

            // This is not necessary!
            mosaicImage.ShuffleImages();

            stopwatch.Stop();
            Console.WriteLine($"Analyzed all the avatars. {stopwatch.Elapsed.TotalSeconds} sec");
            stopwatch.Restart();

            mosaicImage.AnalyzeCells();

            stopwatch.Stop();
            Console.WriteLine($"Analyzed pattern. {stopwatch.Elapsed.TotalSeconds} sec");

            // Save the mosaic image
            var failedFiles = new List<string>();

            stopwatch.Restart();
            Console.WriteLine($"Creating mosaic image.");

            var image = mosaicImage.CreateImage();

            stopwatch.Stop();
            Console.WriteLine($"Created mosaic image. {stopwatch.Elapsed.TotalSeconds} sec");

            using (var stream = new FileStream(result.Value.OutputFile, FileMode.Create))
            {
                image.SaveAsPng(stream);
            }

            Console.WriteLine($"Saved image '{result.Value.OutputFile}'");

            // Write out all the avatars we failed on.
            if (failedFiles.Count > 0)
            {
                Console.WriteLine($"Failed to load {failedFiles.Count} images. Saved errors to 'FailedAvatars.txt'.");
                using (var file = new StreamWriter(Path.Combine(result.Value.OutputFile, "FailedAvatars.txt")))
                    foreach (var item in failedFiles)
                        file.WriteLine(item);
            }

            //Console.WriteLine($"Added {success} of {files.Length} images.");
            
            return 0;
        }
    }
}
