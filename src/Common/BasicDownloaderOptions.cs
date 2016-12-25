namespace Common
{
    using CommandLine;
    using System.IO;
    using System.Net;

    public abstract class BasicDownloaderOptions
    {
        [Option("Output", HelpText = "Output directory.")]
        public string OutputDirectory { get; set; }

        [Option("OutputPattern", HelpText = "Output pattern image.")]
        public string OutputPattern { get; set; }

        public BasicDownloaderOptions()
        {
            this.OutputDirectory = string.Empty;
            this.OutputPattern = string.Empty;
        }

        public static void DownloadAvatar(string logo, string saveFilepath, BasicDownloaderOptions options)
        {
            if (!string.IsNullOrEmpty(logo))
            {
                string extension = Path.GetExtension(logo);
                string saveLocation = Path.Combine(saveFilepath, $"{options.OutputPattern}{extension}");

                WebClient webClient = new WebClient();
                webClient.DownloadFile(logo, saveLocation);
            }
        }
    }
}
