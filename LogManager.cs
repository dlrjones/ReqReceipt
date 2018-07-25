using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Threading;

namespace ReqReceipt
{
    class LogManager
    {
        #region Class Vars & Params
        private static string logFilePath = "";
        private bool debug = false;
       // private ArrayList outgoingData;
        private static LogManager logMngr = null;
        private static NameValueCollection ConfigData = null;
        private char TAB = Convert.ToChar(9);

        //public ArrayList OutgoingData
        //{
        //    set { outgoingData = value; }
        //}

        public string LogFilePath
        {
            set { logFilePath = value; }
        }

        public bool Debug
        {
            set { debug = value; }
        }
        #endregion

        private LogManager()
        {
            // this constructor is private to force the calling program to use GetInstance()
            InitLogMngr();
            //debug = Convert.ToBoolean(ConfigData.Get("debug"));
        }

        private static void InitLogMngr()
        {
            ConfigData = (NameValueCollection)ConfigurationSettings.GetConfig("appSettings");
            logFilePath = ConfigData.Get("logFilePath") + ConfigData.Get("logFile");
        }

        public static LogManager GetInstance()
        {
            if (logMngr == null)
            {
                CreateInstance();
            }
            return logMngr;
        }

        private static void CreateInstance()
        {
            Mutex configMutex = new Mutex();
            configMutex.WaitOne();
            logMngr = new LogManager();
            configMutex.ReleaseMutex();
        }

        public void Write(ArrayList outGoingData)
        {
            string writeText = "";
            try
            {
                if (debug)
                    Write("LogManager/Write:  " + "");
                foreach (string item in outGoingData)
                {
                    writeText += item.Trim() + TAB;
                }
                Write(writeText);
            }
            catch (Exception ex)
            {
                Write("LogManager/Write:  " + ex.Message);
            }
        }

        public void Write(string logText)
        {
            if (!File.Exists(logFilePath))
                File.WriteAllText(logFilePath, "ReqItemStatus Email and ERROR LOG" + Environment.NewLine);
            if (logText.Length > 0)
                File.AppendAllText(logFilePath, DateTime.Now + TAB.ToString() + logText + Environment.NewLine);
        }

    }
}
