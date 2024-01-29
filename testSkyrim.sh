#/bin/sh
export LocalAppData="/mnt/mediaSSD/SteamLibrary/steamapps/compatdata/489830/pfx/drive_c/users/steamuser/AppData/Local/"
project="ComicsCreator/ComicsCreator.csproj"
game="SkyrimSE"
data="/mnt/mediaSSD/SteamLibrary/steamapps/common/Data"
dotnet run --project "$project" -c Release --game "$game" --comics ./Comics2  --data "$data" --output ./output $@ | tee log.log
