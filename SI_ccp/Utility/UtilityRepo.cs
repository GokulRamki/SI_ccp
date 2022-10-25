using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace SI_ccp.Utility
{
    public class UtilityRepo : IUtilityRepo, IDisposable
    {

        public string sEncKey = ConfigurationManager.AppSettings["encKey"].ToString();
        string error_log_path = ConfigurationManager.AppSettings["error_log_path"].ToString();
        string sBugemailTo = ConfigurationManager.AppSettings["bugEmailTo"].ToString();
        string sJEnckey = ConfigurationManager.AppSettings["dk_enc_key"].ToString();
        private string sBugMailSubject = ConfigurationManager.AppSettings["bugMailSubject"].ToString();

        #region Error Log
        private string sLogFormat;
        private string sErrorTime;

        public void ErrorLog_Txt(string sErrMsg, string stackTrace)
        {
            //HttpContext.Current.Session.Clear();
            //sLogFormat used to create log files format :
            // dd/mm/yyyy hh:mm:ss AM/PM ==> Log Message
            sLogFormat = DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString() + " ==> ";

            //this variable used to create log filename format "
            //for example filename : ErrorLogYYYYMMDD
            string sYear = DateTime.Now.Year.ToString();
            string sMonth = DateTime.Now.Month.ToString();
            string sDay = DateTime.Now.Day.ToString();
            sErrorTime = sYear + sMonth + sDay;

            StreamWriter sw = new StreamWriter(error_log_path + sErrorTime + ".txt", true);
            sw.WriteLine(sLogFormat + sErrMsg);
            sw.WriteLine(stackTrace);
            sw.Flush();
            sw.Close();
            SendEmailMessageFROMGMAIL(sBugemailTo, sBugMailSubject, sLogFormat + sErrMsg + stackTrace);

        }

        /// For Extended Log info
        public void ErrorLog_Txt(Exception ex)
        {
            string sLogFormat="";
            string sErrorTime="";

            //HttpContext.Current.Session.Clear();
            //sLogFormat used to create log files format :
            // dd/mm/yyyy hh:mm:ss AM/PM ==> Log Message
            sLogFormat = DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString() + " ==> ";

            //this variable used to create log filename format "
            //for example filename : ErrorLogYYYYMMDD
            string sYear = DateTime.Now.Year.ToString();
            string sMonth = DateTime.Now.Month.ToString();
            string sDay = DateTime.Now.Day.ToString();
            sErrorTime = sYear + sMonth + sDay;

            StreamWriter sw = new StreamWriter(error_log_path + sErrorTime + ".txt", true);

            string sExMsg = "";
            if (!string.IsNullOrEmpty(ex.Message))
            {
                sExMsg = ex.Message;
                sw.WriteLine(sLogFormat + sExMsg);
            }
            if (ex.InnerException != null)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.Message))
                {
                    sExMsg += "<br>" + ex.InnerException.Message;
                    sw.WriteLine(ex.InnerException.Message);
                }
            }
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                sExMsg += "<br>" + ex.StackTrace;
                sw.WriteLine(ex.StackTrace);
            }
            if (ex.Data.Count > 0)
            {
                sw.WriteLine("Extra details:");
                sExMsg += "<br>Extra details:<br>";
                foreach (DictionaryEntry de in ex.Data)
                {
                    sExMsg += "<br>Key : " + de.Key.ToString();
                    sExMsg += "<br>Value : " + de.Value.ToString();
                    sw.WriteLine(" Key: {0,-20}  Value: {1}",
                                      "'" + de.Key.ToString() + "'", de.Value);
                }
            }

            sw.Flush();
            sw.Close();
            SendEmailMessageFROMGMAIL(sBugemailTo, sBugMailSubject, sLogFormat + sExMsg);

        }

        #endregion

        #region SendEmailMessage For Production

        public void SendEmailMessage(string toAddr, string sSubject, string sMessage)
        {
            string sFrom = ConfigurationManager.AppSettings["EmailFrom"];
            string sCC = ConfigurationManager.AppSettings["EmailCCTo"];
            string sBCC = ConfigurationManager.AppSettings["EmailBCCTo"];
            string smtp_client = ConfigurationManager.AppSettings["Smtp_Client"];
            string uname = ConfigurationManager.AppSettings["UName"];
            string pwd = ConfigurationManager.AppSettings["Password"];
            string sPort = ConfigurationManager.AppSettings["Smtp_Port"];
            MailMessage myMail = new MailMessage();
            myMail.From = new MailAddress(sFrom);
            myMail.Subject = sSubject;
            MailAddressCollection myMailTo = new MailAddressCollection();
            myMail.To.Add(toAddr);
            if (!string.IsNullOrEmpty(sCC))
                myMail.CC.Add(sCC);

            if (!string.IsNullOrEmpty(sBCC))
                myMail.Bcc.Add(sBCC);
            StringBuilder sb = new StringBuilder();
            sb.Append(sMessage);
            string strBody = sb.ToString();
            myMail.Body = strBody;
            myMail.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient(smtp_client);
            smtp.Credentials = new NetworkCredential(uname, pwd);
            //  smtp.Port = Convert.ToInt32(sPort);
            try
            {
                smtp.Send(myMail);
                myMail = null;
            }
            catch (System.Exception ex)
            {

            }
        }

        public void SendEmailMessage(string toAddr, string sSubject, string sMessage, out bool bRes, string mail_attachment)
        {
            string sFrom = ConfigurationSettings.AppSettings["EmailFrom"];
            string sCC = ConfigurationSettings.AppSettings["EmailCCTo"];
            string sBCC = ConfigurationSettings.AppSettings["EmailBCCTo"];
            string smtp_client = ConfigurationSettings.AppSettings["Smtp_Client"];
            string uname = ConfigurationSettings.AppSettings["UName"];
            string pwd = ConfigurationSettings.AppSettings["Password"];
            string sPort = ConfigurationManager.AppSettings["Smtp_Port"];
            bRes = false;
            MailMessage myMail = new MailMessage();
            myMail.From = new MailAddress(sFrom);
            myMail.Subject = sSubject;
            MailAddressCollection myMailTo = new MailAddressCollection();
            myMail.To.Add(toAddr);
            if (!string.IsNullOrEmpty(sCC))
                myMail.CC.Add(sCC);
            if (!string.IsNullOrEmpty(sBCC))
                myMail.Bcc.Add(sBCC);
            if (!string.IsNullOrEmpty(mail_attachment))
            {
                System.Net.Mail.Attachment attachment;
                attachment = new System.Net.Mail.Attachment(mail_attachment);
                myMail.Attachments.Add(attachment);
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(sMessage);
            string strBody = sb.ToString();
            myMail.Body = strBody;
            myMail.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient(smtp_client);
            smtp.Credentials = new NetworkCredential(uname, pwd);
            try
            {
                smtp.Send(myMail);
                bRes = true;
                myMail = null;
            }
            catch (System.Exception ex)
            {
                ErrorLog_Txt(ex);
            }
        }
        #endregion

        #region SendEmail using GMAIL ACCOUNT

        public void SendEmailMessageFROMGMAIL(string to, string sSubject, string body)
        {
            string sSMTP = ConfigurationManager.AppSettings["Smtpg_Server"].ToString();
            string sEmail = ConfigurationManager.AppSettings["Smtpg_Mail"].ToString(); ;
            string sPassword = ConfigurationManager.AppSettings["Smtpg_Pwd"].ToString();
            string sPort = ConfigurationManager.AppSettings["Smtp_Port"].ToString();

            //Create email message
            MailMessage message = new MailMessage();
            message.From = new MailAddress(sEmail);
            message.To.Add(to);
            message.Subject = sSubject;
            message.Body = body;
            //Attachment a = new Attachment(filepath);
            //message.Attachments.Add(angel);
            message.IsBodyHtml = true;
            message.Priority = MailPriority.High;
            //Send the message
            SmtpClient client = new SmtpClient(sSMTP);
            client.Port = Convert.ToInt32(sPort);
            client.Credentials = new NetworkCredential(sEmail, sPassword);
            client.EnableSsl = true;
            try
            {
                client.Send(message);
                message = null;
            }
            catch (System.Exception ex)
            {
            }
        }

        #endregion

        #region Encryption/Decryption

        public string AES_ENC(string _input)
        {
            Encryptor encryptor = new Encryptor(sEncKey);
            string sEnc = encryptor.Encrypt<AesEncryptor>(_input);
            //sEnc = ReplaceStr(sEnc);
            return sEnc;
        }
        public string AES_DEC(string _input)
        {
            //string sdecstring = ReplaceStr(_input);
            Encryptor encryptor = new Encryptor(sEncKey);
            string sDec = encryptor.Decrypt<AesEncryptor>(_input);
            return sDec;
        }

        public string AES_JDEC(string _input)
        {
            Encryptor encryptor = new Encryptor(sJEnckey);
            string sDec = encryptor.Decrypt<AesEncryptor>(_input);
            return sDec;
        }
        public string AES_JENC(string _input)
        {
            Encryptor encryptor = new Encryptor(sJEnckey);
            string sEnc = encryptor.Encrypt<AesEncryptor>(_input);
            return sEnc;
        }


        #endregion

        #region Replace string

        public string ReplaceStr(string sString)
        {
            string sRes = sString.Replace(":", "");
            sRes = sRes.Replace("&", "");
            sRes = sRes.Replace(" ", "-");
            sRes = sRes.Replace("'", "-");
            sRes = sRes.Replace(".", "");
            sRes = sRes.Replace("/", "-");
            sRes = sRes.Replace("%", "-");
            sRes = sRes.Replace("+", "-");
            return sRes;
        }

        #endregion

        #region GetUniqueKey
        /// <summary>
        /// Generate unique Key
        /// </summary>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public string GetUniqueKey(int maxSize)
        {
            char[] chars = new char[62];
            string a;
            a = "abcdefghijklmnopqrstuvwxyz1234567890";//abcdefghijklmnopqrstuvwxyz
            chars = a.ToCharArray();
            int size = maxSize;
            byte[] data = new byte[1];
            RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
            crypto.GetNonZeroBytes(data);
            size = maxSize;
            data = new byte[size];
            crypto.GetNonZeroBytes(data);
            StringBuilder result = new StringBuilder(size);
            foreach (byte b in data)
            { result.Append(chars[b % (chars.Length - 1)]); }
            return result.ToString();
        }
        #endregion

        #region SplitLetterAndNumber

        public void SplitLetterAndNumber(string ip_string, out string letters, out string numbers)
        {
            string op_letters = string.Empty;
            string op_numbers = string.Empty;

            foreach (char c in ip_string)
            {
                if (Char.IsLetter(c))
                {
                    op_letters += c;
                }
                if (Char.IsNumber(c))
                {
                    op_numbers += c;
                }
            }

            letters = op_letters;
            numbers = op_numbers;
        }
        #endregion

        public double CalcGST(double dAmt)
        {
            double dRes = 0;
            double dPerc = Convert.ToDouble(ConfigurationManager.AppSettings["GST_Percentage"]);
            dRes = (dAmt * dPerc) / 100;
            return dRes;
        }

        #region Email Validation
        public bool IsValidEmailAddress(string emailAddress)
        {
            bool bRes = true;
            if (!string.IsNullOrEmpty(emailAddress))
            {
                string emailRegex = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                                         @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                                            @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
                System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(emailRegex);
                if (!re.IsMatch(emailAddress))
                {
                    bRes = false;
                }
            }
            return bRes;
        }
        #endregion

        #region Dispose Objects

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    //_DB.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}