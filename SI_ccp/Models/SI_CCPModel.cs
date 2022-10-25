using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace SI_ccp.Models
{
    public class SI_CCPModel
    {
    }

    public class tbl_company_info
    {
        [Key]
        public long id { get; set; }

        [Required()]
        public string company_name { get; set; }

        [Required()]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{1,6}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail address")]
        //[RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail adress")]
        [Remote("check_email_exist", "Admin", HttpMethod = "POST", AdditionalFields = "id", ErrorMessage = "This email already exist!")]
        public string email { get; set; }

        [Required()]
        [DataType(DataType.Password)]
        public string password { get; set; }

        [Required()]
        public string contact_person_name { get; set; }

        //[Required()]
        public decimal credit_amount { get; set; }

        [Required()]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min trans amount")]
        public decimal min_trans_amount { get; set; }

        [Required()]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max trans amount")]
        public decimal max_trans_amount { get; set; }

        [Required()]
        [Display(Name = "Phone Number")]
        public string phone_number { get; set; }

        [Required()]
        [Remote("check_mobile_exist", "Admin", HttpMethod = "POST", AdditionalFields = "id", ErrorMessage = "This Mobile number already exist!")]
        public string mobile_number { get; set; }

        [Required()]
        public string address1 { get; set; }

        public string address2 { get; set; }

        [Required()]
        public string city { get; set; }

        public string country { get; set; }

        public DateTime created_on { get; set; }

        public Nullable<DateTime> modified_on { get; set; }

        public long updated_by { get; set; }

        public bool is_import_topup { get; set; }

        public bool isactive { get; set; }

        public bool isdeleted { get; set; }

        [Required]
        [Display(Name = "Contract Expiry Date")]
        public DateTime? contract_exp_date { get; set; }

        [Required]
        [Display(Name = "Minimum Monthly Spend")]
        public decimal? min_mthly_spend { get; set; }

        public long? sales_person_id { get; set; }
    }

    public class tbl_role
    {
        [Key]
        public int id { get; set; }
        public string role_name { get; set; }
    }

    public class tbl_company_topup_report
    {
        [Key]
        public long id { get; set; }

        //public long user_id { get; set; }

        public long company_id { get; set; }

        public decimal credit_amount { get; set; }

        public string invoice { get; set; }

        public string email { get; set; }

        public string trans_desc { get; set; }

        public DateTime credited_on { get; set; }

        public bool is_recharged { get; set; }

        public int payment_type_id { get; set; }

        public string account_order_no { get; set; }

        public string internal_ref_no { get; set; }

        public Nullable<long> temp_topup_id { get; set; }

        public decimal prev_bal { get; set; }
    }

    public class tbl_company_topup_temp
    {
        [Key]
        public long id { get; set; }

        public long company_id { get; set; }

        public decimal credit_amount { get; set; }

        public string invoice { get; set; }

        public string email { get; set; }

        public string trans_desc { get; set; }

        public DateTime credited_on { get; set; }

        public bool is_recharged { get; set; }

        public int payment_type_id { get; set; }

        public string account_order_no { get; set; }

        public string internal_ref_no { get; set; }

        public int approval_status_id { get; set; }

        public long? sales_person_id { get; set; }

        public long? updated_by { get; set; }
    }

    public class tbl_staff_topup
    {
        [Key]
        public long id { get; set; }

        public long company_id { get; set; }

        public long user_id { get; set; }

        public string first_name { get; set; }

        public string last_name { get; set; }

        public string msisdn_number { get; set; }

        public decimal topup_amount { get; set; }

        public string trans_desc { get; set; }

        public DateTime created_on { get; set; }

        public Nullable<DateTime> trans_date { get; set; }

        public string invoice_number { get; set; }

        public string email { get; set; }

        public bool is_recharged { get; set; }

        public bool isactive { get; set; }

        public bool isdeleted { get; set; }

        public long? sales_person_id { get; set; }

        public string result_code { get; set; }

        public bool is_processed { get; set; }

        public decimal curr_bal { get; set; }
    }

    public class tbl_staff_topup_bundle
    {
        [Key]
        public long id { get; set; }

        public long staff_topup_id { get; set; }

        public long bundle_id { get; set; }

        public string bundle_name { get; set; }

        public decimal bundle_amt { get; set; }

        public bool is_recharged { get; set; }

        public bool is_active { get; set; }

        public bool is_deleted { get; set; }

        public string result_code { get; set; }

        public string result_desc { get; set; }

        public bool is_processed { get; set; }
    }

    public class tbl_staff_topup_trans
    {
        [Key]
        public long id { get; set; }

        public string msisdn_number { get; set; }

        public decimal amount { get; set; }

        public string trans_desc { get; set; }

        public DateTime trans_date { get; set; }

        public string invoice_number { get; set; }

        public string email { get; set; }

        public string ip_address { get; set; }

        public bool is_recharged { get; set; }

        public long company_id { get; set; }

        public long user_id { get; set; }

        public decimal curr_bal { get; set; }

    }

    public class tbl_user
    {
        [Key]
        public long id { get; set; }

        public long company_id { get; set; }

        [Required()]
        public string first_name { get; set; }

        public string last_name { get; set; }

        [Required()]
        //[RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail adress")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{1,6}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail address")]
        [Remote("check_user_email_exist", "Company", HttpMethod = "POST", AdditionalFields = "id", ErrorMessage = "This email already exist!")]
        public string email { get; set; }

        [Required()]
        public string msisdn_number { get; set; }

        [Required()]
        [DataType(DataType.Password)]
        public string password { get; set; }

        //public int role_id { get; set; }

        public DateTime created_on { get; set; }

        public Nullable<DateTime> modified_on { get; set; }

        public bool isactive { get; set; }

        public bool isdeleted { get; set; }
    }

    public class tbl_admin_user
    {
        [Key]
        public long id { get; set; }

        [Required()]
        public string first_name { get; set; }

        public string last_name { get; set; }

        [Required()]
        //[RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail adress")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{1,6}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail address")]
        [Remote("check_user_exist", "Admin", HttpMethod = "POST", AdditionalFields = "id", ErrorMessage = "This email already exist!")]
        public string email { get; set; }

        [Required()]
        public string msisdn_number { get; set; }

        [Required()]
        [DataType(DataType.Password)]
        public string password { get; set; }

        public int role_id { get; set; }

        public DateTime created_on { get; set; }

        public Nullable<DateTime> modified_on { get; set; }

        public bool isactive { get; set; }

        public bool isdeleted { get; set; }
    }


    public class tbl_menu
    {
        [Key()]
        public int id { get; set; }

        public string menu_name { get; set; }

        public string menu { get; set; }

        public bool isactive { get; set; }

        public bool isdeleted { get; set; }
    }

    public class tbl_user_access
    {
        [Key()]
        public long id { get; set; }

        public long user_id { get; set; }

        public long access_id { get; set; }
    }

    public class admin_login_model
    {

        [Required()]
        //[RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail adress")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{1,6}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail address")]
        public string username { get; set; }

        [Required()]
        [DataType(DataType.Password)]
        public string password { get; set; }


    }

    public class company_login_model
    {

        [Required()]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{1,6}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail address")]
        //[RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail adress")]
        public string username { get; set; }

        [Required()]
        [DataType(DataType.Password)]
        public string password { get; set; }


    }

    #region Import Topup

    public class import_topup_model
    {
        //[Required]
        //[RegularExpression(@"^[a-zA-Z'\s]+$", ErrorMessage = "*")]
        public string first_name { get; set; }

        //[Required]
        //[RegularExpression(@"^[a-zA-Z'\s]+$", ErrorMessage = "*")]
        public string last_name { get; set; }

        public string msisdn_number { get; set; }

        //[Required(ErrorMessage = "*")]
        public Nullable<decimal> amount { get; set; }

        //[Required(ErrorMessage = "*")]
        public string invoice { get; set; }

        //[Required]
        //[RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "*")]
        //[RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{1,6}|[0-9]{1,3})(\]?)$", ErrorMessage = "*")]
        public string email { get; set; }

        public DateTime created_on { get; set; }

        public bool is_recharged { get; set; }

        public bool is_error { get; set; }

        public string description { get; set; }

        public Nullable<decimal> bundle1_amt { get; set; }

        public Nullable<decimal> bundle2_amt { get; set; }

        public Nullable<decimal> bundle3_amt { get; set; }

        public List<long> bundle_ids { get; set; }

        public List<long> bid_list { get; set; }

        public string bType { get; set; }

        public string bType2 { get; set; }

        public string bType3 { get; set; }

        public bool is_valid { get; set; }
    }

    public class import_staff_model
    {
        public HttpPostedFileBase staff_csv { get; set; }

        public List<import_topup_model> staff_topup_list { get; set; }

        public tbl_company_info company_info { get; set; }

        //public string company_name { get; set; }

        public string cmd { get; set; }

        //public decimal company_avail_credit_amt { get; set; }

        //public decimal min_transaction_amt { get; set; }

        //public decimal max_transation_amt { get; set; }

        public string staffjson { get; set; }

    }



    #endregion

    public class RechargeModel
    {
        public string username { get; set; }
        public string password { get; set; }
        public string keycode { get; set; }
        public string msisdn { get; set; }
        public string amount { get; set; }

    }

    public class validate_output_model
    {
        public int resultcode { get; set; }
        public string reference { get; set; }
        public string desc { get; set; }
    }

    public class recharge_msisdn_model
    {
        public string reference { get; set; }
        public string msisdn { get; set; }
        public string amount { get; set; }
        public string invoice { get; set; }
        public string masterMsisdn { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string keycode { get; set; }
        public string ocsRef { get; set; }

        public List<recharge_bundle_model> bundleIds { get; set; }
    }

    public class recharge_bundle_model
    {
        public string bundlePlanId { get; set; }

        public string bundlePrice { get; set; }

        public string bundleName { get; set; }
    }



    public class recharge_OP_model
    {
        public long resultCode { get; set; }
        public string rechargeAmount { get; set; }
        public string newBalance { get; set; }
        public string reference { get; set; }
        public string msidnNumber { get; set; }
        public string desc { get; set; }

        public List<ReponseBundleModel> bundleResponse { get; set; }
    }

    #region ReponseBundleModel

    public class ReponseBundleModel
    {
        public string bundleId { get; set; }
        public string bundleName { get; set; }
        public string bundlePrice { get; set; }
        public string resultCode { get; set; }
        public string resultDesc { get; set; }
    }
    #endregion

    public class topup_detail_model
    {
        public long company_id { get; set; }

        public string company_name { get; set; }

        public string email { get; set; }

        public string contact_person_name { get; set; }

        public decimal avail_credit_amount { get; set; }

        public string mobile_number { get; set; }

        public string invoice { get; set; }

        [Required()]
        public int payment_type_id { get; set; }

        [Required()]
        public string description { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        [Display(Name = "Topup amount")]
        public decimal topup_amount { get; set; }

        [Required]
        public string account_order_no { get; set; }

        [Required]
        public string internal_ref_no { get; set; }

        public List<topup_report_model> reports { get; set; }

        public List<tbl_payment_type> pay_types { get; set; }

    }

    public class topup_report_model
    {
        public long comapny_id { get; set; }

        public string company_name { get; set; }

        public string email { get; set; }

        public string mobile_number { get; set; }

        public decimal credit_amount { get; set; }

        public string invoice { get; set; }

        public string trans_desc { get; set; }

        public string internal_ref_no { get; set; }

        public string account_order_no { get; set; }

        public int payment_type_id { get; set; }

        public string payment_type_name { get; set; }

        public DateTime credited_on { get; set; }

        public bool is_recharged { get; set; }

        public string approval_status { get; set; }

        public int approval_status_id { get; set; }

        public long temp_topup_id { get; set; }

        public string sales_person { get; set; }

        public long? sales_person_id { get; set; }

        public DateTime? approved_on { get; set; }
    }

    public class tbl_payment_type
    {
        [Key]
        public int Id { get; set; }

        public string payment_type { get; set; }

        public bool is_active { get; set; }

    }

    public class staff_process_model
    {
        public tbl_company_info company { get; set; }

        public decimal required_amt { get; set; }

    }


    #region BundlePlansModel

    public class BundlePlansModel
    {
        public long bundle_id { get; set; }

        public string bundle_name { get; set; }

        public decimal price { get; set; }
    }

    public class tbl_bundle_plan
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

        public long? company_id { get; set; }

        //public List<tbl_company_info> company_list { get; set; }
    }

    public class tbl_bundle_type
    {
        [Key]
        public int id { get; set; }

        public string bundle_type { get; set; }
    }
    #endregion

    #region StaffsTopupModel

    public class StaffsTopupModel
    {
        public string company_name { get; set; }

        public tbl_staff_topup staff_topup { get; set; }

        public List<tbl_staff_topup_bundle> staff_topup_bundle { get; set; }

        public string sales_person { get; set; }
    }

    #endregion


    public class staff_msisdn_admin_model
    {
        public long? company_id { get; set; }

        public bool? rec_status { get; set; }

        public DateTime? sdate { get; set; }

        public DateTime? edate { get; set; }

        public string mobile_number { get; set; }

        public List<tbl_company_info> company_list { get; set; }

       // public IList<StaffsTopupModel> staffs_topup_list { get; set; }
        public IList<StaffTopupNewModel> staffs_topup_list { get; set; }


        public long? sales_person_id { get; set; }
    }

    #region approve status
    public class tbl_approve_status
    {
        public int id { get; set; }

        public string status { get; set; }
    }
    #endregion

    #region sales person table

    public class tbl_sales_person
    {
        [Key()]
        public long Id { get; set; }

        [Required()]
        [Display(Name = "First Name")]
        public string first_name { get; set; }

        [Display(Name = "Last Name")]
        public string last_name { get; set; }


        [Display(Name = "Email Id")]
        //[RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail adress")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{1,6}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail address")]
        public string email { get; set; }

        [Required()]
        [RegularExpression("([1-9][0-9]*)", ErrorMessage = "contact number must be a numeric number")]
        [Remote("check_sales_person_number_exist", "Admin", HttpMethod = "POST", AdditionalFields = "Id", ErrorMessage = "This Contact Number already exist!")]
        public string contact_number { get; set; }

        public string contact_address { get; set; }

        public bool isActive { get; set; }

        public bool isDeleted { get; set; }

        public DateTime createdOn { get; set; }

        public Nullable<DateTime> modifiedOn { get; set; }
        //  newly added
        public Nullable<long> updated_by { get; set; }
    }

    [NotMapped]
    public class SalesPersonModel : tbl_sales_person
    {
        public string fullName { get; set; }
    }

    public class SalesPersonDropdownModel
    {
        public string salesPersonName { get; set; }

        public long Id { get; set; }
    }
    #endregion

    #region model for list of failed records

    public class TopupResultModel
    {
        public string msisdn_number { get; set; }
        public string bundleId { get; set; }
        public string bundleName { get; set; }
        public string bundlePrice { get; set; }
        public string resultCode { get; set; }
        public string resultDesc { get; set; }
    }

    #endregion

    #region Sales by sales person model
    public class SalesBySalesPersonModel
    {
        public string masterName { get; set; }

        public string masterNumber { get; set; }

        public decimal amount { get; set; }

        public Nullable<DateTime> topupDate { get; set; }

        public string salesPerson { get; set; }

        public long? salesPersonID { get; set; }

    }
    #endregion

    #region for Master Account Expriy

    public class MasterAccExpriyReportModel
    {
        public string company_name { get; set; }

        public string contact_person_name { get; set; }

        public string email { get; set; }

        public string mobile_number { get; set; }

        public DateTime? ContractExpiryDate { get; set; }

        public bool status { get; set; }
    }

    #endregion

    #region For Master Low Balance Report

    public class MasterAccLowBalanceModel
    {
        public string company_name { get; set; }

        public string contact_person_name { get; set; }

        public string email { get; set; }

        public string mobile_number { get; set; }

        public decimal available_amount { get; set; }

        public decimal? required_amount { get; set; }

        public decimal? outstanding_amount { get; set; }

        public string salesPerson { get; set; }

        public long? salesPersonID { get; set; }
    }

    #endregion

    public class tbl_si_ccp_invoice
    {
        [Key()]
        public long id { get; set; }

        public long invoice_no { get; set; }

        public string invoice_txt { get; set; }

        public long company_id { get; set; }

        [Required]
        [Display(Name = "Topup Amount")]
        public Nullable<long> total_quantity { get; set; }

        [Required]
        [Display(Name = "Unit Cost")]
        public Nullable<decimal> unit_price { get; set; }

        [Required]
        [Display(Name = "Total Amount")]
        public Nullable<decimal> total_amt { get; set; }

        public Nullable<decimal> item_val { get; set; }

        public Nullable<decimal> total_gst_val { get; set; }

        public DateTime created_on { get; set; }
    }

    public class InvoiceModel
    {
        public tbl_si_ccp_invoice invoicedetail { get; set; }

        public long id { get; set; }

        public string company_name { get; set; }

        public string email { get; set; }

        public string contact_person_name { get; set; }

        public string phone_number { get; set; }

        public string mobile_number { get; set; }

    }













    #region tbl_evd_track

    public partial class tbl_ccp_track
    {
        [Key]
        public long id { get; set; }

        public long user_id { get; set; }

        public long rpt_cat_id { get; set; }

        public long action_id { get; set; }

        public string ip_address { get; set; }

        public string url_accesses { get; set; }

        public int role_id { get; set; }

        //public string action_performed { get; set; }

        public DateTime created_on { get; set; }

        public string access_from { get; set; }

        public string sdate { get; set; }

        public string stime { get; set; }

        public string url_referal { get; set; }

        public string msisdn { get; set; }
    }

    #endregion

    #region tbl_evd_track_action

    public partial class tbl_ccp_track_action
    {
        [Key]
        public long id { get; set; }

        public string action_name { get; set; }

        public DateTime created_on { get; set; }

        public bool is_active { get; set; }

        public bool is_deleted { get; set; }
    }

    #endregion

    #region tbl_evd_report_cat

    public partial class tbl_ccp_report_cat
    {
        [Key]
        public long id { get; set; }

        public string report_cat_name { get; set; }

        public DateTime created_on { get; set; }

        public DateTime? modified_on { get; set; }

        public DateTime? deleted_on { get; set; }

        public bool is_active { get; set; }

        public bool is_deleted { get; set; }
    }
    #endregion


    #region AuditTrailModel
    public class ccp_AuditModel
    {

        public long user_id { get; set; }

        public string user_name { get; set; }

        public long rpt_cat_id { get; set; }

        public string rpt_cat_name { get; set; }

        public long action_id { get; set; }

        public string action_name { get; set; }

        public string ip_address { get; set; }

        public string url_accesses { get; set; }

        public DateTime? created_on { get; set; }

        public int role_id { get; set; }

        public string role_name { get; set; }

        public string access_from { get; set; }

        public string sdate { get; set; }

        public string url_referal { get; set; }

        public string msisdn { get; set; }
    }
    #endregion


    public class tbl_user_login_his
    {
        [Key]
        public long Id { get; set; }

        public long user_id { get; set; }

        public DateTime created_on { get; set; }

        public string log_from { get; set; }
    }


    public class tbl_company_login_his
    {
        [Key]
        public long Id { get; set; }

        public long company_id { get; set; }

        public DateTime created_on { get; set; }

        public string log_from { get; set; }
    }
    #region StaffsTopupModel

    public class StaffTopupNewModel
    {
        public string company_name { get; set; }

        public string sales_person { get; set; }

        public string first_name { get; set; }

        public string last_name { get; set; }

        public string msisdn_number { get; set; }

        public decimal? topup_amount { get; set; }

        public string invoice_number { get; set; }

        public bool? is_recharged { get; set; }

        public bool? isactive { get; set; }

        public bool? isdeleted { get; set; }
        public string trans_desc { get; set; }

        public DateTime? trans_date { get; set; }
        public long? staff_topup_id { get; set; }

        public decimal? balance { get; set; }

        public string bundle_name { get; set; }

        public decimal? bundle_amt { get; set; }
        public long? sales_person_id { get; set; }
        public bool? is_processed { get; set; }
        public string email { get; set; }
    }

    #endregion

    public class temp_staff_topup
    {
        public string msisdn { get; set; }
        public string bundle_name { get; set; }
        public long? bundle_id { get; set; }
    }
}