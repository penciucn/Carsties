using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace   AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly AuctionDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;

    public AuctionsController(
        AuctionDbContext context, 
        IMapper mapper, 
        IPublishEndpoint publishEndpoint)
    {
        this._context = context;
        this._mapper = mapper;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions([FromQuery] string date)
    {
        var query  =_context.Auctions.OrderBy(x=>x.Item.Make).AsQueryable();

        if(!string.IsNullOrEmpty(date))
        {
            if (!DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDate))
            {
                return BadRequest("Invalid date query parameter. Use ISO-8601 format.");
            }

            var updatedAfter = parsedDate.ToUniversalTime();
            query = query.Where(x => x.UpdatedAt > updatedAfter);
        }

        return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
    {
        var auction = await _context.Auctions
        .Include(x=>x.Item)
        .FirstOrDefaultAsync(x=> x.Id == id);

        if(auction == null)
        {
            return NotFound();
        }

        return _mapper.Map<AuctionDto>(auction);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
    {
        var auction = _mapper.Map<Auction>(auctionDto);

        auction.Seller = User.Identity.Name;

        _context.Auctions.Add(auction);

        var newlyCreatedAuction = _mapper.Map<AuctionDto>(auction);
        await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newlyCreatedAuction));

        var result =    await _context.SaveChangesAsync() > 0;

        if(!result)
        {
            return BadRequest("Failed to create auction");
        }
     
        return CreatedAtAction(
            nameof(GetAuctionById), 
            new { id = auction.Id },
            newlyCreatedAuction);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto auctionDto)
    {
        var auction = await _context.Auctions
        .Include(x => x.Item)
        .FirstOrDefaultAsync(x => x.Id == id);

        if(auction == null)
        {
            return NotFound();
        }

        if(auction.Seller != User.Identity.Name)
        {
            return Forbid();
        }
        _mapper.Map(auctionDto, auction.Item);
        auction.UpdatedAt = DateTime.UtcNow;

        var result = await _context.SaveChangesAsync() > 0;

        if(!result)
        {
            return BadRequest("Failed to update auction");
        }

        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var auction = await _context.Auctions.FindAsync(id);

        if(auction == null)
        {
            return NotFound();
        }
 
        if(auction.Seller != User.Identity.Name)
        {
            return Forbid();
        }

        _context.Auctions.Remove(auction);
        var result = await _context.SaveChangesAsync() > 0;

        if(!result)
        {
            return BadRequest("Failed to delete auction");
        }

        return NoContent();
    }
}