using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simpleConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
             TODO: 
             * httplistener multithreaded
             * try/catch
             * dokumentation
             * source -> paketnamen
             * source -> passwort
             * export nach dlc? falls moeglich
             
             */
            Helper helper = new Helper();
            helper.Run();

            Console.ReadLine();
        }
    }
}
