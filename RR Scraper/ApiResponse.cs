// ReSharper disable InconsistentNaming

namespace RR_Scraper;

public record AuthorApiResponse
{
    public long totalWords;
    public int receivedReviews;
    public int receivedRatings;
    public int followers;
    public int favorites;
}

public record RRApiResponse
{
    // ApiResponse myDeserializedClass = JsonConvert.DeserializeObject<ApiResponse>(myJsonResponse);
    public int id;
    public Author author;
    public string title;
    public string cover;
    public string status;
    public DateTime firstUpdate;
    public DateTime lastUpdate;
    public List<string> tags;
    public int followers;
    public int favorites;
    public int views;
    public Ratings ratings;
    public long pages;
    public int chapters;
    public Donation donation;

    public record Donation
    {
        public string PayPal;
        public string Patreon;
    }

    public record Author
    {
        public int id;
        public string username;
        public string avatar;
    }

    public record Ratings
    {
        public float styleScore;
        public float storyScore;
        public float characterScore;
        public float grammarScore;
        public float overallScore;
        public int count;
    }
}