using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace CalculatorServiceLibrary
{
    public class CalculatorService : ICalculatorService
    {
        private static long angle;
        private static PerformanceCounter counter;
        private static Random random;

        static CalculatorService()
        {
            angle = 0;
            if (!PerformanceCounterCategory.Exists("Test category"))
            {
                PerformanceCounterCategory.Create("Test category", "Description",
                    PerformanceCounterCategoryType.SingleInstance,
                    "Test counter", "Description");
            }
            counter = new PerformanceCounter("Test category", "Test counter");
            counter.ReadOnly = false;
            counter.RawValue = 0;
            random = new Random();
        }

        public double Add(double n1, double n2)
        {
            double result = n1 + n2;
            Console.WriteLine("Received Add({0},{1})", n1, n2);
            Console.WriteLine("Return: {0}", result);
            counter.RawValue = (long)(30.0 * Math.Sin(angle * Math.PI / 180.0) + 50.0);
            angle = (angle + 3) % 360;
            Thread.Sleep(random.Next(100));
            return result;
        }

        public double Subtract(double n1, double n2)
        {
            double result = n1 - n2;
            Console.WriteLine("Received Subtract({0},{1})", n1, n2);
            Console.WriteLine("Return: {0}", result);
            return result;
        }

        public double Multiply(double n1, double n2)
        {
            double result = n1 * n2;
            Console.WriteLine("Received Multiply({0},{1})", n1, n2);
            Console.WriteLine("Return: {0}", result);
            return result;
        }

        public double Divide(double n1, double n2)
        {
            double result = n1 / n2;
            Console.WriteLine("Received Divide({0},{1})", n1, n2);
            Console.WriteLine("Return: {0}", result);
            return result;
        }
    }
}
