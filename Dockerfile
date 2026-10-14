FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props FlagsNotify.sln ./
COPY src/FlagsNotify.Domain/FlagsNotify.Domain.csproj src/FlagsNotify.Domain/
COPY src/FlagsNotify.Application/FlagsNotify.Application.csproj src/FlagsNotify.Application/
COPY src/FlagsNotify.Infrastructure/FlagsNotify.Infrastructure.csproj src/FlagsNotify.Infrastructure/
COPY src/FlagsNotify.Api/FlagsNotify.Api.csproj src/FlagsNotify.Api/
RUN dotnet restore src/FlagsNotify.Api/FlagsNotify.Api.csproj

COPY src/ src/
RUN dotnet publish src/FlagsNotify.Api/FlagsNotify.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FlagsNotify.Api.dll"]
