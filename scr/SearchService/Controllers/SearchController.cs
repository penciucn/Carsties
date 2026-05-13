using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using SearchService;
using SearchService.RequestHelpers;

namespace SearchService.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<Item>> SearchItems(
           [FromQuery] SearchParams searchParams)
        {
            var query = DB.Default.PagedSearch<Item, Item>();
            query.Sort(x => x.Ascending(i => i.Make));
            if (!string.IsNullOrWhiteSpace(searchParams.SearchTerm))
            {
                query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
            }

            query = searchParams.OrderBy switch
            {
                "make" => query.Sort(x => x.Ascending(i => i.Make)),
                "new" => query.Sort(x => x.Descending(i => i.CreatedAt)),
                _ => query.Sort(x => x.Ascending(a => a.AuctionEnd))
            };

           query = searchParams.FilterBy switch
            {
                "finished" => query.Match(i => i.AuctionEnd < DateTime.UtcNow),
                "endingSoon" => query.Match(i => i.AuctionEnd <= DateTime.UtcNow.AddHours(-6) && i.AuctionEnd > DateTime.UtcNow),
                _ => query.Match(x=>x.AuctionEnd > DateTime.UtcNow)
            };

            if (!string.IsNullOrEmpty(searchParams.Seller))
            {
                query = query.Match(i => i.Seller == searchParams.Seller);
            }

            if (!string.IsNullOrEmpty(searchParams.Winner))
            {
                query = query.Match(i => i.Winner == searchParams.Winner);
            }

            query.PageNumber(searchParams.PageNumber);
            query.PageSize(searchParams.PageSize);

            var results = await query.ExecuteAsync();

            return Ok(new
            {
                results = results.Results,
                pageCount = results.PageCount,
                totalCount = results.TotalCount,
            });
        }
    }
}