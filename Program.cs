using System;
using System.Net;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace HahaServer
{
    class Program
    {
        //БД константы
        const string serverIP = "localhost";
        const string login = "test";
        const string nameBD = "mydb";
        const string password = "popit";

        //
        static bool DEBUG = true;

        static void Main(string[] args)
        {
            Console.WriteLine("Запуск...");



            Thread mainThread = new Thread(new ThreadStart(startListen));
            mainThread.Start();
            menu(mainThread);


            /*listen(listener);
            listen(listener);
            // останавливаем прослушивание подключений
            listener.Stop();
            Console.WriteLine("Обработка подключений завершена");
            Console.Read();*/
            
           
        }
        static void menu(Thread main_thread)
        {
            bool working = true;
            while (working)
            {
                string command = Console.ReadLine();
                switch (command)
                {
                    case "debug on":
                        Console.WriteLine("debug mod on");
                        DEBUG = true;
                        break;
                    case "debug off":
                        Console.WriteLine("debug mod off");
                        DEBUG = false;
                        break;
                    case "shutdown":
                        working = false;
                        main_thread.Abort();
                        break;
                    default:
                        Console.WriteLine("unknown command");
                        break;
                }
            }
        }

        static void messaging(string message)
        {
            Console.WriteLine(message);
        }


        static void listen(HttpListener listener)
        {
            Console.WriteLine("Ожидание подключений...");
            // метод GetContext блокирует текущий поток, ожидая получение запроса 
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            Console.WriteLine(request.Headers);
            using (Stream stream = request.InputStream)
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    Console.WriteLine(reader.ReadToEnd());
                }
            }

            // получаем объект ответа
            HttpListenerResponse response = context.Response;
            // создаем ответ в виде кода html
            string responseStr = "<html><head><meta charset='utf8'></head><body>Привет мир!</body></html>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseStr);
            // получаем поток ответа и пишем в него ответ
            /*response.Headers.Add("ReferrerPolicy: \"unsafe-url\"");
            response.Headers.Add("Access-Control-Allow-Headers: *");
            response.Headers.Add("Access-Control-Allow-Origin: *");*/
            response.AppendHeader("ReferrerPolicy", "unsafe-url");
            response.AppendHeader("Access-Control-Allow-Headers", "*");
            response.AppendHeader("Access-Control-Allow-Origin", "*");
            response.AppendHeader("Access-Control-Allow-Methods", "*");

            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // закрываем поток
            output.Close();
        }

        static void startListen()
        {
            HttpListener listener;
            listener = new HttpListener();
            // установка адресов прослушки
            listener.Prefixes.Add("http://80.87.192.94:80/connection/");
            listener.Start();

            while(true)
            {
                // метод GetContext блокирует текущий поток, ожидая получение запроса 
                HttpListenerContext context = listener.GetContext();
                Thread newRequest = new Thread(new ParameterizedThreadStart(solveRequest));
                
                newRequest.Start(context);
            }

        }
        /// <summary>
        /// Обработка запросов. В отдельный поток
        /// </summary>
        /// <param name="x"></param>
        static void solveRequest(object x ) //метод-поток
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
            Console.WriteLine("Вот что пришло " + jsonRpc);
            string response;
            try
            {
                JObject requestDeserialized = JObject.Parse(jsonRpc);
                
                switch (requestDeserialized.SelectToken("method").ToString())
                {
                    case "sendSMS":
                        response = sendSMS(requestDeserialized.SelectToken("params").SelectToken("phone").ToString());
                        break;
                    case "pinCheck":
                        response = pinCheck(requestDeserialized.SelectToken("params").SelectToken("pin").ToString(), requestDeserialized.SelectToken("params").SelectToken("phone").ToString());
                    break;
                    default:
                        response = unKnownMethod(requestDeserialized.SelectToken("method").ToString());
                        Console.WriteLine("Пришел неизвестный метод");
                        break;
                }
            }
            catch
            {
                response = "";
            }
            
            // получаем объект ответа
            HttpListenerResponse responseHttp = context.Response;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(response);
            responseHttp.AppendHeader("ReferrerPolicy", "unsafe-url");
            responseHttp.AppendHeader("Access-Control-Allow-Headers", "*");
            responseHttp.AppendHeader("Access-Control-Allow-Origin", "*");
            responseHttp.AppendHeader("Access-Control-Allow-Methods", "*");

            responseHttp.ContentLength64 = buffer.Length;
            Stream output = responseHttp.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // закрываем поток
            output.Close();
        }

        static string unKnownMethod(string method)
        {
            JObject param = new JObject();
            param.Add("message","unkmown method");
            JObject response = new JObject();
            response.Add("jsonrpc", "2.0");
            //response.Add("id", 1);
            response.Add("method", method);
            response.Add("params", param);
            return response.ToString();
        }




        /// <summary>
        /// Авторизация или регистрация 
        /// </summary>
        /// <param name="phone"></param>
        static string sendSMS(string phone)
        {
            Console.WriteLine("Работаю ");
            DataBase dataBase = null;
            try 
            {
                dataBase = new DataBase(serverIP, login, nameBD, password); //работаем с БД
                //dataBase.connect();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            if (DEBUG)
            {
                dataBase.Notify += messaging;
            }

            phone = String.Join("", phone.Split('+', ' ', '-')); //удаляем парашу
            Console.WriteLine("Работаю " + phone);
            Random random = new Random();
            
            int queue = random.Next(100);
            string pin = "";
            for (int i=0; i < 4; i++) //генерируем пин
            {
                Random rnd = new Random();
                pin+=rnd.Next(0, 10).ToString();
            }
            bool reg = dataBase.isExistsPatient(phone);
            dataBase.authPatient(phone, pin); // авторизация пользователя

            SMSAERO sms = new SMSAERO();
            if (phone == "79517177545")                         //мой:)
            {
                Console.WriteLine("Это тестовый номер");
                Console.WriteLine("Pin: " + pin);
            }
            else
            {
                sms.send_sms_now(phone, "Ваш код №" + (queue).ToString() + " " + pin, 0, 2); //отправляем смс
            }

            JObject response = new JObject();
            if (reg)
            {
                response.Add("type", "approved");
            }
            else
            {
                response.Add("type", "new");
            }
            response.Add("queue", queue);
            Console.WriteLine(response.ToString());
            return response.ToString();
        }

        static string pinCheck(string pin, string phone)
        {
            JObject response = new JObject();
            phone = String.Join("", phone.Split('+', ' ', '-')); //удаляем парашу
            DataBase dataBase = null;
            try
            {
                dataBase = new DataBase(serverIP, login, nameBD, password); //работаем с БД
                //dataBase.connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            if (DEBUG)
            {
                dataBase.Notify += messaging;
            }
            if (dataBase.checkAuth(phone, pin))
            {
                response.Add("type", "approved");
                response.Add("token", dataBase.getPatientToken(phone));
            }
            else
            {
                response.Add("type", "cancel");
            }
            Console.WriteLine(response.ToString());
            return response.ToString();
        }

        
    }
}
