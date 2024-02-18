#/bin/sh
export LocalAppData="/mnt/mediaSSD/SteamLibrary/steamapps/compatdata/489830/pfx/drive_c/users/steamuser/AppData/Local/"
project="ComicsCreator/ComicsCreator.csproj"
game="SkyrimSE"
data="/mnt/mediaSSD/SteamLibrary/steamapps/common/Skyrim Special Edition/Data"
dotnet run --project "$project" -c Release --game "$game" --comics ./EPUB  --data "$data" --output ./output $@ | tee log.log
#mkdir -p "/mnt/mediaSSD/Bethesda/MO2 Instances/Skyrim Special Edition/mods/Custom Books"
#cp -r output/* "/mnt/mediaSSD/Bethesda/MO2 Instances/Skyrim Special Edition/mods/Custom Books"
#ln -s "$(pwd)/output" "/mnt/mediaSSD/Bethesda/MO2 Instances/Skyrim Special Edition/mods/Custom Books"
