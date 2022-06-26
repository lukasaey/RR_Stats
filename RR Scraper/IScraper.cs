namespace RR_Scraper
{
    public interface IScraper
    {
        Task<List<RRData>> Scrape(IEnumerable<int> pages);
    }
}
