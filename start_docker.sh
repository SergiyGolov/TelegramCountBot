#!/bin/bash

killall ngrok

# source: https://gist.github.com/mihow/9c7f559807069a03e302605691f85572?permalink_comment_id=4245050#gistcomment-4245050
eval "$(
  cat .env_bash | awk '!/^\s*#/' | awk '!/^\s*$/' | while IFS='' read -r line; do
    key=$(echo "$line" | cut -d '=' -f 1)
    value=$(echo "$line" | cut -d '=' -f 2-)
    echo "export $key=\"$value\""
  done
)"

ngrok http https://localhost:443 > /dev/null &

read -s -p "Certificate password: " password

echo

# source: https://stackoverflow.com/questions/39471457/ngrok-retrieve-assigned-subdomain/40144313
ngrok_adress=$(curl --silent --show-error http://127.0.0.1:4040/api/tunnels | sed -nE 's/.*public_url":"https:..([^"]*).*/\1/p')

curl https://api.telegram.org/bot${DEBT_BOT_TOKEN}/setWebhook?url=https://${ngrok_adress}/api/bot/debt
echo
curl https://api.telegram.org/bot${SCRABBLE_BOT_TOKEN}/setWebhook?url=https://${ngrok_adress}/api/bot/scrabble
echo

docker run --rm -it -p 443:443 -e ASPNETCORE_URLS="https://0.0.0.0" -e ASPNETCORE_HTTPS_PORTS=443 -e ASPNETCORE_Kestrel__Certificates__Default__Password="${password}" -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx -v ${HOME}/.aspnet/https:/https/ -v ${PWD}/db:/App/db telegramcountbot-image:latest