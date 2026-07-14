namespace OrgWiki.Domain.Analysis;

public sealed class KnowledgeAnalysis
{
    private KnowledgeAnalysis() { }
    public KnowledgeAnalysis(Guid uploadId, AiMode aiMode, string model)
    { UploadId = uploadId; AiMode = aiMode; Model = model; Status = KnowledgeAnalysisStatus.Pending; CreatedAtUtc = DateTime.UtcNow; }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UploadId { get; private set; }
    public KnowledgeAnalysisStatus Status { get; private set; }
    public AiMode AiMode { get; private set; }
    public string Model { get; private set; } = string.Empty;
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public int? InputTokens { get; private set; }
    public int? OutputTokens { get; private set; }
    public int? TotalTokens { get; private set; }
    public long? DurationMilliseconds { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ResultJson { get; private set; }
    public bool IsCurrent { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public void Start() { Status = KnowledgeAnalysisStatus.Processing; IsCurrent = true; StartedAtUtc = DateTime.UtcNow; }
    public void RecordUsage(int? input, int? output, int? total) { InputTokens = input; OutputTokens = output; TotalTokens = total; }
    public void Complete(string resultJson, int? input, int? output, int? total, long duration)
    { Status = KnowledgeAnalysisStatus.Completed; IsCurrent = true; ResultJson = resultJson; InputTokens = input; OutputTokens = output; TotalTokens = total; DurationMilliseconds = duration; CompletedAtUtc = DateTime.UtcNow; }
    public void Fail(string error, long duration)
    { Status = KnowledgeAnalysisStatus.Failed; IsCurrent = false; ErrorMessage = error; DurationMilliseconds = duration; CompletedAtUtc = DateTime.UtcNow; }
}
