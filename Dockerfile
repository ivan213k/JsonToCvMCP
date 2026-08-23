FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet publish JsonToCvApi/JsonToCvApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ARG SEMANTIC_VERSION=dev
ENV SemanticVersion=$SEMANTIC_VERSION
ENV ASPNETCORE_ENVIRONMENT=Production

ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

COPY --from=build /app/publish .
RUN dotnet JsonToCvApi.dll --playwright-install \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8080
ENTRYPOINT ["dotnet", "JsonToCvApi.dll"]
