namespace RR_Scraper
{
    // ReSharper disable once InconsistentNaming
    public class RRData
    {
        public FictionData Fiction;
        public AuthorData Author;
        public PatreonData? Patreon;

        public struct FictionData
        {
            public int Id;
            public DateTime Date;
            public string Title;
            public int Follows;
            public long WordCount;
            public DateTime Created;
            public int Chapters;
            public int AuthorId;
            public int? PatreonId;
        }

        public struct PatreonData
        {
            public int Id;
            public DateTime Date;
            public int? Patrons;
            public int? Income;
        }

        public struct AuthorData
        {
            public int Id;
            public DateTime Date;
            public string Username;
            public long WordCount;
            public int Followers;
        }
    }
}