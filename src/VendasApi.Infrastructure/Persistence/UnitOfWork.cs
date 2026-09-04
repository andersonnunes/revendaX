using VendasApi.Application.Ports;

namespace VendasApi.Infrastructure.Persistence;

/// <summary>
/// Implementa <see cref="IUnitOfWork"/> com uma transação explícita do EF Core, envolvendo as
/// chamadas de repositório passadas em <paramref name="operacao"/> (cada uma com seu próprio
/// `SaveChangesAsync`) numa única transação de banco — ou todas persistem, ou nenhuma.
/// </summary>
public class UnitOfWork(VendasDbContext dbContext) : IUnitOfWork
{
    public async Task ExecutarAtomicamenteAsync(Func<Task> operacao, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await operacao();
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
