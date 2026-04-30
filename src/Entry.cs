namespace Game;

class Entry
{
    public static void Main(string[] args)
    {
        bool server = args.Contains("-server");

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
}
