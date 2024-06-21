using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TcpHexClient
{
    public interface ITcpHexClient
    {
        Task<bool> ConnectAsync();
        Task CloseAsync();
        Task<byte[]> ReceiveAsync(int recvLength);
        Task<byte[]> SendAsync(byte[] data, int recvLength = 0);
        Task ClearRecvBufferAsync();
    }
}
