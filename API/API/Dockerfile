# Etap budowania
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Skopiuj plik projektu i przywróć zależności
COPY ["API.csproj", "./"]
RUN dotnet restore

# Skopiuj pozostałe pliki i opublikuj aplikację
COPY . .
RUN dotnet publish -c Release -o /app

# Etap uruchomieniowy
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080

ENTRYPOINT ["dotnet", "API.dll"]