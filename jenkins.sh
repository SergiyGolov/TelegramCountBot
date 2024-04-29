#!/bin/bash

# source: https://gist.github.com/mihow/9c7f559807069a03e302605691f85572?permalink_comment_id=4245050#gistcomment-4245050
eval "$(
  cat .env_bash | awk '!/^\s*#/' | awk '!/^\s*$/' | while IFS='' read -r line; do
    key=$(echo "$line" | cut -d '=' -f 1)
    value=$(echo "$line" | cut -d '=' -f 2-)
    echo "export $key=\"$value\""
  done
)"

cp -r ${CONFIG_FOLDER}. .
chmod +x build_docker.sh
chmod +x start_docker.sh
./build_docker.sh
./start_docker.sh