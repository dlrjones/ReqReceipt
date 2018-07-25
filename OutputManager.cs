using System;
using System.Collections;
using System.Net.Mail;
using System.IO;
using KeyMaster;
using LogDefault;

namespace ReqReceipt
{
    class OutputManager
    {
        #region ClassVariables
        private ArrayList reqItems = new ArrayList();
        private ArrayList debugCCList = new ArrayList();
        private string debugRecipientList = "";        
        private string reqNo = "";
        private string reqDate = ""; 
        private string userName = "";
        private string recipient = "";
        private string body = "";
        private string attachmentPath = "";
        private string extension = "";
        private string helpFile = "pmmhelp.txt";
        private string unameVarients = "";
        private string currentAcctNo = "";
        private string[] ccList;
        private bool debug = false;
        private bool trace = false;
        private bool addAttachment = false;
        private char TAB = Convert.ToChar(9);
        private Hashtable itemReq = new Hashtable();
        private Hashtable itemReqLine = new Hashtable();
        private Hashtable itemsThatChanged = new Hashtable();
        private Hashtable itemItemNo = new Hashtable();
        private Hashtable itemDesc = new Hashtable();
        private Hashtable debugCCRecip = new Hashtable();
        private Hashtable itemNoDescr = new Hashtable();
        private Hashtable reqItem = new Hashtable(); //reqID - itemList  (a comma seperated list of all item_no's on a given req)
        private Hashtable itemDescr = new Hashtable();//itemNo - descr
        private Hashtable itemQty = new Hashtable(); // key=itemNo valu=qty ordered
        private Hashtable itemUM = new Hashtable(); // key=itemNo valu=unit of measure
        private LogManager lm = LogManager.GetInstance();
        private int fileCount = 1;
        #endregion
        #region parameters
        public ArrayList ReqItems
        {
            set { reqItems = value; }
        }
        public string ReqNo
        {
            set { reqNo = value; }
        }
        public string CurrentAcctNo
        {
            set { currentAcctNo = value; }
        }
        public string ReqDate
        {
            set { reqDate = value; }
        }
        public string UserName
        {
            set { userName = value; }
        }
        public string DebugRecipientList
        {
            set { debugRecipientList = value; }
        }
        public ArrayList DebugCCList
        {
            set { debugCCList = value; }
        }
        public string UnameVarients
        {
            set { unameVarients = value; }
        }
        public string AttachmentPath
        {
            set { attachmentPath = value; }
        }
        public Hashtable ItemReq
        {
            set { itemReq = value; }
        }
        public Hashtable ItemReqLine
        {
            set { itemReqLine = value; }
        }
        public Hashtable ItemsThatChanged
        {
            set { itemsThatChanged = value; }
        }
        public Hashtable ItemItemNo
        {
            set { itemItemNo = value; }
        }
        public Hashtable ItemDesc
        {
            set { itemDesc = value; }
        }
        private Hashtable reqBuyer = null;
        public Hashtable DebugCCRecip
        {
            set { debugCCRecip = value; }
        }
        public Hashtable ReqBuyer
        {
            set { reqBuyer = value; }
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

        public void SendOutput()
        {
            //ProcessManager.SendOutput has a ForEach loop which invokes this method once for each recipient in the recipient list
            if (trace) { lm.Write("TRACE:  OutputManager/SendOutput"); }
            FormatEmail();
           // FormatAttachment();
            SendMail();
        }

        private void FormatEmail()
        {
            if (trace) { lm.Write("TRACE:  OutputManager/FormatEmail"); }
            string desc = "";
            string status = "";
            string[] dateTime = reqDate.Split(" ".ToCharArray());
            string[] itemList;

            try
            {
                body = "The requisition you submitted -  Requisition # " + reqNo + " on " + dateTime[0].ToString() +
                        " - was accepted into HEMM at " + dateTime[1].ToString() + " " + dateTime[2].ToString() +
                        " and is starting through the approval process. " +
                        "Please contact your buyer group at " + reqBuyer[reqNo] + " if you have any questions." + Environment.NewLine + Environment.NewLine;

                //these next 4 lines (counting 'foreach' as one line) were added to include the item# and description for the items on each req.
                //this is a test beyond the original test of simply sending the req receipt email
                GetItemList(); //added to include item# and Descr   (reqNo)
                itemList = reqItem[reqNo].ToString().Split(",".ToCharArray());
                body += "ITEM #" + TAB + TAB + "QTY" + TAB + "UM" + TAB + "DESCRIPTION" + Environment.NewLine;
                foreach (string item in itemList)
                {
                    if(item.Length > 6)
                       // body += item + TAB + TAB + itemDesc[item.Trim()].ToString().Trim() + Environment.NewLine;
                        body += item + TAB + itemQty[item.Trim()].ToString().Trim() + TAB + itemUM[item.Trim()].ToString().Trim() + TAB +  itemDesc[item.Trim()].ToString().Trim() + Environment.NewLine;
                    else
                        body += item + TAB + TAB + itemQty[item.Trim()].ToString().Trim() + TAB + itemUM[item.Trim()].ToString().Trim() + TAB +  itemDesc[item.Trim()].ToString().Trim() + Environment.NewLine;
                    //body += item + TAB + TAB + TAB + itemDesc[item.Trim()].ToString().Trim() + TAB + itemQty[item.Trim()].ToString().Trim() + TAB + itemUM[item.Trim()].ToString().Trim() + Environment.NewLine;
                }
            }
            catch(Exception ex)
            {
                lm.Write("OutputManager/FormatEmail:  ERROR:  " + ex.Message);
            }
            if (!userName.Contains("@"))
                userName += "@uw.edu";
            recipient = userName;            
        }

        private void GetItemList()              //string reqno
        {
            //retrieves the hashtables reqItem and itemDescr from the DataSet Manager
            if (trace) { lm.Write("TRACE:  OutputManager/GetItemList"); }
            DataSetManager dsm = DataSetManager.GetInstance();
            reqItem = dsm.ReqItem;
            itemDesc = dsm.ItemDescr;
            itemQty = dsm.ItemQty;
            itemUM = dsm.ItemUM;
    }

        private string NonCtlgCheck(string itemNo)
        {//filter out the non-catalog items -- may not be used
            if (trace) { lm.Write("TRACE:  OutputManager/NonCtlgCheck"); }
            if (itemNo.Contains("~"))
                itemNo = "Non Catalog";
            return itemNo;
        }       

        private void SendMail()
        {
            if (trace) { lm.Write("TRACE:  OutputManager/SendMail"); }
            try
            { 
                string[] dbugRecipList = null;
                string[] dbugCCRecip = null;
                string logMssg = "";
                bool mailError = false;
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.uw.edu");
                mail.From = new MailAddress("pmmhelp@uw.edu");
                if (debug)
                {
                    dbugRecipList = debugRecipientList.Split(",".ToCharArray());
                    //debug = true;
                    foreach (string recip in dbugRecipList)
                    {
                        if (recip.Equals(recipient))  //this conditional was added to test the app with a few specific recipients 
                        {                              //who would see only their reqs and not all active reqs
                            mail.To.Add(recip);
                        }
                    }                    
                }
                else //not in debug mode
                {                    
                    mail.To.Add(recipient);
                }
                //for certain cost centers send a receipt to the manager.
                try
                {
                    foreach (object key in debugCCRecip.Keys)
                    {
                        if (key.ToString().Trim().Equals(currentAcctNo.Trim()))
                        {
                            dbugCCRecip = debugCCRecip[key].ToString().Split(",".ToCharArray());
                            foreach (string emailTO in dbugCCRecip)
                            {
                                mail.To.Add(emailTO);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lm.Write("EmailTO Error: " + ex.Message);
                }

                //       mail.To.Add("dlrjones@uw.edu");
                mail.Subject = "Requisition " + reqNo + " Receipt";
                if (debug)
                //    mail.Subject += "       " + recipient;  //commented out to accommodate a specific test
                        mail.Body = body +
                            Environment.NewLine + Environment.NewLine +
                            "Thanks," +
                            Environment.NewLine +
                            Environment.NewLine +
                            "PMMHelp" + Environment.NewLine +
                            "UW Medicine Harborview Medical Center" + Environment.NewLine +
                            "Supply Chain Management Informatics" + Environment.NewLine +
                            "206-598-0044" + Environment.NewLine +
                            "pmmhelp@uw.edu";
                        mail.ReplyToList.Add("pmmhelp@uw.edu");
                        if (addAttachment)
                        {
                            Attachment attachment;
                            attachment = new Attachment(attachmentPath + extension);
                            mail.Attachments.Add(attachment);
                        }
                        SmtpServer.Port = 587;
                        SmtpServer.Credentials = new System.Net.NetworkCredential("pmmhelp", GetKey());
                        SmtpServer.EnableSsl = true;
                try
                { //debug=true sends all emails to dlrjones@uw.edu
                    if (!debug && mail.To.Count > 0)
                        SmtpServer.Send(mail);
                    logMssg = "OutputManager/SendMail:  Sent To  " + mail.To + "  [Req# " + reqNo + "]  mailTO count: " + mail.To.Count;

                    if (debug && mail.To.Count > 0)
                    {
                        logMssg += "       (for " + recipient + ")";
   ////////////                     SmtpServer.Send(mail); //comment this out to prevent emails going to dlrjones while debug = true
                    }                   
                }
                catch (SmtpException ex)
                {
                    lm.Write("SendMail SmtpException:  " + ex.Message + Environment.NewLine + ex.InnerException);
                    mailError = true;
                }
                catch (Exception ex)
                {
                    lm.Write("SendMail Error " + ex.Message + Environment.NewLine + ex.InnerException);
                    mailError = true;
                }
                // }
                if (mailError)
                {//sometimes SendMail errors out with a message like "The message or signature supplied for verification has been altered"
                 //or "The buffers supplied to a function was too small". Sending it a second time sometimes works.
                    try
                    {
                        SmtpServer.Send(mail);
                        lm.Write("SendMail mail resent: " + mail.To.ToString() + "       (for " + recipient + ")");
                    }
                    catch (Exception ex)
                    {
                        lm.Write("SendMail mailError 2  " + ex.Message + Environment.NewLine + ex.InnerException);
                        logMssg = logMssg.Replace("Sent", "NOT Sent");
                    }
                }                  

                lm.Write(logMssg);
                mailError = false;
                }
            catch (Exception ex)
            {
                string mssg = ex.Message;
                lm.Write("OutputManager/SendMail: Exception    " + mssg);
            }
        
        }       

        public string GetKey()
        {
            if (trace) { lm.Write("TRACE:  OutputManager/GetKey"); }
            //NameValueCollection ConfigData = (NameValueCollection)ConfigurationSettings.GetConfig("appSettings");
            //attachmentPath = ConfigData.Get("attachmentPath");
            string[] key = File.ReadAllLines(attachmentPath + helpFile);
            return StringCipher.Decrypt(key[0],"pmmhelp");            
        }
    }
}
