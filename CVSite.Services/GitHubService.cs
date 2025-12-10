using Microsoft.Extensions.Options;
using Octokit;

namespace CVSite.Services;

public class GitHubService : IGitHubService
{
    private readonly GitHubClient _client;
    private readonly GitHubOptions _options;

    public GitHubService(IOptions<GitHubOptions> options)
    {
        _options = options.Value;
        
        _client = new GitHubClient(new ProductHeaderValue("CVSite"));
        
        if (!string.IsNullOrEmpty(_options.Token))
        {
            _client.Credentials = new Credentials(_options.Token);
        }
    }

    public async Task<List<PortfolioRepositoryDto>> GetPortfolioAsync()
    {
        if (string.IsNullOrEmpty(_options.UserName))
        {
            throw new InvalidOperationException("GitHub UserName is not configured.");
        }

        var repositories = await _client.Repository.GetAllForUser(_options.UserName);
        var portfolio = new List<PortfolioRepositoryDto>();

        foreach (var repo in repositories)
        {
            // Gather details
            var languages = await _client.Repository.GetAllLanguages(repo.Owner.Login, repo.Name);
            
            // Get last commit - requires querying commits
            // Note: This might be heavy if there are many repos. 
            // We take the latest one.
            DateTimeOffset? lastCommitDate = null;
            try 
            {
                var commits = await _client.Repository.Commit.GetAll(repo.Owner.Login, repo.Name, new ApiOptions { PageSize = 1, PageCount = 1 });
                lastCommitDate = commits.FirstOrDefault()?.Commit.Author.Date;
            }
            catch (Exception)
            {
                // Some repos might be empty or have access issues
            }

            // Get pull requests count (open and closed? The requirement says "how many pull requests were made", usually implies total)
            // Searching issues/PRs is the most efficient way to get a count without iterating pages
            var prRequest = new SearchIssuesRequest($"repo:{repo.Owner.Login}/{repo.Name} type:pr");
            var prResult = await _client.Search.SearchIssues(prRequest);
            
            portfolio.Add(new PortfolioRepositoryDto
            {
                Name = repo.Name,
                Languages = languages.Select(l => l.Name).ToList(),
                LastCommit = lastCommitDate ?? repo.UpdatedAt, // Fallback to UpdatedAt if commit fetch fails
                Stars = repo.StargazersCount,
                PullRequests = prResult.TotalCount,
                Url = repo.HtmlUrl
            });
        }

        return portfolio;
    }

    public async Task<List<RepositoryDto>> SearchRepositoriesAsync(string? name, string? language, string? username)
    {
        var request = new SearchRepositoriesRequest(name ?? string.Empty);
        
        if (!string.IsNullOrEmpty(language))
        {
            if (Enum.TryParse<Language>(language, true, out var langEnum))
            {
                request.Language = langEnum;
            }
            else
            {
                // Octokit's Language enum might not cover everything, or we might need to use InQualifier if supported.
                // For now, let's try to map generic strings if possible or just ignore strict enum parsing if Octokit allows string?
                // Octokit Language property is an Enum. 
                // A workaround for custom languages is usually not straightforward in strong typed wrapper.
                // But let's stick to the Enum if possible, or skip if invalid.
            }
        }

        if (!string.IsNullOrEmpty(username))
        {
            request.User = username;
        }

        var result = await _client.Search.SearchRepo(request);

        return result.Items.Select(repo => new RepositoryDto
        {
            Name = repo.Name,
            Owner = repo.Owner.Login,
            Url = repo.HtmlUrl,
            Stars = repo.StargazersCount,
            Language = repo.Language,
            Description = repo.Description
        }).ToList();
    }

    public async Task<DateTimeOffset?> GetUserLastActivityAsync()
    {
        if (string.IsNullOrEmpty(_options.UserName))
        {
            return null;
        }

        try
        {
            var events = await _client.Activity.Events.GetAllUserPerformed(_options.UserName, new ApiOptions { PageSize = 1, PageCount = 1 });
            return events.FirstOrDefault()?.CreatedAt;
        }
        catch
        {
            return null;
        }
    }
}
