#!/usr/bin/env sh
dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true
