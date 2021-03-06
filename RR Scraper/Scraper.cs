#nullable enable
namespace RR_Scraper;

using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using DataAccess;

public class Scraper : IScraper
{
    private static readonly Regex BrokeLinkRx =
        new(@"https://www.patreon.com/user?.+u=(?<num>\d+)", RegexOptions.Compiled);

    private static readonly Regex CurrRangeRx =
        new(@"\$(?<num1>\d+)(?<str1>\w*) - \$(?<num2>\d+)(?<str2>\w*)", RegexOptions.Compiled);

    private static readonly Regex ScriptJsonRx =
        new(
            @"Object.assign\(window.patreon.bootstrap, (?<json>.+)\);      Object.assign\(window.patreon.campaignFeatures",
            RegexOptions.Compiled);

    private static readonly Regex FictionIdRx =
        new(@"https*://www.royalroad.com/fiction/(?<id>\d+)/\w+",
            RegexOptions.Compiled);

    private readonly Dictionary<int, int> _ficIdToPatreonId;

    private readonly JObject _ratesDict = GetRates();

    private readonly int _degreeOfParallelism;
    private readonly int _timesToRepeatRequest;
    private readonly string _apikey;

    private Scraper(Dictionary<int, int> ficIdToPatreonId, string apikey, int degreeOfParallelism = 2, int timesToRepeatRequest = 2)
    {
        _degreeOfParallelism = degreeOfParallelism;
        _timesToRepeatRequest = timesToRepeatRequest;
        _apikey = apikey;
        _ficIdToPatreonId = ficIdToPatreonId;
    }

    public static async Task<Scraper> CreateAsync(IDataAccess data, string apikey, int degreeOfParallelism = 2, int timesToRepeatRequest = 2)
    {
        var sql = "SELECT DISTINCT id, patreon_id FROM fictions WHERE patreon_id IS NOT NULL;";
        var query = await data.QueryAsync<(int FicId, int PatId), dynamic>(sql, new { });
        var dict = query.ToDictionary(t => t.FicId, t => t.PatId);
        return new Scraper(dict, apikey, degreeOfParallelism, timesToRepeatRequest);
    }

    private static JObject GetRates()
    {
        string urlString = "https://v6.exchangerate-api.com/v6/3c4aff827b16ea7a36e4924e/latest/USD";
        var webClient = new HttpClient();
        var json = webClient.GetStringAsync(urlString).Result;
        var obj = JObject.Parse(json);
        var rates = obj["conversion_rates"]?.ToString();
        if (rates == null)
        {
            throw new Exception("Failed to initialize currency conversion dictionary.");
        }
        return JObject.Parse(rates);
    }


    private static int? GetRangeAvg(string range)
    {
        // "$2K - $7K" -> 4500
        var rx = CurrRangeRx.Match(range);
        if (!rx.Success) return null;

        int num1 = int.Parse(rx.Groups["num1"].Value);
        int num2 = int.Parse(rx.Groups["num2"].Value);

        if (rx.Groups["str1"].Value.Any()) num1 *= 1000;
        if (rx.Groups["str2"].Value.Any()) num2 *= 1000;

        return (num1 + num2) / 2;
    }

    private async Task<HtmlDocument?> LoadFromWebAsync(string url)
    {
        HtmlDocument? doc = null;
        bool failed = false;
        int counter = 0;
        do
        {
            try
            {
                doc = await new HtmlWeb().LoadFromWebAsync(url);
            }
            catch (Exception e)
            {
                failed = true;
                Console.WriteLine($"\ntype: {e.GetType()}");
            }
        } while (failed && counter++ < _timesToRepeatRequest);

        return doc;
    }

