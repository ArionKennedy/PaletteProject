namespace Backend.Constants;

public static class TmdbConstants
{
    public const string PROVIDER_NAME = "tmdb";
    public const string CONFIG_SECTION = "Tmdb";

    // Relative to BaseUrl, so they must NOT start with a slash.
    public const string SEARCH_MOVIE_PATH = "search/movie";
    public const string MOVIE_PATH = "movie";

    public const string BEARER_SCHEME = "Bearer";
    public const string RELEASE_DATE_FORMAT = "yyyy-MM-dd";
    public const int DEFAULT_TIMEOUT_SECONDS = 10;
}
