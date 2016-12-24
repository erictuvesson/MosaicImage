@echo off

SET BIN_PATH=bin\release

SET ARG1=%1

echo
echo -- Download all the followers avatars.
"%BIN_PATH%\twitter\TwitterDownloadFollowers.exe" -t %ARG1% 

echo
echo -- Build the mosaic image. (Lets hope the pattern is a .jpeg)
"%BIN_PATH%\CreateMosaicImage.exe" --input %ARG1%_data\Avatars --inputImage %ARG1%_data\%ARG1%_pattern.jpeg --output %ARG1%_data\%ARG1%.png

echo
"%ARG1%_data\%ARG1%.png"
