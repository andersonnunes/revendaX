using Microsoft.EntityFrameworkCore;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Infrastructure.Persistence;

public class VendasDbContext(DbContextOptions<VendasDbContext> options) : DbContext(options)
{
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Veiculo>(veiculo =>
        {
            veiculo.ToTable("Veiculos");
            veiculo.HasKey(v => v.Id);
            veiculo.Property(v => v.Marca).IsRequired();
            veiculo.Property(v => v.Modelo).IsRequired();
            veiculo.Property(v => v.Cor).IsRequired();
            veiculo.Property(v => v.Placa).IsRequired().HasMaxLength(7);
            veiculo.Property(v => v.Preco).HasColumnType("numeric(12,2)");
            veiculo.Property(v => v.Status).HasConversion<string>();

            // Reforça no schema a regra de negócio de unicidade já checada pela Application
            // (CadastrarVeiculoUseCase) — defesa em profundidade contra corrida entre duas
            // requisições concorrentes com a mesma placa (a checagem em memória sozinha não
            // impede isso).
            veiculo.HasIndex(v => v.Placa).IsUnique();
        });
    }
}
