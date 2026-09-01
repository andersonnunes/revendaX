# Build no contexto raiz do repositório (revendaX/): docker build -f src/Gateway/Dockerfile .
#
# Tags pinadas (não "10.0" flutuante) pelo mesmo motivo do IdentityApi/VendasApi: evitar o
# runtime 10.0.10, vulnerável ao CVE-2026-62901 (HIGH, DoS) — ver docs/adr/0003-dotnet-10.md.
FROM mcr.microsoft.com/dotnet/sdk:10.0.101 AS build
WORKDIR /src

COPY ["src/Gateway/Gateway.csproj", "src/Gateway/"]
RUN dotnet restore "src/Gateway/Gateway.csproj"

COPY src/Gateway/. src/Gateway/
WORKDIR /src/src/Gateway
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11 AS final
WORKDIR /app
COPY --from=build /app/publish .

# A imagem base já expõe ASPNETCORE_HTTP_PORTS=8080 por padrão (.NET 8+).
EXPOSE 8080
ENTRYPOINT ["dotnet", "Gateway.dll"]
