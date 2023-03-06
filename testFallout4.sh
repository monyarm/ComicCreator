#/bin/sh
export LocalAppData="/home/monyarm/Games/SteamLibrary/compatdata/377160/pfx/drive_c/users/steamuser/AppData/Local/"
project="ComicsCreator/ComicsCreator.csproj"
game="Fallout4"
data="/home/monyarm/Games/SteamLibrary/common/Fallout 4/Data"
dotnet run --project "$project" -c Release --game "$game" --comics $@  --data "$data" --output ./output  | tee log.log
