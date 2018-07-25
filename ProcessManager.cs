using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.IO;
using LogDefault;



namespace ReqReceipt
{
    class ProcessManager
    {
        #region Class Variables & Parameters
        private DataSet dsYesterday = new DataSet();
        private DataSet dsCurrentChangeDates;
        private Hashtable itemChangeDate = new Hashtable();
        private Hashtable itemReq = new Hashtable();
        private Hashtable itemReqLine = new Hashtable();
        private Hashtable itemsThatChanged = new Hashtable();
        private Hashtable itemItemNo = new Hashtable();
        private Hashtable itemDesc = new Hashtable();
        private Hashtable itemLogin = new Hashtable();
        private Hashtable receiptList = new Hashtable();
        private Hashtable reqDate = new Hashtable();
        private Hashtable userItems = new Hashtable();
        private Hashtable debugCCRecip = new Hashtable();
        private Hashtable reqCC = new Hashtable();
        private DataSetManager dsm = DataSetManager.GetInstance();
        private static NameValueCollection ConfigData = null;
        private bool debug = false;
        private bool trace = false;
        private LogManager lm = LogManager.GetInstance();

        class UserNameVariant
        {
            //takes in the path to the text file containing the names of users who have an email id different from their AMC id.
            //each entry looks like this - mdanna|dannam
            Hashtable userItems = new Hashtable();
            private string unamePath = "";
           // private LogManager lm = LogManager.GetInstance();

            public string UnamePath
            {
                set { unamePath = value; }
            }
            public Hashtable UserItems
            {
                get { return userItems; }
                set
                {
                    userItems = value;
                    GetUserNameVariant();
                }
            }

            private void GetUserNameVariant()
            {
                string[] users = File.ReadAllLines(unamePath);
                ArrayList tmpValu = new ArrayList();
                string[] user;

                foreach (string name in users)
                {
                    user = name.Split("|".ToCharArray());
                    if (user.Length > 0)
                    {
                        if (user.Length > 1)
                        {
                            //change the userItems entry for users that have a different email
                            if (userItems.ContainsKey(user[0]))
                            {
                                try
                                {
                                    tmpValu = (ArrayList)userItems[user[0]];
                                    userItems.Remove(user[0]);
                                    userItems.Add(user[1], tmpValu);
                                }
                                catch (Exception ex)
                                {
                                    (LogManager.GetInstance()).Write("ProcessManager/GetUserNameVariant:  " + ex.Message);
                                }
                            }
                        }
                    }
                }
            }
            //          WHERE LOGIN_ID IN ('mharrington','sbuckingham','mdanna','bsimmons','ayyoub','fyoshioka',
            //'jvarghese','mvasiliades','bwalker','dlam','sgoss','enewcombe','aclouse','jrudd',
            //'kburkette','ewardenburg','mofo','ccastillo')
        }
        public DataSet DSYesterday
        {
            get { return dsYesterday; }
            set { dsYesterday = value; }
        }
        public bool Debug
        {
            set { debug = value; }
        }
        public bool Trace
        {
            set { trace = value; }
        }
        #endregion

        public ProcessManager()
        {
            ConfigData = (NameValueCollection)ConfigurationSettings.GetConfig("appSettings");
        }

        public void Begin()
        {
            if (trace) { lm.Write("TRACE:  ProcessManager/Begin"); }
            CreateDebugCCRecipTable(ConfigData.Get("debugCCRecip"));
            StoreNewReqs();
            SendOutput(); //gets instance of OutputManager
        }

