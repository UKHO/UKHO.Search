# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG BUILD_CONFIGURATION=Release

COPY ["UKHO.ADDS.Search.FileShareEmulator.csproj", "./"]
RUN dotnet restore "UKHO.ADDS.Search.FileShareEmulator.csproj"

COPY . .
RUN dotnet publish "UKHO.ADDS.Search.FileShareEmulator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UKHO.ADDS.Search.FileShareEmulator.dll"]
