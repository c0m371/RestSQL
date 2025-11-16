FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .
WORKDIR "/src/src/RestSQL.Api"
RUN dotnet build "./RestSQL.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RestSQL.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Include repository README in the image so it can be used as the Docker Hub description
# (the build context for this workflow is the repository root, so README.md is available under /src in the build stage)
COPY --from=build /src/README.md /app/README.md
ENTRYPOINT ["dotnet", "RestSQL.Api.dll"]