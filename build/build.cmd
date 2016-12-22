@echo off

SET BIN_PATH=bin\release

SET ARG1=%1

echo
echo -- Download all the followers avatars. @%ARG1%
"%BIN_PATH%\TwitchDownloadFollowers.exe" -c %ARG1% 

echo
echo -- Build the mosaic image.
"%BIN_PATH%\CreateMosaicImage.exe" --input %ARG1%_data\Avatars --inputImage %ARG1%_data\%ARG1%.jpeg --output %ARG1%_data\%ARG1%.png

echo
"%ARG1%_data\%ARG1%.png"
