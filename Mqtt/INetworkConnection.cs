using System.Threading.Tasks;

namespace yoban.Mqtt
{
    public interface INetworkConnection
    {
        Task ConnectAsync();
        Task<int> ReadAsync(byte[] buffer, int offset, int size);
        Task<int> WriteAsync(byte[] buffer, int offset, int size);
    }
}
