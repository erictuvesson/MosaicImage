namespace TwitterDownloadFollowers
{
    using CommandLine;
    using System.Linq;

    /// <summary>
    /// Command Line Arguments
    /// </summary>
    public class Options
    {
        public Options()
        {

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


            return 0;
        }
    }
}
