using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Ports;
using VendasApi.Domain.Compras;

namespace VendasApi.Infrastructure.Persistence;

public class EfCompraRepository(VendasDbContext dbContext) : ICompraRepository
{
    public Task<Compra?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Compras.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AdicionarAsync(Compra compra, CancellationToken cancellationToken)
    {
        dbContext.Compras.Add(compra);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarAsync(Compra compra, CancellationToken cancellationToken)
    {
        dbContext.Compras.Update(compra);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
