using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VendasApi.Infrastructure.Persistence;

/// <summary>
/// Só para as ferramentas de design-time do EF Core (`dotnet ef migrations add`) — a
/// connection string real vem de <c>Infrastructure/DependencyInjection.cs</c> em tempo de
/// execução (via `IConfiguration`, resolvida por DI). Sem isso, `dotnet ef` precisaria que o
/// projeto de host (`VendasApi`) referenciasse `Microsoft.EntityFrameworkCore.Design`
/// diretamente só para gerar migração — este factory evita vazar uma dependência de
/// ferramenta de desenvolvimento para o projeto de host.
/// </summary>
public class VendasDbContextFactory : IDesignTimeDbContextFactory<VendasDbContext>
{
    public VendasDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VendasDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=vendas;Username=vendas;Password=vendas");
        return new VendasDbContext(optionsBuilder.Options);
    }
}
