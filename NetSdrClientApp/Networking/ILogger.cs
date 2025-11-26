using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    // Інтерфейс для логування повідомлень у бібліотечних класах.
    // Це дозволяє легко інтегрувати різні системи логування (NLog, Serilog, або ConsoleLogger)
    // без зміни коду обгорток.
    public interface ILogger
    {
        void Log(string message);
        void LogError(string message);
    }
}
