namespace Common
{
    using CommandLine;

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
    }
}
