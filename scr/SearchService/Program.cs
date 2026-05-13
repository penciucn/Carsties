
using MassTransit;
using Polly;
using Polly.Extensions.Http;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SearchService.Consumers.AuctionCreatedConsumer>();
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));
    x.UsingRabbitMq((context, cfg) =>
    {
       cfg.ReceiveEndpoint("search-auction-created", e =>
       {
           e.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(5)));         
           e.ConfigureConsumer<SearchService.Consumers.AuctionCreatedConsumer>(context);
       });
       
       cfg.ConfigureEndpoints(context);      
    });
});
var app= builder.Build();

app.UseAuthorization();
app.MapControllers();

app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
{
    await SearchService.DbInitializer.InitDb(app);
}
catch(Exception ex)
{
    Console.WriteLine($"Error during DB initialization: {ex.Message}");
}
});

// Initialize MongoDB before accepting requests.


app.Run();

static IAsyncPolicy<HttpResponseMessage> GetPolicy()
=>HttpPolicyExtensions
.HandleTransientHttpError()
.OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
.WaitAndRetryForeverAsync(_=>TimeSpan.FromSeconds(5), (result, timeSpan) =>
{
    Console.WriteLine($"Request failed with {result.Result?.StatusCode}. Waiting {timeSpan} before retrying.");
});
