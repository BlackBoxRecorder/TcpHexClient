using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TcpHexClient;

namespace TcpHex
{
    public class TcpHexClient : ITcpHexClient
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private readonly string _serverIp;
        private readonly int _serverPort;

        private readonly ITcpHexLogger _logger;
        private CancellationTokenSource cts;
        private readonly Stopwatch swTimeout = Stopwatch.StartNew();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public TcpHexClient(string ip, int port, ITcpHexLogger logger = null)
        {
            _serverIp = ip;
            _serverPort = port;

            if (logger != null)
            {
                _logger = logger;
            }
            else
            {
                _logger = new TcpHexLogger();
            }
        }

        #region Property

        /// <summary>
        /// 是否连接到服务端
        /// </summary>
        public bool Connected
        {
            get
            {
                if (_tcpClient != null)
                {
                    return _tcpClient.Connected;
                }

                return false;
            }
        }

        /// <summary>
        /// 读取指定长度数据超时（ms）
        /// </summary>
        public int Timeout { get; set; } = 1000;

        /// <summary>
        /// 接收数据超时（ms）
        /// </summary>
        public int ReceiveTimeout { get; set; } = 1000;

        /// <summary>
        /// 发送数据超时（ms）
        /// </summary>
        public int SendTimeout { get; set; } = 1000;

        /// <summary>
        /// 接收缓冲区大小
        /// </summary>
        public int ReceiveBufferSize { get; set; } = 8192;

        /// <summary>
        /// 发送缓冲区大小
        /// </summary>
        public int SendBufferSize { get; set; } = 8192;

        /// <summary>
        /// 默认128，发送/接收的字节数组长度小于128，则打印到控制台
        /// </summary>
        public int LogBytesLimit { get; set; } = 128;

        #endregion

        #region Public

