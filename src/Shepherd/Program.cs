using System.Threading.Tasks;

namespace Shepherd
{
    public static class Program
    {
        private static Task Main(string[] args)
        {
            var daemon = new Daemon();
            return daemon.Run();
        }
    }
}