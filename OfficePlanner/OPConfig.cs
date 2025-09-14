namespace OfficePlanner;

public record OPConfig
{
    public string OidcAuthority { get; init; }
    public string OidcClientId { get; init; }
    public string OidcClientSecret { get; init; }
    public string ConnectionString { get; init; }
    public string AdminPassword { get; init; }

    public OPConfig(ConfigurationManager configurationManager)
    {
        var oidcSection = configurationManager.GetSection("OIDC") ?? throw new InvalidOperationException("Configuration missing: OIDC");
        this.OidcAuthority = oidcSection["Authority"] ?? throw new InvalidOperationException("Configuration missing: OIDC.Authority");
        this.OidcClientId = oidcSection["ClientId"] ?? throw new InvalidOperationException("Configuration missing: OIDC.ClientId"); ;
        this.OidcClientSecret = oidcSection["ClientSecret"] ?? throw new InvalidOperationException("Configuration missing: OIDC.ClientSecret");
        this.ConnectionString = configurationManager["DbConnectionString"] ?? throw new InvalidOperationException("Configuration missing: DbConnectionString");
        this.AdminPassword = configurationManager["AdminPassword"] ?? throw new InvalidOperationException("Configuration missing: AdminPassword");
    }
}
