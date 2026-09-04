using System.Text.Json;
using Microsoft.JSInterop;

namespace Frontend.Services;

/// <summary>
/// Histórico de compras feitas neste navegador (US4.4) — guardado em `localStorage`, não numa
/// listagem server-side, porque a US3.4 decidiu explicitamente não ter `GET /compras` (só
/// consulta por id). Limitação aceita e documentada no refinamento: não sincroniza entre
/// dispositivos/navegadores diferentes — resolver isso de verdade exigiria a US3.4 ganhar uma
/// listagem no backend, fora do escopo desta história.
/// </summary>
public class HistoricoComprasLocal(IJSRuntime js)
{
    private const string Chave = "revendax:minhas-compras";

    /// <summary>Registra uma compra recém-criada — chamado ao abrir `/compras/{id}`, não só
    /// logo após o `POST` (assim um link direto pra uma compra também a adiciona ao histórico
    /// deste navegador, não só o fluxo de compra em si).</summary>
    public async Task AdicionarAsync(Guid compraId)
    {
        var ids = await ListarAsync();
        if (ids.Contains(compraId))
        {
            return;
        }

        ids.Insert(0, compraId); // mais recente primeiro
        await js.InvokeVoidAsync("localStorage.setItem", Chave, JsonSerializer.Serialize(ids));
    }

    public async Task<List<Guid>> ListarAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", Chave);
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? [];
        }
        catch (JsonException)
        {
            // Conteúdo corrompido/editado manualmente no localStorage — não trava a tela por
            // causa disso, só trata como "sem histórico".
            return [];
        }
    }
}
