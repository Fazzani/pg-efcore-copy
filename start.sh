#!/bin/bash

NOW=$(date +"%m-%d-%Y-%H-%M-%S")

function migrate_and_upadte_db(){
 dotnet ef migrations add  "mig-$NOW" -s ./TestConsoleApp/ -c "BloggingContext" -v && \
 dotnet ef database update -s TestConsoleApp
}

EF_MIGRATE_UPDATE=false
BENCHMARK=false
 
PARAMS=""
while (( "$#" )); do
  case "$1" in
    -m|--migrate)
      EF_MIGRATE_UPDATE=true
      shift
      ;;
     -b|--benchmark)
      BENCHMARK=true
      shift
      ;;
    -*|--*=) # unsupported flags
      echo "Error: Unsupported flag $1" >&2
      exit 1
      ;;
    *) # preserve positional arguments
      PARAMS="$PARAMS $1"
      shift
      ;;
  esac
done
# set positional arguments in their proper place
eval set -- "$PARAMS"


[[ "$EF_MIGRATE_UPDATE" = true ]] && migrate_and_upadte_db
[[ "$BENCHMARK" = true ]] && sudo dotnet run -c Release