#/bin/sh
export LocalAppData="/mnt/mediaSSD/SteamLibrary/steamapps/compatdata/377160/pfx/drive_c/users/steamuser/AppData/Local/"
project="ComicsCreator/ComicsCreator.csproj"
game="Fallout4"
data="/mnt/mediaSSD/SteamLibrary/steamapps/common/Fallout 4/Data"
dotnet run --project "$project" -c Release --game "$game" --comics $@  --data "$data" --output ./output  | tee log.log
#mkdir -p "/mnt/mediaSSD/Bethesda/MO2 Instances/Fallout 4/mods/Custom Comics"
#cp -r output/* "/mnt/mediaSSD/Bethesda/MO2 Instances/Fallout 4/mods/Custom Comics"
#ln -s "$(pwd)/output" "/mnt/mediaSSD/Bethesda/MO2 Instances/Fallout 4/mods/Custom Comics"
