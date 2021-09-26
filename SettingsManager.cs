using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.IO;

namespace HahaServer
{
    class SettingsManager
    {
        List<string> PropList;
        string fileName = "settings.txt";

        public SettingsManager(List<string> props)
        {
            PropList = props;
            FileInfo file = new FileInfo(fileName);
            if (!file.Exists)
            {
                JObject jObject = new JObject();
                foreach (string prop in PropList)
                {
                    jObject[prop] = "";
                }
                using (StreamWriter sw = new StreamWriter(fileName, false, System.Text.Encoding.Default))
                {
                    sw.WriteLine(jObject.ToString());
                }
            }
            

        }

        public JObject getPropetris()
        {
            string getProp;
            using (StreamReader sr = new StreamReader(fileName))
            {
                getProp = sr.ReadToEnd();
            }
            return JObject.Parse(getProp);
        }

        public bool setPropetry(string propName, string value)
        {
            string getProp;
            using (StreamReader sr = new StreamReader(fileName))
            {
                getProp = sr.ReadToEnd();
            }
            JObject jObject = JObject.Parse(getProp);
            if (PropList.Exists(x=> x == propName))
            {
                jObject[propName] = value;
                using (StreamWriter sw = new StreamWriter(fileName, false, System.Text.Encoding.Default))
                {
                    sw.WriteLine(jObject.ToString());
                }
                return true;
            }
            return false;
        }
    }
}
