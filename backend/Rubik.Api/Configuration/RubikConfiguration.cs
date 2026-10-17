namespace Rubik.Api.Configuration;

public interface IRubikConfiguration
{
    /// <summary>URL of the React app ("links:ui", defaults to the Vite dev server).</summary>
    string UiUrl { get; }

    /// <summary>Origins allowed by CORS ("cors:origins", defaults to the UI URL).</summary>
    IReadOnlyList<string> CorsOrigins { get; }

    /// <summary>How long an unused session is kept ("sessions:slidingExpirationMinutes", default 60).</summary>
    TimeSpan SessionSlidingExpiration { get; }
}

public class RubikConfiguration : IRubikConfiguration
{
    public RubikConfiguration(IConfiguration config)
    {
        UiUrl = config[RubikConstants.ConfigurationKey.UiUrl] ?? RubikConstants.Link.DefaultUiUrl;

        var origins = new List<string>();
        foreach (var child in config.GetSection(RubikConstants.ConfigurationKey.CorsOrigins).GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(child.Value))
            {
                origins.Add(child.Value);
            }
        }

        CorsOrigins = origins.Count > 0 ? origins : [UiUrl];

        var minutes = config.GetValue(
            RubikConstants.ConfigurationKey.SessionSlidingExpirationMinutes,
            RubikConstants.Session.DefaultSlidingExpirationMinutes);
        SessionSlidingExpiration = TimeSpan.FromMinutes(minutes > 0 ? minutes : RubikConstants.Session.DefaultSlidingExpirationMinutes);
    }

    public string UiUrl { get; }

    public IReadOnlyList<string> CorsOrigins { get; }

    public TimeSpan SessionSlidingExpiration { get; }
}
