# Mosaic Image
This is a simple program to take all the followers of a twitch channel or a twitter account and put them into a mosaic image following a color pattern.

> Twitch has a limit so you can only get ~1600 followers.

> Twitter has a limit of 5k.

## Getting Started

1. Edit _'src\TwitchDownloadFollowers\Build.cs'_ or _'src\TwitterDownloadFollowers\Build.cs'_ depending on which program you are going to use.
2. Build solution in Release Mode.
3. Run _'build\build_twitch.cmd'_ or _'build\build_twitter.cmd'_ with the account name. example _'build {account}'_.

## Projects
### CreateMosaicImage
Creates a mosaic image of multiple small ones and a pattern image to match. 

### TwitchDownloadFollowers
Download a twitch channels followers avatars and saves them to be used by [CreateMosaicImage]().

### TwitterDownloadFollowers
Download a twitter accounts followers avatars and saves them to be used by [CreateMosaicImage]().
