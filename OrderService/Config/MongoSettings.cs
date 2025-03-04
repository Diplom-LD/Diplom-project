namespace OrderService.Config
{
    public class MongoSettings
    {
        public required string ConnectionString { get; set; }
        public required string DatabaseName { get; set; }
    }
}
