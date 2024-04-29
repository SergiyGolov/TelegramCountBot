#!/bin/bash

cp -r /usr/share/TelegramCountBot_config/. . #TODO: find a way to avoid to hardcore this folder path here
chmod +x build_docker.sh
chmod +x start_docker.sh
./build_docker.sh
./start_docker.sh