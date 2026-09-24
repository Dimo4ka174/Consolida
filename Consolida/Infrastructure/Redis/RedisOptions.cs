namespace Consolida.Infrastructure.Redis
{
    public class RedisOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 6379;
        public string? Password { get; set; }
        public string InstanceName { get; set; } = "Consolida_";
    }
}
