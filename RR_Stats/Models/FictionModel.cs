namespace RR_Stats.Models;

public record FictionModel
{ 
    public int id;
    public DateTime from_date;
    public int author_id;
    public int patreon_id;
    public string title;
    public int follows;
    public long word_count;
    public DateTime created;
    public int chapters;
}