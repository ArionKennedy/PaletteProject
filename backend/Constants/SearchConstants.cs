namespace Backend.Constants;

public static class SearchConstants
{
    public const string SEARCH_ROUTE = ApiConstants.API_PREFIX + "/search/{type}";
    public const int MAX_SEARCH_RESULTS = 10;
    public const int MAX_QUERY_LENGTH = 200;
}