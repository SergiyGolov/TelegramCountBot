# Prerequisites

You'll need:
- docker
- ngrok

# How to build
Create a folder on your server containing these 3 files:
- `.env_bash` which contains your bot tokens, follow example from `.env_bash.example`
- `appsettings.json` which contains your bot tokens and your telegram user id, follow example from `appsettings.example.json`
- `.cert_password` which contains the password for the ssl certificate used by Kestrel

Then you should put the path to this folder in `jenkins.sh` by replacing `/root/TelegramCountBot_config/`

Finally you can run `jenkins.sh` which will copy the contents of the above mentioned folder to the current directory, build and run the container.