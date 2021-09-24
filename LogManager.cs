using System;
using System.Collections.Generic;
using System.IO;

namespace HahaServer
{
    class LogManager
    {
        static string Folder = "logs";
        string FileName;
        string Path;

        public LogManager()
        {
            NewFile();
            if (!Directory.Exists("logs"))
            {
                Directory.CreateDirectory("logs");
            }
        }


        public void WriteToLog(string text)
        {
            Path = Folder + '\\' + FileName;
            using (StreamWriter sw = new StreamWriter(Path, true, System.Text.Encoding.Default))
            {
                sw.WriteLine(DateTime.UtcNow.ToShortTimeString() + ' ' + text);
            } 
        }
        public void NewFile()
        {
            FileName = DateTime.Today.ToShortDateString() + ".txt";
        }
    }
}
