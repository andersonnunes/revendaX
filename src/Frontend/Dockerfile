# Build no contexto raiz do repositório (revendaX/): docker build -f src/Frontend/Dockerfile .
#
# Diferente dos outros Dockerfiles deste repositório (IdentityApi/VendasApi/Gateway), o
# estágio final aqui não é o runtime do ASP.NET Core — Blazor WebAssembly publica só
# HTML/CSS/JS/`.wasm` estáticos, sem nenhum processo .NET rodando em produção. `nginx` serve
# esses arquivos, mesmo padrão já usado com sucesso num desafio anterior deste autor.
FROM mcr.microsoft.com/dotnet/sdk:10.0.101 AS build
WORKDIR /src

# IdentityApi.Domain entra aqui só pelo CpfValidator (US4.2, referenciado via
# <ProjectReference> em Frontend.csproj) — precisa estar no contexto de build por causa
# disso, mesmo o Dockerfile sendo do Frontend, não do IdentityApi.
COPY ["src/Frontend/Frontend.csproj", "src/Frontend/"]
COPY ["src/IdentityApi.Domain/IdentityApi.Domain.csproj", "src/IdentityApi.Domain/"]
RUN dotnet restore "src/Frontend/Frontend.csproj"

COPY src/Frontend/. src/Frontend/
COPY src/IdentityApi.Domain/. src/IdentityApi.Domain/
WORKDIR /src/src/Frontend
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM nginx:1.27-alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY src/Frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 8080
