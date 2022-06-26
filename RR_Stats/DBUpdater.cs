namespace RR_Stats;

using RR_Scraper;
using DataAccess;
using System.Threading;

public class DBUpdater : BackgroundService, IDisposable
{
    private readonly string _connectionString;
    private readonly IEnumerable<int> _pageRange;
    private readonly IScraper _scraper;

    public DBUpdater(string connectionString, IEnumerable<int> pageRange, IScraper scraper)
    {
        _connectionString = connectionString;
        _pageRange = pageRange;
        _scraper = scraper;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            var rrDatas = await _scraper.Scrape(_pageRange);

            var pats = rrDatas
                .Select(rrData => rrData.Patreon)
                .Where(patData => patData != null)
                .Select(p => p);
            var authors = rrDatas.Select(rrData => rrData.Author);
            var fics = rrDatas.Select(rrData => rrData.Fiction);

            var data = new DataAccess();

            await data.Execute(
                "INSERT INTO authors (id, from_date, word_count, followers, username) " +
                "VALUES (@id, @date, @wordCount, @followers, @username) ON DUPLICATE KEY UPDATE id=id, from_date=from_date;",
                authors.Select(a => new
                {
                    id = a.Id,
                    date = a.Date,
                    wordCount = a.WordCount,
                    followers = a.Followers,
                    username = a.Username
                })
                    .ToArray(),
                _connectionString
            );
            await data.Execute(
                "INSERT INTO patreons (id, from_date, patrons, income) " +
                "VALUES (@id, @date, @patrons, @income) ON DUPLICATE KEY UPDATE id=id, from_date=from_date;",
                pats.Select(p => new { id = p.Id, date = p.Date, patrons = p.Patrons, income = p.Income }).ToArray(),
                _connectionString
            );
            await data.Execute(
                "INSERT INTO fictions (id, from_date, author_id, title, follows, word_count, created, chapters, patreon_id) " +
                "VALUES (@id, @date, @authorId, @title, @follows, @wordCount, @created, @chapters, @patreonId) ON DUPLICATE KEY UPDATE id=id, from_date=from_date;",
                fics.Select(f => new
                {
                    id = f.Id,
                    date = f.Date,
                    authorId = f.AuthorId,
                    follows = f.Follows,
                    wordCount = f.WordCount,
                    created = f.Created,
                    chapters = f.Chapters,
                    patreonId = f.PatreonId,
                    title = f.Title
                }).ToArray(),
                _connectionString
            );

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}

