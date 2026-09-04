using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VendasApi.Application.Compras;

namespace VendasApi.Infrastructure.Compras;

/// <summary>
/// Agenda a expiração de reservas não pagas (US3.5) periodicamente — só orquestra o timer;
/// toda a lógica de negócio (o que conta como expirado, como cancelar) vive em
/// <see cref="ICancelarComprasExpiradasUseCase"/>, testável sem esperar nenhum
/// <see cref="Task.Delay"/> real. Cria um escopo de DI por execução porque o caso de uso (e o
/// <c>DbContext</c> por trás dele) é `Scoped`, enquanto este serviço é `Singleton` — mesmo
/// motivo pelo qual todo `BackgroundService` que consome serviços `Scoped` precisa de
/// <see cref="IServiceScopeFactory"/>.
/// </summary>
public class ExpiracaoReservaBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ExpiracaoReservaBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timeout = TimeSpan.FromMinutes(configuration.GetValue("Compras:TimeoutReservaMinutos", 30));
        var intervalo = TimeSpan.FromMinutes(configuration.GetValue("Compras:IntervaloVerificacaoMinutos", 1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var useCase = scope.ServiceProvider.GetRequiredService<ICancelarComprasExpiradasUseCase>();
                await useCase.ExecutarAsync(timeout, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // O próprio caso de uso já isola falha por compra — chegar aqui é algo mais
                // grave (ex.: banco fora do ar). Não deixa o loop morrer por causa disso.
                logger.LogError(ex, "Falha ao executar o job de expiracao de reservas.");
            }

            try
            {
                await Task.Delay(intervalo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
