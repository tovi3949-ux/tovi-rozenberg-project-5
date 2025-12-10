namespace CVSite.Services;

public interface IGitHubService
{
    Task<List<PortfolioRepositoryDto>> GetPortfolioAsync();
    Task<List<RepositoryDto>> SearchRepositoriesAsync(string? name, string? language, string? username);
    Task<DateTimeOffset?> GetUserLastActivityAsync();
}

public class PortfolioRepositoryDto
{
    public string Name { get; set; }
    public List<string> Languages { get; set; } = new();
    public DateTimeOffset? LastCommit { get; set; }
    public int Stars { get; set; }
    public int PullRequests { get; set; }
    public string Url { get; set; }
}

public class RepositoryDto
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public string Url { get; set; }
    public int Stars { get; set; }
    public string Language { get; set; }
    public string Description { get; set; }
}
