using Microsoft.Extensions.Logging;
using VendasApi.Application.Ports;

namespace VendasApi.Application.Compras;

/// <summary>
/// Orquestra a expiração de reservas não pagas (US3.5): lista compras `Pendente` expiradas,
/// cancela cada uma e libera o veículo correspondente, uma compra por vez — cada uma na sua
/// própria transação atômica via <see cref="IUnitOfWork"/>, para que uma falha isolada não
/// impeça o resto do lote de ser processado (só registrada via log, sem interromper o laço).
/// Chamado periodicamente por um <c>BackgroundService</c> (Infrastructure/host), nunca por um
/// endpoint HTTP — não existe gatilho manual para esta história ("como sistema", não "como
/// cliente/vendedor").
/// </summary>
public class CancelarComprasExpiradasUseCase(
    ICompraRepository compraRepository,
    IVeiculoRepository veiculoRepository,
    IUnitOfWork unitOfWork,
    ILogger<CancelarComprasExpiradasUseCase> logger) : ICancelarComprasExpiradasUseCase
{
    public async Task ExecutarAsync(TimeSpan timeoutReserva, CancellationToken cancellationToken)
    {
        var limite = DateTimeOffset.UtcNow - timeoutReserva;
        var comprasExpiradas = await compraRepository.ListarPendentesExpiradasAsync(limite, cancellationToken);

        foreach (var compra in comprasExpiradas)
        {
            try
            {
                var veiculo = await veiculoRepository.ObterPorIdAsync(compra.VeiculoId, cancellationToken);
                if (veiculo is null)
                {
                    // Defensivo — não deveria acontecer: a invariante da US3.1/US3.2 garante
                    // que uma compra Pendente sempre corresponde a um veículo existente.
                    logger.LogWarning(
                        "Compra {CompraId} expirada referencia veiculo {VeiculoId} inexistente — pulando.",
                        compra.Id, compra.VeiculoId);
                    continue;
                }

                compra.Cancelar();
                veiculo.LiberarReserva();

                await unitOfWork.ExecutarAtomicamenteAsync(
                    async () =>
                    {
                        await compraRepository.AtualizarAsync(compra, cancellationToken);
                        await veiculoRepository.AtualizarAsync(veiculo, cancellationToken);
                    },
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha ao cancelar a compra expirada {CompraId}.", compra.Id);
            }
        }
    }
}
