using System.Net;
using System.Net.Sockets;
using System.Text;
using Game.Client.Data.Files;
using Raylib_cs;

namespace Game.Client.Net;

static class GameClient
{
    private const int MAX_PACKET_SIZE = 1024;

    private static NetServerFileData? config;
    private static TcpClient? client;
    private static NetworkStream? stream;
    private static bool running = false;
    private static Thread? receiveThread;

    public static void Init()
    {
        config = Resources.GetJson("resources/data/net_config.json", NetServerFileDataCtx.Default.NetServerFileData);

        IPAddress ip = IPAddress.Parse("127.0.0.1");
        if (IPAddress.TryParse(config.Ip, out IPAddress? parsedAddr))
        {
            ip = parsedAddr;
        }

        client = new TcpClient();
        client.Connect(new IPEndPoint(ip, config.Port));
        stream = client.GetStream();

        receiveThread = new Thread(new ThreadStart(UpdateConnection))
        {
            IsBackground = false,
        };
        receiveThread.Start();

        stream.Write(Encoding.UTF8.GetBytes("Damian Meler"));
        running = true;

        Raylib.TraceLog(TraceLogLevel.Info, "[NET CLIENT] Connected to server");
    }

    private static void ProcessData(byte[] buf, int bytesCount)
    {
        if (bytesCount == 0)
        {
            Raylib.TraceLog(TraceLogLevel.Info, "[NET CLIENT] Disconnected from the server");
            running = false;
            return;
        }
    }

    private static void UpdateConnection()
    {
        while (running)
        {
            byte[] buf = new byte[MAX_PACKET_SIZE];
            int bytesRecieved = stream!.Read(buf, 0, buf.Length);
            ProcessData(buf, bytesRecieved);
        }
    }

    public static void Shutdown()
    {
        running = false;
    }
}
