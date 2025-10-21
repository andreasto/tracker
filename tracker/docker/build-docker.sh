#!/usr/bin/env bash
dotnet publish ../src/tracker.App/tracker.App.csproj --os linux --arch x64 -c Release -p:PublishProfile=DefaultContainer