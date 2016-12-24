# Twitch Mosaic
This is a simple program to take all the followers of a twitch channel and put them into a mosaic image.

> Twitch has a limit so you can only get ~1600 followers.

I made all the steps into smaller programs to make it easier to understand for both me and you.

1. Download all the avatars (TwitchDownloadFollowers)
2. Create the mosaic image (CreateMosaicImage)
	This program could be used for other project as well.

## Getting Started

1. Edit ```TwitchClientId``` in _'src\TwitchDownloadFollowers\Build.cs'_.
2. Build solution in Release Mode.
3. Run _'build\build.cmd'_ with the channel name. example _'build {channel}'_.

## Projects
### CreateMosaicImage
Creates a mosaic image of multiple small ones and a pattern image to match. 

### TwitchDownloadFollowers
Download a twitch channels followers avatars and saves them to be used by [CreateMosaicImage]().
