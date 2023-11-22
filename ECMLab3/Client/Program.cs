using System.IO.Pipes;
using System.Runtime.CompilerServices;

public struct Structure
{
    public double a;
    public double b;
    public double result;
}

class Client
{
    static void Main(string[] args)
    {
        Console.Title = "Clent";
        if (args.Length > 0)
        {
            using (NamedPipeClientStream Client = new(".", args[0], PipeDirection.InOut))
            {
                Client.Connect();
                try
                {
                    while (true)
                    {
                        byte[] bytes = new byte[Unsafe.SizeOf<Structure>()];
                        Client.Read(bytes, 0, bytes.Length);
                        Structure receivedData = Unsafe.As<byte, Structure>(ref bytes[0]);
                        Console.WriteLine($"Received data: a = {receivedData.a}, b = {receivedData.b}");
                        double a = receivedData.a;
                        double b = receivedData.b;
                        int n = 100000;

                        receivedData.result = TrapezoidalRule(a, b, n);
                        Console.WriteLine(receivedData.result);
                        byte[] modified_bytes = new byte[Unsafe.SizeOf<Structure>()];
                        Unsafe.As<byte, Structure>(ref modified_bytes[0]) = receivedData;
                        Client.Write(modified_bytes, 0, modified_bytes.Length);
                    }
                }
                catch (Exception) { }
            }
        }
    }
    static double Function(double x)
    {
        return 2 * Math.Sin(x);
    }

    static double TrapezoidalRule(double a, double b, int n)
    {
        double h = (b - a) / Convert.ToDouble(n);
        double result = 0.5 * (Function(a) + Function(b));

        for (int i = 1; i < n; i++)
        {

            double x = a + i * h;
            result += Function(x);
        }

        result *= h;
        Console.WriteLine(result);
        return result;
    }
}

