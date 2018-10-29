using System.IO;
using System.Threading.Tasks;

namespace yoban.Mqtt
{
    public interface INetworkConnection
    {
        string HostName { get; }
        int Port { get; }
        Task<Stream> ConnectAsync();
    }
}
