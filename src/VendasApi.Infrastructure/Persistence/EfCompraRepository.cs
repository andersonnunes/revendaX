using VendasApi.Application.Ports;
using VendasApi.Domain.Compras;

namespace VendasApi.Infrastructure.Persistence;

public class EfCompraRepository(VendasDbContext dbContext) : ICompraRepository
{
    public async Task AdicionarAsync(Compra compra, CancellationToken cancellationToken)
    {
        dbContext.Compras.Add(compra);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
