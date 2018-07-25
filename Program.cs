using System;
using System.Collections.Specialized;
using System.Configuration;
using LogDefault;



namespace ReqReceipt
{
    class Program
    {
        #region class variables
        private static string corpName = "";  //HMC, UWMC
        private static string logPath = "";
        private static bool debug = false;
        private static bool trace = false;
        private static bool cleanUp = false;
        private static LogManager lm = LogManager.GetInstance();
        private static NameValueCollection ConfigData = null;
        #endregion
        static void Main(string[] args)
        {

            // check the cleanUp param
            if (args.Length > 0)
            {
                cleanUp = true;
            }
            //LogManager lm = LogManager.GetInstance();
            ConfigData = (NameValueCollection)ConfigurationSettings.GetConfig("appSettings");
            try
            {                
                lm.Write("Program/Main:  " + "BEGIN");
                GetParameters();                
                LoadData();
                if (cleanUp)
                    lm.Write("Program/Main:  " + "END");
                else
                {                   
                    Process();
                    lm.Write("Program/Main:  " + "END");
                }
            }
            catch (Exception ex)
            {
                lm.Write("Program/Main:  " + ex.Message);
            }
            finally
            {
                Environment.Exit(1);
            }
        }
     
        private static void GetParameters()
        {
            debug = Convert.ToBoolean(ConfigData.Get("debug"));
            trace = Convert.ToBoolean(ConfigData.Get("trace"));
            lm.LogFilePath = ConfigData.Get("logFilePath");
            lm.LogFile = ConfigData.Get("logFile");
            lm.Debug = debug;
        }

        private static void LoadData()
        {
            DataSetManager dsm = DataSetManager.GetInstance();
            //DataSetManager is a singleton so getting the instance here sets it up for the other classes in the app.
            dsm.Debug = debug;
            dsm.Trace = trace;
            if (cleanUp)
            {   //cleanUp needs to be run once each day after midnight.
                //The initial select query (DataSetManager.BuildTodayQuery) only looks for records from the previous run through to the current run time.
                //To run TruncateReqItemReceipt, launch ReqItemStatus wih the number "1" as a parameter (or anything, really - as you can see above 
                //it only looks at the number of arguments over 0). You'll probably want this on its own Scheduled Task (currently set at 12:30AM).
                (LogManager.GetInstance()).Write("Program/Main.LoadData:  " + "CleanUp - hmcmm_ReqItemReceipt");
                dsm.TruncateReqItemReceipt();
            }
            else
            {
                dsm.LoadTodaysDataSet();
                dsm.LoadYesterdayList();
            }
        }
      
        private static void Process()
        {
            ProcessManager pm = new ProcessManager();
            pm.Debug = debug;
            pm.Trace = trace;
            pm.Begin();
        }

    }
}

