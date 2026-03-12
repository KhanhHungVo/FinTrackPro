FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/FinTrackPro.API/FinTrackPro.API.csproj", "src/FinTrackPro.API/"]
COPY ["src/FinTrackPro.Application/FinTrackPro.Application.csproj", "src/FinTrackPro.Application/"]
COPY ["src/FinTrackPro.Domain/FinTrackPro.Domain.csproj", "src/FinTrackPro.Domain/"]
COPY ["src/FinTrackPro.Infrastructure/FinTrackPro.Infrastructure.csproj", "src/FinTrackPro.Infrastructure/"]
COPY ["src/FinTrackPro.BackgroundJobs/FinTrackPro.BackgroundJobs.csproj", "src/FinTrackPro.BackgroundJobs/"]

RUN dotnet restore "src/FinTrackPro.API/FinTrackPro.API.csproj"

COPY . .
WORKDIR "/src/src/FinTrackPro.API"
RUN dotnet build "FinTrackPro.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FinTrackPro.API.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinTrackPro.API.dll"]
