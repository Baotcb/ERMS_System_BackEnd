FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ERMS.API/ERMS.API.csproj", "ERMS.API/"]
COPY ["ERMS.Application/ERMS.Application.csproj", "ERMS.Application/"]
COPY ["ERMS.Domain/ERMS.Domain.csproj", "ERMS.Domain/"]
COPY ["ERMS.Infrastructure/ERMS.Infrastructure.csproj", "ERMS.Infrastructure/"]
RUN dotnet restore "./ERMS.API/ERMS.API.csproj"
COPY . .
WORKDIR "/src/ERMS.API"
RUN dotnet build "./ERMS.API.csproj" -c $BUILD_CONFIGURATION -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ERMS.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ERMS.API.dll"]