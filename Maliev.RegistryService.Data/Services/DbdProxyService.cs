using System.Net;
using System.Text.Json;
using System.Web;
using HtmlAgilityPack;
using Maliev.RegistryService.Data.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Maliev.RegistryService.Data.Services;

public interface IDbdProxyService
{
    Task<IEnumerable<CompanyProfile>> LookupAsync(string query, int limit);
}

public class DbdProxyService : IDbdProxyService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DbdProxyService> _logger;
    private readonly HttpClient _httpClient;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private static readonly SemaphoreSlim _concurrencySemaphore = new(20, 20); // Limit total concurrent curl processes

    public DbdProxyService(
        IDistributedCache cache, 
        ILogger<DbdProxyService> logger,
        HttpClient httpClient)
    {
        _cache = cache;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<CompanyProfile>> LookupAsync(string query, int limit)
    {
        if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<CompanyProfile>();

        string cacheKey = $"dbd_lookup_{query}_{limit}";
        var cachedData = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogInformation("Returning cached DBD results for {Query}", query);
            return JsonSerializer.Deserialize<IEnumerable<CompanyProfile>>(cachedData) ?? Enumerable.Empty<CompanyProfile>();
        }

        try
        {
            var results = await FetchFromDbdUpstreamAsync(query, limit);
            
            // Sort results to prioritize exact matches or closer matches
            var sortedResults = results
                .OrderBy(r => r.NameTh == query ? 0 : 1)
                .ThenBy(r => r.NameTh.StartsWith(query) ? 0 : 1)
                .ToList();

            if (sortedResults.Any())
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(sortedResults), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheDuration
                });
            }

            return sortedResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data from DataForThai for {Query}", query);
            throw;
        }
    }

    private async Task<IEnumerable<CompanyProfile>> FetchFromDbdUpstreamAsync(string query, int limit)
    {
        try
        {
            await _concurrencySemaphore.WaitAsync();
            var url = "https://www.dataforthai.com/api/company";
            _logger.LogInformation("Fetching company data via curl for {Query}", query);

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "curl",
                Arguments = $"-s -m 5 -X POST \"{url}\" " +
                            $"-H \"User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36\" " +
                            $"-H \"X-Requested-With: XMLHttpRequest\" " +
                            $"-H \"Content-Type: application/x-www-form-urlencoded; charset=UTF-8\" " +
                            $"-H \"Accept: */*\" " +
                            $"-d \"mode=search_comp&data[searchtext]={HttpUtility.UrlEncode(query)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null) throw new Exception("Failed to start curl process");

            var json = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var searchResults = ParseSearchJson(json, limit);
            
            // Fetch details in parallel
            var tasks = searchResults.Select(FetchDetailAsync);
            return await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch from DataForThai.");
            throw;
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    private async Task<CompanyProfile> FetchDetailAsync(CompanyProfile baseProfile)
    {
        try
        {
            await _concurrencySemaphore.WaitAsync();
            var detailUrl = $"https://www.dataforthai.com/company/{baseProfile.TaxId}/printview";
            
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "curl",
                Arguments = $"-s -m 5 -L \"{detailUrl}\" " +
                            $"-H \"User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null) return baseProfile;

            var html = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            _concurrencySemaphore.Release();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            string address = "";
            string capital = "";
            string regDate = "";
            string bizType = "";
            string nameEn = "";

            // 1. Extract English Name from h3 (Verified in Page Snapshot)
            var h3Node = doc.DocumentNode.SelectSingleNode("//h3");
            if (h3Node != null)
            {
                nameEn = HttpUtility.HtmlDecode(h3Node.InnerText?.Trim() ?? "");
            }

            // Fallback for English Name: Keywords
            if (string.IsNullOrWhiteSpace(nameEn))
            {
                var metaKeywords = doc.DocumentNode.SelectSingleNode("//meta[@name='Keywords']");
                var keywords = metaKeywords?.GetAttributeValue("content", "");
                if (!string.IsNullOrEmpty(keywords))
                {
                    var parts = keywords.Split(',');
                    if (parts.Length > 1) nameEn = parts[1].Trim();
                }
            }

            // 2. Extract Data from Tables
            var tables = doc.DocumentNode.SelectNodes("//table");
            if (tables != null)
            {
                foreach (var table in tables)
                {
                    var rows = table.SelectNodes(".//tr");
                    if (rows == null) continue;

                    foreach (var row in rows)
                    {
                        var cells = row.SelectNodes(".//td");
                        if (cells == null || cells.Count < 2) continue;

                        for (int i = 0; i < cells.Count - 1; i++)
                        {
                            var key = HttpUtility.HtmlDecode(cells[i].InnerText?.Trim() ?? "");
                            var val = HttpUtility.HtmlDecode(cells[i+1].InnerText?.Trim() ?? "");

                            if (key.Contains("ประกอบธุรกิจ") || (key == "ธุรกิจ" && !val.Contains("เข้าสู่ระบบ")))
                            {
                                // Remove internal HR or sub-labels if they appear in the value
                                var cleanedVal = val.Split("หมวดธุรกิจ")[0].Trim();
                                if (!string.IsNullOrEmpty(cleanedVal) && !cleanedVal.Contains("เข้าสู่ระบบ"))
                                {
                                    bizType = cleanedVal;
                                }
                            }
                            else if (key == "จดทะเบียน" || key == "วันที่จดทะเบียน")
                            {
                                regDate = val;
                            }
                            else if (key == "ทุนจดทะเบียน")
                            {
                                capital = val;
                            }
                            else if (key == "ที่ตั้ง" || key.Contains("แผนที่"))
                            {
                                // Address is usually in the second cell
                                // If the first cell contains "แผนที่" as well as "ที่ตั้ง", val is the address
                                if (!string.IsNullOrEmpty(val) && !val.Contains("แผนที่"))
                                {
                                    address = val;
                                }
                            }
                        }
                    }
                }
            }

            // 3. Last resort fallback for address from Google Maps links
            if (string.IsNullOrEmpty(address))
            {
                var mapsLink = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'google.co.th/maps/search/')]");
                if (mapsLink != null)
                {
                    // The link text is usually the address
                    address = mapsLink.InnerText?.Trim() ?? "";
                }
            }

            // Cleaning
            address = Clean(address);
            capital = Clean(capital);
            regDate = Clean(regDate);
            bizType = Clean(bizType);
            nameEn = Clean(nameEn);

            return baseProfile with 
            { 
                NameEn = nameEn,
                Address = address, 
                Capital = capital, 
                RegistrationDate = regDate, 
                BusinessType = bizType 
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch details for {TaxId}", baseProfile.TaxId);
            return baseProfile;
        }
    }

    private string Clean(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var cleaned = System.Text.RegularExpressions.Regex.Replace(input, @"\s+", " ").Trim();
        return cleaned.Replace("&nbsp;", " ").Trim();
    }

    private IEnumerable<CompanyProfile> ParseSearchJson(string json, int limit)
    {
        using var doc = JsonDocument.Parse(json);
        
        if (!doc.RootElement.TryGetProperty("data", out var dataArray) || dataArray.ValueKind != JsonValueKind.Array)
        {
            return Enumerable.Empty<CompanyProfile>();
        }

        var profiles = new List<CompanyProfile>();
        foreach (var item in dataArray.EnumerateArray().Take(limit))
        {
            var taxId = item.TryGetProperty("jp_no", out var p1) ? p1.GetString() ?? string.Empty : string.Empty;
            var nameTh = item.TryGetProperty("full_tname", out var p2) ? p2.GetString() ?? string.Empty : string.Empty;
            var status = item.TryGetProperty("status_tname", out var p3) ? p3.GetString() ?? string.Empty : string.Empty;

            profiles.Add(new CompanyProfile(
                taxId,
                nameTh,
                string.Empty,
                status,
                "https://www.dataforthai.com"
            ));
        }

        return profiles;
    }
}
