FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AnaliticAsd/AnaliticAsd.csproj", "AnaliticAsd/"]
RUN dotnet restore "AnaliticAsd/AnaliticAsd.csproj"

COPY ["AnaliticAsd/", "AnaliticAsd/"]
WORKDIR /src/AnaliticAsd
RUN dotnet publish "AnaliticAsd.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "AnaliticAsd.dll"]
