namespace RR_Scraper;

public class TestScraper : IScraper
{
    public Task<List<RRData>> Scrape(IEnumerable<int> pages)
    {
        var results = new List<RRData>();
        var rand = new Random();
        foreach (var (_, i) in pages.Select((_, i) => (_, i)))
        {
            var fic = new RRData.FictionData(i, i, i, DateTime.Now, "title", rand.Next(), rand.Next(), DateTime.Now, rand.Next());
            var author = new RRData.AuthorData(i, DateTime.Now, "username", rand.Next(), rand.Next());
            var pat = new RRData.PatreonData(i, DateTime.Now, rand.Next(), rand.Next());
            var data = new RRData(fic, author, pat);
            results.Add(data);
        }
        return Task.FromResult(results);
    }
}
