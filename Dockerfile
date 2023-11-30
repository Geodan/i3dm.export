FROM mcr.microsoft.com/dotnet/sdk:6.0 as build
WORKDIR /src
COPY . .
RUN dotnet build "src/i3dm.export.csproj" -c Release
RUN dotnet publish "src/i3dm.export.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build /app /app/
ENTRYPOINT ["dotnet", "i3dm.export.dll"]