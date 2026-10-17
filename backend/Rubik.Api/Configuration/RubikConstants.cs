namespace Rubik.Api.Configuration;

public static class RubikConstants
{
    public static class ConfigurationKey
    {
        /// <summary>URL of the React app, linked from the Swagger UI top bar.</summary>
        public const string UiUrl = "links:ui";

        /// <summary>Origins allowed by CORS (array).</summary>
        public const string CorsOrigins = "cors:origins";

        /// <summary>Minutes a session is kept after its last use.</summary>
        public const string SessionSlidingExpirationMinutes = "sessions:slidingExpirationMinutes";
    }

    public static class Link
    {
        /// <summary>The Vite dev server.</summary>
        public const string DefaultUiUrl = "http://localhost:5173";

        public const string UiLabel = "UI";
    }

    public static class Route
    {
        public const string CubeController = "api/cube";
        public const string Health = "/health";
        public const string Swagger = "swagger";
    }

    public static class Policy
    {
        public const string Cors = "RubikCors";
    }

    public static class ProblemExtension
    {
        public const string Token = "token";
        public const string Position = "position";
        public const string TokenNumber = "tokenNumber";
    }

    public static class Session
    {
        public const int DefaultSlidingExpirationMinutes = 60;
    }

    public static class Scramble
    {
        public const int MinLength = 1;
        public const int MaxLength = 100;
    }
}
