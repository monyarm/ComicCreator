#/bin/sh
export LocalAppData="/home/monyarm/Games/SteamLibrary/compatdata/489830/pfx/drive_c/users/steamuser/AppData/Local/"
project="ComicsCreator/ComicsCreator.csproj"
game="SkyrimSE"
data="/home/monyarm/Games/SteamLibrary/common/Skyrim Special Edition/Data"
dotnet run --project "$project" -c Release --game "$game" --comics ./Comics2  --data "$data" --output ./output $@ | tee log.log
