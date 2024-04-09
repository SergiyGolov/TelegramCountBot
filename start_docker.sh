#!/bin/bash
read -s -p "Certificate password: " password

docker run --rm -it -p 7132:7132 -e ASPNETCORE_URLS="https://0.0.0.0" -e ASPNETCORE_HTTPS_PORTS=7132 -e ASPNETCORE_Kestrel__Certificates__Default__Password="${password}" -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx -v ${HOME}/.aspnet/https:/https/ -v ${PWD}/db:/App/db telegramcountbot-image:latest