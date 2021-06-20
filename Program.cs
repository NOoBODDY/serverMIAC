using System;
using System.Net;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

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
                    case "test anal":
                        Console.WriteLine(photoAnalize("1cac447de0804e52abbf74ab41749678", Convert.ToBase64String(File.ReadAllBytes("t2.jpg"))));
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

            while (true)
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
        static void solveRequest(object x) //метод-поток
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
                    case "getHistory":
                        response = getHistory(requestDeserialized.SelectToken("params").SelectToken("token").ToString());
                        break;
                    case "setData":
                        response = setData(requestDeserialized.SelectToken("params").SelectToken("token").ToString(),
                            Convert.ToInt32(requestDeserialized.SelectToken("params").SelectToken("topPress")),
                            Convert.ToInt32(requestDeserialized.SelectToken("params").SelectToken("lowPress")),
                            Convert.ToInt32(requestDeserialized.SelectToken("params").SelectToken("pulse")),
                            Convert.ToInt32(requestDeserialized.SelectToken("params").SelectToken("saturation")),
                            Convert.ToInt64(requestDeserialized.SelectToken("params").SelectToken("unixtime")),
                            requestDeserialized.SelectToken("params").SelectToken("tag").ToString()
                            ); ;
                        break;
                    case "photoAnalize":
                        response = photoAnalize(requestDeserialized.SelectToken("params").SelectToken("token").ToString(), requestDeserialized.SelectToken("params").SelectToken("photo").ToString());
                        break;
                    case "saveForm":
                        response = saveForm(requestDeserialized);
                        break;
                    default:
                        response = unKnownMethod(requestDeserialized.SelectToken("method").ToString());
                        Console.WriteLine("Пришел неизвестный метод");
                        Console.WriteLine(jsonRpc);
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
            param.Add("message", "unkmown method");
            JObject response = new JObject();
            response.Add("jsonrpc", "2.0");
            //response.Add("id", 1);
            response.Add("method", method);
            response.Add("params", param);
            return response.ToString();
        }




        static string sendSMS(string phone)
        {
            DataBase dataBase = null;
            try
            {
                dataBase = new DataBase(serverIP, login, nameBD, password); //работаем с БД
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            if (DEBUG)
            {
                dataBase.Notify += messaging;
            }

            phone = String.Join("", phone.Split('+', ' ', '-')); //удаляем парашу
            Random random = new Random();

            int queue = random.Next(100);
            string pin = "";
            for (int i = 0; i < 4; i++) //генерируем пин
            {
                Random rnd = new Random();
                pin += rnd.Next(0, 10).ToString();
            }
            bool reg = dataBase.isExistsPatient(phone);
            SMSAERO sms = new SMSAERO();
            if (phone == "79517177545")                         //мой:)
            {
                pin = "0000";
                Console.WriteLine("Это тестовый номер");
                Console.WriteLine("Pin: " + pin);
            }
            else
            {
                sms.send_sms_now(phone, "Ваш код №" + (queue).ToString() + " " + pin, 0, 2); //отправляем смс
            }
            dataBase.authPatient(phone, pin); // авторизация пользователя
            Console.WriteLine(pin);




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
                Console.WriteLine("Получаем пациента");
                Patient patient = dataBase.getPatient(phone);

                Patient.Params average = dataBase.getAverageParams(phone);

                JObject param = new JObject();

                param.Add("id", patient.Id);
                param.Add("firstname", patient.FirstName);
                param.Add("surname", patient.SurName);
                param.Add("lastname", patient.LastName);
                param.Add("snils", patient.Snils);
                param.Add("aTop", average.TopPress);
                param.Add("aLow", average.LowPress);
                param.Add("aPulse", average.Pulse);
                param.Add("aSaturation", average.Saturation);
                response.Add("patient", param);
            }
            else
            {
                response.Add("type", "cancel");
            }
            return response.ToString();
        }

        static string getHistory(string token)
        {
            DataBase dataBase = null;
            try
            {
                dataBase = new DataBase(serverIP, login, nameBD, password); //работаем с БД
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            if (DEBUG)
            {
                dataBase.Notify += messaging;
            }

            Patient patient = dataBase.getHistoryParams(token);
            //Console.WriteLine("тут");
            JArray response = new JArray();
            foreach(Patient.Params i in patient.getParams())
            {
                JObject one = JObject.FromObject(i);
                response.Add(one);
            }
            Console.WriteLine(response.ToString());
            return response.ToString();
        }

        static string setData(string token, int topPress, int lowPress, int pulse, int saturation, long unixtime, string tag)
        {
            JObject response = new JObject();
            //Console.WriteLine("Пациента надо найти");
            DataBase dataBase = null;
            try
            {
                dataBase = new DataBase(serverIP, login, nameBD, password); //работаем с БД
            }
            catch (Exception ex)
            {
                response.Add("err", ex.Message);
                Console.WriteLine(ex);
            }
            if (DEBUG)
            {
                dataBase.Notify += messaging;
            }

            Patient patient = dataBase.getPatient(token);
            //Console.WriteLine("Пациента нашли");
            try
            {
                dataBase.addInfoPatient(token, topPress, lowPress, pulse, saturation, unixtime, tag);
                response.Add("type", "done");
            }
            catch (Exception ex)
            {
                response.Add("err", ex.Message);
            }
            //Console.WriteLine("ответ готов");
            return response.ToString();

        }

        static string photoAnalize(string token, string basePhoto)
        {
            string name = "tonometr";
            DataBase dataBase = null;
            try
            {
                dataBase = new DataBase(serverIP, login, nameBD, password); //работаем с БД
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            if (DEBUG)
            {
                dataBase.Notify += messaging;
            }

            string input = dataBase.getScope(token);

            var param = input.Split(' ');
            Image image = null;
            try 
            {
                if (basePhoto.IndexOf("base64") == -1)
                {
                    image = Image.FromStream(new MemoryStream(Convert.FromBase64String(basePhoto)));
                }
                else
                {
                    image = Image.FromStream(new MemoryStream(Convert.FromBase64String(basePhoto.Substring(22))));
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            
            bool flag = true;
            int i = 1;
            while (flag)
            {
                FileInfo fileInf = new FileInfo(name + ".jpg");
                if (fileInf.Exists)
                {
                    name += i.ToString();
                    i++;
                }
                else
                {
                    image.Save(name + ".jpg", ImageFormat.Jpeg);
                    flag = false;
                }
            }
            string putt = param[1] + ' ' + param[0] + ' ' + param[3] + ' ' + param[2] + ' ' + param[5] + ' ' + param[4];
            ProcessStartInfo procStartInfo = new ProcessStartInfo("python3", "main.py " + name + ".jpg " + putt);
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;

            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();
            String result = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            if (DEBUG)
            {
                Console.WriteLine("Получаем это: " + result);
            }
            

            var res = result.Split(' ');

            JObject json = new JObject();
            if (res.Length == 3)
            {
                json.Add("top", res[0]);
                json.Add("low", res[1]);
                json.Add("pulse", res[2]);
            }
            else
            {
                json.Add("err","badPhoto");
            }
            return json.ToString();
        }
        

        static string saveForm(JObject json)
        {
            
            DataBase dataBase = null;
            try
            {
                dataBase = new DataBase(serverIP, login, nameBD, password); //работаем с БД
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            if (DEBUG)
            {
                dataBase.Notify += messaging;
            }
            dataBase.setInfoPatient(
                json.SelectToken("token").ToString(),
                json.SelectToken("snils").ToString(),
                convertToBool(json.SelectToken("isIrrationalEating").ToString()),
                Convert.ToInt32(json.SelectToken("age").ToString()),
                convertToBool(json.SelectToken("fat").ToString()),
                convertToBool(json.SelectToken("female").ToString()),
                convertToBool(json.SelectToken("smoking").ToString()),
                convertToBool(json.SelectToken("diabetes").ToString()),
                Convert.ToInt32(json.SelectToken("weight").ToString()),
                convertToBool(json.SelectToken("research").ToString()),
                convertToBool(json.SelectToken("leftVentricularHypertension").ToString()),
                convertToBool(json.SelectToken("thickeningCarotidArteryWall").ToString()),
                convertToBool(json.SelectToken("increasedStiffnessArteryWall").ToString()),
                convertToBool(json.SelectToken("moderateIncreaseInSerumCreatinine").ToString()),
                convertToBool(json.SelectToken("research").ToString())
                );
            JObject result = new JObject();
            result.Add("type","done");
            return result.ToString();
        }

        static bool convertToBool (string booling)
        {
            return booling == "true";
        }

        static string getHistorySnils(string snils)
        {
            DataBase dataBase = null;
            try
            {
                dataBase = new DataBase(serverIP, login, nameBD, password); //работаем с БД
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            if (DEBUG)
            {
                dataBase.Notify += messaging;
            }
            Patient patient = dataBase.getPatient(snils);
            patient = dataBase.getHistoryParams(patient.Token);
            JArray response = new JArray();
            foreach (Patient.Params i in patient.getParams())
            {
                JObject one = JObject.FromObject(i);
                response.Add(one);
            }
            Console.WriteLine(response.ToString());
            return response.ToString();
        }

    }
}