    private async Task<RRData?> GetFictionAsync(int id)
    {
        var json = await LoadFromWebAsync($"https://www.royalroad.com/api/stats/fiction/{id}?apikey={_apikey}");
        if (json == null) return null;

        var fictionResponse = JsonConvert.DeserializeObject<RRFictionApiResponse>(json.Text);
        if (fictionResponse == null) return null;

        RRData.PatreonData? patreonData = null;
        try
        {
            var patreonId = _ficIdToPatreonId[id];
            patreonData = await GetPatreon(patreonId);
        }
        catch (KeyNotFoundException)
        {
            var link = fictionResponse.donation.Patreon;
            if (!string.IsNullOrEmpty(link))
            {
                patreonData = await ScrapePatreon(fictionResponse.donation.Patreon);
            }
        }

        var profileResponse = await GetAuthorProfile(fictionResponse.author.id);

        var profileData = new RRData.AuthorData
        (
            fictionResponse.author.id,
            DateTime.Now,
            fictionResponse.author.username,
            profileResponse.totalWords,
            profileResponse.followers
        );

        var fictionData = new RRData.FictionData
        (
            fictionResponse.id,
            fictionResponse.author.id,
            patreonData?.Id,
            DateTime.Now,
            fictionResponse.title,
            fictionResponse.followers,
            // RR considers a page 275 words
            fictionResponse.pages * 275,
            fictionResponse.firstUpdate,
            fictionResponse.chapters
        );

        return new RRData(fictionData, profileData, patreonData);
    }

    private async Task<List<RRData>> GetFictionsAsync(IEnumerable<string> urls)
    {
        var fictions = new List<RRData>();

        var block = new ActionBlock<(string, int)>(async args =>
            {
                RRData? fiction;
                try
                {
                    fiction = await GetFictionAsync(args.Item2);
                }
                catch (TaskCanceledException)
                {
                    // likely a timeout, ignoring it
                    return;
                }

                if (fiction != null) fictions.Add(fiction);
            },
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _degreeOfParallelism });

        foreach (string url in urls)
        {
            var rx = FictionIdRx.Match(url);
            var id = int.Parse(rx.Groups["id"].Value);
            block.Post((url, id));
        }

        block.Complete();
        await block.Completion;

