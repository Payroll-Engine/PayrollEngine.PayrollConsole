FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src

# copy solution and project files
COPY ["PayrollEngine.PayrollConsole.sln", "./"]
COPY ["PayrollConsole/PayrollEngine.PayrollConsole.csproj", "PayrollConsole/"]
COPY ["Commands/PayrollEngine.PayrollConsole.Commands.csproj", "Commands/"]

# copy Directory.Build.props
COPY ["Directory.Build.props", "./"]

RUN dotnet restore "PayrollEngine.PayrollConsole.sln"

# copy everything else
COPY . .
WORKDIR "/src/PayrollConsole"
RUN dotnet publish "PayrollEngine.PayrollConsole.csproj" -c Release -o /app/publish

# final stage
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=0 /app/publish .
ENTRYPOINT ["dotnet", "PayrollEngine.PayrollConsole.dll"] 