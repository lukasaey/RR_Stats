namespace RR_Scraper;

// ReSharper disable once InconsistentNaming
public record RRData
{
    public FictionData Fiction;
    public AuthorData Author;
    public PatreonData? Patreon;

    public RRData(FictionData fiction, AuthorData author, PatreonData? patreon)
    {
        Fiction = fiction;
        Author = author;
        Patreon = patreon;
    }

    public record FictionData
    {
        public int Id;
        public int AuthorId;
        public int? PatreonId;
        public DateTime Date;
        public string Title;
        public int Follows;
        public long WordCount;
        public DateTime Created;
        public int Chapters;

        public FictionData(int id, int authorId, int? patreonId, DateTime date, string title, int follows, long wordCount, DateTime created, int chapters)
        {
            Id = id;
            AuthorId = authorId;
            PatreonId = patreonId;
            Date = date;
            Title = title;
            Follows = follows;
            WordCount = wordCount;
            Created = created;
            Chapters = chapters;
        }
    }

    public record PatreonData
    {
        public int Id;
        public DateTime Date;
        public int? Patrons;
        public int? Income;

        public PatreonData(int id, DateTime date, int? patrons, int? income)
        {
            Id = id;
            Date = date;
            Patrons = patrons;
            Income = income;
        }
    }

    public record AuthorData
    {
        public int Id;
        public DateTime Date;
        public string Username;
        public long WordCount;
        public int Followers;

        public AuthorData(int id, DateTime date, string username, long wordCount, int followers)
        {
            Id = id;
            Date = date;
            Username = username;
            WordCount = wordCount;
            Followers = followers;
        }
    }
}