        return fictions;
    }

    public async Task<RRData.PatreonData?> GetPatreon(int id)
    {
        var json = await LoadFromWebAsync($"https://www.patreon.com/api/campaigns/{id}");
        if (json == null) return null;

        var response = JObject.Parse(json.Text);
        var attrs = response["data"]?["attributes"];

        if (attrs == null) return null;

        var patronCount = attrs["patron_count"]?.Value<int>();

        var pledgeSum = attrs["pledge_sum"]?.Value<float>();

        int? income = null;
        if (pledgeSum.HasValue)
        {
            var rate = _ratesDict[attrs["pledge_sum_currency"]!.Value<string>()]?.Value<float>();

            if (rate != null)
            {
                var conversionRate = 1.0f / rate.Value;

                // it's stored as an int with a float value, so we need to divide by 100 to get the actual value,
                // then we simply round and convert
                income = (int)Math.Round(pledgeSum.Value / 100 * conversionRate);
            }
        }
        else
        {
            var str = await GetGraphtreonIncome("https://graphtreon.com/creator/" + response["included"]?[0]?["vanity"]);
            if (str != null)
                income = GetRangeAvg(str);
        }

        return new RRData.PatreonData
        (
            id,
            DateTime.Now,
            patronCount,
            income
        );
    }

    private async Task<RRData.PatreonData?> ScrapePatreon(string url)
    {
        var doc = await LoadFromWebAsync(url);
        if (doc == null)
        {
            return null;
        }

        while (doc.DocumentNode.SelectSingleNode("title") != null)
        {
            var redirectUrl = doc.DocumentNode.SelectSingleNode("p/a").GetAttributeValue("href", "");
            doc = await LoadFromWebAsync("https://www.patreon.com" + redirectUrl);
            if (doc == null)
            {
                return null;
            }
        }

        var rx = BrokeLinkRx.Match(url).Groups["num"];
        if (rx.Success)
        {
            url = $"https://www.patreon.com/user?u={rx.Value}";
        }

        var scriptList = doc.DocumentNode.SelectNodes("/html/head/script");

        if (!scriptList.Any())
        {
            await Task.Delay(5000);
            doc = await LoadFromWebAsync(url);
            if (doc == null)
            {
                return null;
            }

            scriptList = doc.DocumentNode.SelectNodes("/html/head/script");
            if (!scriptList.Any())
            {
                return null;
            }
        }

        var list = scriptList.Where(node => node.InnerHtml.Contains("window.patreon = window.patreon || {};"))
            .ToList();

        if (!list.Any())
        {
            return null;
        }

        var jsonDict = JObject.Parse(ScriptJsonRx
            .Match(list[0].InnerText.Replace("\n", string.Empty))
            .Groups["json"].Value);

        // a JSON with all the data needed
        var attrs = jsonDict["campaign"]?["data"]?["attributes"];
        var campaignId = jsonDict["campaign"]?["data"]?["id"];

        // page doesn't have a proper patreon
        if (attrs == null) return null;
        if (campaignId == null) return null;

        // in case of no public income, graphtreon is used instead
        int? income = null;
        if (attrs["pledge_sum"] == null)
        {
            var str = await GetGraphtreonIncome($"https://graphtreon.com/creator/{url.AsSpan(24)}");
            if (str != null)
                income = GetRangeAvg(str);
        }
        else
        {
            var rate = _ratesDict[attrs["pledge_sum_currency"]!.ToString()]?.Value<float>();

            if (rate != null)
            {
                var conversionRate = 1 / rate.Value;

                float pledge_sum = attrs["pledge_sum"]!.Value<float>();
                // it's stored weirdly, so we need to divide by 100 to get the actual value,
                // then we simply round and cast
                income = (int)Math.Round(pledge_sum / 100 * conversionRate);
            }
        }

        var patrons = attrs["patron_count"]?.Value<int>();

        return new RRData.PatreonData
        (
            campaignId.Value<int>(),
            DateTime.Today,
            patrons,
            income
        );
    }

    private async Task<string?> GetGraphtreonIncome(string url)
    {
        var doc = await LoadFromWebAsync(url);
        if (doc == null) return null;

        var avgIncome = doc.DocumentNode
            .SelectSingleNode("/html/body/div[1]/div/div[1]/div/section/div[2]/div/div[4]/div/div/div[2]/span");

        if (avgIncome != null)
            return avgIncome.InnerText.Trim();

        return null;
    }

    private async Task<AuthorApiResponse> GetAuthorProfile(int id)
    {
        var json = await LoadFromWebAsync($"https://www.royalroad.com/api/stats/author/{id}?apikey={_apikey}");
        Debug.Assert(json != null);

        var response = JsonConvert.DeserializeObject<AuthorApiResponse>(json.Text);

        if (response == null) throw new Exception("Author api response is null");
        return response;
    }

    public async Task<List<RRData>> ScrapeSearchPages(IEnumerable<string> urls)
    {
        var links = new List<string>();

        var block = new ActionBlock<string>(async url =>
            {
                var doc = await LoadFromWebAsync(url);
                if (doc == null)
                {
                    throw new Exception("Error: Failed to get search pages.");
                }

                var nodes = doc.DocumentNode.SelectNodes(
                    "/html/body/div[3]/div/div/div/div/div/div/div/div[1]/div/div[1]/div");

                var list = nodes.Select(node =>
                    "https://www.royalroad.com" +
                    node.SelectSingleNode("div/h2/a")
                        .GetAttributeValue("href", "")).ToList();

                lock (links)
                    links.AddRange(list);
            },
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

        foreach (string url in urls)
        {
            block.Post(url);
        }

        block.Complete();
        await block.Completion;

        return await GetFictionsAsync(links);
    }

    public async Task<List<RRData>> Scrape(IEnumerable<int> pages)
    {
        return await ScrapeSearchPages(pages.Select(x => $"https://www.royalroad.com/fictions/search?page={x}"));
    }
}