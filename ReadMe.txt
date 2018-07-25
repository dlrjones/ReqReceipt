This app uses the UWM-HEBI1/uwm_BIAdmin/hmcmm_ReqItemReceipt table. 
Every night (12:01am or there abouts) the table is truncated (by invoking this app with a parameter of "1").
For each run of this app, with no parameters, a list of reqs submitted since midnight is compared against the hmcmm_ReqItemReceipt table. 
If the req is already in the table then it is ignored. If it's not in the table then it gets flagged for sending an email to the requestor 
(the receipt) and then inserted into the table.


// Email Password
/*
The password for the SendMail portion of this app is stored in the file [attachmentPath]\pmmhelp.txt (find attachmentPath in the config file).
The referenced library KeyMaster is used to decrypt the password at run time. There is another app called EncryptAndHash which provides a front
end that you can use to change the password when that becomes necessary. The key to the encrypted pw file is pmmhelp and the path to EncryptAndHash is 
\\Lapis\h_purchasing$\Purchasing\PMM IS data\HEMM Apps\Executables\.
*/



