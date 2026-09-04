using VendasApi.Application.Ports;

namespace VendasApi.Application.Compras;

/// <summary>
/// Lista todas as compras do cliente autenticado (qualquer status), mais recente primeiro —
/// extensão da US3.4, que originalmente só tinha consulta por id (decisão revisitada: o
/// frontend bônus (Épico 4) guardava esse histórico em `localStorage`, por navegador; esta
/// listagem substitui aquela limitação por uma fonte de verdade do servidor). Sem checagem de
/// dono aqui — diferente de <see cref="ConsultarCompraUseCase"/>, o filtro por `clienteId` já
/// é a própria consulta, não uma validação sobre um resultado já buscado.
/// </summary>
public class ListarComprasDoClienteUseCase(ICompraRepository compraRepository) : IListarComprasDoClienteUseCase
{
    public async Task<IReadOnlyList<CompraResult>> ExecutarAsync(string clienteId, CancellationToken cancellationToken)
    {
        var compras = await compraRepository.ListarPorClienteAsync(clienteId, cancellationToken);
        return compras.Select(c => c.ToResult()).ToList();
    }
}
