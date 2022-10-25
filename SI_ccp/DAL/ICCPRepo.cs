using SI_ccp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SI_ccp.DAL
{
    interface ICCPRepo:IDisposable
    {

        bool SendStaffsTopupEmail(long company_id,out string msg);

        List<topup_report_model> get_topup_reports(DateTime sdate, DateTime edate);

        tbl_user check_user_login(admin_login_model obj_login);

        List<tbl_company_info> get_companies();

        bool insert_company(tbl_company_info obj_comp);

        bool update_company(tbl_company_info obj_comp);

        bool check_email_exist(string email, int? id);

        bool insert_company_topup_trans(tbl_company_topup_report obj_com);

        bool insert_company_topup_temp(tbl_company_topup_temp obj_com);

        List<tbl_company_topup_report> company_topup_report(long com_id);

        tbl_company_info get_activecompanyby_id(long id);

        tbl_company_info get_company_id(long id);


        //User
        tbl_staff_topup get_staff_topupby_userid(long user_id, string msisdn_no);

        List<StaffTopupNewModel> get_staff_topup_by_userid(long user_id);

        tbl_staff_topup get_staff_topupby_id(long id);

        bool update_staff_topup(tbl_staff_topup topuplist);

        bool insert_staff_topup(tbl_staff_topup topuplist, List<long> bundle_ids, List<tbl_bundle_plan> objBundle_plans);

        tbl_user get_activeuserby_id(long id);

        tbl_user get_userby_id(long id);

        //company
        tbl_company_info check_company_login(company_login_model obj_login);

        bool check_user_email_exist(string email, int? id);

        List<tbl_user> getuserby_compid(long company_id);

        bool update_user(tbl_user obj_user);

        bool insert_user(tbl_user obj_user);

        List<tbl_staff_topup> get_staff_topupby_comp(long comp_id);

        List<StaffTopupNewModel> get_staff_topupby_company(string sdate, string edate, long comp_id = 0, long user_id = 0);

        List<tbl_staff_topup_trans> get_staff_topup_transby_comp(DateTime sTransFrom, DateTime sTransTo,long comp_id);

        List<tbl_payment_type> get_paytype_list();

        List<topup_report_model> get_topup_reportsby_comp(long comp_id);

        List<topup_report_model> get_temp_topup(DateTime sdate, DateTime edate);

        List<topup_report_model> get_temp_topup_by_comp(long comp_id);

        //List<BundlePlansModel> GetBundle_Plans();

        List<tbl_bundle_plan> GetBundle_Plans();

        List<tbl_staff_topup_bundle> GetStaffs_Bunlde_Topup(long staff_topup_id);

        #region get_staff_topup

        List<StaffTopupNewModel> get_staff_topup();

        List<StaffTopupNewModel> get_staff_topup(long comp_id);
        #endregion

        List<tbl_approve_status> GetApprove_Status();

        bool approve_topup(long id, int status_id, string reason, long updated_by);

        List<CompanyModel> GetCompanies();

        List<BundlePlanDetailsModel> get_bundleplanDetails();

        List<MasterAccExpriyReportModel> masterAccExpiry();

        List<MasterAccLowBalanceModel> get_MasterAccLowBalanceDetails();

        InvoiceModel getInvoice(InvoiceModel obj_invoice_details, long company_id);

        bool get_staff_topup(string msisdn_no);

        tbl_user getuser(long id);



        //newly added
        List<ccp_AuditModel> get_auditlog(string aces_from, string sdate, string edate);
        void Ccp_Tracking(ccp_AuditModel objtrack);
        tbl_admin_user Check_Admin_UserLogin(admin_login_model objLoginModel, out int failed_his_count);

        bool CreateLoginHistory_Admin_User(string email);
        tbl_company_info Check_Company_Login(company_login_model objLoginModel, out int failed_his_count);
        bool Create_Login_History_Company_Login(string email);
        bool check_mobile_exist(string msisdn, long? id);


        bool get_staff_topup_v1(string msisdn_no, long bundle_id); // newly added 01/09/2022
        void SaveTempStaffTopup(long bundle_id, string bunde_name, string msisdn);
        void TrucateTempStaffTopup();
        List<temp_staff_topup> GetStaffTopupDuplicate();

        #region Autocomplete
        List<CompanyAutoModel> GetCompanyNameAutoComplete(string name);
        #endregion

        #region staff topup validation
        import_staff_model StaffTopupAllAmountVal(import_staff_model objdata, out bool res, out string msg);
        import_staff_model StaffTopupAmountVal(import_staff_model objdata, out bool res);
        import_staff_model StaffTopupBundleIdVal(import_staff_model objReq, out bool res);

        #endregion

    }
}
