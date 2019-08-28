using System;
using System.Collections;
using OleDBDataManager;
using System.Data;
using System.Collections.Specialized;
using System.Configuration;
using System.Threading;
using LogDefault;


namespace ReqReceipt
{
    class DataSetManager
    {
        #region class variables
        private DataSet dsCurrent;
        private DataSet dsYesterday;
        private DataSet dsCurrentChangeDates;
        private Hashtable itemsThatChanged = new Hashtable();
        private Hashtable sendReceipt = new Hashtable();
        private Hashtable reqDate = new Hashtable();
        private Hashtable reqCC = new Hashtable();
        private Hashtable reqItem = new Hashtable(); //KEY=reqID  VALU=itemList  (a comma seperated list of all items on a given req)
        private Hashtable itemDescr = new Hashtable(); // key=itemNo valu=descr
        private Hashtable itemQty = new Hashtable(); // key=itemNo valu=qty ordered
        private Hashtable itemUM = new Hashtable(); // key=itemNo valu=unit of measure
        private Hashtable reqBuyer = new Hashtable(); // key=reqID valu=list of team email
        private Hashtable reqBuyerTeamName = new Hashtable(); // key=reqID valu=Buyer team abbreviation
                                                      // private Hashtable codeBuyerEmail = new Hashtable(); // key=buyerCode valu=email of buyer group
        private ArrayList partialCCList = new ArrayList();
        private static string connectStrHEMM = "";
        private static string connectStrBIAdmin = "";
    //    private static string buyerTeams = "";
        private static string userName = "";
        private static NameValueCollection ConfigData = null;
        protected static ODMDataFactory ODMDataSetFactory = null;
        private static DataSetManager dsm = null;
       //rj private static LogManager lm = LogManager.GetInstance();
        private static bool debug = false;
        private static bool trace = false;
        private ArrayList tableRecord = new ArrayList();
        private LogManager lm = LogManager.GetInstance();
        #region parameters
        public DataSet DsYesterday
        {
            get { return dsYesterday; }
            set { dsYesterday = value; }
        }

        public ArrayList PartialCCList
        {
            set { partialCCList = value; }
        }

        public DataSet DSCurrent
        {
            get { return dsCurrent; }
            set { dsCurrent = value; }
        }

        public DataSet DSCurrentChangeDates
        {
            get { return dsCurrentChangeDates; }
            set { dsCurrentChangeDates = value; }
        }
        public Hashtable SendReceipt
        {
            get { return sendReceipt; }
            set { sendReceipt = value; }
        }
        public Hashtable ReqDate
        {
            get { return reqDate; }
            set { reqDate = value; }
        }
        public Hashtable ReqBuyer
        {
            get { return reqBuyer; }
            set { reqBuyer = value; }
        }
        public Hashtable ReqCC
        {
            get { return reqCC; }
            set { reqCC = value; }
        }
        public Hashtable ReqItem
        {
            get { return reqItem; }
        }
        public Hashtable ItemDescr
        {
            get { return itemDescr; }
        }
        public Hashtable ItemQty
        {
            get { return itemQty; }
        }
        public Hashtable ItemUM
        {
            get { return itemUM; }
        }
        public Hashtable ItemsThatChanged
        {
            set { itemsThatChanged = value; }
        }
        public string UserName
        {
            get { return userName; }
        }
        public bool Debug
        {
            set { debug = value; }
        }
        public bool Trace
        {
            set { trace = value; }
        }
        public ArrayList TableRecord
        {
            get { return tableRecord; }
            set { tableRecord = value; }
        }
        #endregion parameters
        #endregion

        public DataSetManager()
        {
            InitDataSetManager();
        }

        private void InitDataSetManager()
        {
            //string[] buyers;
        //    string[] codeList;
            if (trace) { lm.Write("TRACE:  DataSetManager/InitDataSetManager"); }
            lm.Debug = debug;
            ODMDataSetFactory = new ODMDataFactory();        
            ConfigData = (NameValueCollection)ConfigurationManager.GetSection("appSettings");
            connectStrHEMM = ConfigData.Get("cnctHEMM_HMC");
            connectStrBIAdmin = ConfigData.Get("cnctBIAdmin_HMC");
            //buyerTeams = ConfigData.Get("buyerTeams");
            //buyers = buyerTeams.Split(";".ToCharArray());
            //foreach(string byer in buyers)
            //{
            //    codeList = byer.Split(":".ToCharArray());
            //    codeBuyerEmail.Add(codeList[0], codeList[1]);
            //}
        }

