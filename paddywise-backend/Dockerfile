# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["PaddyWise.Api.csproj", "./"]
RUN dotnet restore "PaddyWise.Api.csproj"

# Copy source code and publish release build
COPY . .
RUN dotnet publish "PaddyWise.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render exposes the port in the PORT environment variable
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=\"http://+:${PORT:-8080}\" && exec dotnet PaddyWise.Api.dll"]
