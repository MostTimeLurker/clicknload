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
    public struct cnl2Item
    {
        public String rawRequestUrl;

        public String rawSource;
        public String rawKey;
        public String rawCrypted;

        public Dictionary<String, String> rawValues;

        public String key;
        public String[] files;
    }

    class cnl2Helper
    {
        public cnl2Item item;

        public cnl2Helper()
        {
            this.item = new cnl2Item();
        }

        public void doProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            this.item.rawRequestUrl = request.RawUrl;

            String antwort = "";

            switch (this.item.rawRequestUrl)
            {
                case "/flash/":
                    antwort = "JDownloader";
                    break;
                case "/jdcheck.js":
                    antwort = "jdownloader=true";
                    break;
                case "/flash/addcrypted2":
                case "/flash/add":
                    this.doProcessEncryptedData(request);
                    antwort = "success";
                    break;
                default:
                    antwort = "Unbekannter Request '" + this.item.rawRequestUrl + "'";
                    break;
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
        private voi doProcessEncryptedData(HttpListenerRequest request)
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

            this.item.rawSource = this.doSaveGet("source", postData);
            this.item.rawCrypted = this.doSaveGet("crypted", postData);
            this.item.rawKey = this.doSaveGet("jk", postData);

            this.item.rawValues = postData;

            if (this.item.rawKey != "")
            {
                this.item.key = this.item.rawKey;

                this.item.key = this.item.key.Substring(this.item.key.IndexOf("'") + 1);
                this.item.key = this.item.key.Substring(0, this.item.key.IndexOf("'"));
            }

            this.doDecodeText();
        }

        private void doDecodeText()
        {
            Byte[] byteData = Convert.FromBase64String(this.item.rawCrypted);
            String local_key = HexToString(this.item.key);


            RijndaelManaged rijndaelCipher = new RijndaelManaged();
            rijndaelCipher.Mode = CipherMode.CBC;
            rijndaelCipher.Padding = PaddingMode.Zeros;
            rijndaelCipher.KeySize = 256;
            rijndaelCipher.BlockSize = 128;

            byte[] pwdBytes = Encoding.Default.GetBytes(local_key);
            byte[] keyBytes = new byte[16];

            int len = pwdBytes.Length;
            if (len > keyBytes.Length) len = keyBytes.Length;

            Array.Copy(pwdBytes, keyBytes, len);

            rijndaelCipher.Key = keyBytes;
            rijndaelCipher.IV = keyBytes;

            var transform = rijndaelCipher.CreateDecryptor();

            byte[] cipherBytes = transform.TransformFinalBlock(byteData, 0, byteData.Length);
            String result = Encoding.UTF8.GetString(cipherBytes).Trim("\0".ToCharArray());
            this.item.files = result.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
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

    }
}
