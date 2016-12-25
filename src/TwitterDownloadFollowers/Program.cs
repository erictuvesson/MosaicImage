namespace TwitterDownloadFollowers
{
    using CommandLine;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Tweetinvi;

    /// <summary>
    /// Command Line Arguments
    /// </summary>
    public class Options : Common.BasicDownloaderOptions
    {
        [Option('t', "target", Required = true, HelpText = "They Twitter Account we are targeting.")]
        public string TargetAccount { get; set; }

        public Options()
        {
            this.TargetAccount = string.Empty;
        }
    }

    class Program
    {
        // Use to debug Twitterinvi : ExceptionHandler.GetLastException()

        static int Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            if (result.Errors.Count() > 0)
            {
                return 1;
            }

            // Authenticate to Twitter.
            // TODO: Is this needed for this task? 
            Auth.SetUserCredentials(Build.ConsumerKey, Build.ConsumerSecret, Build.UserAccessToken, Build.UserAccessSecret);

            // Wait for authenticated user.
            var authenticatedUser = UserAsync.GetAuthenticatedUser();
            authenticatedUser.Wait();

            // Validate that we authenticated.
            if (authenticatedUser.Result == null)
            {
                Console.WriteLine("Failed to authenticate to Twitter.");
                return 1;
            }

            // Get the twitter account.
            var user = User.GetUserFromScreenName(result.Value.TargetAccount);
            if (user == null)
            {
                Console.WriteLine("Cannot find Twitter account.");
                return 1;
            }

            Console.WriteLine($"{result.Value.TargetAccount} has {user.FollowersCount} followers.");

            // Directory where we are saving the data
            string dir = string.IsNullOrEmpty(result.Value.OutputDirectory) ? $"{result.Value.TargetAccount}_data" : result.Value.OutputDirectory;
            string dirAvatars = Path.Combine(dir, "Avatars");
            string dirAvatarsAbsPath = Path.GetFullPath(dirAvatars);

            // Create the directories
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(dirAvatars);

            // Download the targeted account image as pattern.
            Common.BasicDownloaderOptions.DownloadAvatar(user.ProfileImageUrl400x400, dir, result.Value);

            // Get the account followers.
            var followers = new List<long>(user.GetFollowerIds());
            int followerGatheredCount = followers.Count;

            // Download all the logos
            int successLogos = 0;

            int followersPerPage = 50;
            int pages = (int)Math.Ceiling(followers.Count / (float)followersPerPage);
            for (int i = 0; i < pages; i++)
            //Parallel.For(0, pages, i =>
            {
                var pageFollowers = followers.Take(followersPerPage);
                followers.RemoveRange(0, Math.Min(followers.Count, followersPerPage));

                var actualFollowers = User.GetUsersFromIds(pageFollowers);

                int sessionSuccess = 0;
                foreach (var follower in actualFollowers)
                {
                    if (!follower.DefaultProfile) //< TODO: Not always true
                    {
                        sessionSuccess++;

                        var logo = follower.ProfileImageUrl400x400;

                        string extension = Path.GetExtension(logo);
                        string saveLocation = Path.Combine(dirAvatarsAbsPath, $"{follower.ScreenName}{extension}");

                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(logo, saveLocation);
                    }
                }

                Interlocked.Add(ref successLogos, sessionSuccess);
            }

            Console.WriteLine($"{successLogos} of {followerGatheredCount}({user.FollowersCount}) followers has avatars.");

            return 0;
        }
    }
}
