using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Raylib_cs;

namespace Game;

class Entry
{
    public static void Main(string[] args)
    {
        bool server = args.Contains("-server");

        Thread.CurrentThread.Name = "Main";

        unsafe
        {
            Raylib.SetTraceLogCallback(&Trace);
        }

        if (!server)
        {
            var prog = new Client.Program(args);
            prog.Run();
            prog.Shutdown();
        }
        else
        {
            var prog = new Server.Program(args);
            prog.Run();
            prog.Shutdown();
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void Trace(int logLvl, sbyte* format, sbyte* args)
    {
        string msg = Logging.GetLogMessage(new IntPtr(format), new IntPtr(args));

        ConsoleColor typeCol;
        switch ((TraceLogLevel)logLvl)
        {
            case TraceLogLevel.Info:
                typeCol = ConsoleColor.Cyan;
                break;
            case TraceLogLevel.Debug:
                typeCol = ConsoleColor.Green;
                break;
            case TraceLogLevel.Warning:
                typeCol = ConsoleColor.Yellow;
                break;
            case TraceLogLevel.Error:
                typeCol = ConsoleColor.Red;
                break;
            case TraceLogLevel.Fatal:
                typeCol = ConsoleColor.DarkRed;
                break;
            default:
                typeCol = ConsoleColor.Gray;
                break;
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write('[');
        Console.ForegroundColor = typeCol;
        Console.Write(((TraceLogLevel)logLvl).ToString());
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(']');
        Console.Write(' ');
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(msg);
        Console.WriteLine();
        Console.ResetColor();
    }
}
