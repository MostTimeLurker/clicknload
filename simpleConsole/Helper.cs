using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using Microsoft.SqlServer.Server;

namespace simpleConsole
{
    class Helper
    {
        private HttpListener listener = new HttpListener();


        // netsh http add urlacl http://+:8008/ user=Everyone listen=true
        // C:\Windows\system32>netsh http add urlacl http://127.0.0.1:9666/ user=Jeder listen=yes
        public Helper()
        {
            this.listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            //this.listener.Prefixes.Add("http://localhost:9666/");
            this.listener.Prefixes.Add("http://127.0.0.1:9666/");
        }

        public void Run()
        {
            this.listener.Start();
            while (listener.IsListening)
            {
                wait_request();
            }

            Console.WriteLine(".-....b");
            Console.ReadLine();
            this.listener.Stop();
        }

        private void wait_request()
        {
            IAsyncResult result = this.listener.BeginGetContext(callback, listener);
            result.AsyncWaitHandle.WaitOne();
        }

        private void callback(IAsyncResult result)
        {
            if (this.listener.IsListening)
            {
                HttpListenerContext context = this.listener.EndGetContext(result);
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;


                String rawUrl = request.RawUrl;


                String antwort = "";
                if (rawUrl == "/flash/")
                {
                    antwort = "JDownloader";
                }
                if (rawUrl == "/jdcheck.js")
                {
                    antwort = "jdownloader=true";
                }

                //if (rawUrl == "/flash/addcrypted2")
                if ((rawUrl == "/flash/addcrypted2") || (rawUrl == "/flash/add"))
                {
                    String encodedData = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();
                    String decodedData = HttpUtility.UrlDecode(encodedData);

                    //Console.WriteLine(decodedData);

                    // source=http://download.serienjunkies.org/f-cf71f42575178275/ul_tvs-between-dl-nfhd-x264-101.html&jk=function f(){ return '31393238303030343036323831323939';}&crypted=Ko8CsFALJ/WopqkfLbqyRDewc82TyJd2wzbw454UeXcLf5ozAQnUQkXGhnj1xbG0Mz/9qw30PTqY3QPhWt7H8HdKewctVU13xTsZ3LFT1w7NnDN4h+f6/nKB1bOgC6qBk7+stzvtSqwIeMfHIKcPZNvbDbRormNboQEx+PHZRNgenFaq+in8NjBfQaqYB6ClSFQo6VdSdvrXyXPEW91d9EIqhTvWky+GkANeJqud03+xndo9/OaQu4aI2UjUyKGovHIXGFjCKkrax0NPamdwW0d0L3KC+WYQpAZ5vIB/PFw=
                    String[] data = decodedData.Split('&');

                    Dictionary<String, String> postData = new Dictionary<String, String>();
                    for (int i = 0; i < data.Length; i++)
                    {
                        String ldata = data[i];
                        String lkey = ldata.Substring(0, ldata.IndexOf("="));
                        String lvalue = ldata.Substring(ldata.IndexOf("=") + 1);

                        postData.Add(lkey, lvalue);
                    }

                    //foreach (KeyValuePair<string, string> pair in postData)
                    //{
                    //    Console.WriteLine("key   " + pair.Key);
                    //    Console.WriteLine("value " + pair.Value);
                    //    Console.WriteLine();
                    //}

                    //// jk=function f(){ return '31393238303030343036323831323939';}
                    String sourceUrl = this.doSaveGet("source", postData);
                    String key = this.doSaveGet("jk", postData);
                    String content = this.doSaveGet("crypted", postData);

                    if (key != "")
                    {
                        key = key.Substring(key.IndexOf("'") + 1);
                        key = key.Substring(0, key.IndexOf("'"));
                    }
                    //Console.WriteLine(key);
                    //Console.WriteLine(content);

                    this.doDecodeText(key, content);

                    antwort = "success";
                }

                if (antwort != "")
                {
                    // JDownloader
                    StreamWriter sw = new StreamWriter(response.OutputStream);
                    sw.WriteLine(antwort);
                    sw.Close();
                }



                context.Response.StatusCode = 200;
                context.Response.StatusDescription = "OK";
                context.Response.Close();
            }
            else
            {
                return;
            }
        }

        private String doSaveGet(String key, Dictionary<String, String> dict)
        {
            if (dict.ContainsKey(key))
                return dict[key];

            return "";
        }

        public static string HexToString(string hex)
        {
            var buffer = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                string hexdec = hex.Substring(i, 2);
                buffer[i / 2] = byte.Parse(hexdec, NumberStyles.HexNumber);
            }
            return Encoding.UTF8.GetString(buffer);//we could even have passed this encoding in for greater flexibility.
        }

        public String[] doDecodeText(String key, String data, Encoding encoding = null)
        {
            Byte[] byteData = Convert.FromBase64String(data);
            key = HexToString(key);


            RijndaelManaged rijndaelCipher = new RijndaelManaged();
            rijndaelCipher.Mode = CipherMode.CBC;
            rijndaelCipher.Padding = PaddingMode.Zeros;
            rijndaelCipher.KeySize = 256;
            rijndaelCipher.BlockSize = 128;

            byte[] pwdBytes = Encoding.Default.GetBytes(key);
            byte[] keyBytes = new byte[16];

            int len = pwdBytes.Length;
            if (len > keyBytes.Length) len = keyBytes.Length;

            Array.Copy(pwdBytes, keyBytes, len);

            rijndaelCipher.Key = keyBytes;
            rijndaelCipher.IV = keyBytes;

            var transform = rijndaelCipher.CreateDecryptor();

            byte[] cipherBytes = transform.TransformFinalBlock(byteData, 0, byteData.Length);
            String result = Encoding.UTF8.GetString(cipherBytes).Trim("\0".ToCharArray());
            String[] dateien = result.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            // "http://ul.to/czyny6og\r\nhttp://ul.to/59lc13po\r\nhttp://ul.to/7lbesv7y\r\nhttp://ul.to/16a4jso2\r\nhttp://ul.to/3tqjph07\r\nhttp://ul.to/351a0hh7\0\0\0\0\0\0\0\0"
            //Console.WriteLine("result '" + result + "'");

            return dateien;

        }

    }
}
