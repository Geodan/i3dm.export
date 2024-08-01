FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
COPY . .
WORKDIR src
RUN dotnet build "i3dm.export.csproj" -c Release
RUN dotnet publish "i3dm.export.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app /app/
ENTRYPOINT ["dotnet", "i3dm.export.dll"]