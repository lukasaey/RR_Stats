namespace RR_Scraper;

// ReSharper disable once InconsistentNaming
public record RRData
{
    public FictionData Fiction;
    public AuthorData Author;
    public PatreonData? Patreon;

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
    }

    public record PatreonData
    {
        public int Id;
        public DateTime Date;
        public int? Patrons;
        public int? Income;
    }

    public record AuthorData
    {
        public int Id;
        public DateTime Date;
        public string Username;
        public long WordCount;
        public int Followers;
    }
}