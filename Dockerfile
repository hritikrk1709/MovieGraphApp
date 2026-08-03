# ---------- Build Stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Install Node.js
RUN apt-get update && apt-get install -y curl
RUN curl -fsSL https://deb.nodesource.com/setup_22.x | bash -
RUN apt-get install -y nodejs

WORKDIR /src

# Copy the project
COPY . .

# Install frontend dependencies
WORKDIR /src/frontend
RUN npm install

# Build React
RUN npm run build

# Publish .NET app
WORKDIR /src
RUN dotnet publish -c Release -o /app/publish

# ---------- Runtime Stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "MovieGraphApp.dll"]