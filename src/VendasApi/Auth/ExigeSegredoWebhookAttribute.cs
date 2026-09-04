using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VendasApi.Auth;

/// <summary>
/// Protege um endpoint de webhook (US3.3) por segredo compartilhado (header
/// `X-Webhook-Secret`, comparado a `Pagamentos:WebhookSecret`) — não `[Authorize]`, porque
/// quem chama é um sistema externo (gateway de pagamento, ainda que mock), não um usuário com
/// token JWT do Keycloak. Simplificação consciente: um provedor real assinaria o payload
/// (ex.: HMAC), fora de escopo aqui.
/// </summary>
public class ExigeSegredoWebhookAttribute : Attribute, IAsyncActionFilter
{
    private const string CabecalhoSegredo = "X-Webhook-Secret";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var segredoEsperado = configuration["Pagamentos:WebhookSecret"];
        var segredoRecebido = context.HttpContext.Request.Headers[CabecalhoSegredo].ToString();

        if (string.IsNullOrEmpty(segredoEsperado) || segredoRecebido != segredoEsperado)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
