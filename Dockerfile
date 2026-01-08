# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["PayrollEngine.PayrollConsole.sln", "./"]
COPY ["PayrollConsole/PayrollEngine.PayrollConsole.csproj", "PayrollConsole/"]
COPY ["Commands/PayrollEngine.PayrollConsole.Commands.csproj", "Commands/"]
COPY ["Directory.Build.props", "./"]

# Restore dependencies (cached layer)
RUN dotnet restore "PayrollEngine.PayrollConsole.sln"

# Copy remaining source files and publish
COPY . .
WORKDIR "/src/PayrollConsole"
RUN dotnet publish "PayrollEngine.PayrollConsole.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PayrollEngine.PayrollConsole.dll"]
