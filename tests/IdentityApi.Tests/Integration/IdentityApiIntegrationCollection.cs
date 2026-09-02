namespace IdentityApi.Tests.Integration;

/// <summary>
/// Uma única instância de <see cref="MailKeycloakFixture"/> (Keycloak + Mailpit) compartilhada
/// entre todas as classes de teste de integração do `identity-api` — sem isso, cada classe com
/// <c>IClassFixture</c> sobe seu próprio container (xUnit cria uma instância por classe, não
/// por tipo), e três classes viravam três Keycloaks simultâneos só pra rodar a mesma suíte.
/// Classes na mesma <c>[Collection]</c> rodam sequencialmente entre si (não em paralelo) — é
/// o trade-off aceito pra pagar o custo de subir o container uma única vez.
/// </summary>
[CollectionDefinition(nameof(IdentityApiIntegrationCollection))]
public class IdentityApiIntegrationCollection : ICollectionFixture<MailKeycloakFixture>;
