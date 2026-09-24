using Serilog.Core;
using Serilog.Events;

namespace Consolida.Infrastructure.Logging
{
    /// <summary>
    /// Custom Enricher/Кастомный компонент для логирования, чтобы выводил только наименование класса
    /// </summary>
    public class ShortSourceContextEnricher : ILogEventEnricher
    {
        private readonly string[] _ourNamespaces = new[]
        {
            "Consolida.",
            "Application.",
            "DB."
        };

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextValue))
            {
                var fullName = sourceContextValue.ToString().Trim('"');
                string displayName;

                bool isOurCode = _ourNamespaces.Any(ns => fullName.StartsWith(ns));

                if (isOurCode)
                {
                    displayName = fullName.Split('.').Last();
                }
                else
                {
                    displayName = fullName;
                }

                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ShortSourceContext", displayName));
            }
        }
    }
}
