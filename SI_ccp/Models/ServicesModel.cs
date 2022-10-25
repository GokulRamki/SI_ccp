using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SI_ccp.Models
{
    public class ReturnModel
    {
        public string resultCode { get; set; }
    }

    public class BundlePlanServiceModel
    {

        //public long Id { get; set; }

        //public string UserId { get; set; }

        //public string PlanName { get; set; }

        //public string Description { get; set; }

        //public double Price { get; set; }

        //public string _sPrice { get; set; }

        //public string Size { get; set; }

        //public int Validity { get; set; }

        //public string AccountType { get; set; }

        //public bool isPostpaid { get; set; }

        //public bool isActive { get; set; }

        //public Nullable<int> orderby { get; set; }

        ////public Dictionary<string,object> htmlAttributes { get; set; }
        //public string htmlAttributes { get; set; }

        //public bool isChecked { get; set; }

        //public bool isVoice { get; set; }

        //public bool isDeleted { get; set; }

        //public string sStatus { get; set; }

        //public string sType { get; set; }

        //public bool isPlanActive { get; set; }

        //public string SmsAccountType { get; set; }

        //public string VoiceAccountType { get; set; }

        //public string IddAccountType { get; set; }

        //public string voiceSize { get; set; }

        //public string smsCount { get; set; }

        //public string voice_desc { get; set; }

        //public string sms_desc { get; set; }

        //public string validity_txt { get; set; }

        //public bool isOnlyData { get; set; }

        //public int bundle_type_id { get; set; }

        //public bool isCCtopup { get; set; }

        //public bool isCorporate { get; set; }

        public long bundle_id { get; set; }

        public string bundle_Name { get; set; }

        public string desc { get; set; }

        public decimal Price { get; set; }

        public string Validity { get; set; }

        public bool isActive { get; set; }

        public int bundle_type_id { get; set; }
    }

    public class BundleServiceOPModel
    {
        public string resultCode { get; set; }
    }

    public class bundleModel
    {
        public long Id { get; set; }
        public System.DateTime Created_at { get; set; }
        public string PlanName { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string Size { get; set; }
        public Nullable<int> Validity { get; set; }
        public Nullable<bool> isPostpaid { get; set; }
        public Nullable<bool> isActive { get; set; }
        public Nullable<System.DateTime> Modified_on { get; set; }
        public Nullable<int> orderby { get; set; }
        public bool isVoice { get; set; }
        public Nullable<bool> isDelete { get; set; }
        public string AccountType { get; set; }
        public string SmsAccountType { get; set; }
        public string VoiceAccountType { get; set; }
        public string IddAccountType { get; set; }
        public string UlAccountType { get; set; }
        public string voiceSize { get; set; }
        public string smsCount { get; set; }
        public Nullable<bool> isOnlyData { get; set; }
        public string voice_desc { get; set; }
        public string sms_desc { get; set; }
        public string validity_txt { get; set; }
        public Nullable<bool> isCorporate { get; set; }
        public string ussddesc { get; set; }
        public Nullable<bool> isUssd { get; set; }
        public string menu { get; set; }
        public string roamingVoiceAccountType { get; set; }
        public string roamingSmsAccountType { get; set; }
        public string romingDataAccountType { get; set; }
    }

    public class getBundleTypeModel : ReturnModel
    {
        public int bdleTypeId { get; set; }
    }

public class getBundlePlanModel
    {
        public string resultcode { get; set; }

        public bundleModel bundles { get; set; }
    }
}