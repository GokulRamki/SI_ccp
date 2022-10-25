using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SI_ccp.Models
{


    public class userlist_model
    {
        public long Id { get; set; }

        public int role_id { get; set; }

        public string role_name { get; set; }

        public string username { get; set; }

        public string emailid { get; set; }

        public bool is_active { get; set; }
    }

    public class user_model
    {
        public long Id { get; set; }

        [Required()]
        [Display(Name = "Role")]
        public int role_id { get; set; }

        [Required()]
        [Display(Name = "First Name")]
        public string first_name { get; set; }

        [Required()]
        [Display(Name = "Last Name")]
        public string last_name { get; set; }

        [Required()]
        [Display(Name = "Mobile Number")]
        public string mobile_number { get; set; }

        [Required()]
        [Display(Name = "Email")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{1,6}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail address")]
        //[RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail adress")]
        [Remote("check_user_exist", "Admin", HttpMethod = "POST", AdditionalFields = "Id", ErrorMessage = "This email id is already exist!")]
        public string email { get; set; }


        public bool send_login { get; set; }

        public bool is_active { get; set; }

        public List<user_access_model> UserAccessList { get; set; }

    }

    public class user_access_model
    {
        public int access_id { get; set; }

        public string level_name { get; set; }

        public bool selected { get; set; }
    }

    public class CompanyModel
    {
        public tbl_company_info company { get; set; }

        public string sales_person { get; set; }
    }

   public class BundlePlanModel
    {
        public tbl_bundle_plan bundlePlan { get; set; }

        public string bundleType { get; set; }
    }

    public class BundlePlan_Model
    {
        
        public long? id { get; set; }

        [Required()]
        [Display(Name = "Bundle ID")]
        public long bundleId { get; set; }

        [Required()]
        [Display(Name = "Plan Name")]
        public string planName { get; set; }

        public string Description { get; set; }

        [Required()]
        [Display(Name = "Plan Price")]
        public double Price { get; set; }

        [Required()]
        [Display(Name = "Size")]
        public string Size { get; set; }

        [Required()]
        [Display(Name = "Plan Validity")]
        public Nullable<int> Validity { get; set; }

        public bool isPostpaid { get; set; }

        public bool isActive { get; set; }
        public Nullable<int> orderby { get; set; }
        public bool isVoice { get; set; }

        [Required()]
        [Display(Name = "Account Type")]
        public string AccountType { get; set; }

        [Required()]
        [Display(Name = "Sms Account Type")]
        public string SmsAccountType { get; set; }

        [Required()]
        [Display(Name = "Voice Account Type")]
        public string VoiceAccountType { get; set; }

        [Required()]
        [Display(Name = "Idd Account Type")]
        public string IddAccountType { get; set; }
        public string UlAccountType { get; set; }
        public string voiceSize { get; set; }
        public string smsCount { get; set; }
        public bool isOnlyData { get; set; }       

        [Required()]
        [Display(Name = "Bundle Type")]
        public int bundle_type_id { get; set; }

        public string btype { get; set; }

        public bool isSelfcare { get; set; }

        public bool isApi { get; set; }

        public long? comp_id { get; set; }
    }   

    public class BundlePlanDetailsModel
    {
        public long bundle_id { get; set; }

        public string bundle_name { get; set; }
    }

    public class BundleCompanyModel
    {
        public tbl_bundle_plan bundlePlan { get; set; }
        public string companyname { get; set; }
    }

    public class BundleCompany
    {
        [Key]
        public long id { get; set; }

        public long bundle_id { get; set; }

        public string bundle_name { get; set; }

        public string description { get; set; }

        public string validity { get; set; }

        public decimal price { get; set; }

        public int bundle_type_id { get; set; }

        public bool is_active { get; set; }

        public string btype { get; set; }

        public bool is_deleted { get; set; }

        public DateTime created_on { get; set; }

        public DateTime? modified_on { get; set; }

        public long? updated_by { get; set; }

        public bool is_selfcare { get; set; }

        public bool is_api { get; set; }

        public long company_id { get; set; }

      
        
     }

    public class CompanyAutoModel
    {
        public long id { get; set; }
        public string company_name { get; set; }
    }
}