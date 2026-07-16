namespace OrgWiki.Infrastructure.Analysis;

public sealed class OpenAiProviderException : InvalidOperationException
{
    public OpenAiProviderException(string stage, int statusCode, string? requestId, string? errorCode, string? errorMessage)
        : base($"OpenAI {stage} request failed with HTTP {statusCode}.")
    {
        StatusCode = statusCode;
        RequestId = requestId;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public int StatusCode { get; }
    public string? RequestId { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
}
