// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using TcpHex;

Console.WriteLine("Hello, World!");

TcpHex.TcpHexClient tcp = new("127.0.0.1", 19999);
tcp.LogBytesLimit = 0;
bool connected = await tcp.ConnectAsync();
Console.WriteLine($"连接：{connected} 属性Connected：{tcp.Connected}");

TestCmdGetSet();

await TestSendRecv(tcp);

static async Task TestSendRecv(TcpHexClient tcp)
{
    int counter = 0;

    Stopwatch sw = Stopwatch.StartNew();

    while (true)
    {
        ///await Task.Delay(1);
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
            if (counter % 10000 == 0)
            {
                Console.WriteLine(
                    $"线程{Environment.CurrentManagedThreadId} 循环：{counter} -- 发送：{dataBytes.Length}  --  接收：{recv.Length}"
                );
            }
        }
        else
        {
            Console.WriteLine($"发送失败：发送接收数据不相等");
        }

        if (sw.Elapsed.TotalSeconds > 100)
        {
            Console.WriteLine($"总共收发：{counter}");
            Console.WriteLine("END");
            break;
        }
    }

    Console.ReadLine();
}

///生成要发送的模拟数据
static byte[] GeneData()
{
    var random = new Random();
    int len = random.Next(16, 64);
    var dataBytes = new byte[len];

    random.NextBytes(dataBytes);

    return dataBytes;
}

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

    cmd.SetValue<byte>(0, b);
    cmd.SetValue<ushort>(1, us);
    cmd.SetValue<short>(3, s);
    cmd.SetValue<uint>(5, ui);
    cmd.SetValue<int>(9, i);
    cmd.SetValue<ulong>(13, ul); //13 14 15 16 , 17 18 19 20
    cmd.SetValue<long>(21, l); //21 22 23 24 , 25 26 27 28
    cmd.SetValue<float>(29, f); //29 30 31 32
    cmd.SetValue<double>(33, d); //33 34 35 36 , 37 38 39 40

    byte byteValue = cmd.GetValue<byte>(0);
    ushort ushortValue = cmd.GetValue<ushort>(1);
    short shortValue = cmd.GetValue<short>(3);
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
