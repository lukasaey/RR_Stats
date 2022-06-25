using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RR_Scraper
{
    public interface IScraper
    {
        Task<List<RRData>> Scrape(IEnumerable<int> pages);
    }
}
