using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace simpleConsole
{
    class HttpHelper
    {
        public event Action<HttpListenerContext, object> ProcessRequest;

        private readonly HttpListener listener = new HttpListener();
        private readonly Thread serverThread;
        private readonly Thread[] workerThreads;

        private readonly ManualResetEvent _stop, _ready;
        private Queue<HttpListenerContext> _queue;

        // netsh http add urlacl http://+:8008/ user=Everyone listen=true
        // C:\Windows\system32>netsh http add urlacl http://127.0.0.1:9666/ user=Jeder listen=yes
        public HttpHelper(int maxConcurentThreads = 5, int min = 0)
        {
            this.listener.Prefixes.Add("http://127.0.0.1:9666/");
            this.listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;

            this.workerThreads = new Thread[maxConcurentThreads];
            this._queue = new Queue<HttpListenerContext>();
            this._stop = new ManualResetEvent(false);
            this._ready = new ManualResetEvent(false);

            this.serverThread = new Thread(handleListenerRequest);
        }

        public void Start()
        {
            this.listener.Start();
            this.serverThread.Start();

            for (int i = 0; i < workerThreads.Length; i++)
            {
                workerThreads[i] = new Thread(new ParameterizedThreadStart( HttpWorker));
                workerThreads[i].Start(i);
            }
        }

        public void Stop()
        {
            this._stop.Set();
            this.serverThread.Join();

            foreach (Thread worker in this.workerThreads)
                worker.Join();

            this.listener.Stop();
        }


        public void handleListenerRequest()
        {
            while (this.listener.IsListening)
            {
                var context = this.listener.BeginGetContext(ContextReady, null);

                if (0 == WaitHandle.WaitAny(new[] { _stop, context.AsyncWaitHandle }))
                    return;
            }

        }

        private void ContextReady(IAsyncResult ar)
        {
            try
            {
                lock (_queue)
                {
                    _queue.Enqueue(this.listener.EndGetContext(ar));
                    _ready.Set();
                }
            }
            catch { return; }
        }

        private void HttpWorker(object threadNumber)
        {
            WaitHandle[] wait = new[] { _ready, _stop };
            while (0 == WaitHandle.WaitAny(wait))
            {
                HttpListenerContext context;
                lock (_queue)
                {
                    if (_queue.Count > 0)
                        context = _queue.Dequeue();
                    else
                    {
                        _ready.Reset();
                        continue;
                    }
                }

                try { ProcessRequest(context, threadNumber); }
                catch (Exception e) { Console.Error.WriteLine(e); }
            }
        }

    }
}
