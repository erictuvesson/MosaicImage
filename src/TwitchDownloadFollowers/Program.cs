namespace TwitchDownloadFollowers
{
    using CommandLine;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using TwitchCSharp.Clients;
    using TwitchCSharp.Helpers;
    using TwitchCSharp.Models;

    public class Options
    {
        [Option('c', "channel", Required = true, HelpText = "They Twitch Channel we are targeting.")]
        public string TargetChannel { get; set; }

        [Option("Output", HelpText = "Output directory.")]
        public string OutputDirectory { get; set; }

        [Option("OutputFollowers", HelpText = "Output all the followers into a textfile.")]
        public bool OutputFollowersList { get; set; }

        public string OutputFollowersListFile = "Followers.txt";
        
        public Options()
        {
            this.TargetChannel = "";
            this.OutputDirectory = string.Empty;
            this.OutputFollowersList = true;
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
            
            // Create client and diagnostics
            var stopwatch = new Stopwatch();
            var client = new TwitchReadOnlyClient(Common.Build.TwitchClientId);

            // Get the amount of followers the channel has
            var initFollowersData = client.GetFollowers(result.Value.TargetChannel, new PagingInfo() { PageSize = 0 });

            var page = new PagingInfo() { Page = 1 };
            var followers = new List<Follower>();

            // Notify that we start collecting
            Console.WriteLine("Fetching all the followers.");
            Console.Title = "0.0%";
            stopwatch.Start();

            // Collect all the followers.
            int pageCount = (int)(initFollowersData.Total / page.PageSize);
            for (int i = 0; i < pageCount; i++)
            {
                // TODO: Could I make this in parallel?
                var pagedFollower = client.GetFollowers(result.Value.TargetChannel, page);
                if (string.IsNullOrEmpty(pagedFollower.Error))
                {
                    followers.AddRange(pagedFollower.List);
                }
                else
                {
                    Console.WriteLine($"ERROR: {pagedFollower.Error}");
                }

                page.Page++;
            }

            // Directory where we are saving the data
            string dir = $"{result.Value.TargetChannel}_data"; // TODO: OutputDirectory
            string dirAvatars = Path.Combine(dir, "Avatars");
            string dirAvatarsAbsPath = Path.GetFullPath(dirAvatars);

            // Create the directories
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(dirAvatars);

            // Write all the follower display names into a text file.
            Task.Run(() =>
            {
                using (var file = new StreamWriter(Path.Combine(dir, result.Value.OutputFollowersListFile)))
                {
                    foreach (var follower in followers)
                    {
                        file.WriteLine(follower.User.DisplayName);
                    }
                }
            });

            // Raport how many followers we collected and display the diagnostics time.
            stopwatch.Stop();
            Console.WriteLine($"{followers.Count} of {initFollowersData.Total} followers gathered. {stopwatch.Elapsed.TotalSeconds} sec");
            stopwatch.Restart();

            // Download all the follower avatars
            var avatars = DownloadAvatars(followers, dirAvatarsAbsPath, progress =>
            {
                string proc = string.Format("{0:00.0}%", progress);
                Console.Title = proc;
            });

            // Raport all the end results.
            stopwatch.Stop();
            Console.Title = "100.0%";

            Console.WriteLine($"{avatars.Count} of {initFollowersData.Total} followers has avatars. {stopwatch.Elapsed.TotalSeconds} sec");
            
            return 0;
        }

        /// <summary> Download all the follower avatars. </summary>
        /// <returns>the followers that has avatars. </returns>
        static List<string> DownloadAvatars(List<Follower> followers, string avatarsSavePath, Action<float> progressCallback)
        {
            // TODO: Download all the image using concurrency 
            var avatars = new List<string>();
            for (var i = 0; i < followers.Count; i++)
            {
                var follower = followers[i];
                var logo = follower.User.Logo;
                if (!string.IsNullOrEmpty(logo))
                {
                    string saveLocation = Path.Combine(avatarsSavePath, $"{follower.User.DisplayName}.jpeg");

                    WebClient webClient = new WebClient();
                    webClient.DownloadFile(logo, saveLocation);

                    avatars.Add(follower.User.DisplayName);
                }

                // Raport every 10 follower so it doesnt take to much performance
                if ((i % 10) == 0)
                {
                    progressCallback(((float)i / (float)followers.Count) * 100.0f);
                }
            }

            return avatars;
        }
    }
}
