# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/PostulaAI.AI/PostulaAI.AI.csproj src/PostulaAI.AI/
COPY src/PostulaAI.Api/PostulaAI.Api.csproj src/PostulaAI.Api/
RUN dotnet restore src/PostulaAI.Api/PostulaAI.Api.csproj
COPY src/ src/
RUN dotnet publish src/PostulaAI.Api/PostulaAI.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "PostulaAI.Api.dll"]
