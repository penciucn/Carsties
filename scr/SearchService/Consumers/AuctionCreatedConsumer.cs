using AutoMapper;
using MassTransit;
using MongoDB.Entities;

using DB = MongoDB.Entities.DB;

namespace SearchService.Consumers
{
    public class AuctionCreatedConsumer: IConsumer<Contracts.AuctionCreated>
    {
        private readonly IMapper _mapper;

        public AuctionCreatedConsumer(IMapper mapper)
        {
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<Contracts.AuctionCreated> context)
        {
            Console.WriteLine($"Received AuctionCreated event for AuctionId: {context.Message.Id}");
            var item = _mapper.Map<Item>(context.Message);      

            await DB.Default.SaveAsync(item); // Save to MongoDB
        }
    }
}