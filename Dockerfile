﻿FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/TestBot/TestBot.csproj", "src/TestBot/"]
RUN dotnet restore "src/TestBot/TestBot.csproj"

COPY . .

WORKDIR "/src/src/TestBot"
RUN dotnet build "TestBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TestBot.dll"]