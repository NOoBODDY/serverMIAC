﻿using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Drawing.Imaging;
using System.Drawing;
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
        static string serverUrl = "http://80.87.192.94:80/connection/";
        static void Main(string[] args)
        {
            Console.WriteLine("Запуск...");

            //Обозначаем все методы бизнес логики
            Dictionary<string, HttpServer.Method> Methods = new Dictionary<string, HttpServer.Method>();
            Methods.Add("sendSMS", sendSMS);
            Methods.Add("pinCheck", pinCheck);
            Methods.Add("getHistory", getHistory);
            Methods.Add("setData", setData);
            Methods.Add("photoAnalize", photoAnalize);
            Methods.Add("saveForm", saveForm);
            Methods.Add("getHistorySnils", getHistorySnils);
            //

            //Обозначаем заголовки ответа
            Dictionary<string, string> Headers = new Dictionary<string, string>();
            Headers.Add("ReferrerPolicy", "unsafe-url");
            Headers.Add("Access-Control-Allow-Headers", "*");
            Headers.Add("Access-Control-Allow-Origin", "*");
            Headers.Add("Access-Control-Allow-Methods", "*");
            //

            HttpServer server = new HttpServer(serverUrl, Methods, Headers);
            Thread listenerThread = new Thread(new ThreadStart(server.Listen));

            menu(listenerThread);

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


        //исправлено
        static string sendSMS(string parameteres)
        {
            JObject requestDeserialized = JObject.Parse(parameteres); // десериализируем
            string phone = requestDeserialized.SelectToken("phone").ToString();

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
        //исправлено
        static string pinCheck(string parameteres)
        {
            JObject requestDeserialized = JObject.Parse(parameteres); // десериализируем
            string pin = requestDeserialized.SelectToken("pin").ToString();
            string phone = requestDeserialized.SelectToken("phone").ToString();
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
        //исправлено
        static string getHistory(string parameteres)
        {
            JObject requestDeserialized = JObject.Parse(parameteres); // десериализируем
            string token = requestDeserialized.SelectToken("token").ToString();
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
        //исправлено
        static string setData(string parameteres)
        {
            JObject requestDeserialized = JObject.Parse(parameteres);
            string token = requestDeserialized.SelectToken("token").ToString();
            int topPress = Convert.ToInt32(requestDeserialized.SelectToken("topPress").ToString());
            int lowPress = Convert.ToInt32(requestDeserialized.SelectToken("lowPress").ToString());
            int pulse = Convert.ToInt32(requestDeserialized.SelectToken("pulse").ToString());
            int saturation = Convert.ToInt32(requestDeserialized.SelectToken("saturation").ToString());
            long unixtime = Convert.ToInt64(requestDeserialized.SelectToken("unixtime").ToString());
            string tag = requestDeserialized.SelectToken("tag").ToString();
            JObject response = new JObject();
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
        //исправлено
        static string photoAnalize(string parameteres)
        {
            JObject requestDeserialized = JObject.Parse(parameteres);
            string token = requestDeserialized.SelectToken("token").ToString();
            string basePhoto = requestDeserialized.SelectToken("photo").ToString();
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

        //исправлено
        static string saveForm(string parameteres)
        {
            JObject json = JObject.Parse(parameteres);
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
        //исправлено
        static string getHistorySnils(string parameteres)
        {
            JObject requestDeserialized = JObject.Parse(parameteres);
            string snils = requestDeserialized.SelectToken("snils").ToString();
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