        public static DataSetManager GetInstance()
        {
            if (dsm == null)
            {
                CreateInstance();
            }
            return dsm;
        }

        private static void CreateInstance()
        {
            Mutex configMutex = new Mutex();
            configMutex.WaitOne();
            dsm = new DataSetManager();
            configMutex.ReleaseMutex();            
        }

        public void LoadTodaysDataSet()
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/LoadTodaysDataSet"); }
            ODMRequest Request = new ODMRequest();
            Request.ConnectString = connectStrHEMM;
            Request.CommandType = CommandType.Text;
            Request.Command = BuildTodayQuery();
                //"Execute ('" + BuildTodayQuery() + "')";

            //if (debug)
            //    lm.Write("DataSetManager/LoadTodaysDataSet:  " + Request.Command);
            try
            {
                dsCurrent = ODMDataSetFactory.ExecuteDataSetBuild(ref Request);
            }
            catch (Exception ex)
            {
                lm.Write("DataSetManager/LoadTodaysDataSet:  " + ex.Message);
            }
        }

        public void UpdateReqItems()
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/UpdateReqItems"); }
            // private Hashtable itemsThatChanged 
            int item = 0;
            string status = "";

            if (debug) lm.Write("DataSetManager/UpdateReqItems");
            ODMRequest Request = new ODMRequest();
            Request.ConnectString = connectStrBIAdmin;
            Request.CommandType = CommandType.Text;
            foreach (object key in itemsThatChanged.Keys)
            {
                try
                {
                    item = Convert.ToInt32(key);
                    status = itemsThatChanged[item].ToString();
                    Request.Command = BuildReqItemUpdateQuery(item, status);
                        //"Execute ('" + BuildReqItemUpdateQuery(item, status) + "')";
                    ODMDataSetFactory.ExecuteDataWriter(ref Request);
                }
                catch (Exception ex)
                {
                    lm.Write("DataSetManager/UpdateReqItems:  " + ex.Message);
                }
            }
        }

        public void TruncateReqItemReceipt()
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/TruncateReqItemReceipt"); }
            //remove KILLED and COMPLETE reqItems from the hmcmm_ReqItemReceipt table
            ODMRequest Request = new ODMRequest();
            Request.ConnectString = connectStrBIAdmin;
            Request.CommandType = CommandType.Text;
            Request.Command = BuildTruncateReqItemReceipt();
            try
            {
                ReqReceiptCount(); //the "before" count
                ODMDataSetFactory.ExecuteNonQuery(ref Request);
                lm.Write("DataSetManager/TruncateReqItemReceipt:  ENQ Complete");
                ReqReceiptCount(); //the "after" count
            }
            catch (Exception ex)
            {
                lm.Write("DataSetManager/TruncateReqItemReceipt:  " + ex.Message);
            }
        }

        public void GetUserName(string uid)
        {
            ArrayList name = new ArrayList();
            ODMRequest Request = new ODMRequest();
            Request.ConnectString = connectStrHEMM;
            Request.CommandType = CommandType.Text;
            Request.Command = "SELECT NAME FROM USR WHERE LOGIN_ID = '" + uid + "'";
            name = ODMDataSetFactory.ExecuteDataReader(ref Request);
            if (name.Count > 0)
                userName = name[0].ToString().Trim();
        }

        public string GetManagerEmail(string cc)
        {
            string sql = "SELECT EMAIL +'@uw.edu' " +
                         "FROM [uwm_BIAdmin].[dbo].[HMC_DeptContactList] " +
                         "WHERE CCN = " + cc;   // GetCC(reqNo);
            lm.Write("Cost Center: " + cc);
            ArrayList email = new ArrayList();
            ODMRequest Request = new ODMRequest();
            Request.ConnectString = connectStrBIAdmin;
            Request.CommandType = CommandType.Text;
            Request.Command = sql;
            email = ODMDataSetFactory.ExecuteDataReader(ref Request);
            if (email.Count == 0)
                email.Add("");
            return email[0].ToString().Trim();
        }

        private string GetCC(string reqNo)
        {
            string sql = "SELECT ACCT_NO " +
                         "FROM CC " +
                         "JOIN REQ ON REQ.CC_ID = CC.CC_ID " +
                         "WHERE REQ_NO = '" + reqNo + "'";
            ArrayList cc = new ArrayList();
            ODMRequest Request = new ODMRequest();
            Request.ConnectString = connectStrHEMM;
            Request.CommandType = CommandType.Text;
            Request.Command = sql;
            cc = ODMDataSetFactory.ExecuteDataReader(ref Request);
            if (cc.Count == 0)
                cc.Add("");

            return cc[0].ToString().Trim();
        }

        public void InsertTodaysList()
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/InsertTodaysList"); }
            try
            { /////////THIS IS THE LIST THAT GIVEs THE INITIAL EMAIL RECEIPTS 
    
                ODMRequest Request = new ODMRequest();
                Request.ConnectString = connectStrBIAdmin;
                Request.CommandType = CommandType.Text;
                foreach (DataRow drow in dsCurrent.Tables[0].Rows)//dsCurrent is loaded from Program.cs calling dsm.LoadTodaysDataSet();
                {                                                       //req_id (3) & req_item_id (1)
                    if (partialCCList[0].ToString().Length == 0  || partialCCList.Contains(drow[9].ToString().Trim()))
                    {//if a list of cc's isn't available then do this for all cc's otherwise do it for a specific cc in the list
                        bool goodToGo = CheckExistingRecordCount(Convert.ToInt32(drow[3]), Convert.ToInt32(drow[1]));
                        if (goodToGo)
                        {   //insert into hmcmm_ReqItemReceipt if it is NOT already in there - goodToGo being true means 
                            //CheckExistingRecordCount found 0 records - anything being inserted here needs to have a receipt sent. 
                            //the sendReceipt Hashtable collects these req numbers/usernames
                            if (!sendReceipt.ContainsKey(drow[3].ToString().Trim()))
                            {
                                sendReceipt.Add(drow[3].ToString().Trim(), drow[7]);   //key=REQ_ID  valu=LOGIN_ID
                                reqDate.Add(drow[3].ToString().Trim(), drow[8]);   //key=REQ_ID  valu=REQ_DATE
                                reqCC.Add(drow[3].ToString().Trim(), drow[9]);      //key=REQ_ID  valu=CC
                                LoadItemDescr(drow[3].ToString().Trim());       //key=REQ_ID  valu= a comma seperated list of all items on a given req
                            }
                            LoadBuyerList(Request, drow[3].ToString().Trim(),drow[9].ToString().Trim());
                            //reqBuyer.Add

                            Request.Command = BuildReqItemInsertQuery(drow);
                            ODMDataSetFactory.ExecuteNonQuery(ref Request);                           
                        }
                    }
                }
                lm.Write("sendReceipt Count = " + sendReceipt.Count);
            }
            catch (Exception ex)
            {
                lm.Write("DataSetManager/InsertTodaysList:    "  + ex.Message);
            }
        }

        private void LoadBuyerList(ODMRequest Request,string reqID,string cc)
        {
            ArrayList buyerCode = new ArrayList();
            try
            {
                Request.Command = "SELECT EMAIL,TEAM FROM [dbo].[uwm_CC_TEAM] WHERE COST_CENTER = '" + cc + "'";
                buyerCode = ODMDataSetFactory.ExecuteDataReader(Request,2);
                if (!(reqBuyer.ContainsKey(reqID)))
                {
                    reqBuyer.Add(reqID, buyerCode[0].ToString() + "|" + buyerCode[1].ToString().Trim());
                }
            }
            catch (Exception ex)
            {
                lm.Write("DataSetManager/LoadBuyerList:    " + ex.Message);
            }
        }

        private void LoadItemDescr(string reqID)
        {
            //this associates a reqID with the items/descr's on the req
            //itemDescr is a hashtable where key=itemNo valu=descr
            //reqItem is a hashtable where key=reqID valu=a comma seperated list of all items on a given req
            if (trace) { lm.Write("TRACE:  DataSetManager/LoadItemDescr"); }
            DataSet dsItem = new DataSet();
            string itemList = "";  //this is a comma seperated list of all item numbers on a given req      
            try
            {
                dsItem = GetItemDescrQtyUM(reqID);
                if (dsItem.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow dr in dsItem.Tables[0].Rows)
                    { //  reqItem   itemDescr
                        if (!itemDescr.ContainsKey(dr.ItemArray[0].ToString().Trim()))
                        {
                            itemDescr.Add(dr.ItemArray[0].ToString().Trim(), dr.ItemArray[1].ToString().Trim());
                        }
                        if (!itemQty.ContainsKey(dr.ItemArray[0].ToString().Trim()))
                        {
                            itemQty.Add(dr.ItemArray[0].ToString().Trim(), dr.ItemArray[2].ToString().Trim());
                        }
                        if (!itemUM.ContainsKey(dr.ItemArray[0].ToString().Trim()))
                        {
                            itemUM.Add(dr.ItemArray[0].ToString().Trim(), dr.ItemArray[3].ToString().Trim());
                        }
                        if (itemList.Length > 0)//once the list gets started, add a comma between items
                            itemList += ",";
                        itemList += dr.ItemArray[0].ToString().Trim();
                    }
                    reqItem.Add(reqID, itemList);
                }
            }catch(Exception ex)
            {
                lm.Write("DataSetManager/LoadItemDescr:    " + ex.Message);

            }
        }

        private DataSet GetItemDescrQtyUM(string reqID)
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/GetItemDescr"); }
            DataSet dsItemDescr = new DataSet();
            ODMRequest Request = new ODMRequest();
            Request.ConnectString = ConfigData.Get("cnctHEMM_HMC"); 
            Request.CommandType = CommandType.Text;
            Request.Command = "SELECT ITEM_NO, DESCR,QTY,RIGHT(RTRIM(UM_CD),2) " +
                               "FROM REQ_ITEM " +
                               "JOIN ITEM ON REQ_ITEM.ITEM_ID = ITEM.ITEM_ID " +
                               "WHERE REQ_ID = " + reqID;
            try
            {
                dsItemDescr = ODMDataSetFactory.ExecuteDataSetBuild(ref Request);               
            }
            catch (Exception ex)
            {
                lm.Write("DataSetManager/GetItemDescr:  " + ex.Message);
            }
            return dsItemDescr;
        }

        public void LoadYesterdayList()
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/LoadYesterdayList"); }
            if (trace) { lm.Write("TRACE:  DataSetManager/LoadYesterdayList"); }
            //select req_item_id and req_item_stat and put into a hashtable
            ODMRequest Request = new ODMRequest();
            Request.ConnectString = connectStrBIAdmin;
            Request.CommandType = CommandType.Text;
            Request.Command = BuildYesterdayQuery();
                //"Execute ('" + BuildYesterdayQuery() + "')";

            if (debug)
                lm.Write("DataSetManager/LoadYesterdayList:  " + Request.Command);
            try
            {
                dsYesterday = ODMDataSetFactory.ExecuteDataSetBuild(ref Request);
            }
            catch (Exception ex)
            {
                lm.Write("DataSetManager/LoadYesterdayList:  " + ex.Message);
            }
        }

        public void LoadCurrentChanges(string reqItemList)
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/LoadCurrentChanges"); }
            ODMRequest Request = new ODMRequest();
            Request.ConnectString = connectStrHEMM;
            Request.CommandType = CommandType.Text;
            Request.Command = BuildCurrentQuery(reqItemList);
            //"Execute ('" + BuildCurrentQuery(reqItemList) + "')";

            if (debug)
                lm.Write("DataSetManager/LoadTodaysDataSet:  " + Request.Command);
            try
            {
                dsCurrentChangeDates = ODMDataSetFactory.ExecuteDataSetBuild(ref Request);
            }
            catch (Exception ex)
            {
                lm.Write("DataSetManager/LoadTodaysDataSet:  " + ex.Message);
            }
        }

        private string BuildTodayQuery()
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/BuildTodayQuery"); }
            string query = "SELECT DISTINCT " +
                           "RI.LINE_NO [REQ LINE NO], " +  //0
                           "REQ_ITEM_ID, " +                      //1
                           "ITEM.DESCR, " +                        //2
                           "REQ.REQ_ID, " +                        //3
                           "ITEM.ITEM_NO, " +                    //4
                           "CASE RI.STAT " +
                           "WHEN 1 THEN 'Open' " +
                           "WHEN 2 THEN 'Pend Apvl' " +
                           "WHEN 3 THEN 'Approved' " +
                           "WHEN 4 THEN 'Removed' " +
                           "WHEN 5 THEN 'Pending PO' " +
                           "WHEN 6 THEN 'Open Stock Order' " +
                           "WHEN 8 THEN 'Draft' " +
                           "WHEN 9 THEN 'On Order' " +
                           "WHEN 10 THEN 'Killed' " +
                           "WHEN 11 THEN 'Complete' " +
                           "WHEN 12 THEN 'Back Order' " +
                           "WHEN 14 THEN 'On Order' " +
                           "WHEN 24 THEN 'Pend Informational Apvl' " +
                           "ELSE CAST(RI.STAT AS VARCHAR(2)) " +
                           "END [ITEM STAT],  " +                //5
                           "RI.STAT_CHG_DATE, " +            //6
                           "LOGIN_ID, " +                  //7
                           "REQ.REC_CREATE_DATE, " +    //8
                           "ACCT_NO, " +              //9
                           "QTY, " +                //10
                           "RIGHT(RTRIM(UM_CD),2) UM " +   //11
                           "FROM dbo.REQ_ITEM  RI  " +
                           "JOIN dbo.REQ ON REQ.REQ_ID = RI.REQ_ID " +
                           "JOIN dbo.ITEM ON ITEM.ITEM_ID = RI.ITEM_ID " +
                           "JOIN dbo.USR ON USR.USR_ID = REQ.REC_CREATE_USR_ID " +
                           "JOIN CC ON CC.CC_ID = REQ.CC_ID " +
                           "WHERE REQ.REC_CREATE_DATE BETWEEN CONVERT(DATE,GETDATE()) AND CONVERT(DATE,GETDATE() + 1) " +
                           "AND LOGIN_ID <> 'iface' AND REQ_TYPE IN (2) AND RI.STAT <> 8 " +
                           "ORDER BY 8,4,1 ";               //references param 7,3 and 0 above  --  AND REQ_TYPE <> 3  RI.STAT = 8 = "Draft"
                                                            // Req_Type changed to 8 for par forms to catch actual par form submissions
            return query;
        }

        private string BuildYesterdayQuery()
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/BuildYesterdayQuery"); }
            string query =
                "SELECT REQ_ID,REQ_ITEM_ID, STAT_CHG_DATE,[REQ LINE NO],LOGIN_ID,DESCR,ITEM_NO " +
                "FROM hmcmm_ReqItemReceipt " +
                "Order by REQ_ID,[REQ LINE NO] DESC";
            return query;
        }

        private string BuildCurrentQuery(string reqItemList)
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/BuildCurrentQuery"); }
            string query =
            //    SELECT REQ_ID,STAT_CHG_DATE,STAT
            //     FROM REQ_ITEM
            //    WHERE STAT_CHG_DATE > CAST('6/6/2017 12:45:00.000' AS DATETIME)
            //    AND STAT = 2
           "SELECT " +
            "REQ_ITEM_ID, " +   //0
            "CASE REQ_ITEM.STAT " +
            "WHEN 1 THEN 'Open' " +
            "WHEN 2 THEN 'Pend Apvl' " +
            "WHEN 3 THEN 'Approved' " +
            "WHEN 4 THEN 'Removed' " +
            "WHEN 5 THEN 'Pending PO' " +
            "WHEN 6 THEN 'Open Stock Order' " +
            "WHEN 8 THEN 'Draft' " +
            "WHEN 9 THEN 'On Order' " +
            "WHEN 10 THEN 'Killed' " +
            "WHEN 11 THEN 'Complete' " +
            "WHEN 12 THEN 'Back Order' " +
            "WHEN 14 THEN 'On Order' " +
            "WHEN 15 THEN 'Auto PO' " +
             "WHEN 24 THEN 'Pend Informational Apvl' " +
            "ELSE CAST(REQ_ITEM.STAT AS VARCHAR(2)) " +
            "END [ITEM STAT],  " +    //1
            "REQ_ITEM.STAT_CHG_DATE " +  //2
            "FROM dbo.REQ_ITEM " +
            "WHERE REQ_ITEM_ID in ( " + reqItemList +
            ")";
            return query;
        }

        private string BuildReqItemUpdateQuery(int reqItem, string status)
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/BuildReqItemUpdateQuery"); }
            string query =
             "UPDATE " +
             "hmcmm_ReqItemReceipt " +
             "SET " +
             "STAT_CHG_DATE = '" + DateTime.Now + "'," +
             "STAT = '" + status + "' " +
             "WHERE REQ_ITEM_ID in (" + reqItem + ")";
            //////"UPDATE " +
            ////// "hmcmm_ReqItemReceipt " +
            ////// "SET " +
            ////// "STAT_CHG_DATE = ''" + DateTime.Now + "''," +
            ////// "STAT = ''" + status + "'' " +
            ////// "WHERE REQ_ITEM_ID in (" + reqItem + ")";
            return query;
        }

        private string BuildTruncateReqItemReceipt()
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/BuildTruncateReqItemReceipt"); }
            return "Truncate table dbo.hmcmm_ReqItemReceipt";
        }

        private string BuildReqItemDeleteQuery()
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/BuildReqItemDeleteQuery"); }
            string[] dt = DateTime.Now.ToString().Split(" ".ToCharArray());
            return "DELETE FROM dbo.hmcmm_ReqItemReceipt WHERE STAT IN ('Killed', 'Complete', 'Removed') AND STAT_CHG_DATE < CONVERT(datetime, '" + dt[0] + "', 101)";
        }

        private string BuildReqItemCountQuery(string dateToday)
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/BuildReqItemCountQuery"); }
            return "SELECT COUNT(*) FROM dbo.hmcmm_ReqItemReceipt";
        }

        private string BuildReqItemInsertQuery(DataRow dRow)
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/BuildReqItemInsertQuery"); }
            string query = "";
            query = "INSERT INTO [dbo].[hmcmm_ReqItemReceipt] VALUES(" +
                           dRow[0] + //REQ_LINE
                           "," + dRow[1] + //REQ_ITEM_ID
                           ",'" + CheckForSingleQuotes(dRow[2].ToString()) + //ITEM_DESC
                           "'," + dRow[3] + //REQ_ID
                           ",'" + CheckForNonCatalog(dRow[4].ToString().Trim()) + //ITEM_NO
                           "','" + dRow[5] + //STAT
                           "','" + dRow[6] + //STAT_CHG_DATE
                           "','" + dRow[7].ToString().Trim() + //LOGIN_ID
                           "','" + dRow[8] + //REQ_CREATE_DATE
                           "')";
            return query;
        }
       
        private bool CheckExistingRecordCount(int REQ_ID, int RI_ID)
        {//return TRUE when a req_id/req_item_id combination is NOT in the hmcmm_ReqItemReceipt table
            if (trace) { lm.Write("TRACE:  DataSetManager/CheckExistingRecordCount"); }
            bool goodToGo = false;
            ArrayList reqCount = new ArrayList();
            ODMRequest Request = new ODMRequest();
            Request.ConnectString = connectStrBIAdmin;
            Request.CommandType = CommandType.Text;
            Request.Command = "SELECT COUNT(*) FROM [dbo].[hmcmm_ReqItemReceipt] WHERE REQ_ITEM_ID = " + RI_ID +
                              " AND REQ_ID = " + REQ_ID;
            reqCount = ODMDataSetFactory.ExecuteDataReader(Request);
            if (Convert.ToInt32(reqCount[0]) == 0)
                goodToGo = true;

            return goodToGo;
        }

        private void ReqReceiptCount()
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/ReqReceiptCount"); }
            ArrayList count = new ArrayList();
            ODMRequest Request = new ODMRequest();
            string[] dtYesterday = DateTime.Now.AddDays(-1).ToString().Split(" ".ToCharArray()); //this prints yesterday's date
            string[] dtToday = DateTime.Now.ToString().Split(" ".ToCharArray()); //this is for the get COUNT(*) query.
            Request.ConnectString = connectStrBIAdmin;
            Request.CommandType = CommandType.Text;
            Request.Command = BuildReqItemCountQuery(dtToday[0]);
            try
            {
                count = ODMDataSetFactory.ExecuteDataReader(ref Request);
                //if (debug && count.Count > 0)                
                lm.Write("Req Receipt Count for " + dtYesterday[0] + " : " + count[0]);

            }
            catch (Exception ex)
            {
                lm.Write("DataSetManager/ReqReceiptCount:  " + ex.Message);
            }
        }

        private string CheckForSingleQuotes(string desc)
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/CheckForSingleQuotes"); }
            string[] quote = desc.Split("'".ToCharArray());
            desc = "";
            for (int x = 0; x < quote.Length; x++)
            {
                desc += quote[x] + "''";
            }
            desc = desc.Substring(0, desc.Length - 2);
            return desc;
        }

        private string CheckForNonCatalog(string item_no)
        {
            if (trace) { lm.Write("TRACE:  DataSetManager/CheckForNonCatalog"); }
            return item_no.Contains("~[") ? "non-catalog" : item_no;
        }
    }
}
