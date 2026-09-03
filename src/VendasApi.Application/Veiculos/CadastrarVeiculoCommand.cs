using System.ComponentModel.DataAnnotations;

namespace VendasApi.Application.Veiculos;

/// <summary>
/// Comando de entrada do caso de uso "cadastrar veículo" — é também o corpo esperado em
/// POST /veiculos; o controller faz o bind direto nisso, sem DTO HTTP intermediário (mesmo
/// padrão do `identity-api`, ver `CriarClienteCommand`).
///
/// `Ano`/`Preco` não têm `[Required]` — em tipos de valor não anuláveis o atributo não tem
/// efeito (ausência no JSON vira o `default`, não dispara a validação). A validação de
/// intervalo/positividade desses dois é regra de negócio (`Veiculo.Cadastrar`, mapeada para
/// 422), não checagem de formato — um valor ausente cai nela naturalmente (`0` está fora do
/// intervalo de ano e não é preço positivo).
/// </summary>
public class CadastrarVeiculoCommand
{
    [Required(ErrorMessage = "marca é obrigatória")]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "modelo é obrigatório")]
    public string Modelo { get; set; } = string.Empty;

    public int Ano { get; set; }

    [Required(ErrorMessage = "cor é obrigatória")]
    public string Cor { get; set; } = string.Empty;

    public decimal Preco { get; set; }

    [Required(ErrorMessage = "placa é obrigatória")]
    public string Placa { get; set; } = string.Empty;
}
