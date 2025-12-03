using Cascade.Sample.Contracts.Commands;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using Microsoft.Extensions.DependencyInjection;

Console.Title = "OrderService";

var builder = Host.CreateApplicationBuilder(args);

var endpointConfiguration = new EndpointConfiguration("OrderService");

// Add serializer
endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

// Use SQL Server transport
var connectionString = "Server=localhost;Database=CascadeSamples;Trusted_Connection=True;TrustServerCertificate=True;";

var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
transport.ConnectionString(connectionString);
transport.DefaultSchema("dbo");

// Use SQL Server for subscriptions too
var subscriptions = transport.SubscriptionSettings();
subscriptions.DisableSubscriptionCache();

// Enable installers to auto-create queues
endpointConfiguration.EnableInstallers();

builder.UseNServiceBus(endpointConfiguration);

var host = builder.Build();

Console.WriteLine("OrderService starting...");
Console.WriteLine("Press 'P' to place an order, 'Q' to quit");

_ = host.RunAsync();

while (true)
{
  var key = Console.ReadKey(true);

  if (key.Key == ConsoleKey.Q)
  {
    break;
  }

  if (key.Key == ConsoleKey.P)
  {
    var orderId = Guid.NewGuid().ToString()[..8];
    var messageSession = host.Services.GetRequiredService<IMessageSession>();

    await messageSession.SendLocal(new PlaceOrder
    {
      OrderId = orderId,
      CustomerId = $"CUST-{Random.Shared.Next(1000, 9999)}",
      ProductName = "Widget Pro",
      Amount = Random.Shared.Next(10, 500)
    });

    Console.WriteLine($"Sent PlaceOrder: {orderId}");
  }
}

await host.StopAsync();