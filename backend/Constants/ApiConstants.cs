namespace Backend.Constants;

public static class ApiConstants
{
    public const string API_PREFIX = "/api";

    // used in Features/Health
    public const string HEALTH_ROUTE = API_PREFIX + "/health";
    public const string HEALTH_STATUS_OK = "ok";

    //used in Shared/Errors/ApiExceptionHandler
    public const string PROBLEM_JSON_CONTENT_TYPE = "application/problem+json";
    public const string TRACE_ID_KEY = "traceId";
}