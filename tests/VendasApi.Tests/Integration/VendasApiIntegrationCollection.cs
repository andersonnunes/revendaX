namespace VendasApi.Tests.Integration;

/// <summary>
/// Uma única instância de <see cref="VendasApiTestEnvironment"/> (Keycloak + Postgres)
/// compartilhada entre todas as classes de teste de integração do `vendas-api` — ver
/// doc-comment de <see cref="VendasApiTestEnvironment"/>.
/// </summary>
[CollectionDefinition(nameof(VendasApiIntegrationCollection))]
public class VendasApiIntegrationCollection : ICollectionFixture<VendasApiTestEnvironment>;
