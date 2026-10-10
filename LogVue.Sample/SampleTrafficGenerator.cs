namespace LogVue.Sample;

/// <summary>
/// Writes a steady trickle of realistic-looking events so the dashboard has something to show.
/// </summary>
public sealed class SampleTrafficGenerator(ILogger<SampleTrafficGenerator> logger) : BackgroundService
{
    private static readonly string[] Customers = ["Contoso", "Fabrikam", "Northwind", "Tailspin", "Wingtip"];
    private static readonly string[] Products = ["Keyboard", "Monitor", "Mouse", "Headset", "Webcam", "Dock"];
    private static readonly string[] Paths = ["/orders", "/orders/{id}", "/products", "/cart", "/checkout"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var random = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            Emit(random);

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(random.Next(200, 1500)), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void Emit(Random random)
    {
        string customer = Customers[random.Next(Customers.Length)];
        string product = Products[random.Next(Products.Length)];
        int orderId = random.Next(1000, 99999);

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid().ToString("N")[..12],
            ["Tenant"] = customer,
        });

        switch (random.Next(100))
        {
            case < 30:
                logger.LogDebug("Cache lookup for {Product} took {ElapsedMs} ms", product, random.Next(1, 40));
                break;

            case < 70:
                logger.LogInformation(
                    "Order {OrderId} placed by {Customer} for {Quantity} x {Product} totalling {Total:C}",
                    orderId, customer, random.Next(1, 6), product, Math.Round((random.NextDouble() * 900) + 20, 2));
                break;

            case < 82:
                logger.LogInformation(
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms",
                    random.Next(3) == 0 ? "POST" : "GET",
                    Paths[random.Next(Paths.Length)],
                    200,
                    random.NextDouble() * 120);
                break;

            case < 92:
                logger.LogWarning(
                    "Inventory for {Product} is low: {Remaining} left, reorder threshold {Threshold}",
                    product, random.Next(0, 5), 5);
                break;

            case < 98:
                try
                {
                    throw new InvalidOperationException($"Payment gateway declined order {orderId}.",
                        new TimeoutException("The gateway did not respond within 30 seconds."));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Payment failed for order {OrderId} ({Customer})", orderId, customer);
                }
                break;

            default:
                logger.LogCritical("Database connection pool exhausted after {Attempts} attempts", random.Next(3, 10));
                break;
        }
    }
}