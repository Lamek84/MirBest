# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем только csproj-файлы первыми — restore кешируется отдельным слоем
# и не перезапускается при изменении одного только .cs-кода.
COPY AutoPartsStore.sln ./
COPY AutoPartsStore.Core/AutoPartsStore.Core.csproj AutoPartsStore.Core/
COPY AutoPartsStore.Data/AutoPartsStore.Data.csproj AutoPartsStore.Data/
COPY AutoPartsStore.Infrastructure/AutoPartsStore.Infrastructure.csproj AutoPartsStore.Infrastructure/
COPY AutoPartsStore.Web/AutoPartsStore.Web.csproj AutoPartsStore.Web/
COPY AutoPartsStore.Tests/AutoPartsStore.Tests.csproj AutoPartsStore.Tests/

RUN dotnet restore AutoPartsStore.Web/AutoPartsStore.Web.csproj

COPY . .

RUN dotnet publish AutoPartsStore.Web/AutoPartsStore.Web.csproj \
    --no-restore \
    --configuration Release \
    --output /app/publish

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Без root — контейнер не должен работать от имени администратора.
RUN adduser --disabled-password --gecos "" appuser
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AutoPartsStore.Web.dll"]
