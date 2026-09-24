using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Consolida.Infrastructure.Redis
{
    public class RedisConnectionService : IDisposable
    {
        private readonly Lazy<ConnectionMultiplexer> _lazyConnection;

        public RedisConnectionService(IOptions<RedisOptions> options)
        {
            var opts = options.Value;
            var config = ConfigurationOptions.Parse($"{opts.Host}:{opts.Port}");
            config.AbortOnConnectFail = false;
            if (!string.IsNullOrEmpty(opts.Password))
                config.Password = opts.Password;

            _lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
                ConnectionMultiplexer.Connect(config));
        }

        public ConnectionMultiplexer Connection => _lazyConnection.Value;

        public void Dispose()
        {
            if (_lazyConnection.IsValueCreated)
                _lazyConnection.Value.Dispose();
        }
    }
}
