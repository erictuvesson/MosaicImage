# Twitch Mosaic
This is a simple program to take all the followers of a twitch channel and put them into a mosaic image.

I made all the steps into smaller programs to make it easier to understand for both me and you.

1. Download all the avatars (TwitchDownloadFollowers)
2. Create the mosaic image (CreateMosaicImage)
	This program could be used for other project as well.

## Getting Started

1. Add a Twitch App ClientId 'src\Common\Build.cs' key to TwitchClientId.
2. Build project in Release Mode
3. Go to 'build\' and create a folder called {channel}_data and place the mosaic texture in there.
4. Run 'build\build.cmd' with the channel name. example 'build {channel}', it would be good to have more then 2k followers to get a better image.
