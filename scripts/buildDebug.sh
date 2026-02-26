#!/bin/bash

mkdir ../Debug -p
#rm ../Debug/*
dotnet build ../src/Project.sln -c Debug -o ../Debug
dotnet ef database update --project ../src/Database/Database.Context/Database.Context.csproj --startup-project ../src/Project.HttpServer/Project.HttpServer.csproj
cd ../Debug
./Project.HttpServer