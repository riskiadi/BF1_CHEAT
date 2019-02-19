using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using static Charm;

namespace PZ_BF4
{
    class Pro
    {

        public static string NamePlayer;

        static void Main(string[] args)
        {
            Console.WriteLine("Enter your Origin ID: ");
            NamePlayer = Console.ReadLine();
            if (RPM.Inject("bf1"))
            {
                Console.WriteLine("Have fun " + NamePlayer + "  :)  ");
                Console.WriteLine("Wait: ");

                Thread.Sleep(2000);
                Console.WriteLine("Done: ");

                Overlay overlay = new Overlay();

                Console.ReadLine();
            }
            Console.ReadKey();


        }

    }
}