        private void SendOutput()
        {
            if (trace) {lm.Write("TRACE:  ProcessManager/SendOutput");}
            OutputManager om = new OutputManager();
            om.Debug = debug;
            om.AttachmentPath = ConfigData.Get("attachmentPath");
            om.ItemReq = itemReq;
            om.ItemReqLine = itemReqLine;
            om.ItemsThatChanged = itemsThatChanged;
            om.ItemItemNo = itemItemNo;
            om.ItemDesc = itemDesc;
            om.ReqBuyer = dsm.ReqBuyer;
            GetUserNameVariant();
            int itemID = 0;
            try
            {
                if (debug)
                {
                    om.DebugRecipientList = ConfigData.Get("debugRecipientList");                    
                }
                om.DebugCCRecip = debugCCRecip;
                //when reciptList is empty, debug will not send emails to the debug recipients
                //because SendOutput is only called in this foreach loop.
                //this loop is what sends out the emails...
                foreach (DictionaryEntry de in receiptList)
                {
                    om.ReqNo = de.Key.ToString();
                    om.UserName = de.Value.ToString().Trim();
                    om.ReqDate = (reqDate[de.Key]).ToString();
                    om.Trace = trace;
                    om.CurrentAcctNo = (reqCC[de.Key]).ToString();
                    om.SendOutput();
                }
            }
            catch (Exception ex)
            {
                lm.Write("ProcessManager/SendOutput:  ERROR:  " + ex.Message);
            }
        }

        private ArrayList GetCCList(string ccList)
        {
            if (trace) { lm.Write("TRACE:  ProcessManager/GetCCList"); }
            ArrayList costCenters = new ArrayList();
            string[] costCenterList = ccList.Split(",".ToCharArray());
            foreach(string cc in costCenterList)
            {
                costCenters.Add(cc);
            }
            return costCenters;
        }

        //changes are made to the userItems list to replace the user names which AREN'T also the email name        
        private void GetUserNameVariant()
        {
            if (trace) { lm.Write("TRACE:  ProcessManager/GetUserNameVariant"); }
            UserNameVariant unv = new UserNameVariant();
            unv.UnamePath = ConfigData.Get("unameVariantList");
            unv.UserItems = userItems;
            userItems = unv.UserItems;
        }

        private void StoreNewReqs()
        {
            if (trace) { lm.Write("TRACE:  ProcessManager/StoreNewReqs"); }
            dsm.DebugCCList = GetCCList(ConfigData.Get("debugCCList"));
            dsm.InsertTodaysList();
            receiptList = dsm.SendReceipt;
            reqDate = dsm.ReqDate;
            reqCC = dsm.ReqCC;
        }

        private Hashtable Clone(Hashtable htInput)
        {
            if (trace) { lm.Write("TRACE:  ProcessManager/Clone"); }
            if (debug) lm.Write("ProcessManager/Clone");
            Hashtable ht = new Hashtable();

            foreach (DictionaryEntry dictionaryEntry in htInput)
            {
                if (dictionaryEntry.Value is string)
                {
                    ht.Add(dictionaryEntry.Key, new string(dictionaryEntry.Value.ToString().ToCharArray()));
                }
                else if (dictionaryEntry.Value is Hashtable)
                {
                    ht.Add(dictionaryEntry.Key, Clone((Hashtable)dictionaryEntry.Value));
                }
                else if (dictionaryEntry.Value is ArrayList)
                {
                    ht.Add(dictionaryEntry.Key, new ArrayList((ArrayList)dictionaryEntry.Value));
                }
            }

            return ht;
        }

        private void CreateDebugCCRecipTable(string ccCollection)
        {   //ccCollection ="6074:olsenj@uw.edu,lbrandle@uw.edu;6087:roxeboo@uw.edu;"
            //this allows for a test where more than one person wants to see the receipts for a req on a given cost center.
            if (trace) { lm.Write("TRACE:  ProcessManager/CreateDebugCCRecipTable"); }
            string[] costCenter;
            string[] ccGroup = ccCollection.Split(";".ToCharArray());
            foreach(string cc in ccGroup)
            {
                if (cc.Length > 0)
                {
                    costCenter = cc.Split(":".ToCharArray());
                    if (!debugCCRecip.ContainsKey(costCenter[0]))
                        debugCCRecip.Add(costCenter[0], costCenter[1]);
                }
            }

        }
    }
}
