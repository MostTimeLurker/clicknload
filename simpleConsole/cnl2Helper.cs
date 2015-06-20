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

namespace simpleConsole
{
    class cnl2Helper
    {
        public void doProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            
            String rawUrl = request.RawUrl;
            //Console.WriteLine(rawUrl);
            
            String antwort = "";
            String[] dateien;

            switch (rawUrl)
            {
                case "/flash/":
                    antwort = "JDownloader";
                    break;
                case "/jdcheck.js":
                    antwort = "jdownloader=true";
                    break;
                case "/flash/addcrypted2":
                case "/flash/add":
                    dateien = this.doProcessEncryptedData(request);
                    antwort = "success";
                    break;
                default:
                    break;
            }

            if (antwort == "")
            {
                antwort = "Unbekannter Request '" + rawUrl + "'";
            }

            StreamWriter sw = new StreamWriter(response.OutputStream);
            sw.WriteLine(antwort);
            sw.Close();

            context.Response.StatusCode = 200;
            context.Response.StatusDescription = "OK";
            context.Response.Close();
        }

        // Umbauen in Object mit allen POST Daten'?
        // Fehlerbehandlung!
        private String[] doProcessEncryptedData(HttpListenerRequest request)
        {
            String encodedData = new StreamReader(request.InputStream, request.ContentEncoding).ReadToEnd();
            String decodedData = HttpUtility.UrlDecode(encodedData);

            String[] data = decodedData.Split('&');

            Dictionary<String, String> postData = new Dictionary<String, String>();
            for (int i = 0; i < data.Length; i++)
            {
                String ldata = data[i];
                String lkey = ldata.Substring(0, ldata.IndexOf("="));
                String lvalue = ldata.Substring(ldata.IndexOf("=") + 1);

                postData.Add(lkey, lvalue);
            }

            //// jk=function f(){ return '31393238303030343036323831323939';}
            String sourceUrl = this.doSaveGet("source", postData);
            String key = this.doSaveGet("jk", postData);
            String content = this.doSaveGet("crypted", postData);

            if (key != "")
            {
                key = key.Substring(key.IndexOf("'") + 1);
                key = key.Substring(0, key.IndexOf("'"));
            }

            return this.doDecodeText(key, content);
        }

        private String doSaveGet(String key, Dictionary<String, String> dict)
        {
            if (dict.ContainsKey(key))
                return dict[key];

            return "";
        }

        private static string HexToString(string hex)
        {
            var buffer = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                string hexdec = hex.Substring(i, 2);
                buffer[i / 2] = byte.Parse(hexdec, NumberStyles.HexNumber);
            }
            return Encoding.UTF8.GetString(buffer);//we could even have passed this encoding in for greater flexibility.
        }

        private String[] doDecodeText(String key, String data, Encoding encoding = null)
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

            return dateien;

        }


    }
}
