using Microsoft.EntityFrameworkCore;
using VendasApi.Application.Ports;
using VendasApi.Domain.Compras;

namespace VendasApi.Infrastructure.Persistence;

public class EfCompraRepository(VendasDbContext dbContext) : ICompraRepository
{
    public Task<Compra?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Compras.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Compra>> ListarPendentesExpiradasAsync(DateTimeOffset limite, CancellationToken cancellationToken) =>
        await dbContext.Compras
            .Where(c => c.Status == StatusCompra.Pendente && c.CriadoEm < limite)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Compra>> ListarPorClienteAsync(string clienteId, CancellationToken cancellationToken) =>
        await dbContext.Compras
            .Where(c => c.ClienteId == clienteId)
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync(cancellationToken);

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
