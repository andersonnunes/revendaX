using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Ports;
using VendasApi.Domain.Veiculos;

namespace VendasApi.Infrastructure.Persistence;

public class EfVeiculoRepository(VendasDbContext dbContext) : IVeiculoRepository
{
    public Task<Veiculo?> ObterPorPlacaAsync(string placa, CancellationToken cancellationToken) =>
        dbContext.Veiculos.FirstOrDefaultAsync(v => v.Placa == placa, cancellationToken);

    public async Task AdicionarAsync(Veiculo veiculo, CancellationToken cancellationToken)
    {
        dbContext.Veiculos.Add(veiculo);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
