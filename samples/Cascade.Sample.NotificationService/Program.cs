using Microsoft.Extensions.Hosting;

Console.Title = "Notifications";

var builder = Host.CreateApplicationBuilder(args);

var endpointConfiguration = new EndpointConfiguration("NotificationService");

endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

var connectionString = "Server=localhost;Database=CascadeSamples;Trusted_Connection=True;TrustServerCertificate=True;";

var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
transport.ConnectionString(connectionString);
transport.DefaultSchema("dbo");

var subscriptions = transport.SubscriptionSettings();
subscriptions.DisableSubscriptionCache();

endpointConfiguration.EnableInstallers();

builder.UseNServiceBus(endpointConfiguration);

var host = builder.Build();

Console.WriteLine("Notifications service starting...");
Console.WriteLine("Listening for OrderPlaced events...");
Console.WriteLine("Press 'Q' to quit");

_ = host.RunAsync();

while (true)
{
    var key = Console.ReadKey(true);
    if (key.Key == ConsoleKey.Q)
    {
        break;
    }
}

await host.StopAsync();
