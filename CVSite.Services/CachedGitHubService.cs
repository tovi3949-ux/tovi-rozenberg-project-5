using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CVSite.Services;

public class CachedGitHubService : IGitHubService
{
    private readonly IGitHubService _innerService;
    private readonly IMemoryCache _cache;
    private readonly GitHubOptions _options;
    private readonly ILogger<CachedGitHubService> _logger;

    public CachedGitHubService(
        IGitHubService innerService, 
        IMemoryCache cache, 
        IOptions<GitHubOptions> options,
        ILogger<CachedGitHubService> logger)
    {
        _innerService = innerService;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<PortfolioRepositoryDto>> GetPortfolioAsync()
    {
        string cacheKey = $"Portfolio_{_options.UserName}";
        string lastActivityCacheKey = $"LastActivity_{_options.UserName}";

        // קבלת הפעילות האחרונה שנשמרה ב-cache
        _cache.TryGetValue(lastActivityCacheKey, out DateTimeOffset? cachedLastActivity);

        // בדיקת הפעילות הנוכחית ב-GitHub
        var currentLastActivity = await _innerService.GetUserLastActivityAsync();

        // אם יש פעילות חדשה או שאין cache - נקה את ה-cache ושלוף מחדש
        bool shouldRefresh = !_cache.TryGetValue(cacheKey, out List<PortfolioRepositoryDto>? portfolio) ||
                            (currentLastActivity.HasValue && cachedLastActivity.HasValue && currentLastActivity > cachedLastActivity) ||
                            (currentLastActivity.HasValue && !cachedLastActivity.HasValue);

        if (shouldRefresh)
        {
            if (cachedLastActivity.HasValue && currentLastActivity.HasValue && currentLastActivity > cachedLastActivity)
            {
                _logger.LogInformation("?? GitHub activity detected! Cached: {Cached}, Current: {Current}. Refreshing cache...", 
                    cachedLastActivity, currentLastActivity);
            }
            else
            {
                _logger.LogInformation("?? No cache found. Fetching from GitHub...");
            }

            // שליפת נתונים טריים
            portfolio = await _innerService.GetPortfolioAsync();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30)); // Cache ארוך יותר כי אנחנו בודקים שינויים

            _cache.Set(cacheKey, portfolio, cacheEntryOptions);
            
            // שמירת זמן הפעילות האחרונה
            if (currentLastActivity.HasValue)
            {
                _cache.Set(lastActivityCacheKey, currentLastActivity.Value, cacheEntryOptions);
            }
        }
        else
        {
            _logger.LogInformation("? Returning from cache. No changes detected.");
        }

        return portfolio!;
    }

    public Task<List<RepositoryDto>> SearchRepositoriesAsync(string? name, string? language, string? username)
    {
        return _innerService.SearchRepositoriesAsync(name, language, username);
    }

    public Task<DateTimeOffset?> GetUserLastActivityAsync()
    {
        return _innerService.GetUserLastActivityAsync();
    }
}
