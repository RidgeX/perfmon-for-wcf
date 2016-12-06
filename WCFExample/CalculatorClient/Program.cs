using CalculatorClient.CalculatorService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CalculatorClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Press <ENTER> to start client.");
            Console.WriteLine();
            Console.ReadLine();

            CalculatorServiceClient client = new CalculatorServiceClient();

            double value1 = 100.0;
            double value2 = 15.99;
            double result = client.Add(value1, value2);
            Console.WriteLine("Add({0},{1}) = {2}", value1, value2, result);

            value1 = 145.0;
            value2 = 76.54;
            result = client.Subtract(value1, value2);
            Console.WriteLine("Subtract({0},{1}) = {2}", value1, value2, result);

            value1 = 9.0;
            value2 = 81.25;
            result = client.Multiply(value1, value2);
            Console.WriteLine("Multiply({0},{1}) = {2}", value1, value2, result);

            value1 = 22.0;
            value2 = 7.0;
            result = client.Divide(value1, value2);
            Console.WriteLine("Divide({0},{1}) = {2}", value1, value2, result);

            PerformanceCounter counter = new PerformanceCounter("Test category", "Test counter");
            Random random = new Random();
            bool running = true;

            while (running)
            {
                value1 = random.NextDouble() * 100.0;
                value2 = random.NextDouble() * 100.0;
                Parallel.Invoke(
                    () => client.Add(value1, value2),
                    () => client.Add(value1, value2),
                    () => client.Add(value1, value2)
                );
                Console.WriteLine("Test: {0}", counter.NextValue());
            }

            client.Close();

            Console.WriteLine();
            Console.WriteLine("Press <ENTER> to terminate client.");
            Console.ReadLine();
        }
    }
}
