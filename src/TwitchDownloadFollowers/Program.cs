namespace TwitchDownloadFollowers
{
    using CommandLine;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using TwitchCSharp.Clients;
    using TwitchCSharp.Helpers;
    using TwitchCSharp.Models;

    /// <summary>
    /// Command Line Arguments
    /// </summary>
    public class Options : Common.BasicDownloaderOptions
    {
        [Option('c', "channel", Required = true, HelpText = "They Twitch Channel we are targeting.")]
        public string TargetChannel { get; set; }

        [Option("max", Required = true, HelpText = "Max amount of followers we are fetching.")]
        public int MaxFollowers { get; set; }

        [Option("OutputFollowers", HelpText = "Output all the followers into a textfile.")]
        public bool OutputFollowersList { get; set; }

        public string OutputFollowersListFile = "Followers.txt";
        
        public Options()
        {
            this.TargetChannel = "";
            this.MaxFollowers = 1600; //< https://github.com/justintv/Twitch-API/issues/320
            this.OutputFollowersList = true;
        }
    }

    class Program
    {
        static ConcurrentBag<Follower> Followers = new ConcurrentBag<Follower>();

        static int Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            if (result.Errors.Count() > 0)
            {
                return 1;
            }
            
            if (string.IsNullOrEmpty(result.Value.OutputPattern))
            {
                result.Value.OutputPattern = $"{result.Value.TargetChannel}_pattern";
            }
            
            var client = new TwitchReadOnlyClient(Build.TwitchClientId);
            var stopwatch = new Stopwatch();

            // Notify that we start are collecting the followers
            Console.WriteLine($"Fetching all(max: {result.Value.MaxFollowers}) the followers. @{result.Value.TargetChannel}");
            stopwatch.Start();

            // Get all the followers.
            var totalFollowers = FetchFollowers(client, result.Value);

            // Raport how many followers we collected and display the diagnostics time.
            stopwatch.Stop();
            Console.WriteLine($"{Followers.Count} of {totalFollowers} followers gathered. {stopwatch.Elapsed.TotalSeconds} sec");

            // Directory where we are saving the data
            string dir = string.IsNullOrEmpty(result.Value.OutputDirectory) ? $"{result.Value.TargetChannel}_data" : result.Value.OutputDirectory;
            string dirAvatars = Path.Combine(dir, "Avatars");
            string dirAvatarsAbsPath = Path.GetFullPath(dirAvatars);

            // Create the directories
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(dirAvatars);

            // Write all the followers display names into a text file.
            if (result.Value.OutputFollowersList)
            {
                Task.Run(() => SaveFollowersToText(Path.Combine(dir, result.Value.OutputFollowersListFile)));
            }

            stopwatch.Restart();

            // Download channel image as pattern
            var targetChannel = client.GetChannel(result.Value.TargetChannel);
            DownloadAvatar(targetChannel.Logo, dir, result.Value);

            // Download all the follower avatars
            var avatars = DownloadAvatars(dirAvatarsAbsPath);

            // Raport all the end results.
            stopwatch.Stop();

            Console.WriteLine($"{avatars.Count} of {Followers.Count}({totalFollowers}) followers has avatars. {stopwatch.Elapsed.TotalSeconds} sec");

            return 0;
        }

        /// <summary>
        /// Fetch all the twitch followers.
        /// </summary>
        /// <returns> followers the channel has. </returns>
        static int FetchFollowers(TwitchReadOnlyClient client, Options options)
        {
            // Get the amount of followers the channel has
            var initFollowersData = client.GetFollowers(options.TargetChannel, new PagingInfo() { PageSize = 0 });

            var page = new PagingInfo()
            {
                Page = 1,
                PageSize = 100
            };

            // Collect all the followers.
            var pageCount = (int)Math.Ceiling(initFollowersData.Total / (float)page.PageSize);
            for (int i = 0; i < pageCount; i++)
            {
                // TODO: Could improve this to make it more precise
                if (Followers.Count > options.MaxFollowers)
                    break;

                // TODO: Could I make the requests in parallel?
                var pagedFollower = client.GetFollowers(options.TargetChannel, page);
                if (string.IsNullOrEmpty(pagedFollower.Error))
                {
                    foreach (var item in pagedFollower.List)
                        Followers.Add(item);
                }

                page.Page++;
            }

            return (int)initFollowersData.Total;
        }

        static void SaveFollowersToText(string filepath)
        {
            using (var file = new StreamWriter(filepath))
            {
                foreach (var follower in Followers)
                {
                    file.WriteLine(follower.User.DisplayName);
                }
            }
        }

        /// <summary> 
        /// Download all the follower avatars. 
        /// </summary>
        /// <returns> the followers that has avatars. </returns>
        static ConcurrentBag<string> DownloadAvatars(string avatarsSavePath)
        {
            var avatars = new ConcurrentBag<string>();
            Parallel.ForEach(Followers, follower =>
            {
                var logo = follower.User.Logo;
                if (!string.IsNullOrEmpty(logo))
                {
                    string extension = Path.GetExtension(logo);
                    string saveLocation = Path.Combine(avatarsSavePath, $"{follower.User.DisplayName}{extension}");

                    WebClient webClient = new WebClient();
                    webClient.DownloadFile(logo, saveLocation);

                    avatars.Add(follower.User.DisplayName);
                }
            });
            return avatars;
        }

        static void DownloadAvatar(string logo, string saveFilepath, Options options)
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
