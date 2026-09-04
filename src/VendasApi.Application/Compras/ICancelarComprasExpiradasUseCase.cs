namespace VendasApi.Application.Compras;

public interface ICancelarComprasExpiradasUseCase
{
    /// <summary>Cancela toda compra `Pendente` criada há mais de <paramref name="timeoutReserva"/> e libera o veículo correspondente.</summary>
    Task ExecutarAsync(TimeSpan timeoutReserva, CancellationToken cancellationToken);
}
