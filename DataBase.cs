﻿using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace HahaServer
{
    /// <summary>
    /// Класс, описывающий пациента. Может хранить всю статистику о нём
    /// </summary>
    public class Patient
    {
        #region vars
        List<Params> parametres = new List<Params>();//лист с измерениями
        private int id;
        private string firstName;
        private string surName;
        private string lastName;
        private string token;
        private string phone;
        private string snils;
        public int Id { get { return id; } private set { id = value; } }
        public string FirstName { get { return firstName; } private set { firstName = value; } }
        public string SurName { get { return this.surName; } private set { surName = value; } }
        public string LastName { get { return this.lastName; } private set { lastName = value; } }
        public string Token { get { return this.token; } private set { token = value; } }
        public string Phone { get { return this.phone; } private set { phone = value; } }
        public string Snils { get { return this.snils; } private set { snils = value; } }
        #endregion
        public Patient(int id, string firstName, string surName, string lastName, string token, string phone)
        {
            this.firstName = firstName;
            this.id = id;

            this.surName = surName;
            this.lastName = lastName;
            this.token = token;
            this.phone = phone;
        }
        public Patient(string phone, string token)
        {
            Phone = phone;
            Token = token;
        }

        public Patient(int id, string firstName, string surName, string lastName, string token, string phone, string snils)
        {
            Id = id;
            FirstName = firstName;
            SurName = surName;
            LastName = lastName;
            Token = token;
            Phone = phone;
            Snils = snils;
        }
        #region getInfo

        /// <summary>
        /// Получить все измерения пациента
        /// </summary>
        /// <returns></returns>
        public List<Params> getParams() { return this.parametres; }
        /// <summary>
        /// Получить строку со всей инфой
        /// </summary>
        /// <returns></returns>
        public string toString()
        {
            return Id.ToString() + " " + FirstName + " " + SurName + " " + LastName + " " + Token + " " + Phone;
        }
        #endregion


        /// <summary>
        /// Добавит измерение пациенту
        /// </summary>
        /// <param name="p"></param>
        public void addParams(Params p)
        {
            parametres.Add(p);
        }

        /// <summary>
        /// Класс, хранящий в себе одну запись измерений пациента
        /// </summary>
        public class Params
        {
            private int lowPress;
            private int topPress;
            private int pulse;
            private int saturation;
            private long unixtime;
            private string tag;
            private int id;
            public string Tag { get { return tag; } private set { tag = value; } }
            public int Id { get { return id; } private set { id = value; } }
            public int LowPress { get { return this.lowPress; } private set { this.lowPress = value; } }
            public int TopPress { get { return this.topPress; } private set { this.topPress = value; } }
            public int Pulse { get { return this.pulse; } private set { this.pulse = value; } }
            public int Saturation { get { return this.saturation; } private set { this.saturation = value; } }
            public long Unixtime { get { return this.unixtime; } private set { this.unixtime = value; } }
            public Params(int id, int lowpress, int toppress, int pulse, long unixtime, string tag, int saturation = 0)
            {
                this.Tag = tag;
                this.Id = id;
                this.LowPress = lowpress;
                this.TopPress = toppress;
                this.Pulse = pulse;
                this.Unixtime = unixtime;
                if (saturation != 0)
                {
                    this.Saturation = saturation;
                }
            }
            public string toString()
            {
                return lowPress.ToString() + " " + topPress.ToString() + " " + pulse.ToString() + " " + unixtime.ToString();
            }

        }

    }
    class DataBase
    {

        #region vars
        public delegate void Status(string message);
        public event Status Notify;
        private string connectionParams;
        private MySqlConnection ConnectionDef;
        private static bool isConnected = false;
        private MySqlDataAdapter daUser;
        private MySqlDataReader reader;
        MySqlConnectionStringBuilder conn_string = new MySqlConnectionStringBuilder();
        #endregion
        public DataBase(string ServerIP, string Login, string NameBD, string Password)
        {

            conn_string.Server = "127.0.0.1";
            conn_string.Port = 3306;
            conn_string.UserID = "root";
            conn_string.Password = "qort0408";
            conn_string.Database = "mydb";


            //conn_string.Server = "127.0.0.1";
            //conn_string.Port = 3306;
            //conn_string.UserID = "root";
            //conn_string.Password = "qort0408";
            //conn_string.Database = "hakaton1806";



            ConnectionDef = new MySqlConnection(conn_string.ToString());
        }




        #region ForPatient

        public Patient.Params getAverageParams(string tokenOrPhoneOrSnils)
        {
            Notify?.Invoke("Started getAverageParams");
            List<Patient.Params> paramses = new List<Patient.Params>();
            Patient.Params param = new Patient.Params(0, 0, 0, 0,0, "aboba");
            Patient patient = null;

            try
            {
                using (ConnectionDef)
                {
                    ConnectionDef.Open();
                    string request = "";
                    switch (tokenOrPhoneOrSnils.Length)
                    {
                        case 32:
                            request = "SELECT id, firstname, surname,lastname,token,phonenum,snils FROM patient " +
                           "WHERE token=\"" + tokenOrPhoneOrSnils + "\";";
                            break;
                        case 11:
                            request = "SELECT id, firstname, surname,lastname,token,phonenum,snils FROM patient " +
                           "WHERE phonenum=\"" + tokenOrPhoneOrSnils + "\";";
                            break;
                        case 14:
                            request = "SELECT id, firstname, surname,lastname,token,phonenum,snils FROM patient" +
                           "WHERE snils=\"" + tokenOrPhoneOrSnils + "\";";
                            break;
                        default: throw new Exception("Введен неккоректный аргумент");
                    }
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    using (MySqlDataReader reader = cmdSel.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            patient = new Patient(Int32.Parse(reader[0].ToString()), reader[1].ToString(),
                                reader[2].ToString(), reader[3].ToString(), reader[4].ToString(), reader[5].ToString(), reader[6].ToString());
                        }
                        else
                        {
                            Notify?.Invoke("Пациент не найден");
                        }
                    }
                    request = "SELECT id, lowpress, topPress, pulse, unixtime, tag, saturation FROM params where patientid =+" + patient.Id + "+ AND unixtime>(UNIX_TIMESTAMP()-7*24*60*60);";

                    using (MySqlDataReader reader = cmdSel.ExecuteReader())
                    {

                        while (reader.Read())
                        {
                            Patient.Params par = new Patient.Params(Int32.Parse(reader[0].ToString()),
                                Int32.Parse(reader[1].ToString()), Int32.Parse(reader[2].ToString()), Int32.Parse(reader[3].ToString()), Int64.Parse(reader[5].ToString()), reader[4].ToString(), Int32.Parse(reader[5].ToString()));
                            paramses.Add(par);
                        }

                    }
                    int lowPress = 0, topPress = 0, pulse = 0, satiration = 0, unixtime = 0;
                    foreach (Patient.Params i in paramses)
                    {
                        lowPress += i.LowPress;
                        topPress += i.TopPress;
                        satiration += i.Saturation;

                    }
                    lowPress = lowPress / paramses.Count;
                    topPress = topPress / paramses.Count;
                    satiration = satiration / paramses.Count;
                    param = new Patient.Params(0,lowPress, topPress, pulse, unixtime, "aboba",satiration);

                }

            }
            catch (Exception e)
            {
                Notify?.Invoke(e.Message);
            }
            return param;
        }

        /// <summary>
        /// Проверить существует ли пациент
        /// </summary>
        /// <param name="phone"></param>
        public bool isExistsPatient(string phone)
        {
            Notify?.Invoke("Started isExistsPatient");
            try
            {
                using (ConnectionDef)
                {

                    ConnectionDef.Open();
                    string request;
                    request = "SELECT * FROM patient WHERE phonenum=\"" + phone + "\";";
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    MySqlDataReader reader = cmdSel.ExecuteReader();
                    if (reader.Read())
                    {
                        Notify?.Invoke("Пользователь найден");
                        reader.Close();
                        return true;
                    }
                    else
                    {
                        Notify?.Invoke("Пользователь не найден");
                        reader.Close();
                        return false;
                    }

                }
            }
            catch (Exception e)
            {
                Notify?.Invoke(e.Message);
                return false;
            }


        }

        /// <summary>
        /// Авторизация или регистрация пользователя
        /// </summary>
        /// <param name="phone"></param>
        /// <param name="codenum"></param>
        public void authPatient(string phone, string codenum)
        {
            Notify?.Invoke("Started authPatient");
            if (isExistsPatient(phone))
            {
                using (ConnectionDef)
                {
                    ConnectionDef.Open();
                    try
                    {
                        string request;
                        request = "UPDATE patient SET codenum=\"" + codenum + "\" WHERE phonenum=\"" + phone + "\";";
                        MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                        cmdSel.ExecuteNonQuery();
                        Notify?.Invoke("Код авторизации добавлен");
                    }
                    catch (Exception e)
                    {
                        Notify?.Invoke(e.Message);
                    }
                }

            }
            else
            {
                Notify?.Invoke("Пользователя не существует, запущена регистрация");
                addPatient(phone, Guid.NewGuid().ToString("N"), codenum);
            }


        }

        //Работает
        /// <summary>
        /// Узнать токен пациента
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public string getPatientToken(string phone)
        {
            Notify?.Invoke("Started getPatientToken");
            Notify?.Invoke("Ищем токен пользователя с номером телефона " + phone);
            string token = null;
            try
            {
                Notify?.Invoke("До сих пор ищем токен пользователя с номером телефона " + phone);
                using (ConnectionDef)
                {
                    Notify?.Invoke("Ещё не нашли токен пользователя с номером телефона " + phone);
                    ConnectionDef.Open();
                    string request;
                    request = "SELECT token FROM patient WHERE phonenum=\"" + phone + "\";";
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    using (MySqlDataReader reader = cmdSel.ExecuteReader())
                    {
                        Notify?.Invoke("Ищем токен пользователя с номером телефона " + phone + " \nДа где он там?????");
                        if (reader.Read())
                        {
                            token = reader[0].ToString();
                            Notify?.Invoke("Найден токен " + token + " пользователя с номером телефона " + phone);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Notify?.Invoke(e.Message);
            }




            return token;
        }

        public Patient getPatient(string tokenOrPhoneOrSnils)
        {
            Notify?.Invoke("Started getPatient");
            Patient patient = null;
            string type;
            try
            {
                using (ConnectionDef)
                {
                    ConnectionDef.Open();
                    string request = "";
                    switch (tokenOrPhoneOrSnils.Length)
                    {
                        case 32:
                            request = "SELECT id, firstname, surname,lastname,token,phonenum,snils FROM patient " +
                           "WHERE token=\"" + tokenOrPhoneOrSnils + "\";";
                            type = "токену";
                            Notify?.Invoke("Поиск по номеру токену");
                            break;
                        case 11:
                            request = "SELECT id, firstname, surname,lastname,token,phonenum,snils FROM patient " +
                           "WHERE phonenum=\"" + tokenOrPhoneOrSnils + "\";";
                            type = "номеру телефона";
                            Notify?.Invoke("Поиск по номеру телефона");
                            break;
                        case 14:
                            request = "SELECT id, firstname, surname,lastname,token,phonenum,snils FROM patient " +
                           "WHERE snils=\"" + tokenOrPhoneOrSnils + "\";";
                            Notify?.Invoke("Поиск по снилсу");
                            type = "снилсу";
                            break;
                        default: throw new Exception("Введен неккоректный аргумент");
                    }
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    using (MySqlDataReader reader = cmdSel.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Notify?.Invoke("Пациент найден по "+type+" " + tokenOrPhoneOrSnils);
                            patient = new Patient(Int32.Parse(reader[0].ToString()), reader[1].ToString(),
                                reader[2].ToString(), reader[3].ToString(), reader[4].ToString(), reader[5].ToString(), reader[6].ToString());
                        }
                        else
                        {
                            Notify?.Invoke("Пациент не найден");
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Notify?.Invoke(e.Message);
            }


            return patient;
        }

        /// <summary>
        /// Проверяет код введенный пациентом
        /// </summary>
        /// <param name="phone"></param>
        /// <param name="codenum"></param>
        /// <returns></returns>
        public bool checkAuth(string phone, string codenum)
        {
            Notify?.Invoke("Started checkAuth");
            try
            {
                using (ConnectionDef)
                {
                    ConnectionDef.Open();
                    string request;
                    request = "SELECT codenum FROM patient WHERE phonenum=\"" + phone + "\";";
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    using (reader = cmdSel.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string phonecode = reader[0].ToString();
                            Notify?.Invoke("Код в базе: " + phonecode + " \nКод введенный пользователем: " + codenum);
                            reader.Close();
                            if (phonecode.Equals(codenum)) return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Notify?.Invoke(e.Message);
            }
            return false;
        }

        //Передедать, не должно быть токена, или всё норм
        /// <summary>
        /// Добавляем пациента без врача(только телефон,токен и коддоступа)
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="surname"></param>
        /// <param name="lastName"></param>
        /// <param name="phone"></param>
        /// <param name="token"></param>
        /// <param name="doctorID"></param>
        private void addPatient(string phone, string token, string phonecode)
        {
            Notify?.Invoke("Started addPatient");
            try
            {
                using (ConnectionDef)
                {
                    ConnectionDef.Open();
                    string request;
                    request = "INSERT INTO patient (phonenum,token,CodeNum) VALUES " +
                        "(\"" + phone + "\",\"" + token + "\",\"" + phonecode + "\");";
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    cmdSel.ExecuteNonQuery();
                    Notify?.Invoke("Пользователь добавлен");
                }
            }
            catch (Exception e)
            {
                Notify?.Invoke(e.Message);
            }


        }

        //Делать lastName null или записывать пустоту
        /// <summary>
        /// Добавляем пациента с врачом
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="surname"></param>
        /// <param name="lastName"></param>
        /// <param name="phone"></param>
        /// <param name="token"></param>
        /// <param name="doctorID"></param>
        public void addPatient(string phone, int doctorID, string firstName, string surname, string lastName = null)
        {
            Notify?.Invoke("Started addPatient");
            try
            {
                using (ConnectionDef)
                {
                    int patientID;
                    string request;
                    string token = Guid.NewGuid().ToString("N");
                    if (lastName != null)
                    {
                        request = "INSERT INTO patient (firstname, surname, lastName, phonenum, token) VALUES " +
                       "(\"" + firstName + "\",\"" + surname + "\",\"" + lastName + "\",\"" + phone + "\",\"" + token + "\");";
                    }
                    else
                    {
                        request = "INSERT INTO patient (firstname, surname, phonenum,token) VALUES " +
                       "(\"" + firstName + "\",\"" + surname + "\",\"" + phone + "\",\"" + token + "\");";
                    }
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    cmdSel.ExecuteNonQuery();
                    Notify?.Invoke("Пользователь добавлен");
                    request = "SELECT id FROM patient WHERE token = \"" + token + "\";";
                    cmdSel = new MySqlCommand(request, ConnectionDef);
                    patientID = Int32.Parse(cmdSel.ExecuteScalar().ToString());
                    request = "INSERT INTO doctorpatient (patientid,doctorid) VALUES (" + patientID + "," + doctorID + ");";
                    cmdSel = new MySqlCommand(request, ConnectionDef);
                    cmdSel.ExecuteNonQuery();
                    Notify?.Invoke("Пользователь привязан к врачу");
                }
            }

            catch (Exception e)
            {
                Notify?.Invoke(e.Message);
            }

        }
        public string getScope(string token)
        {
            Notify?.Invoke("Started getScope with token " + token);
            string res = null;
            int pacientID = 0;
            try
            {
                using (ConnectionDef)
                {
                    ConnectionDef.Open();
                    string request = "SELECT id FROM patient WHERE token=\"" + token + "\";";
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    using (MySqlDataReader reader = cmdSel.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            pacientID = Int32.Parse(reader[0].ToString());
                        }
                        else
                        {
                            Notify?.Invoke("ПОльзователь с токеном " + token + " не найден");
                        }
                    }
                    request = "SELECT MAX(topPress), MIN(toppress), MAX(lowPress), MIN(lowPress), MAX(pulse), MIN(Pulse) FROM params WHERE pacientid=" + pacientID + " AND unixtime=UNIX_TIMESTAMP()-7*24*60*60;";
                    cmdSel = new MySqlCommand(request, ConnectionDef);
                    using (MySqlDataReader reader = cmdSel.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            res = reader[0].ToString() + " " + reader[1].ToString() + " " + reader[2].ToString() + " " + reader[3].ToString() + " " + reader[4].ToString() + " " + reader[5].ToString();
                            if(reader[0].ToString().Equals(reader[1].ToString()) || reader[2].ToString().Equals(reader[3].ToString()) || reader[4].ToString().Equals(reader[5].ToString()))
                            {               
                                Notify?.Invoke("У пользователя недостаточно измерений");
                                return null;
                            }
                        }
                        else
                        {
                            Notify?.Invoke("У пользователя нет измерений");
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Notify?.Invoke(e.Message);
            }
            Notify?.Invoke("Выбраны границы: " +res);
            Notify?.Invoke("Stop getScope");
            return res;
        }
        //Работает
        /// <summary>
        /// Добавляем статистику пациента
        /// </summary>
        /// <param name="token"></param>
        /// <param name="topPress"></param>
        /// <param name="lowPress"></param>
        /// <param name="Pulse"></param>
        public void addInfoPatient(string token, int topPress, int lowPress, int Pulse, int saturation, long unixtime, string tag)
        {
            Notify?.Invoke("Started addInfoPatient");
            int patientID = 0;
            try
            {
                using (ConnectionDef)
                {
                    ConnectionDef.Open();
                    string request;
                    request = "SELECT id from patient where token=\"" + token + "\";";
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    using (reader = cmdSel.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            patientID = Int32.Parse(reader[0].ToString());
                        }
                    }
                    request = "INSERT INTO params (patientId, unixtime, topPress, lowPress,pulse, saturation, tag) VALUES " +
                        "(" + patientID + "," + unixtime + "," + topPress + "," + lowPress + "," + Pulse + "," + saturation + ",\""+tag+"\");";
                    cmdSel = new MySqlCommand(request, ConnectionDef);
                    cmdSel.ExecuteNonQuery();
                    Notify?.Invoke("Измерение пользователя " + patientID + " добавлено");
                }

            }
            catch (Exception e)
            {
                Notify?.Invoke(e.Message);
            }
        }

        //Надо обсудить что отправляет приложение
        /// <summary>
        /// Получаем историю, нужно поменять возвращаемый тип
        /// </summary>
        /// <param name="patientID"></param>     
        public Patient getHistoryParams(string token)
        {
            Notify?.Invoke("Started getHistoryParams");
            Patient patient = null;
            string request;
            try
            {
                using (ConnectionDef)
                {
                    ConnectionDef.Open();
                    request = "SELECT id, firstname, surname, lastname, token, phonenum FROM patient WHERE token=\"" + token + "\";";
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    using (MySqlDataReader reader = cmdSel.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string s = reader[0].ToString();
                            int id = Int32.Parse(s);
                            patient = new Patient(Int32.Parse(reader[0].ToString()), reader[1].ToString(), reader[2].ToString(), reader[3].ToString(), reader[4].ToString(), reader[5].ToString());
                            Notify?.Invoke("Find patient with " + token + ": " + patient.toString());
                        }
                        else
                        {
                            Notify?.Invoke("Patient with token " + token + " not found");
                            return null;
                        }
                    }
                    int patientID = patient.Id;
                    request = "SELECT  id, lowpress, toppress, pulse, unixtime, tag FROM params WHERE patientid=" + patientID + " ORDER BY id DESC;";
                    cmdSel = new MySqlCommand(request, ConnectionDef);
                    using (MySqlDataReader reader = cmdSel.ExecuteReader())
                    {
                        
                        while (reader.Read())
                        {
                            Patient.Params para = new Patient.Params(Int32.Parse(reader[0].ToString()), 
                                Int32.Parse(reader[1].ToString()), Int32.Parse(reader[2].ToString()), 
                                Int32.Parse(reader[3].ToString()),
                                Int32.Parse(reader[5].ToString()),reader[4].ToString());
                            patient.addParams(para);
                            Notify?.Invoke(para.toString());
                        }
                        Notify?.Invoke("Returned params of patientID " + patientID);
                    }
                }
            }
            catch (Exception e)
            {
                Notify?.Invoke(e.Message);
            }
            Notify?.Invoke("Stop getHistoryParams");
            return patient;
        }
        #endregion


        #region ForDoctor

        /// <summary>
        /// Добавляем врача
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="surname"></param>
        /// <param name="lastName"></param>
        public void addDoctor(string login, string pass, string firstName, string surname, string lastName = null)
        {
            Notify?.Invoke("Started addDoctor");
            if (isConnected)
            {
                try
                {
                    string request;
                    if (lastName == null)
                    {
                        request = "INSERT INTO doctor (login, pass, firstname, surname) VALUES " +
                       "(\"" + login + "\",\"" + pass + "\",\"" + firstName + "\",\"" + surname + "\");";
                    }
                    else
                    {
                        request = "INSERT INTO doctor (login, pass, firstname, surname, lastName) VALUES " +
                       "(\"" + login + "\",\"" + pass + "\",\"" + firstName + "\",\"" + surname + "\",\"" + lastName + "\");";
                    }

                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    cmdSel.ExecuteNonQuery();
                    Notify?.Invoke("Врач добавлен");
                }
                catch (Exception e)
                {
                    Notify?.Invoke(e.Message);
                }
            }
            else
            {
                Notify?.Invoke("Нет подключения к БД");
            }
        }

        /// <summary>
        /// Вернуть список пациентов конкретного доктора
        /// </summary>
        /// <param name="doctorID"></param>
        public List<Patient> getPatientList(int doctorID)
        {
            Notify?.Invoke("Started getPatientList");
            List<Patient> patients = new List<Patient>();
            if (isConnected)
            {
                try
                {

                    string request;
                    request = "SELECT id, firstname, surname, lastname, token, phonenum FROM patient WHERE id IN (SELECT patientID FROM doctorpatient WHERE doctorid=" + doctorID + ");";
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    using (MySqlDataReader reader = cmdSel.ExecuteReader())
                    {
                        Notify?.Invoke("Список пациентов врача " + doctorID);
                        while (reader.Read())
                        {
                            Patient patient = new Patient(Int32.Parse(reader[0].ToString()), reader[1].ToString(), reader[2].ToString(), reader[3].ToString(), reader[4].ToString(), reader[5].ToString());
                            Notify?.Invoke(patient.toString());
                            patients.Add(patient);
                        }
                    }
                }
                catch (Exception e)
                {
                    Notify?.Invoke(e.Message);
                }
            }
            else
            {
                Notify?.Invoke("Нет подключения к БД");
            }
            return patients;
        }

        #endregion


        #region chat

        //Переделать так как отправка сразу нескольким врачам. Добавить лист/массив получателей
        /// <summary>
        /// Отправка сообщения от пользователя
        /// </summary>
        /// <param name="patientID"></param>
        /// <param name="doctorID"></param>
        /// <param name="text"></param>
        public void MessageFromPacient(int patientID, string text)
        {
            Notify?.Invoke("Started MessageFromPacient");
            if (isConnected)
            {
                List<int> doctorsID = new List<int>();
                try
                {
                    string request;
                    request = "SELECT doctorid FROM doctorpatient WHERE pacientid=" + patientID + ";";
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    using (MySqlDataReader reader = cmdSel.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            doctorsID.Add(Int32.Parse(reader[0].ToString()));
                        }
                    }
                    string time = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                    foreach (int doctorID in doctorsID)
                    {
                        request = "INSERT INTO chat (senderID, adresatID, message,unixtime) VALUES " +
                       "(" + patientID + "," + doctorID + ",\"" + text + "\"," + time + ");";
                        cmdSel = new MySqlCommand(request, ConnectionDef);
                        cmdSel.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Notify?.Invoke(e.Message);
                }
            }
            else
            {
                Notify?.Invoke("Нет подключения к БД");
            }
        }

        #endregion


        public List<Patient> getPatients(string[] snilses)
        {
            Notify?.Invoke("Started getPatients");
            List<Patient> patients = new List<Patient>();
            if (isConnected)
            {

                for (int i = 0; i < snilses.Length; i++)
                {

                    Patient patient = null;
                    string request = "SELECT id, firstname, surname,lastname,token,phonenum,snils " +
                           "WHERE snils=\"" + snilses[i] + "\";";
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    using (MySqlDataReader reader = cmdSel.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            patient = new Patient(Int32.Parse(reader[0].ToString()), reader[1].ToString(),
                                reader[2].ToString(), reader[3].ToString(), reader[4].ToString(), reader[5].ToString(), reader[6].ToString());

                        }
                    }

                    int patientid = patient.Id;
                    request = "SELECT id, lowpress,toppress, pulse, unixtime,tag,saturation FROM params WHERE patientid = " + patient + ";";
                    using (MySqlDataReader reader = cmdSel.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Patient.Params par = new Patient.Params(Int32.Parse(reader[0].ToString()), Int32.Parse(reader[0].ToString()), Int32.Parse(reader[1].ToString()),
                                Int32.Parse(reader[2].ToString()), Int32.Parse(reader[3].ToString()), reader[4].ToString(), Int32.Parse(reader[5].ToString()));
                            patient.addParams(par);
                        }
                    }
                }
            }
            else
            {
                Notify?.Invoke("Нет подключения к БД");
            }
            return patients;
        }

        //Переделать
        /// <summary>
        /// Получить историю сообщений
        /// </summary>
        /// <param name="patientID"></param>
        /// <param name="doctorID"></param>
        /// <param name="sender"></param>
        public void getHistoryMessages(int patientID, int doctorID)
        {
            Notify?.Invoke("Started getHistoryMessages");
            

                try
                {

                    string request;
                    request = "SELECT * FROM chat WHERE patientid =" + patientID + " AND doctorid=" + doctorID + ";";
                    MySqlCommand cmdSel = new MySqlCommand(request, ConnectionDef);
                    MySqlDataReader reader = cmdSel.ExecuteReader();
                    while (reader.Read())
                    {
                        Console.WriteLine(reader[0] + " " + reader[1]);
                    }
                    Notify?.Invoke("Похожие строки поиска возвращены");
                    reader.Close();
                }
                catch (Exception e)
                {
                    Notify?.Invoke(e.Message);
                }
            
            
        }

        //Работает
        #region Connecting


        /// <summary>
        /// Проверка, есть ли подключение
        /// </summary>
        public bool isConnect()
        {
            return isConnected;
        }
        /// <summary>
        /// Создаём соединение
        /// </summary>
        /// <returns></returns>
        public bool connect()
        {
            if (!isConnected)
            {
                try
                {
                    ConnectionDef.Open();
                    Notify?.Invoke("Подключение установлено");
                    isConnected = true;
                }
                catch (Exception e)
                {
                    Notify?.Invoke(e.Message);

                    return false;
                }
                return true;
            }
            else
            {
                Notify?.Invoke("Подключение уже установлено");
                return false;
            }
        }

        /// <summary>
        /// Закрыть соединение
        /// </summary>
        public void closeCon()
        {
            if (isConnected)
            {
                ConnectionDef.Close();
                isConnected = false;
                Notify?.Invoke("Соединение с БД разорвано");
            }
            else Notify?.Invoke("Нет подключения к БД");
        }

        #endregion
    }
}
