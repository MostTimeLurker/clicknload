using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace simpleConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
             TODO: 
             * httplistener multithreaded done
             * try/catch
             * dokumentation
             * source -> paketnamen
             * source -> passwort
             * export nach dlc? falls moeglich
             
             */
            HttpHelper httpHelper = new HttpHelper();
            httpHelper.ProcessRequest += HttpHelperOnProcessRequest;
            httpHelper.Start();
            //HttpHelper.Run();

            Console.WriteLine("------");
            Console.ReadLine();
            httpHelper.Stop();
        }

        private static void HttpHelperOnProcessRequest(HttpListenerContext httpListenerContext, object threadNumber)
        {
            Console.WriteLine("HttpHelperOnProcessRequest BEGIN " + threadNumber);

            cnl2Helper cnl2 = new cnl2Helper();
            cnl2.doProcessRequest(httpListenerContext);


            Console.WriteLine("HttpHelperOnProcessRequest END " + threadNumber);
        
        }
    }
}
