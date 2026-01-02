# Build frontend
FROM node:22-alpine AS frontend
WORKDIR /src
COPY src/Cascade.Web/package*.json ./
RUN npm ci
COPY src/Cascade.Web/ ./
RUN npm run build -- --outDir /app/wwwroot

# Build backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY src/Cascade.Core/Cascade.Core.csproj src/Cascade.Core/
COPY src/Cascade.Collector/Cascade.Collector.csproj src/Cascade.Collector/
RUN dotnet restore src/Cascade.Collector/Cascade.Collector.csproj

# Copy source code and build
COPY src/Cascade.Core/ src/Cascade.Core/
COPY src/Cascade.Collector/ src/Cascade.Collector/

RUN dotnet publish src/Cascade.Collector/Cascade.Collector.csproj -c Release -o /app/publish --no-restore

# Copy frontend build output directly to publish output
COPY --from=frontend /app/wwwroot /app/publish/wwwroot/

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Cascade.Collector.dll"]
