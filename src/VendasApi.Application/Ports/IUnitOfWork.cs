namespace VendasApi.Application.Ports;

/// <summary>
/// Envolve múltiplas chamadas de repositório numa única transação de banco. Cada método de
/// repositório (`AdicionarAsync`/`AtualizarAsync`) já chama seu próprio `SaveChangesAsync` (ver
/// `EfVeiculoRepository`) — sem este port, duas chamadas em sequência seriam duas transações
/// separadas, não uma. Introduzido na US3.1 porque `IniciarCompraUseCase` é o primeiro caso de
/// uso que precisa gravar dois agregados (`Veiculo` + `Compra`) atomicamente; os casos de uso
/// do Épico 2 (um agregado só) continuam sem precisar dele.
/// </summary>
public interface IUnitOfWork
{
    Task ExecutarAtomicamenteAsync(Func<Task> operacao, CancellationToken cancellationToken);
}
