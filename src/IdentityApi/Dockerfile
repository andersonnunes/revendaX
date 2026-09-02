# Build no contexto raiz do repositório (revendaX/): docker build -f src/IdentityApi/Dockerfile .
#
# Tags pinadas em vez de "10.0" flutuante: em 2026-09-01 a tag "10.0" ainda resolvia para o
# runtime 10.0.10, vulnerável ao CVE-2026-62901 (HIGH, DoS), corrigido na 10.0.11 (achado via
# `trivy image` nesta mesma data — reconferir a versão corrigida antes de assumir que segue
# atual). Reavaliar este pin quando a tag "10.0" também apontar para 10.0.11+.
FROM mcr.microsoft.com/dotnet/sdk:10.0.101 AS build
WORKDIR /src

# IdentityApi.csproj referencia IdentityApi.Domain/.Application/.Infrastructure — os 4
# projetos precisam estar no contexto para o restore seguir as ProjectReference corretamente.
COPY ["src/IdentityApi/IdentityApi.csproj", "src/IdentityApi/"]
COPY ["src/IdentityApi.Domain/IdentityApi.Domain.csproj", "src/IdentityApi.Domain/"]
COPY ["src/IdentityApi.Application/IdentityApi.Application.csproj", "src/IdentityApi.Application/"]
COPY ["src/IdentityApi.Infrastructure/IdentityApi.Infrastructure.csproj", "src/IdentityApi.Infrastructure/"]
RUN dotnet restore "src/IdentityApi/IdentityApi.csproj"

COPY src/IdentityApi/. src/IdentityApi/
COPY src/IdentityApi.Domain/. src/IdentityApi.Domain/
COPY src/IdentityApi.Application/. src/IdentityApi.Application/
COPY src/IdentityApi.Infrastructure/. src/IdentityApi.Infrastructure/
WORKDIR /src/src/IdentityApi
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11 AS final
WORKDIR /app
COPY --from=build /app/publish .

# A imagem base já expõe ASPNETCORE_HTTP_PORTS=8080 por padrão (.NET 8+).
EXPOSE 8080
ENTRYPOINT ["dotnet", "IdentityApi.dll"]
