using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SI_ccp.Utility
{
    interface IUtilityRepo:IDisposable
    {

        void ErrorLog_Txt(Exception ex);

        void SendEmailMessage(string toAddr, string sSubject, string sMessage);

        void SendEmailMessageFROMGMAIL(string to, string sSubject, string body);

        string AES_ENC(string _input);

        string AES_DEC(string _input);

        string AES_JENC(string _input);

        string AES_JDEC(string _input);

        string ReplaceStr(string sString);

        string GetUniqueKey(int maxSize);

        void SplitLetterAndNumber(string ip_string, out string letters, out string numbers);

        double CalcGST(double dAmt);

        void SendEmailMessage(string toAddr, string sSubject, string sMessage, out bool bRes, string mail_attachment);

        bool IsValidEmailAddress(string emailAddress);

        void ErrorLog_Txt(string sErrMsg, string stackTrace);
    }
}
