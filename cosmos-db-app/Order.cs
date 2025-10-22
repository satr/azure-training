namespace cosmos_db_app;
public sealed class Order
{
    public string id { get; set; } = default!;          // required by Cosmos
    public string orderId { get; set; } = default!;     // partition key (string!)
    public string content { get; set; } = default!;
    public DateTime processedAt { get; set; }
}