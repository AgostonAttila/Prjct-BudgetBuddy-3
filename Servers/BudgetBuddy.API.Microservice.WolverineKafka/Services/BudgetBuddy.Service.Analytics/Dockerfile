FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["Services/BudgetBuddy.Service.Analytics/BudgetBuddy.Service.Analytics.csproj", "Services/BudgetBuddy.Service.Analytics/"]
COPY ["Shared/BudgetBuddy.Shared.Kernel/BudgetBuddy.Shared.Kernel.csproj", "Shared/BudgetBuddy.Shared.Kernel/"]
COPY ["Shared/BudgetBuddy.Shared.Infrastructure/BudgetBuddy.Shared.Infrastructure.csproj", "Shared/BudgetBuddy.Shared.Infrastructure/"]
COPY ["Shared/BudgetBuddy.Shared.Messages/BudgetBuddy.Shared.Messages.csproj", "Shared/BudgetBuddy.Shared.Messages/"]
RUN dotnet restore "Services/BudgetBuddy.Service.Analytics/BudgetBuddy.Service.Analytics.csproj"
COPY . .
WORKDIR "/src/Services/BudgetBuddy.Service.Analytics"
RUN dotnet build "BudgetBuddy.Service.Analytics.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BudgetBuddy.Service.Analytics.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "BudgetBuddy.Service.Analytics.dll"]
