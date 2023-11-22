using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.CompilerServices;

public struct Structure
{
    public double a;
    public double b;
    public double result;
}

class PipeServer
{
    private static PriorityQueue<Structure, int> dataQueue = new PriorityQueue<Structure, int>();
    private static Mutex mutex = new Mutex();
    private static Mutex mutFile = new Mutex();
    private static int count = 0;
    private static string path = "C:\\Users\\stafe\\source\\repos\\ECMLab3\\ECMLab3\\Client\\bin\\Debug\\net7.0\\Client.exe";
    private static StreamWriter file = new StreamWriter($"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\output.txt", true);
    private static CancellationTokenSource source = new CancellationTokenSource();
    private static CancellationToken token = source.Token;
    private static async Task Main()
    {


        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            source.Cancel();
        };

        try
        {
            await Task.WhenAll(SenderTask(token), ReceiverTask(token)); ;
        }
        catch (Exception error)
        {
            Console.WriteLine(error.Message);
        }
        finally
        {
            file.Close();
        }
    }
    static Task SenderTask(CancellationToken token)
    {
        return Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                double _n = 0, _m = 0;
                int _priority = 0;
                
                while (!token.IsCancellationRequested)
                {
                    Console.WriteLine("Enter A: ");
                    try
                    {
                        _n = double.Parse(Console.ReadLine());
                        break;
                    }
                    catch 
                    {
                        ColoredMsg("A must be double!", 1);
                    }
                }
                while (!token.IsCancellationRequested)
                {
                    Console.WriteLine("Enter B: ");
                    try
                    {
                        _m = double.Parse(Console.ReadLine());
                        break;
                    }
                    catch
                    {
                        ColoredMsg("B must be double!", 1);
                    }
                }
                while (!token.IsCancellationRequested)
                {
                    Console.WriteLine("Enter Priority: ");
                    try
                    {
                        _priority = int.Parse(Console.ReadLine());
                        break;
                    }
                    catch
                    {
                        ColoredMsg("Priority must be double!", 1);
                    }
                }



                Structure data = new Structure
                {
                    a = _n,
                    b = _m,
                };
                mutex.WaitOne();
                dataQueue.Enqueue(data, _priority);
                mutex.ReleaseMutex();
                await Task.Delay(1000);
            }
        });
    }

    static Task ReceiverTask(CancellationToken token)
    {
        return Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                Structure st;
                int pr;
                mutex.WaitOne();
                bool flag = dataQueue.TryDequeue(out st, out pr);
                mutex.ReleaseMutex();
                if (flag)
                {
                    ClientConnect(st, token);
                }
            }
        });
    }

    static async void ClientConnect(Structure st, CancellationToken token)
    {
        try
        {
            byte[] dataBytes = new byte[Unsafe.SizeOf<Structure>()];
            Unsafe.As<byte, Structure>(ref dataBytes[0]) = st;
            NamedPipeServerStream pipeServer = new($"channel{count}", PipeDirection.InOut);
            Console.WriteLine("Waiting for client connection...");
            Process myProcess = new Process();
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.FileName = path;
            myProcess.StartInfo.Arguments = $"channel{count}";
            myProcess.StartInfo.CreateNoWindow = true;
            myProcess.Start();
            await pipeServer.WaitForConnectionAsync();
            Console.WriteLine("Client connected");
            await pipeServer.WriteAsync(dataBytes, 0, dataBytes.Length);
            byte[] receivedBytes = new byte[Unsafe.SizeOf<Structure>()];
            if (await pipeServer.ReadAsync(receivedBytes, 0, receivedBytes.Length) == receivedBytes.Length)
            {
                st = Unsafe.As<byte, Structure>(ref receivedBytes[0]);
            }
            mutFile.WaitOne();
            string res = $"a = {st.a}; b = {st.b}; priority = {0}; result = {st.result}";
            file.WriteLine(res);
            Console.WriteLine(res);
            Console.Beep();
            mutFile.ReleaseMutex();
            pipeServer.Close();
            count++;
            await myProcess.WaitForExitAsync(token);
        }
        catch (Exception) { }
    }

    static void ColoredMsg(string msg, uint level) 
    {
        switch (level)
        {
            case 0:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(msg);
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case 1:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(msg);
                Console.ForegroundColor = ConsoleColor.White; 
                break;
            case 2:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg);
                Console.ForegroundColor= ConsoleColor.White;
                break;
        }
    }

}