        /// <summary>
        /// 连接到服务端，当连接异常断开后会自动重连
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ConnectAsync()
        {
            if (Connected)
            {
                return true;
            }

            try
            {
                if (!await semaphore.WaitAsync(Timeout))
                {
                    throw new TimeoutException("等待获取锁超时");
                }

                await Connect()
                    .ContinueWith(x =>
                    {
                        Task.Run(ReconnectTask);
                    });
            }
            finally
            {
                semaphore.Release();
            }

            return Connected;
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public async Task CloseAsync()
        {
            try
            {
                if (!await semaphore.WaitAsync(Timeout))
                {
                    throw new TimeoutException("等待获取锁超时");
                }

                if (cts != null)
                {
                    cts.Cancel();
                    cts.Dispose();
                    cts = null;
                }

                if (_tcpClient != null)
                {
                    _tcpClient.Close();
                    _tcpClient = null;
                }
            }
            finally
            {
                semaphore.Release();
            }

            _logger?.LogInfo("关闭连接");
        }

        /// <summary>
        /// 接收指定长度的数据
        /// </summary>
        /// <param name="recvLength"></param>
        /// <returns></returns>
        public async Task<byte[]> ReceiveAsync(int recvLength)
        {
            try
            {
                if (!await semaphore.WaitAsync(Timeout))
                {
                    throw new TimeoutException("等待获取锁超时");
                }

                return await Receive(recvLength);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.Message, ex);
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// 发送数据，并接收指定长度的数据；当 recvLength 等于 0 时，只发送不接收。
        /// </summary>
        /// <param name="data">要发送的数据，字节数组</param>
        /// <param name="recvLength">要接收的数据长度</param>
        /// <returns></returns>
        public async Task<byte[]> SendAsync(byte[] data, int recvLength = 0)
        {
            try
            {
                if (!await semaphore.WaitAsync(Timeout))
                {
                    throw new TimeoutException("等待获取锁超时");
                }

                return await SendAndRecv(data, recvLength);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.Message, ex);
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// 清空接收区缓存的数据
        /// </summary>
        /// <returns></returns>
        public async Task ClearRecvBufferAsync()
        {
            try
            {
                if (!await semaphore.WaitAsync(Timeout))
                {
                    throw new TimeoutException("等待获取锁超时");
                }

                byte[] buffer = new byte[2048];
                while (_networkStream.DataAvailable)
                {
                    _ = await _networkStream.ReadAsync(buffer, 0, 2048);
                }

                _logger.LogInfo("清空接收区缓存");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            finally
            {
                semaphore.Release();
            }
        }

        #endregion

        #region Private

        private async Task<bool> Connect()
        {
            try
            {
                _tcpClient = new TcpClient
                {
                    ReceiveTimeout = ReceiveTimeout,
                    SendTimeout = SendTimeout,
                    SendBufferSize = SendBufferSize,
                    ReceiveBufferSize = ReceiveBufferSize,
                };

                _logger?.LogInfo("正在连接...");

                await _tcpClient.ConnectAsync(_serverIp, _serverPort);
                _networkStream = _tcpClient.GetStream();

                _logger?.LogInfo($"成功连接到服务端 {_serverIp}:{_serverPort}");

                return true;
            }
            catch (SocketException ex)
            {
                _logger?.LogError($"Socket Error: {ex.SocketErrorCode}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError("连接到服务端异常： ", ex);
            }

            _tcpClient?.Close();

            return false;
        }

        private async Task ReconnectTask()
        {
            if (cts == null)
            {
                cts = new CancellationTokenSource();
            }
            int reconnectDelay = 100;

            while (cts != null && !cts.Token.IsCancellationRequested)
            {
                if (_tcpClient != null && _tcpClient.Connected)
                {
                    await Task.Delay(100);
                    continue;
                }

                bool connected = await Connect();
                if (connected)
                {
                    reconnectDelay = 0;
                }

                _logger?.LogInfo($"重连 {(connected ? "成功" : "失败")}");

                if (reconnectDelay < 10000)
                {
                    reconnectDelay += 100;
                }

                await Task.Delay(reconnectDelay);
            }
        }

        private async Task<byte[]> SendAndRecv(byte[] data, int recvLength)
        {
            CheckConnectionState();

            try
            {
                await _networkStream.WriteAsync(data, 0, data.Length);
                await _networkStream.FlushAsync();
                if (data.Length < LogBytesLimit)
                {
                    _logger?.LogTrace($"发送数据：{Utils.BytesToHexString(data)}");
                }
            }
            catch
            {
                _tcpClient.Close(); //发送数据异常，关闭连接
                _tcpClient = null;
                throw;
            }

            if (recvLength == 0)
            {
                return new byte[0];
            }

            return await Receive(recvLength);
        }

        private async Task<byte[]> Receive(int recvLength)
        {
            CheckConnectionState();

            swTimeout.Restart();
            byte[] buffer = new byte[recvLength];
            int totalBytesRead = 0;

            while (totalBytesRead < recvLength)
            {
                if (swTimeout.Elapsed.TotalMilliseconds > Timeout)
                {
                    if (!_tcpClient.Client.Poll(100, SelectMode.SelectRead))
                    {
                        _tcpClient.Close();
                        _tcpClient = null;
                        throw new InvalidOperationException("套接字读取失败，连接可能已断开，或者服务端没有发送数据");
                    }

                    throw new TimeoutException($"读取指定长度的数据超时, {Timeout}ms");
                }

                if (!_networkStream.DataAvailable)
                {
                    //没有可读取的数据就继续循环，直到 swTimeout 超时
                    continue;
                }

                try
                {
                    int bytesRead = await _networkStream.ReadAsync(
                        buffer,
                        totalBytesRead,
                        recvLength - totalBytesRead
                    );

                    if (bytesRead == 0)
                    {
                        //如果没有可读取的数据，ReadAsync 会等待读取；
                        //如果 ReadAsync 返回 0，表示到达网络流末端
                        throw new EndOfStreamException();
                    }

                    totalBytesRead += bytesRead;
                }
                catch
                {
                    _tcpClient.Close(); //接收数据异常，关闭连接
                    _tcpClient = null;
                    throw;
                }
            }

            if (buffer.Length < LogBytesLimit)
            {
                _logger?.LogTrace($"接收数据：{Utils.BytesToHexString(buffer)}");
            }

            return buffer;
        }

        private void CheckConnectionState()
        {
            if (_networkStream == null || _tcpClient == null || !_tcpClient.Connected)
            {
                //当发送或接收数据异常时，会将 _tcpClient 关闭，Connected = false
                throw new InvalidOperationException("连接中断");
            }
        }

        #endregion
    }
}
