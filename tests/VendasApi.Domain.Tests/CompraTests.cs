using VendasApi.Domain.Compras;

namespace VendasApi.Domain.Tests;

public class CompraTests
{
    [Fact]
    public void Iniciar_DadosValidos_CriaCompraPendente()
    {
        var veiculoId = Guid.NewGuid();

        var compra = Compra.Iniciar(veiculoId, "cliente-123", 89900.00m);

        Assert.NotEqual(Guid.Empty, compra.Id);
        Assert.Equal(veiculoId, compra.VeiculoId);
        Assert.Equal("cliente-123", compra.ClienteId);
        Assert.Equal(89900.00m, compra.Preco);
        Assert.Equal(StatusCompra.Pendente, compra.Status);
    }

    [Fact]
    public void Iniciar_GeraIdsDiferentesParaCadaCompra()
    {
        var primeira = Compra.Iniciar(Guid.NewGuid(), "cliente-123", 89900.00m);
        var segunda = Compra.Iniciar(Guid.NewGuid(), "cliente-123", 89900.00m);

        Assert.NotEqual(primeira.Id, segunda.Id);
    }
}
