# TcpHexClient



> TcpHexClient是一个基于C#语言的TCP通信库，它作为一个TCP客户端，专门用于与下位机进行交互，收发16进制数据的命令。在该通信模式中，下位机充当TCP服务端，而TcpHexClient则作为客户端向服务端发送命令，并采用一问一答的方式进行通信。需要用户自行解析数据帧。

源码比较简单，详细使用方法请查看源码。

**使用示例：**

```csharp
TcpHexClient tcp = new("192.168.2.3", 19999);
bool connected = await tcp.ConnectAsync();
Console.WriteLine($"连接：{connected} 属性Connected：{tcp.Connected}");

var dataBytes = new byte[8]; // 要发送的数据
dataBytes.SetValue<int>(0, 1234);
dataBytes.SetValue<float>(4, 123.45f);

int recvLength = 8; //要接收的数据长度
byte[] recv; //接收到回复数据

try
{
    recv = await tcp.SendAsync(dataBytes, recvLength);
    var i = recv.GetValue<int>(0);
    var f = recv.GetValue<float>(4);
    Console.WriteLine($"i = {i}, f = {f}");
    //i = 1234, f = 123.45
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}
```

### 特点：

- 自动重连，连接断开后自动重连
- 一问一答（一发一收）模式，指定要接收的字节长度
- 线程安全，可以在多线程中进行收发
- 打印收发数据
- .Net Standard 2.0开发，可以在.Net Framework 和 .Net core 中使用，没有依赖

### 属性：

| 属性名称          | 描述                                                         | 默认值 |
| ----------------- | ------------------------------------------------------------ | ------ |
| Connected         | TcpClient连接状态                                            | false  |
| Timeout           | 读取数据的超时时间，                                         | 1000ms |
| ReceiveTimeout    | 同TcpClient属性ReceiveTimeout                                | 1000ms |
| SendTimeout       | 同TcpClient属性SendTimeout                                   | 1000ms |
| ReceiveBufferSize | 同TcpClient属性ReceiveBufferSize                             | 8192   |
| SendBufferSize    | 同TcpClient属性SendBufferSize                                | 8192   |
| LogBytesLimit     | 默认128，发送/接收的字节数组长度小于128，则将16进制字符串打印到控制台 | 128    |

### 方法：

#### ConnectAsync

连接到TCP服务端，返回连接状态。会启动线程监听连接状态，断线后开始重连。

#### CloseAsync

关闭TCP连接

#### ReceiveAsync

接收指定长度的字节数据。可能会出现连接断开，超时等异常。

#### SendAsync

发送数据，并接收指定长度的回复。可能会出现连接断开，超时等异常。

#### 日志

程序中有一个日志接口，默认会打印日志到控制台，你可以实现此借口

```csharp
public interface ITcpHexLogger
{
    void LogTrace(string message);
    void LogDebug(string message);
    void LogInfo(string message);
    void LogError(string message, Exception ex);
}
```

#### 扩展方法

程序中还有几个byte数组的扩展方法，用于数值和byte数组的互转，具体示例如下：

```csharp
static void TestCmdGetSet()
{
    byte[] cmd = new byte[41];

    const byte b = 0xAA;
    const ushort us = 12345;
    const short s = -4321;
    const uint ui = 1987654321;
    const int i = -123456789;
    const ulong ul = 1234567890;
    const long l = -9876543210;
    const float f = 123.456f;
    const double d = 1234567.1234567d;
    const float epsilon = 0.0001f;

    cmd.SetValue<byte>(0, b); //第0个字节开始，写入一个byte
    cmd.SetValue<ushort>(1, us); //第1个字节开始，写入一个ushort
    cmd.SetValue<short>(3, s);
    cmd.SetValue<uint>(5, ui);
    cmd.SetValue<int>(9, i); //第9个字节开始，写入一个int
    cmd.SetValue<ulong>(13, ul); //13 14 15 16 , 17 18 19 20
    cmd.SetValue<long>(21, l); //21 22 23 24 , 25 26 27 28
    cmd.SetValue<float>(29, f); //29 30 31 32
    cmd.SetValue<double>(33, d); //33 34 35 36 , 37 38 39 40

    byte byteValue = cmd.GetValue<byte>(0);
    ushort ushortValue = cmd.GetValue<ushort>(1);
    short shortValue = cmd.GetValue<short>(3); //从第3个字节开始，读取一个short
    uint uintValue = cmd.GetValue<uint>(5);
    int intValue = cmd.GetValue<int>(9);
    ulong ulongValue = cmd.GetValue<ulong>(13);
    long longValue = cmd.GetValue<long>(21);
    float floatValue = cmd.GetValue<float>(29);
    double doubleValue = cmd.GetValue<double>(33);

    Debug.Assert(byteValue == b);
    Debug.Assert(ushortValue == us);
    Debug.Assert(shortValue == s);
    Debug.Assert(uintValue == ui);
    Debug.Assert(intValue == i);
    Debug.Assert(ulongValue == ul);
    Debug.Assert(longValue == l);
    Debug.Assert(Math.Abs(floatValue - f) <= epsilon);
    Debug.Assert(Math.Abs(doubleValue - d) <= epsilon);
}

```

### 使用示例：

详细使用方法请查看仓库源码。

```csharp
TcpHexClient tcp = new("192.168.2.3", 19999, new TcpHexLogger());
bool connected = await tcp.ConnectAsync();
tcp.LogBytesLimit = 0;
Console.WriteLine($"连接：{connected} 属性Connected：{tcp.Connected}");
await TestSendRecv(tcp);
Console.ReadLine();

static async Task TestSendRecv(TcpHexClient tcp)
{
    int counter = 0;
    Stopwatch sw = Stopwatch.StartNew();

    while (true)
    {
        var dataBytes = GeneData();

        byte[] recv;

        try
        {
            if (tcp.Connected)
            {
                recv = await tcp.SendAsync(dataBytes, dataBytes.Length);
            }
            else
            {
                Thread.Sleep(1000);
                continue;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            continue;
        }

        if (recv.SequenceEqual(dataBytes))
        {
            counter++;
            if (counter % 10 == 0)
            {
                Console.WriteLine(
                    $"循环：{counter} -- 发送：{dataBytes.Length} --  接收：{recv.Length}"
                );
            }
        }
        else
        {
            Console.WriteLine($"发送失败：发送接收数据不相等");
        }

        if (sw.Elapsed.TotalSeconds > 60)
        {
            Console.WriteLine($"总共收发：{counter}");
            Console.WriteLine("END");
            break;
        }
    }

}

///生成要发送的字节数组
static byte[] GeneData()
{
    var random = new Random();
    int len = random.Next(16, 64);
    var dataBytes = new byte[len];
    random.NextBytes(dataBytes);
    return dataBytes;
}


```

