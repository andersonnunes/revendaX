using Microsoft.EntityFrameworkCore;
using VendasApi.Domain.Compras;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Infrastructure.Persistence;

public class VendasDbContext(DbContextOptions<VendasDbContext> options) : DbContext(options)
{
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();

    public DbSet<Compra> Compras => Set<Compra>();

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

            // Concorrência otimista via xmin (coluna de sistema do Postgres) — todo UPDATE de
            // Veiculo passa a checar se a linha mudou desde a leitura; se mudou, o EF Core
            // lança DbUpdateConcurrencyException (mapeada para 409 no DomainExceptionHandler).
            // Protege qualquer escrita concorrente sobre o mesmo veículo — não só duas compras
            // disputando o mesmo registro, também uma edição correndo contra uma compra.
            //
            // Shadow property (sem CLR property em Veiculo — Domain continua sem saber que
            // existe controle de concorrência), não o método de conveniência
            // `UseXminAsConcurrencyToken()`: ele existia no provider Npgsql até a versão 8.x e
            // foi removido a partir da 9.x (confirmado inspecionando o assembly instalado) —
            // este projeto está pinado em 10.0.3. O nome da shadow property já mapeia para a
            // coluna física `xmin` por convenção, sem precisar de `HasColumnName`.
            veiculo.Property<uint>("xmin").IsRowVersion();
        });

        modelBuilder.Entity<Compra>(compra =>
        {
            compra.ToTable("Compras");
            compra.HasKey(c => c.Id);
            compra.Property(c => c.ClienteId).IsRequired();
            compra.Property(c => c.Preco).HasColumnType("numeric(12,2)");
            compra.Property(c => c.Status).HasConversion<string>();

            // FK sem navegação de volta em Veiculo — Compra referencia Veiculo, não o
            // contrário (agregados independentes, ver Compra). Restrict porque veículo nunca
            // é removido fisicamente (só soft delete via Ativo, US2.5).
            compra.HasOne<Veiculo>().WithMany().HasForeignKey(c => c.VeiculoId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
