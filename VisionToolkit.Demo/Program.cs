using OpenCvSharp;
using System.Text;
using VisionToolkit.Calibration;
using VisionToolkit.Demo.Test;

namespace VisionToolkit.Demo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("=== VisionToolkit Demo ===");
            Console.WriteLine();

            Console.WriteLine("1. Camera Calibration");
            Console.WriteLine("2. Template Matching");
            Console.WriteLine("3. Find Line");

            Console.Write("\nSelect Tool: ");

            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    CalibrationDemo.Run();
                    break;

                case "2":
                    Console.WriteLine(
                        "Template Matching Demo Coming Soon...");
                    break;

                case "3":
                    Console.WriteLine(
                        "Find Line Tool Coming Soon...");
                    break;

                default:
                    Console.WriteLine(
                        "[ERROR] Invalid option.");
                    break;
            }
        }
    }
}