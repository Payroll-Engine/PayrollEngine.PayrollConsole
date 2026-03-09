# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG GITHUB_TOKEN
WORKDIR /src

# Copy solution and project files
COPY ["PayrollEngine.PayrollConsole.sln", "./"]
COPY ["PayrollConsole/PayrollEngine.PayrollConsole.csproj", "PayrollConsole/"]
COPY ["Commands/PayrollEngine.PayrollConsole.Commands.csproj", "Commands/"]
COPY ["Directory.Build.props", "./"]

# Configure GitHub Packages NuGet source
RUN dotnet nuget add source "https://nuget.pkg.github.com/Payroll-Engine/index.json" \
    --name github \
    --username github-actions \
    --password ${GITHUB_TOKEN} \
    --store-password-in-clear-text

# Restore dependencies (cached layer)
RUN dotnet restore "PayrollEngine.PayrollConsole.sln"

# Copy remaining source files and publish
COPY . .
WORKDIR "/src/PayrollConsole"
RUN dotnet publish "PayrollEngine.PayrollConsole.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PayrollEngine.PayrollConsole.dll"]
