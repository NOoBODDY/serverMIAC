using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace HahaServer
{
    class HttpServer
    {
        HttpListener Listener;
        /// <summary>
        /// Use to create dictionary with your methods
        /// </summary>
        /// <param name="param"></param>
        public delegate string Method(string param); //делегат для передачи функций обработки методов


        Dictionary<string, Method> Methods; //словарь <имя метода , функция метода>
        Dictionary<string, string> Headers; //словарь заголовков ответа <имя заголовка, значение>
        public HttpServer(string url, Dictionary<string, Method> methods, Dictionary<string, string> responseHeaders) //конструктор
        {
            Listener = new HttpListener(); //инициализируем
            Listener.Prefixes.Add(url); // забиваем адрес
            Methods = methods;
            Headers = responseHeaders;
        }

        void Solve(object x) // распределение методов
        {
            string jsonRpc;
            HttpListenerContext context = (HttpListenerContext)x;
            HttpListenerRequest request = context.Request;
            using (Stream stream = request.InputStream)
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    jsonRpc = reader.ReadToEnd();
                }
            }
            JObject requestDeserialized = JObject.Parse(jsonRpc); // десериализируем

            string result = Methods[requestDeserialized.SelectToken("method").ToString()]?.Invoke(jsonRpc);
            HttpListenerResponse responseHttp = context.Response;
            foreach (KeyValuePair<string,string> header in Headers)
            {
                responseHttp.AddHeader(header.Key, header.Value);
            }

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(result);
            responseHttp.ContentLength64 = buffer.Length;
            Stream output = responseHttp.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

        }

        /// <summary>
        /// Use in the new Thread
        /// </summary>
        public void Listen()
        {
            while (true)
            {
                HttpListenerContext context = Listener.GetContext();
                Thread newRequest = new Thread(new ParameterizedThreadStart(Solve));
                newRequest.Start(context);
            }
        }

    }
}
