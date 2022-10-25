using SI_ccp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SI_ccp.DAL;
using SI_ccp.Utility;
using System.Configuration;
using MvcPaging;
using System.IO;
using System.Xml.Linq;
using System.Text;
using RazorPDF;

namespace SI_ccp.Controllers
{
    public class AdminController : Controller
    {

        #region Repo
        private SI_CCPDBEntities _context;
        private UnitOfWork _uow;
        private IUtilityRepo _util_repo;
        private ICCPRepo _ccp_repo;
        private IAdmin_Repo _admin_repo;
        private int default_page_size = Convert.ToInt32(ConfigurationManager.AppSettings["pgSize"]);
        static long admin_user_id = 0;
        private string Access_from = "Admin";
        private string PP_Trigger;
        public AdminController()
        {
            this._context = new SI_CCPDBEntities();
            this._uow = new UnitOfWork();
            this._util_repo = new UtilityRepo();
            this._ccp_repo = new CCPRepo();
            this._admin_repo = new Admin_Repo();
            this.PP_Trigger = ConfigurationManager.AppSettings["PP_Trigger"];

            if (admin_user_id > 0)
                ViewBag.Menus_val = _admin_repo.get_user_menus(admin_user_id);
        }

        #endregion

        #region Login

        [HttpGet]
        public ActionResult Login()
        {
            try
            {
                string pwd = _util_repo.AES_DEC("ClylN3hyds2FsSpLDQ5ofQ==");
                Session.RemoveAll();

            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }

        [HttpPost]
        public ActionResult Login(admin_login_model obj_login)
        {
            try
            {
                long user_id = 0;
                int role_id = 0;
                Session.RemoveAll();
                int failed_log_count = 0;
                XElement doc = XElement.Load(Server.MapPath("~/EmailTemplate/email_template.xml"));
                XElement AccLocked_Msg = doc.Element("User_AccLocked_Msg");
                XElement InvalidLogin_Msg = doc.Element("User_InvalidLogin_Msg");
                if (ModelState.IsValid)
                {
                    tbl_admin_user obj_user = new tbl_admin_user();
                    if (PP_Trigger.ToLower() == "true")
                    {
                        obj_user = _ccp_repo.Check_Admin_UserLogin(obj_login, out failed_log_count);
                        if (obj_user != null)
                        {
                            Session["admin_id"] = obj_user.id;
                            Session["username"] = obj_user.first_name + " " + obj_user.last_name;
                            Session["role_id"] = obj_user.role_id;
                            Session["mobile_no"] = obj_user.msisdn_number;

                            #region Audit_track
                            user_id = Convert.ToInt64(Session["admin_id"]);
                            role_id = Convert.ToInt32(Session["role_id"]);
                            LogTrackingInfo(user_id, role_id, 8, 1);/*"Login"*/
                            #endregion

                            if (obj_user != null && failed_log_count <= 2)
                            {
                                if (obj_user.role_id == 1)
                                    return RedirectToAction("companies");
                                else
                                {
                                    List<tbl_menu> menus = _admin_repo.get_user_menus(obj_user.id);
                                    admin_user_id = obj_user.id;
                                    if (menus.Count > 0)
                                        return RedirectToAction(menus[0].menu);
                                    else
                                        return RedirectToAction("Login");
                                }
                            }
                            else if (failed_log_count <= 1)
                            {
                                _ccp_repo.CreateLoginHistory_Admin_User(obj_login.username);
                                ViewBag.Msg = InvalidLogin_Msg.Value;
                            }
                            else if (failed_log_count == 2)
                            {
                                _ccp_repo.CreateLoginHistory_Admin_User(obj_login.username);
                                ViewBag.Msg = AccLocked_Msg.Value;
                            }
                            else
                            {
                                ViewBag.Msg = AccLocked_Msg.Value;
                            }

                        }
                        else
                            ViewBag.Msg = InvalidLogin_Msg.Value;
                    }
                    else
                    {
                        obj_user = _admin_repo.check_admin_login(obj_login);

                        if (obj_user != null)
                        {
                            Session["admin_id"] = obj_user.id;
                            Session["username"] = obj_user.first_name + " " + obj_user.last_name;
                            Session["role_id"] = obj_user.role_id;


                            #region Audit_track
                            user_id = Convert.ToInt64(Session["admin_id"]);
                            role_id = Convert.ToInt32(Session["role_id"]);
                            LogTrackingInfo(user_id, role_id, 8, 1);/*"Login"*/
                            #endregion

                            if (obj_user.role_id == 1)
                                return RedirectToAction("companies");
                            else
                            {
                                List<tbl_menu> menus = _admin_repo.get_user_menus(obj_user.id);
                                admin_user_id = obj_user.id;
                                if (menus.Count > 0)
                                    return RedirectToAction(menus[0].menu);
                                else
                                    return RedirectToAction("Login");
                            }

                        }
                        else
                            ViewBag.Msg = "Invalid Username & Password";
                    }

                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View(obj_login);
        }

        [HttpGet]
        public ActionResult Logout()
        {
            try
            {
                #region Audit_track
                long user_id = Convert.ToInt64(Session["admin_id"]);
                int role_id = Convert.ToInt32(Session["role_id"]);
                LogTrackingInfo(user_id, role_id, 9, 2);/*"Logout"*/
                #endregion
                Session.Clear();

            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View("Login");
        }

        #endregion

        #region companies

        public ActionResult companies(int? page, string email, string company_name, string mobile_no, bool? ddlstatus, long? spId)
        {

            IList<CompanyModel> obj_company = new List<CompanyModel>();
            try
            {
                if (Session["admin_id"] == null && Session["role_id"] == null)
                    return RedirectToAction("Login");

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "companies"))
                        return RedirectToAction("Logout");
                    else
                        ViewBag.TopupAccess = _admin_repo.check_menu_access(A_id, R_id, "topup");

                }

                #region Audit_track
                LogTrackingInfo(A_id, R_id, 5, 5);/*"companies"*/
                #endregion
                ViewData["email"] = email;
                ViewData["company_name"] = company_name;
                ViewData["mobile_no"] = mobile_no;
                ViewData["ddlstatus"] = ddlstatus;

                int current_page_index = page.HasValue ? page.Value : 1;

                obj_company = _ccp_repo.GetCompanies();

                if (obj_company.Count > 0)
                {

                    if (!string.IsNullOrEmpty(company_name))
                        obj_company = obj_company.Where(c => c.company.company_name.Trim().ToLower().Contains(company_name.Trim().ToLower())).ToList();

                    if (!string.IsNullOrEmpty(email))
                        obj_company = obj_company.Where(c => c.company.email == email.Trim()).ToList();

                    if (!string.IsNullOrEmpty(mobile_no))
                        obj_company = obj_company.Where(c => c.company.mobile_number == mobile_no.Trim()).ToList();

                    if (ddlstatus != null)
                        obj_company = obj_company.Where(c => c.company.isactive == ddlstatus).ToList();

                    if (spId != null)
                        obj_company = obj_company.Where(c => c.company.sales_person_id == spId).ToList();

                }
                TempData["companies"] = obj_company;

                obj_company = obj_company.ToPagedList(current_page_index, default_page_size);

                ViewBag.SalesPerson = _admin_repo.GetSalesPerson().Where(x => x.isActive == true).ToList();

                if (Request.IsAjaxRequest())
                    return PartialView("_ajax_companies", obj_company);
                else
                    return View(obj_company);

            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View(obj_company);
        }

        #region create company

        [HttpGet]
        public ActionResult create_company()
        {
            try
            {
                if (Session["admin_id"] == null && Session["role_id"] == null)
                    return RedirectToAction("Login");

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);

                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "companies"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track
                LogTrackingInfo(A_id, R_id, 2, 6);/*"create_company"*/
                #endregion
                ViewBag.SalesPerson = _admin_repo.GetSalesPerson().Where(x => x.isActive == true).ToList();
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }

        [HttpPost]
        public ActionResult create_company(tbl_company_info company, bool? send_email)
        {
            try
            {
                if (Session["admin_id"] == null && Session["role_id"] == null)
                    return RedirectToAction("Login");

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);

                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "companies"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track
                LogTrackingInfo(A_id, R_id, 1, 6, company.mobile_number);/*"create_company"*/
                #endregion
                //if (ModelState.IsValid && company != null)
                //{

                ViewBag.SalesPerson = _admin_repo.GetSalesPerson().Where(x => x.isActive == true).ToList();
                var mobile_exist = _ccp_repo.check_mobile_exist(company.mobile_number, company.id);
                if (mobile_exist)
                {


                    company.created_on = DateTime.Now;
                    company.password = _util_repo.AES_ENC(_util_repo.GetUniqueKey(6));
                    company.isdeleted = false;
                    company.credit_amount = 0;

                    company.updated_by = Convert.ToInt64(Session["admin_id"].ToString());

                    bool bRes = _ccp_repo.insert_company(company);

                    if (bRes == true)
                    {
                        //tbl_company_topup_report obj_com = new tbl_company_topup_report();
                        //obj_com.company_id = company.id;
                        ////obj_com.created_on = DateTime.Now;
                        //obj_com.credit_amount = company.credit_amount;
                        //obj_com.credited_on = DateTime.Now;
                        //obj_com.email = company.email;
                        //obj_com.invoice = "";
                        //obj_com.is_recharged = true;
                        //obj_com.trans_desc = "Initial Credit Amount";

                        //bool bcTRes = _ccp_repo.insert_company_topup_trans(obj_com);

                        //if (bcTRes)
                        //{
                        if (send_email == true)
                            send_company_email(company.email, company.company_name, company.password);

                        TempData["successmsg"] = "Company details inserted successfully.";
                        return RedirectToAction("companies");
                        //}
                    }
                }
                else
                {
                    ViewBag.errMsg = "This Mobile number already exist";
                }

                //}

            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View(company);
        }

        #endregion

        #region update company


        [HttpGet]
        public ActionResult update_company(long id)
        {
            try
            {
                if (Session["admin_id"] == null && Session["role_id"] == null)
                    return RedirectToAction("Login");

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);

                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "companies"))
                        return RedirectToAction("Logout");
                }
                ViewBag.SalesPerson = _admin_repo.GetSalesPerson().Where(x => x.isActive == true).ToList();
                tbl_company_info company = new tbl_company_info();

                if (id > 0)
                {
                    company = _ccp_repo.get_company_id(id);

                    if (company != null)
                    {
                        #region Audit_track
                        LogTrackingInfo(A_id, R_id, 2, 7, company.mobile_number);/*"update_company"*/
                        #endregion
                        //company.password = _util_repo.AES_DEC(company.password);
                        return View(company);
                    }
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }

        [HttpPost]
        public ActionResult update_company(tbl_company_info company, bool? send_email)
        {
            try
            {
                if (Session["admin_id"] == null && Session["role_id"] == null)
                    return RedirectToAction("Login");

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);

                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "companies"))
                        return RedirectToAction("Logout");
                }

                //if (ModelState.IsValid)
                //{
                ViewBag.SalesPerson = _admin_repo.GetSalesPerson().Where(x => x.isActive == true).ToList();
                tbl_company_info obj_comp = _ccp_repo.get_company_id(company.id);
                var mobile_exist = _ccp_repo.check_mobile_exist(company.mobile_number, company.id);
                if (mobile_exist)
                {

                    if (obj_comp != null)
                    {
                        #region Audit_track
                        LogTrackingInfo(A_id, R_id, 3, 7, company.mobile_number);/*"update_company"*/
                        #endregion
                        obj_comp.address1 = company.address1;
                        obj_comp.address2 = company.address2;
                        obj_comp.city = company.city;
                        obj_comp.company_name = company.company_name;
                        obj_comp.contact_person_name = company.contact_person_name;
                        obj_comp.email = company.email;
                        obj_comp.isactive = company.isactive;
                        obj_comp.mobile_number = company.mobile_number;
                        obj_comp.phone_number = company.phone_number;
                        obj_comp.min_trans_amount = company.min_trans_amount;
                        obj_comp.max_trans_amount = company.max_trans_amount;
                        obj_comp.is_import_topup = company.is_import_topup;


                        obj_comp.modified_on = DateTime.Now;
                        obj_comp.updated_by = Convert.ToInt64(Session["admin_id"].ToString());
                        obj_comp.sales_person_id = company.sales_person_id;
                        obj_comp.contract_exp_date = company.contract_exp_date;
                        obj_comp.min_mthly_spend = company.min_mthly_spend;

                        bool bRes = _ccp_repo.update_company(obj_comp);

                        if (bRes == true)
                        {
                            if (send_email == true)
                                send_company_email(obj_comp.email, obj_comp.company_name, obj_comp.password);

                            TempData["successmsg"] = "Company details Updated Successfully.";
                            return RedirectToAction("companies");
                        }
                    }
                }
                else
                {
                    ViewBag.errMsg = "This Mobile number already exist";
                }


                //}
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }

        #endregion

        #region delete company

        public JsonResult delete_company(long id)
        {
            string Res = "false";
            try
            {
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);


                //if (Session["admin_id"] != null && Session["role_id"] != null && Session["role_id"].ToString() == "1")
                //{

                tbl_company_info company = new tbl_company_info();

                company = _ccp_repo.get_company_id(id);

                if (company != null)
                {
                    #region Audit_track
                    LogTrackingInfo(A_id, R_id, 4, 8, company.mobile_number);/*"delete_company"*/
                    #endregion
                    company.isdeleted = true;
                    company.updated_by = Convert.ToInt64(Session["admin_id"].ToString());

                    bool bRes = _ccp_repo.update_company(company);

                    if (bRes)
                        Res = "true";
                }
                // }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return Json(new { Status = Res });
        }

        #endregion

        #region Check company mail exist

        public JsonResult check_email_exist(string email, int? id)
        {

            bool bRet = false;

            bRet = _ccp_repo.check_email_exist(email, id);

            return Json(bRet);
        }

        #endregion

        #region send_company_email(Method)

        private void send_company_email(string email, string company_name, string pwd)
        {

            long A_id = Convert.ToInt64(Session["admin_id"]);
            int R_id = Convert.ToInt32(Session["role_id"]);
            #region Audit_track
            LogTrackingInfo(A_id, R_id, 10, 10);/*"send_company_email"*/
            #endregion
            string password = _util_repo.AES_DEC(pwd);
            XElement doc = XElement.Load(Server.MapPath("~/EmailTemplate/email_template.xml"));
            XElement emailsubj = doc.Element("SendCompanyEmail_Subj");
            XElement emailBody = doc.Element("SendCompanyEmail_Body");
            string sData = emailBody.Value;
            sData = sData.Replace("#CompanyName#", company_name).Replace("#Email#", email).Replace("#Password#", password);

            string hostURL = Request.Url.AbsoluteUri.Replace(Request.Url.AbsolutePath, "");
            sData = sData.Replace("#LoginURL#", hostURL + "/Company/Login");

            _util_repo.SendEmailMessage(email, emailsubj.Value.Trim(), sData);
        }

        #endregion

        #region Export csv Companies

        public void companies_csv_export()
        {
            try
            {
                if (TempData["companies"] != null && Session["admin_id"] != null)
                {
                    List<CompanyModel> compaies_reports = new List<CompanyModel>();

                    compaies_reports = (List<CompanyModel>)TempData["companies"];
                    long A_id = Convert.ToInt64(Session["admin_id"]);
                    int R_id = Convert.ToInt32(Session["role_id"]);
                    #region Audit_track
                    LogTrackingInfo(A_id, R_id, 6, 11);/*"companies_csv_export"*/
                    #endregion
                    if (compaies_reports != null && compaies_reports.Count > 0)
                    {
                        TempData["companies"] = compaies_reports;

                        string filename = "CompaniesReport" + DateTime.Now.Date.ToShortDateString();

                        Response.ClearContent();
                        Response.AddHeader("content-disposition", "attachment;filename=" + filename + ".csv");
                        Response.ContentType = "text/csv";

                        //string sw = string.Empty;
                        //sw = _uow.cug_FinanceCSV_repo.ToCsv<Finance_Model>(",", FinanceReport);

                        StringWriter sw = new StringWriter();
                        sw.WriteLine("\"Company Name\",\"Contact Name\",\"Credit Amount(K)\",\"Email\",\"Mobile Number\",\"City\",\"Created On\",\"Status\",\"Sales Person\",\"Contract Expiry Date\"");

                        foreach (var line in compaies_reports)
                        {
                            //string recharge_status = line.is_recharged == true ? "Success" : "Failed";

                            sw.WriteLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\"",
                                                       //line.comapny_id,
                                                       line.company.company_name,
                                                       line.company.contact_person_name,
                                                       line.company.credit_amount,
                                                       line.company.email,
                                                       line.company.mobile_number,
                                                       line.company.city,
                                                       line.company.created_on.ToString("dd-MM-yyyy"),
                                                       line.company.isactive == true ? "Active" : "Inactive",
                                                       line.sales_person,
                                                       line.company.contract_exp_date.Value.ToString("dd-MM-yyyy")
                                                       ));
                        }

                        Response.Write(sw.ToString());
                        Response.End();
                    }
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
        }

        #endregion


        #endregion

        #region company topup

        public ActionResult topup(long id)
        {
            try
            {
                if (Session["admin_id"] == null && Session["role_id"] == null)
                    return RedirectToAction("Login");

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "topup"))
                        return RedirectToAction("Logout");
                }


                topup_detail_model obj = new topup_detail_model();
                obj.reports = new List<topup_report_model>();

                tbl_company_info comp_details = new tbl_company_info();

                comp_details = _ccp_repo.get_activecompanyby_id(id);

                if (comp_details != null)
                {
                    #region Audit_track
                    LogTrackingInfo(A_id, R_id, 2, 12, comp_details.mobile_number);/*"topup"*/
                    #endregion
                    obj.email = comp_details.email;
                    obj.company_id = comp_details.id;
                    obj.avail_credit_amount = comp_details.credit_amount;
                    obj.company_name = comp_details.company_name;
                    obj.contact_person_name = comp_details.contact_person_name;
                    obj.mobile_number = comp_details.mobile_number;

                    //obj.reports = _ccp_repo.company_topup_report(obj.company_id);
                    obj.reports = _ccp_repo.get_temp_topup_by_comp(obj.company_id);  //_ccp_repo.get_topup_reportsby_comp(obj.company_id);
                    obj.pay_types = _ccp_repo.get_paytype_list();

                    return View(obj);
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }

        [HttpPost]
        public ActionResult topup(topup_detail_model topup_details)
        {
            try
            {
                if (Session["admin_id"] == null && Session["role_id"] == null)
                    return RedirectToAction("Login");

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "topup"))
                        return RedirectToAction("Logout");
                }

                if (topup_details != null)
                {
                    if (topup_details.topup_amount > 0)
                    {
                        if (topup_details.reports == null)
                            topup_details.reports = new List<topup_report_model>();

                        if (topup_details.pay_types == null)
                            topup_details.pay_types = new List<tbl_payment_type>();

                        if (!string.IsNullOrEmpty(topup_details.account_order_no) && topup_details.payment_type_id == 6)
                        {
                            ModelState.Remove("internal_ref_no");
                            ModelState.Remove("description");
                        }
                        else if (!string.IsNullOrEmpty(topup_details.internal_ref_no) && topup_details.payment_type_id == 7)
                        {
                            ModelState.Remove("account_order_no");
                            ModelState.Remove("description");
                        }
                        else if (topup_details.payment_type_id > 0)
                        {
                            ModelState.Remove("account_order_no");
                            ModelState.Remove("internal_ref_no");
                        }


                        if (ModelState.IsValid)
                        {
                            tbl_company_info comp_details = new tbl_company_info();
                            comp_details = _ccp_repo.get_activecompanyby_id(topup_details.company_id);
                            #region Audit_track
                            LogTrackingInfo(A_id, R_id, 1, 12, comp_details.mobile_number);/*"topup"*/
                            #endregion
                            if (comp_details != null)
                            {
                                //comp_details.credit_amount = comp_details.credit_amount + topup_details.topup_amount;

                                //bool bcRes = _ccp_repo.update_company(comp_details);

                                //if (bcRes)
                                //{
                                //tbl_company_topup_report obj_com = new tbl_company_topup_report();
                                //obj_com.company_id = comp_details.id;
                                ////obj_com.created_on = DateTime.Now;
                                //obj_com.credit_amount = topup_details.topup_amount;
                                //obj_com.credited_on = DateTime.Now;
                                //obj_com.email = comp_details.email;
                                //obj_com.invoice = topup_details.invoice;
                                //obj_com.is_recharged = true;
                                //obj_com.trans_desc = topup_details.description;
                                //obj_com.internal_ref_no = topup_details.internal_ref_no;
                                //obj_com.account_order_no = topup_details.account_order_no;
                                //obj_com.payment_type_id = topup_details.payment_type_id;


                                //bool bRes = _ccp_repo.insert_company_topup_trans(obj_com);

                                //topup_details.avail_credit_amount = comp_details.credit_amount;
                                //topup_details.invoice = "";
                                //topup_details.description = "";

                                //ViewBag.SuccessMsg = "Amount K " + topup_details.topup_amount.ToString("F") + " has added successfully";

                                tbl_company_topup_temp obj_com = new tbl_company_topup_temp();
                                obj_com.company_id = comp_details.id;
                                obj_com.credit_amount = topup_details.topup_amount;
                                obj_com.credited_on = DateTime.Now;
                                obj_com.email = comp_details.email;
                                obj_com.invoice = topup_details.invoice;
                                obj_com.is_recharged = true;
                                obj_com.trans_desc = topup_details.description;
                                obj_com.internal_ref_no = topup_details.internal_ref_no;
                                obj_com.account_order_no = topup_details.account_order_no;
                                obj_com.payment_type_id = topup_details.payment_type_id;
                                obj_com.approval_status_id = 1;
                                obj_com.sales_person_id = _uow.company_info_repo.Get(filter: x => x.id == comp_details.id).Select(x => x.sales_person_id).FirstOrDefault();
                                obj_com.updated_by = A_id;

                                bool bRes = _ccp_repo.insert_company_topup_temp(obj_com);
                                if (bRes)
                                    ViewBag.SuccessMsg = "Amount $ " + topup_details.topup_amount.ToString("F") + " has added in queue";
                                //}
                            }
                            //List<tbl_company_topup_report> topup_report = _ccp_repo.company_topup_report(topup_details.company_id);
                            //topup_details.reports = _ccp_repo.get_topup_reportsby_comp(topup_details.company_id);
                            //topup_details.pay_types = _ccp_repo.get_paytype_list();

                            //return View(topup_details);
                        }
                    }
                    else
                        ViewBag.Msg = "Invalid Amount!";

                    //List<tbl_company_topup_report> topup_report = _ccp_repo.company_topup_report(topup_details.company_id);
                    topup_details.reports = _ccp_repo.get_temp_topup_by_comp(topup_details.company_id);  //_ccp_repo.get_topup_reportsby_comp(topup_details.company_id); ;
                    topup_details.pay_types = _ccp_repo.get_paytype_list();

                    return View(topup_details);
                }
                else
                    return RedirectToAction("topup_transactions");
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }

        #endregion

        #region topup Transactions

        public ActionResult topup_transactions(int? page, string sTransFrom, string sTransTo, string sName, string sMsisdn, string sInvoice, string sEmail, int? ddlPayTypeId, long? ddlSalesPerson)
        {
            try
            {
                if (Session["admin_id"] == null || Session["role_id"] == null)
                    return RedirectToAction("Login");

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "topup_transactions"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track
                LogTrackingInfo(A_id, R_id, 5, 13);/*"topup_transactions"*/
                #endregion
                int currentPageIndex = page.HasValue ? page.Value : 1;
                sTransFrom = string.IsNullOrEmpty(sTransFrom) ? DateTime.Now.AddMonths(-1).Date.ToString("dd/MM/yyyy") : sTransFrom;
                sTransTo = string.IsNullOrEmpty(sTransTo) ? DateTime.Now.Date.ToString("dd/MM/yyyy") : sTransTo;

                DateTime sdt = string.IsNullOrEmpty(sTransFrom) ? Convert.ToDateTime(sTransFrom) : DateTime.ParseExact(sTransFrom, @"dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                DateTime edt = string.IsNullOrEmpty(sTransTo) ? Convert.ToDateTime(sTransTo) : DateTime.ParseExact(sTransTo, @"dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);



                ViewData["sTransFrom"] = sTransFrom;
                ViewData["sTransTo"] = sTransTo;
                ViewData["sName"] = sName;
                ViewData["sMsisdn"] = sMsisdn;
                ViewData["sInvoice"] = sInvoice;
                ViewData["sEmail"] = sEmail;
                ViewData["ddlPayTypeId"] = ddlPayTypeId;
                ViewData["ddlSalesPerson"] = ddlSalesPerson;

                ViewBag.PayTypeList = _ccp_repo.get_paytype_list();

                ViewBag.SalesPerson = _admin_repo.GetSalesPerson().Where(x => x.isActive == true).ToList();

                IList<topup_report_model> company_trans = new List<topup_report_model>();

                company_trans = _ccp_repo.get_topup_reports(sdt, edt);
                if (company_trans.Count() > 0)
                {

                    if (!string.IsNullOrEmpty(sName))
                        company_trans = company_trans.Where(S => S.company_name.Trim().ToLower().Contains(sName.Trim().ToLower())).ToList();

                    if (!string.IsNullOrEmpty(sMsisdn))
                        company_trans = company_trans.Where(S => S.mobile_number == sMsisdn.Trim()).ToList();

                    if (!string.IsNullOrEmpty(sInvoice))
                        company_trans = company_trans.Where(S => S.invoice == sInvoice.Trim()).ToList();

                    if (!string.IsNullOrEmpty(sEmail))
                        company_trans = company_trans.Where(S => S.email == sEmail.Trim()).ToList();

                    if (ddlPayTypeId > 0)
                        company_trans = company_trans.Where(S => S.payment_type_id == ddlPayTypeId).ToList();

                    //if (!string.IsNullOrEmpty(sTransFrom))
                    //{
                    //    DateTime from_date = Convert.ToDateTime(sTransFrom);
                    //    company_trans = company_trans.Where(S => S.credited_on.Date >= from_date.Date).ToList();
                    //}

                    //if (!string.IsNullOrEmpty(sTransTo))
                    //{
                    //    DateTime to_date = Convert.ToDateTime(sTransTo);
                    //    company_trans = company_trans.Where(S => S.credited_on.Date <= to_date.Date).ToList();
                    //}

                    if (ddlSalesPerson != null && ddlSalesPerson > 0)
                        company_trans = company_trans.Where(c => c.sales_person_id == ddlSalesPerson).ToList();
                }

                if (company_trans.Count > 0)
                    TempData["topup_transactions"] = company_trans;

                company_trans = company_trans.ToPagedList(currentPageIndex, default_page_size);

                if (Request.IsAjaxRequest())
                    return PartialView("_ajax_topup_transactions", company_trans);
                else
                    return View(company_trans);
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return View();
        }

        #endregion

        #region approve_topup

        public ActionResult approve_topup(int? page, string sTransFrom, string sTransTo, string sName, int? Status)
        {
            try
            {
                if (Session["admin_id"] == null || Session["role_id"] == null)
                    return RedirectToAction("Login");

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "approve_topup"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track

                LogTrackingInfo(A_id, R_id, 5, 22);/*"approve_topup"*/
                #endregion
                int currentPageIndex = page.HasValue ? page.Value : 1;
                sTransFrom = string.IsNullOrEmpty(sTransFrom) ? DateTime.Now.AddMonths(-1).Date.ToString("dd/MM/yyyy") : sTransFrom;
                sTransTo = string.IsNullOrEmpty(sTransTo) ? DateTime.Now.Date.ToString("dd/MM/yyyy") : sTransTo;

                DateTime sdt = string.IsNullOrEmpty(sTransFrom) ? Convert.ToDateTime(sTransFrom) : DateTime.ParseExact(sTransFrom, @"dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                DateTime edt = string.IsNullOrEmpty(sTransTo) ? Convert.ToDateTime(sTransTo) : DateTime.ParseExact(sTransTo, @"dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);

                ViewData["sTransFrom"] = sTransFrom;
                ViewData["sTransTo"] = sTransTo;
                ViewData["sName"] = sName;
                ViewData["Status"] = Status;

                ViewBag.ApproveStatusList = _ccp_repo.GetApprove_Status();

                IList<topup_report_model> company_trans = new List<topup_report_model>();

                company_trans = _ccp_repo.get_temp_topup(sdt, edt);
                if (company_trans.Count() > 0)
                {
                    if (!string.IsNullOrEmpty(sName))
                        company_trans = company_trans.Where(S => S.company_name.Trim().ToLower().Contains(sName.Trim().ToLower())).ToList();

                    if (Status > 0)
                        company_trans = company_trans.Where(S => S.approval_status_id == Status).ToList();

                    //if (!string.IsNullOrEmpty(sTransFrom))
                    //{
                    //    DateTime from_date = Convert.ToDateTime(sTransFrom);
                    //    company_trans = company_trans.Where(S => S.credited_on.Date >= from_date.Date).ToList();
                    //}

                    //if (!string.IsNullOrEmpty(sTransTo))
                    //{
                    //    DateTime to_date = Convert.ToDateTime(sTransTo);
                    //    company_trans = company_trans.Where(S => S.credited_on.Date <= to_date.Date).ToList();
                    //}
                }

                //if (company_trans.Count > 0)
                //    TempData["temp_topup_transactions"] = company_trans;

                company_trans = company_trans.ToPagedList(currentPageIndex, default_page_size);

                if (Request.IsAjaxRequest())
                    return PartialView("_ajax_approve_topup", company_trans);
                else
                    return View(company_trans);
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return View();
        }

        public ActionResult aj_approve_topup(long id, int status, string reason)
        {
            bool bret = false;
            #region Audit_track
            long A_id = Convert.ToInt64(Session["admin_id"]);
            int R_id = Convert.ToInt32(Session["role_id"]);
            #endregion
            if (Session["admin_id"] != null && Session["role_id"] != null)
            {
                long admin_id = long.Parse(Session["admin_id"].ToString());
                int role_id = int.Parse(Session["role_id"].ToString());

                if (role_id == 1 || _admin_repo.check_menu_access(admin_id, role_id, "approve_topup"))
                {
                    #region Audit_track

                    //newcode
                    var objTempTopup = _uow.company_topup_temp_repo.GetByID(id);
                    tbl_company_info objCompany = _uow.company_info_repo.GetByID(objTempTopup.company_id);
                    if (objCompany != null)
                    {
                        LogTrackingInfo(A_id, R_id, 3, 22, objCompany.mobile_number);/*"approve_topup"*/
                    }
                    #endregion

                    bret = _ccp_repo.approve_topup(id, status, reason, admin_id);
                }
            }

            return Json(bret, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region export csv topup transactions

        public void export_csv_companies()
        {
            try
            {
                if (TempData["topup_transactions"] != null && Session["admin_id"] != null)
                {
                    List<topup_report_model> topup_reports = new List<topup_report_model>();

                    topup_reports = (List<topup_report_model>)TempData["topup_transactions"];
                    #region Audit_track
                    long A_id = Convert.ToInt64(Session["admin_id"]);
                    int R_id = Convert.ToInt32(Session["role_id"]);
                    LogTrackingInfo(A_id, R_id, 6, 14);/*"topup_transactions_csv"*/
                    #endregion
                    if (topup_reports != null && topup_reports.Count > 0)
                    {
                        TempData["topup_transactions"] = topup_reports;

                        string filename = "topup_transactions" + DateTime.Now.Date.ToShortDateString();

                        Response.ClearContent();
                        Response.AddHeader("content-disposition", "attachment;filename=" + filename + ".csv");
                        Response.ContentType = "text/csv";

                        //string sw = string.Empty;
                        //sw = _uow.cug_FinanceCSV_repo.ToCsv<Finance_Model>(",", FinanceReport);

                        StringWriter sw = new StringWriter();
                        sw.WriteLine("\"Company Name\",\"Credit Amount\",\"Payment Type\",\"Mobile No\",\"Email\",\"Date/Time\",\"Invoice No\",\"Account Order No\",\"Internal Ref No\",\"Description\",\"Recharge Status\",\"Sales Person\"");

                        foreach (var line in topup_reports)
                        {
                            string recharge_status = line.is_recharged == true ? "Success" : "Failed";

                            sw.WriteLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"{11}\"",
                                                       //line.comapny_id,
                                                       line.company_name,
                                                       line.credit_amount,
                                                       line.payment_type_name,
                                                       line.mobile_number,
                                                       line.email,
                                                       line.credited_on,
                                                       line.invoice,
                                                       line.account_order_no,
                                                       line.internal_ref_no,
                                                       line.trans_desc,
                                                       recharge_status,
                                                       line.sales_person
                                                       ));
                        }

                        Response.Write(sw.ToString());
                        Response.End();
                    }
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
        }


        #endregion

        #region users

        public ActionResult users(string user_name, string email, int? roles, int? page)
        {

            IList<userlist_model> _obj = new List<userlist_model>();
            try
            {
                if (Session["admin_id"] == null && Session["role_id"] == null)
                    return RedirectToAction("Login");

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "users"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track
                LogTrackingInfo(A_id, R_id, 5, 15);/*"users"*/
                #endregion
                ViewData["user_name"] = user_name;
                ViewData["email"] = email;
                ViewData["roles"] = roles;

                int currentPageIndex = page.HasValue ? page.Value : 1;

                ViewBag.roles = _admin_repo.get_admin_user_roles();

                _obj = _admin_repo.get_admin_users();

                if (!string.IsNullOrWhiteSpace(user_name))
                    _obj = _obj.Where(c => c.username.ToLower().Contains(user_name.ToLower())).ToList();

                if (!string.IsNullOrWhiteSpace(email))
                    _obj = _obj.Where(c => c.emailid == email).ToList();

                if (roles != null && roles > 0)
                    _obj = _obj.Where(c => c.role_id == roles).ToList();

                _obj = _obj.ToPagedList(currentPageIndex, default_page_size);

                if (Request.IsAjaxRequest())
                    return PartialView("_ajax_users", _obj);
                else
                    return View(_obj);

            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }

        [HttpGet]
        public ActionResult user_details(long? id)
        {

            user_model _obj = new user_model();
            try
            {
                if (Session["admin_id"] == null && Session["role_id"] == null)
                    return RedirectToAction("Login");

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);

                if (R_id != 1)
                    return RedirectToAction("Logout");


                long C_id = id.HasValue ? id.Value : 0;
                string number = "";
                ViewBag.roles = _admin_repo.get_admin_user_roles();

                if (C_id > 0)
                {
                    _obj = _admin_repo.userby_id(C_id);

                    number = _obj.mobile_number.ToString();
                    _obj.UserAccessList = _admin_repo.get_user_access_list(C_id);
                    #region Audit_track
                    LogTrackingInfo(A_id, R_id, 2, 4, number);/*"user_details"*/
                    #endregion
                    _obj.UserAccessList = _admin_repo.get_user_access_list(C_id);
                }
                else
                    _obj.UserAccessList = _admin_repo.get_user_access_list(null);

                return View(_obj);
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View(_obj);
        }

        [HttpPost]
        public ActionResult user_details(user_model _obj)
        {
            try
            {
                if (Session["admin_id"] == null && Session["role_id"] == null)
                    return RedirectToAction("Login");

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                int action = 3;
                if (R_id != 1)
                    return RedirectToAction("Logout");

                ViewBag.roles = _admin_repo.get_admin_user_roles();

                if (ModelState.IsValid)
                {
                    tbl_admin_user obj_user = new tbl_admin_user();
                    obj_user.id = _obj.Id;
                    obj_user.role_id = _obj.role_id;
                    obj_user.first_name = _obj.first_name;
                    obj_user.last_name = _obj.last_name;
                    obj_user.email = _obj.email;
                    obj_user.msisdn_number = _obj.mobile_number;
                    obj_user.isactive = _obj.is_active;

                    bool bRes = false;
                    if (obj_user.id == 0)
                    {
                        action = 1;
                    }
                    if (obj_user.id == 0)
                    {
                        string rad_str = _util_repo.GetUniqueKey(6);
                        obj_user.password = _util_repo.AES_ENC(rad_str);
                        obj_user.created_on = DateTime.Now;

                        bRes = _admin_repo.insert_admin_user(obj_user);

                        if (bRes)
                            TempData["successmsg"] = "The user has been created successfully";
                    }
                    else
                    {
                        bRes = _admin_repo.update_admin_user(obj_user);

                        if (bRes)
                            TempData["successmsg"] = "The user has been updated successfully";

                        // Delete existing user_access_levels
                        _admin_repo.delete_user_accessby_userid(obj_user.id);
                    }
                    #region Audit_track
                    LogTrackingInfo(A_id, R_id, action, 16, _obj.mobile_number);/*"user_details"*/
                    #endregion
                    if (bRes)
                    {
                        // insert user_access
                        foreach (var useracc in _obj.UserAccessList)
                        {
                            if (useracc.selected)
                            {
                                tbl_user_access objUserAccess = new tbl_user_access();
                                objUserAccess.access_id = Convert.ToInt32(useracc.access_id);
                                objUserAccess.user_id = obj_user.id;

                                _admin_repo.insert_user_access(objUserAccess);
                            }
                        }
                    }

                    if (_obj.send_login == true)
                        EmailUser(obj_user.id);

                    return RedirectToAction("users", "Admin");
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View(_obj);
        }


        [HttpPost]
        public ActionResult delete_user(long id)
        {
            bool bRes = false;
            try
            {
                //if (Session["admin_id"] != null && Session["role_id"] != null && Session["role_id"].ToString() == "1")
                //{
                #region Audit_track
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);

                #endregion
                if (id > 0)
                {
                    tbl_admin_user _obj = _uow.admin_user_repo.GetByID(id);
                    LogTrackingInfo(A_id, R_id, 4, 17, _obj.msisdn_number);/*"delete_user"*/
                    bRes = _admin_repo.delete_admin_user(id);
                }

                return Json(new { Status = "true" });
                //}
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return null;
        }

        public JsonResult check_user_exist(tbl_admin_user obj_user)
        {
            bool bRet = false;
            if (Session["admin_id"] != null && Session["role_id"] != null)
            {

                bRet = _admin_repo.check_admin_user_email_exist(obj_user.email, obj_user.id);

            }
            return Json(bRet);
        }

        #region EmailUser(Method)

        public void EmailUser(long user_id)
        {
            tbl_admin_user obj_user = new tbl_admin_user();

            obj_user = _admin_repo.get_admin_userdetails(user_id);

            if (obj_user != null)
            {
                #region Audit_track
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                LogTrackingInfo(A_id, R_id, 10, 18);/*"EmailUser"*/
                #endregion
                obj_user.password = _util_repo.AES_DEC(obj_user.password);

                XElement doc = XElement.Load(Server.MapPath("~/EmailTemplate/email_template.xml"));
                XElement emailsubj = doc.Element("SendAdminUserEmail_Subj");
                XElement emailBody = doc.Element("SendAdminUserEmail_Body");
                string sData = emailBody.Value;
                sData = sData.Replace("#Email#", obj_user.email).Replace("#Password#", obj_user.password).Replace("#Name#", obj_user.first_name + " " + obj_user.last_name);

                _util_repo.SendEmailMessage(obj_user.email, emailsubj.Value.Trim(), sData);
            }
        }

        #endregion


        #endregion

        #region staff_topup_transactions

        //public ActionResult staff_topup_transactions(int? page, long? company_id, bool? rec_status, string sdate, string edate, string mobile_number, long? sales_person_id)
        //{
        //    staff_msisdn_admin_model objStaff_Msisdn = new staff_msisdn_admin_model();
        //    try
        //    {
        //        if (Session["admin_id"] == null || Session["role_id"] == null)
        //            return RedirectToAction("Login");

        //        long A_id = Convert.ToInt64(Session["admin_id"]);
        //        int R_id = Convert.ToInt32(Session["role_id"]);
        //        if (R_id != 1)
        //        {
        //            if (!_admin_repo.check_menu_access(A_id, R_id, "staff_topup_transactions"))
        //                return RedirectToAction("Logout");
        //        }

        //        #region Audit_track
        //        LogTrackingInfo(A_id, R_id, 5, 19);/*"staff_topup_transactions"*/
        //        #endregion
        //        objStaff_Msisdn.company_list = _ccp_repo.get_companies();

        //        int currentPageIndex = page.HasValue ? page.Value : 1;
        //        ViewData["company_id"] = company_id;
        //        ViewData["rec_status"] = rec_status;
        //        ViewData["sdate"] = sdate;
        //        ViewData["edate"] = edate;
        //        ViewData["mobile_number"] = mobile_number;
        //        ViewData["sales_person_id"] = sales_person_id;

        //        IList<StaffsTopupModel> obj_staff = new List<StaffsTopupModel>();

        //        if (company_id != null)
        //            obj_staff = _ccp_repo.get_staff_topup((long)company_id);
        //        else
        //            obj_staff = _ccp_repo.get_staff_topup();

        //        if (obj_staff.Count() > 0)
        //        {
        //            if (rec_status != null)
        //            {
        //                bool r_status = Convert.ToBoolean(rec_status);
        //                obj_staff = obj_staff.Where(x => x.staff_topup.is_recharged == r_status).ToList();
        //            }

        //            if (!string.IsNullOrEmpty(sdate) && !string.IsNullOrEmpty(edate))
        //            {
        //                DateTime sdt = Convert.ToDateTime(sdate).Date;
        //                DateTime edt = Convert.ToDateTime(edate).AddDays(1).Date;

        //                obj_staff = obj_staff.Where(x => x.staff_topup.trans_date >= sdt && x.staff_topup.trans_date < edt).ToList();
        //            }

        //            if (!string.IsNullOrEmpty(mobile_number))
        //                obj_staff = obj_staff.Where(x => x.staff_topup.msisdn_number == mobile_number).ToList();

        //            if (sales_person_id != null)
        //                obj_staff = obj_staff.Where(x => x.staff_topup.sales_person_id == sales_person_id).ToList();
        //        }

        //        TempData["staff_topup_report"] = obj_staff;

        //        objStaff_Msisdn.staffs_topup_list = obj_staff.ToPagedList(currentPageIndex, default_page_size);

        //        if (Request.IsAjaxRequest())
        //            return PartialView("_ajax_staff_topup", objStaff_Msisdn.staffs_topup_list);

        //        ViewBag.SalesPerson = _admin_repo.GetSalesPerson().Where(x => x.isActive == true).ToList();
        //    }
        //    catch (Exception ex)
        //    {
        //        _util_repo.ErrorLog_Txt(ex);
        //    }

        //    return View(objStaff_Msisdn);
        //}
        public ActionResult staff_topup_transactions(int? page, long? company_id, bool? rec_status, string sdate, string edate, string mobile_number, long? sales_person_id)
        {
            staff_msisdn_admin_model objStaff_Msisdn = new staff_msisdn_admin_model();
            try
            {
                if (Session["admin_id"] == null || Session["role_id"] == null)
                    return RedirectToAction("Login");

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "staff_topup_transactions"))
                        return RedirectToAction("Logout");
                }

                #region Audit_track
                LogTrackingInfo(A_id, R_id, 5, 19);/*"staff_topup_transactions"*/
                #endregion
                objStaff_Msisdn.company_list = _ccp_repo.get_companies();
                ViewBag.SalesPerson = _admin_repo.GetSalesPerson().Where(x => x.isActive == true).ToList();


                int currentPageIndex = page.HasValue ? page.Value : 1;

                sdate = string.IsNullOrEmpty(sdate) ? DateTime.Now.Date.ToString("dd/MM/yyyy") : sdate;
                edate = string.IsNullOrEmpty(edate) ? DateTime.Now.Date.ToString("dd/MM/yyyy") : edate;

                ViewData["company_id"] = company_id;
                ViewData["rec_status"] = rec_status;
                ViewData["sdate"] = sdate;
                ViewData["edate"] = edate;
                ViewData["mobile_number"] = mobile_number;
                ViewData["sales_person_id"] = sales_person_id;

                IList<StaffTopupNewModel> obj_staff = new List<StaffTopupNewModel>();

                if (company_id != null)
                    obj_staff = _ccp_repo.get_staff_topupby_company(sdate, edate, (long)company_id);
                else
                    obj_staff = _ccp_repo.get_staff_topupby_company(sdate, edate);

                if (obj_staff.Count() > 0)
                {
                    if (rec_status != null)
                    {
                        bool r_status = Convert.ToBoolean(rec_status);
                        obj_staff = obj_staff.Where(x => x.is_recharged == r_status).ToList();
                    }

                    //if (!string.IsNullOrEmpty(sdate) && !string.IsNullOrEmpty(edate))
                    //{
                    //    DateTime sdt = DateTime.ParseExact(sdate, "dd/MM/yyyy", null);
                    //    DateTime edt = DateTime.ParseExact(edate, "dd/MM/yyyy", null);

                    //    edt = edt.AddDays(1);

                    //    obj_staff = obj_staff.Where(x => x.trans_date >= sdt && x.trans_date < edt).ToList();
                    //}
                    //else
                    //{
                    //    obj_staff = obj_staff.Where(x => x.trans_date is null).ToList();
                    //}


                    if (!string.IsNullOrEmpty(mobile_number))
                        obj_staff = obj_staff.Where(x => x.msisdn_number == mobile_number).ToList();

                    if (sales_person_id != null)
                        obj_staff = obj_staff.Where(x => x.sales_person_id == sales_person_id).ToList();
                }

                TempData["staff_topup_report"] = obj_staff;

                objStaff_Msisdn.staffs_topup_list = obj_staff.ToPagedList(currentPageIndex, default_page_size);

                if (Request.IsAjaxRequest())
                    return PartialView("_ajax_staff_topup", objStaff_Msisdn.staffs_topup_list);

              
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return View(objStaff_Msisdn);
        }


        public ActionResult staff_topup_transactions_csv()
        {
            try
            {
                if (Session["admin_id"] != null && Session["role_id"] != null && TempData["staff_topup_report"] != null)
                {
                    List<StaffTopupNewModel> obj_staff = (List<StaffTopupNewModel>)TempData["staff_topup_report"];
                    TempData["staff_topup_report"] = obj_staff;
                    if (obj_staff != null)
                    {
                        #region Audit_track
                        long A_id = Convert.ToInt64(Session["admin_id"]);
                        int R_id = Convert.ToInt32(Session["role_id"]);
                        LogTrackingInfo(A_id, R_id, 6, 20);/*"staff_topup_transactions_csv"*/
                        #endregion
                        Response.ClearContent();
                        Response.AddHeader("content-disposition", "attachment;filename=Staff_Topup_Transactions.csv");
                        Response.ContentType = "text/csv";

                        StringWriter sw = new StringWriter();

                        sw.WriteLine("\"Company Name\",\"Staff Name\",\"Mobile Number\",\"Topup Amount($)\",\"Bundle Details\",\"Balance Amount($)\",\"Invoice\",\"Recharge Status\",\"Active Status\",\"Transaction Description\",\"Transaction Date\",\"Sales Person\"");

                        foreach (var line in obj_staff)
                        {
                            decimal tot_amt = 0;
                            StringBuilder sb = new StringBuilder();
                            //if (line != null)
                            //{
                            //    foreach (var item in line.staff_topup_bundle)
                            //    {
                            //        tot_amt += item.bundle_amt;
                            //        sb.Append(item.bundle_name + ": $ " + item.bundle_amt + "  ");
                            //    }
                            //}
                            tot_amt += Convert.ToDecimal(line.bundle_amt);
                            sb.Append(line.bundle_name + ": $ " + line.bundle_amt + "  ");

                            sw.WriteLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"{11}\"",
                                                       line.company_name,
                                                       line.first_name + " " + line.last_name,
                                                       line.msisdn_number,
                                                       line.topup_amount,
                                                       //sb.ToString(),
                                                       string.IsNullOrEmpty(line.bundle_name)?"NA" : line.bundle_name ,
                                                       line.topup_amount - line.balance,
                                                       line.invoice_number,
                                                       line.is_recharged == true ? "Success" : "Pending",
                                                       line.isactive == true ? "Active" : "Inactive",
                                                       line.trans_desc,
                                                       line.trans_date,
                                                       line.sales_person));
                        }

                        Response.Write(sw.ToString());

                        Response.End();
                    }

                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return Content("Access denied");
        }
        #endregion

        #region sales person

        #region sales person details

        public ActionResult SalesPersonDetails(int? page, string Name, string Number, bool? Status)
        {
            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");

            long A_id = Convert.ToInt64(Session["admin_id"]);
            int R_id = Convert.ToInt32(Session["role_id"]);
            if (R_id != 1)
            {
                if (!_admin_repo.check_menu_access(A_id, R_id, "SalesPersonDetails"))
                    return RedirectToAction("Logout");

                if (!_admin_repo.check_menu_access(A_id, R_id, "CreateSalesPerson"))
                    ViewBag.CreateSalesPersonAccess = true;
            }
            #region Audit_track
            LogTrackingInfo(A_id, R_id, 5, 25);/*"EmailtoMaster"*/
            #endregion
            if (R_id == 1)
                ViewBag.CreateSalesPersonAccess = true;

            IList<SalesPersonModel> objSalesList = new List<SalesPersonModel>();
            try
            {
                ViewData["Status"] = Status;
                ViewData["Name"] = Name;
                ViewData["Number"] = Number;

                objSalesList = _admin_repo.GetSalesPerson();

                int currentPageIndex = page.HasValue ? page.Value : 1;

                if (objSalesList != null)
                {
                    if (!string.IsNullOrEmpty(Name))
                        objSalesList = objSalesList.Where(k => k.fullName.ToLower().Contains(Name.ToLower())).ToList();
                    if (!string.IsNullOrEmpty(Number))
                        objSalesList = objSalesList.Where(k => k.contact_number.Contains(Number)).ToList();
                    if (Status != null)
                        objSalesList = objSalesList.Where(k => k.isActive == Status).ToList();

                    objSalesList = objSalesList.ToPagedList(currentPageIndex, default_page_size);

                    if (Request.IsAjaxRequest())
                        return PartialView("_ajax_sales_person", objSalesList);
                    else
                        return View(objSalesList);
                }
                //  }
                //  else
                //      return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();


        }

        #endregion

        #region check sales person number exist
        public JsonResult check_sales_person_number_exist(tbl_sales_person objSales)
        {
            bool bRet = false;
            if (Session["admin_id"] != null && Session["role_id"] != null && objSales != null)
            {
                var alreadyExist = _uow.sales_person_Repo.Get(filter: (m => m.contact_number == objSales.contact_number && m.isDeleted == false && m.Id != objSales.Id)).FirstOrDefault();
                if (alreadyExist == null)
                    bRet = true;
            }
            return Json(bRet);
        }
        #endregion

        #region create sales person
        public ActionResult CreateSalesPerson()
        {
            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");
            try
            {
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "CreateSalesPerson"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track
                LogTrackingInfo(A_id, R_id, 2, 27);/*"CreateSalesPerson"*/
                #endregion
                tbl_sales_person objSales = new tbl_sales_person();
                return View(objSales);
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }

        [HttpPost]
        public ActionResult CreateSalesPerson(tbl_sales_person objSales)
        {
            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");
            try
            {
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "CreateSalesPerson"))
                        return RedirectToAction("Logout");
                }

                if (ModelState.IsValid)
                {
                    #region Audit_track
                    LogTrackingInfo(A_id, R_id, 1, 27, objSales.contact_number);/*"CreateSalesPerson"*/
                    #endregion
                    objSales.createdOn = DateTime.Now;
                    objSales.isDeleted = false;
                    objSales.updated_by = A_id;
                    _uow.sales_person_Repo.Insert(objSales);
                    _uow.Save();
                    if (objSales.Id > 0)
                    {
                        TempData["successmsg"] = "Sales person added successfully";
                        return RedirectToAction("SalesPersonDetails");
                    }
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }
        #endregion

        #region Edit Sales person details
        public ActionResult EditSalesPerson(long id)
        {
            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");
            try
            {
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "CreateSalesPerson"))
                        return RedirectToAction("Logout");
                }

                tbl_sales_person objSales = new tbl_sales_person();
                objSales = _uow.sales_person_Repo.Get(filter: s => s.Id == id && s.isDeleted == false).FirstOrDefault();
                if (objSales != null)
                {
                    #region Audit_track
                    LogTrackingInfo(A_id, R_id, 2, 28, objSales.contact_number);/*"EditSalesPerson"*/
                    #endregion
                    return View(objSales);
                }
                else
                    return RedirectToAction("SalesPersonDetails");
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }
        [HttpPost]
        public ActionResult EditSalesPerson(tbl_sales_person objSales)
        {
            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");
            try
            {
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "CreateSalesPerson"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track
                LogTrackingInfo(A_id, R_id, 3, 28, objSales.contact_number);/*"EditSalesPerson"*/
                #endregion
                tbl_sales_person objSalesUpdate = new tbl_sales_person();
                objSalesUpdate = _uow.sales_person_Repo.Get(filter: s => s.Id == objSales.Id && s.isDeleted == false).FirstOrDefault();
                objSalesUpdate.contact_address = objSales.contact_address;
                objSalesUpdate.contact_number = objSales.contact_number;
                objSalesUpdate.email = objSales.email;
                objSalesUpdate.first_name = objSales.first_name;
                objSalesUpdate.isActive = objSales.isActive;
                objSalesUpdate.last_name = objSales.last_name;
                objSalesUpdate.modifiedOn = DateTime.Now;
                objSalesUpdate.updated_by = A_id;
                _uow.sales_person_Repo.Update(objSalesUpdate);
                _uow.Save();

                if (objSales.Id > 0)
                {
                    TempData["successmsg"] = "Sales person details updated successfully";
                    return RedirectToAction("SalesPersonDetails");
                }

                else
                    return View(objSales);
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }
        #endregion

        #region delete sales person
        public ActionResult delete_sales_person(long id)
        {
            try
            {
                if (Session["admin_id"] != null && Session["role_id"] != null)
                {
                    long A_id = Convert.ToInt64(Session["admin_id"]);
                    int R_id = Convert.ToInt32(Session["role_id"]);
                    if (R_id != 1)
                    {
                        if (!_admin_repo.check_menu_access(A_id, R_id, "CreateSalesPerson"))
                            return RedirectToAction("Logout");
                    }

                    tbl_sales_person _obj = new tbl_sales_person();
                    _obj = _uow.sales_person_Repo.GetByID(id);
                    if (_obj != null)
                    {
                        #region Audit_track
                        LogTrackingInfo(A_id, R_id, 4, 26, _obj.contact_number);/*"delete_sales_person"*/
                        #endregion
                        _obj.isActive = false;
                        _obj.isDeleted = true;
                        _obj.updated_by = A_id;
                        _uow.sales_person_Repo.Update(_obj);
                        _uow.Save();

                    }
                    return Json(new { Status = "true" });
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
                //return Json(new { Status = "false" });
            }
            return null;
        }
        #endregion

        #endregion

        #region BundlePlans

        #region BundlePlansList
        public ActionResult BundlePlans(int? page, string bundleName, long? bundleID, int? bTypeId)
        {
            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");

            long A_id = Convert.ToInt64(Session["admin_id"]);
            int R_id = Convert.ToInt32(Session["role_id"]);
            if (R_id != 1)
            {
                if (!_admin_repo.check_menu_access(A_id, R_id, "BundlePlans"))
                    return RedirectToAction("Logout");
            }
            #region Audit_track
            LogTrackingInfo(A_id, R_id, 5, 29);/*"BundlePlans"*/
            #endregion
            IList<BundlePlanModel> objBundlePlans = new List<BundlePlanModel>();
            try
            {
                ViewData["bundleName"] = bundleName;
                ViewData["bundleID"] = bundleID;
                ViewData["bTypeId"] = bTypeId;

                objBundlePlans = _admin_repo.GetBundlePlans();

                int currentPageIndex = page.HasValue ? page.Value : 1;

                if (objBundlePlans.Count > 0)
                {
                    if (!string.IsNullOrEmpty(bundleName))
                        objBundlePlans = objBundlePlans.Where(k => k.bundlePlan.bundle_name.ToLower().Contains(bundleName.ToLower())).ToList();

                    if (bundleID != null)
                        objBundlePlans = objBundlePlans.Where(k => k.bundlePlan.bundle_id == bundleID).ToList();

                    if (bTypeId != null)
                        objBundlePlans = objBundlePlans.Where(k => k.bundlePlan.bundle_type_id == bTypeId).ToList();
                }

                objBundlePlans = objBundlePlans.ToPagedList(currentPageIndex, default_page_size);

                if (Request.IsAjaxRequest())
                    return PartialView("_ajax_bundle_plans", objBundlePlans);

                ViewBag.BundleTypeList = _uow.bundle_type_repo.Get().ToList();

                ViewBag.Company = _uow.company_info_repo.Get().ToList();       //add 
                    


            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return View(objBundlePlans);
        }
        #endregion

        #region CreateBundlePlan
        public ActionResult CreateBundlePlan()
        {
            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");

            BundlePlan_Model objBundlePlan = new BundlePlan_Model();
            try
            {
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "BundlePlans"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track
                LogTrackingInfo(A_id, R_id, 2, 30);/*"CreateBundlePlan"*/
                #endregion
                ViewBag.BundleTypeList = _uow.bundle_type_repo.Get().ToList();
                ViewBag.Company = _uow.company_info_repo.Get().ToList();   //add

                objBundlePlan.isActive = true;
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View(objBundlePlan);
        }

        [HttpPost]
        public ActionResult CreateBundlePlan(BundlePlan_Model objBundlePlan)
        {
            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");

            try
            {
                ViewBag.BundleTypeList = _uow.bundle_type_repo.Get().ToList();
                ViewBag.Company = _uow.company_info_repo.Get().ToList();

                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "BundlePlans"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track
                LogTrackingInfo(A_id, R_id, 1, 30);/*"CreateBundlePlan"*/
                #endregion
                if (objBundlePlan != null)
                {
                    objBundlePlan.isSelfcare = false;
                    if (!objBundlePlan.isSelfcare)
                    {

                        ModelState.Remove("Size");
                        //ModelState.Remove("bundle_type_id");
                        ModelState.Remove("AccountType");
                        ModelState.Remove("SmsAccountType");
                        ModelState.Remove("VoiceAccountType");
                        ModelState.Remove("IddAccountType");
                    }
                }

                if (ModelState.IsValid)
                {
                    string Msg = string.Empty;
                    bool Res = _admin_repo.CreateBundlePlan(objBundlePlan, A_id, out Msg);
                    if (Res)
                    {
                        TempData["successmsg"] = Msg;
                        return RedirectToAction("BundlePlans");
                    }
                    else
                    {
                        ViewBag.fMsg = Msg;
                    }
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }
        #endregion

        #region EditBundlePlan
        public ActionResult EditBundlePlan(long id)
        {
            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");
            BundlePlan_Model objbd = new BundlePlan_Model();
            try
            {
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "BundlePlans"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track
                LogTrackingInfo(A_id, R_id, 2, 31);/*"EditBundlePlan"*/
                #endregion
                ViewBag.BundleTypeList = _uow.bundle_type_repo.Get().ToList();
                ViewBag.Company = _uow.company_info_repo.Get().ToList();   //add
                objbd = _admin_repo.getbundlebyid(id);

                if (objbd == null)
                    return RedirectToAction("BundlePlans");
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View(objbd);
        }
        [HttpPost]
        public ActionResult EditBundlePlan(BundlePlan_Model objBundlePlan)
        {
            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");
            try
            {
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);

                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "BundlePlans"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track
                LogTrackingInfo(A_id, R_id, 3, 31);/*"EditBundlePlan"*/
                #endregion
                ViewBag.BundleTypeList = _uow.bundle_type_repo.Get().ToList();
                ViewBag.Company = _uow.company_info_repo.Get().ToList();    //add
                objBundlePlan.isSelfcare = false;
                if (!objBundlePlan.isSelfcare)
                {

                    ModelState.Remove("Size");
                    //ModelState.Remove("bundle_type_id");
                    ModelState.Remove("AccountType");
                    ModelState.Remove("SmsAccountType");
                    ModelState.Remove("VoiceAccountType");
                    ModelState.Remove("IddAccountType");
                }
                if (ModelState.IsValid)
                {
                    string Msg = string.Empty;
                    bool res = _admin_repo.EditBundlePlan(objBundlePlan, A_id, out Msg);

                    if (res == true)
                    {
                        TempData["successmsg"] = Msg;
                        return RedirectToAction("BundlePlans");
                    }
                    else
                    {
                        ViewBag.fMsg = Msg;
                    }
                }

            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }
        #endregion

        #region Sales by Salesperson Report

        public ActionResult SalesBySalesPersonReport(int? page, string Name, string Number, string sFrom, string sTo, long? salesPersonID)
        {
            IList<SalesBySalesPersonModel> objSales = new List<SalesBySalesPersonModel>();

            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");

            try
            {
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "SalesBySalesPersonReport"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track

                LogTrackingInfo(A_id, R_id, 5, 33);/*"SalesBySalesPersonReport"*/
                #endregion
                sFrom = string.IsNullOrEmpty(sFrom) ? DateTime.Now.AddMonths(-1).Date.ToString("dd/MM/yyyy") : sFrom;
                sTo = string.IsNullOrEmpty(sTo) ? DateTime.Now.Date.ToString("dd/MM/yyyy") : sTo;

                DateTime sdt = string.IsNullOrEmpty(sFrom) ? Convert.ToDateTime(sFrom) : DateTime.ParseExact(sFrom, @"dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                DateTime edt = string.IsNullOrEmpty(sTo) ? Convert.ToDateTime(sTo) : DateTime.ParseExact(sTo, @"dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);

                ViewData["Name"] = Name;
                ViewData["Number"] = Number;
                ViewData["sFrom"] = sFrom;
                ViewData["sTo"] = sTo;
                ViewData["salesPersonID"] = salesPersonID;

                ViewBag.SalesPerson = _admin_repo.GetSalesPerson().Where(x => x.isActive == true).ToList();

                objSales = _admin_repo.getSalesDetailsBySalesPerson(sdt, edt);

                int currentPageIndex = page.HasValue ? page.Value : 1;
                if (objSales != null)
                {
                    if (!string.IsNullOrEmpty(Name))
                    {
                        objSales = objSales.Where(s => s.masterName != null && s.masterName.ToLower().Contains(Name.ToLower())).ToList();
                    }
                    if (!string.IsNullOrEmpty(Number))
                    {
                        objSales = objSales.Where(s => s.masterNumber != null && s.masterNumber.Contains(Number)).ToList();
                    }
                    //if (!string.IsNullOrEmpty(sFrom) && !string.IsNullOrEmpty(sTo))
                    //{
                    //    DateTime dt = Convert.ToDateTime(sFrom).Date;
                    //    DateTime edt = Convert.ToDateTime(sTo).AddDays(1);
                    //    objSales = objSales.Where(x => x.topupDate >= dt.Date && x.topupDate < edt).ToList();
                    //}
                    if (salesPersonID != null && salesPersonID > 0)
                    {
                        objSales = objSales.Where(s => s.salesPersonID == salesPersonID).ToList();
                    }
                    TempData["salesBySalesPerson"] = objSales;

                    objSales = objSales.ToPagedList(currentPageIndex, default_page_size);
                    if (Request.IsAjaxRequest())
                        return PartialView("_ajax_SalesBySalesPersonReport", objSales);
                    else
                        return View(objSales);
                }


            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return View();
        }

        #endregion

        #region SalesBySalesPersonReportToCSV

        public ActionResult SalesBySalesPersonReportToCSV()
        {
            if (Session["Admin_Id"] == null && Session["Role_Id"] == null)
                return RedirectToAction("Login");

            try
            {
                long A_id = Convert.ToInt64(Session["Admin_Id"]);
                int R_id = Convert.ToInt32(Session["Role_Id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "SalesBySalesPersonReport"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track

                LogTrackingInfo(A_id, R_id, 6, 34);/*"SalesBySalesPersonReportToCSV"*/
                #endregion
                if (TempData["salesBySalesPerson"] != null)
                {
                    IList<SalesBySalesPersonModel> objSales = new List<SalesBySalesPersonModel>();
                    objSales = (List<SalesBySalesPersonModel>)TempData["salesBySalesPerson"];
                    TempData.Keep();
                    if (objSales != null)
                    {
                        Response.ClearContent();
                        Response.AddHeader("content-disposition", "attachment;filename=SalesBySalesPerson.csv");
                        Response.ContentType = "text/csv";

                        StringWriter sw = new StringWriter();

                        sw.WriteLine("\"Company Name\",\"Mobile Number\",\"Topup Amount\",\"Approved Date\",\"Sales Person\"");
                        if (objSales != null)
                        {
                            foreach (var line in objSales)
                            {
                                if (line != null)
                                {



                                    sw.WriteLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\"",
                                                            line.masterName,
                                                            line.masterNumber,
                                                            line.amount,
                                                          line.topupDate,
                                                          line.salesPerson


                                                               ));
                                }
                            }
                        }
                        Response.Write(sw.ToString());

                        Response.End();
                    }
                    else
                    {
                        return Content("Permission denied");
                    }
                }
                else
                {
                    return Content("Permission denied");
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return Content("Permission denied");
        }

        #endregion

        #region DeleteBundlePlan
        public ActionResult DeleteBundlePlan(long id)
        {
            string Res = "false";
            try
            {
                if (Session["admin_id"] != null && Session["role_id"] != null)
                {
                    bool isAllow = false;
                    long A_id = Convert.ToInt64(Session["admin_id"]);
                    int R_id = Convert.ToInt32(Session["role_id"]);
                    if (R_id != 1)
                    {
                        if (_admin_repo.check_menu_access(A_id, R_id, "BundlePlans"))
                            isAllow = true;
                    }
                    else
                        isAllow = true;

                    if (isAllow)
                    {
                        #region Audit_track

                        LogTrackingInfo(A_id, R_id, 4, 32);/*"DeleteBundlePlan"*/
                        #endregion
                        bool Rtn = _admin_repo.DeleteBundlePlan(id, A_id);
                        if (Rtn == true)
                        {
                            Res = "true";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return Json(new { Status = Res }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #endregion

        #region Bundle Plans Report

        public ActionResult BundlePlanDetails(int? page, string bundleName, long? bundleID, int? bTypeId)
        {
            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");

            IList<BundlePlanModel> objBundlePlans = new List<BundlePlanModel>();
            try
            {
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);

                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "BundlePlanDetails"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track
                LogTrackingInfo(A_id, R_id, 5, 56);/*"BundlePlans"*/
                #endregion
                ViewData["bundleName"] = bundleName;
                ViewData["bundleID"] = bundleID;
                ViewData["bTypeId"] = bTypeId;

                objBundlePlans = _admin_repo.GetBundlePlans();

                int currentPageIndex = page.HasValue ? page.Value : 1;

                if (objBundlePlans.Count > 0)
                {
                    if (!string.IsNullOrEmpty(bundleName))
                        objBundlePlans = objBundlePlans.Where(k => k.bundlePlan.bundle_name.ToLower().Contains(bundleName.ToLower())).ToList();

                    if (bundleID != null)
                        objBundlePlans = objBundlePlans.Where(k => k.bundlePlan.bundle_id == bundleID).ToList();

                    if (bTypeId != null)
                        objBundlePlans = objBundlePlans.Where(k => k.bundlePlan.bundle_type_id == bTypeId).ToList();
                }

                TempData["BundlePlanDetails"] = objBundlePlans;

                objBundlePlans = objBundlePlans.ToPagedList(currentPageIndex, default_page_size);

                if (Request.IsAjaxRequest())
                    return PartialView("_ajax_bundle_plan_details", objBundlePlans);

                ViewBag.BundleTypeList = _uow.bundle_type_repo.Get().ToList();
                ViewBag.Company = _uow.company_info_repo.Get().ToList();
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return View(objBundlePlans);
        }


        #region BundlePlanDetailsToCSV

        public ActionResult BundlePlanDetailsToCSV()
        {
            if (Session["Admin_Id"] == null && Session["Role_Id"] == null)
                return RedirectToAction("Login");

            try
            {
                long A_id = Convert.ToInt64(Session["Admin_Id"]);
                int R_id = Convert.ToInt32(Session["Role_Id"]);
                if (R_id != 1)
                {
                    if (!_admin_repo.check_menu_access(A_id, R_id, "BundlePlanDetails"))
                        return RedirectToAction("Logout");
                }
                #region Audit_track
                LogTrackingInfo(A_id, R_id, 5, 55);/*"BundlePlans"*/
                #endregion
                if (TempData["BundlePlanDetails"] != null)
                {
                    IList<BundlePlanModel> objbundle = new List<BundlePlanModel>();
                    objbundle = (List<BundlePlanModel>)TempData["BundlePlanDetails"];
                    TempData.Keep();
                    if (objbundle != null)
                    {
                        Response.ClearContent();
                        Response.AddHeader("content-disposition", "attachment;filename=BundlePlanDetails.csv");
                        Response.ContentType = "text/csv";

                        StringWriter sw = new StringWriter();

                        sw.WriteLine("\"Bundle Id\",\"Bundle Plan Name\",\"Bundle Type\",\"Description\"");
                        if (objbundle != null)
                        {
                            foreach (var line in objbundle)
                            {
                                if (line != null)
                                {



                                    sw.WriteLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\"",
                                                            line.bundlePlan.bundle_id,
                                                            line.bundlePlan.bundle_name,
                                                            line.bundleType,
                                                          line.bundlePlan.description



                                                               ));
                                }
                            }
                        }
                        Response.Write(sw.ToString());

                        Response.End();
                    }
                    else
                    {
                        return Content("Permission denied");
                    }
                }
                else
                {
                    return Content("Permission denied");
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return Content("Permission denied");
        }

        #endregion


        #endregion

        #region for Master Account Expiry Report


        public ActionResult CompanyAccountExpiry(int? page, string Companyname, string mobile_no, string sFrom, string sTo, bool? ddlStatus)
        {
            if (Session["Admin_Id"] == null && Session["Role_Id"] == null)
                return RedirectToAction("Login");

            ViewData["Companyname"] = Companyname;
            ViewData["mobile_no"] = mobile_no;
            ViewData["sFrom"] = sFrom;
            ViewData["sTo"] = sTo;
            ViewData["ddlStatus"] = ddlStatus;

            int currentIndexPage = page.HasValue ? page.Value : 1;
            IList<MasterAccExpriyReportModel> _obj = new List<MasterAccExpriyReportModel>();
            #region Audit_track
            long A_id = Convert.ToInt64(Session["admin_id"]);
            int R_id = Convert.ToInt32(Session["role_id"]);
            LogTrackingInfo(A_id, R_id, 5, 35);/*"CompanyAccountExpiry"*/
            #endregion
            try
            {

                _obj = _ccp_repo.masterAccExpiry();


                if (!string.IsNullOrWhiteSpace(Companyname))
                    _obj = _obj.Where(c => c.company_name.Trim().ToLower().Contains(Companyname.Trim().ToLower())).ToList();

                if (!string.IsNullOrWhiteSpace(mobile_no))
                    _obj = _obj.Where(c => c.mobile_number == mobile_no).ToList();

                if ((!string.IsNullOrWhiteSpace(sFrom)) && (!string.IsNullOrWhiteSpace(sTo)))
                {
                    DateTime vdtFrom = new DateTime();
                    DateTime vdtTo = new DateTime();
                    bool bVal = (DateTime.TryParse(sFrom.Trim(), out vdtFrom) && DateTime.TryParse(sTo.Trim(), out vdtTo));
                    if (bVal)
                    {
                        DateTime dtFrom = Convert.ToDateTime(sFrom);
                        DateTime dtTo = Convert.ToDateTime(sTo).AddDays(1);
                        _obj = _obj.Where(c => c.ContractExpiryDate >= dtFrom && c.ContractExpiryDate <= dtTo).ToList();
                    }
                }

                if (ddlStatus != null)
                    _obj = _obj.Where(c => c.status == ddlStatus).ToList();

                TempData["MasterAccExp_report"] = _obj;

                _obj = _obj.ToPagedList(currentIndexPage, default_page_size);

                if (Request.IsAjaxRequest())
                    return PartialView("_ajax_master_account_expiry", _obj);
                else
                    return View(_obj);
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return View(_obj);
        }


        #region MasterAccountExpiry CSV Report


        public void MasterAccountExpiryCSVlist()
        {
            try
            {
                if (TempData["MasterAccExp_report"] != null)
                {
                    List<MasterAccExpriyReportModel> ExpiryReport = new List<MasterAccExpriyReportModel>();

                    ExpiryReport = (List<MasterAccExpriyReportModel>)TempData["MasterAccExp_report"];
                    #region Audit_track
                    long A_id = Convert.ToInt64(Session["admin_id"]);
                    int R_id = Convert.ToInt32(Session["role_id"]);
                    LogTrackingInfo(A_id, R_id, 6, 36);/*"MasterAccountExpiryCSVlist"*/
                    #endregion
                    if (ExpiryReport != null && ExpiryReport.Count > 0)
                    {
                        TempData["MasterAccExp_report"] = ExpiryReport;

                        string filename = "CompanyAccountExpiryReport_" + DateTime.Now.Date.ToShortDateString();

                        Response.ClearContent();
                        Response.AddHeader("content-disposition", "attachment;filename=" + filename + ".csv");
                        Response.ContentType = "text/csv";


                        //string sw;

                        //sw = _uow.cug_TransCSV_repo.ToCsv<TransModelCSV>(",", TransReport);

                        //Response.Write(sw.ToString());
                        //Response.End();
                        StringWriter sw = new StringWriter();

                        sw.WriteLine("\"Company Name\",\"Contact Person Name\",\"Email Address\",\"Contact Number\",\"Contract Expiry Date\",\"Status\"");

                        foreach (var line in ExpiryReport)
                        {
                            sw.WriteLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\"",

                                line.company_name,
                                line.contact_person_name,
                                line.email,
                                line.mobile_number,
                                string.Format("{0:dd/MM/yyyy}", line.ContractExpiryDate),
                                line.status == true ? "Active" : "InActive"

                                ));
                        }
                        Response.Write(sw.ToString());
                        Response.End();
                    }
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
        }

        #endregion
        #endregion

        #region Master Account Low Balance Report

        public ActionResult CompanyAccountLowBalance(int? page, string CompanyName, string CPersonName, string mobile_no, long? salesPersonID)
        {
            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");

            IList<MasterAccLowBalanceModel> obj_LowBalList = new List<MasterAccLowBalanceModel>();

            try
            {
                ViewData["CompanyName"] = CompanyName;
                ViewData["CPersonName"] = CPersonName;
                ViewData["mobile_no"] = mobile_no;
                ViewData["salesPersonID"] = salesPersonID;

                ViewBag.SalesPerson = _admin_repo.GetSalesPerson().Where(x => x.isActive == true).ToList();

                obj_LowBalList = _ccp_repo.get_MasterAccLowBalanceDetails();

                int currentPageIndex = page.HasValue ? page.Value : 1;
                #region Audit_track
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                LogTrackingInfo(A_id, R_id, 5, 49);/*"CompanyAccountLowBalance"*/
                #endregion
                if (obj_LowBalList != null)
                {

                    if (!string.IsNullOrWhiteSpace(CompanyName))
                        obj_LowBalList = obj_LowBalList.Where(c => c.company_name.Trim().ToLower().Contains(CompanyName.Trim().ToLower())).ToList();

                    if (!string.IsNullOrWhiteSpace(CPersonName))
                        obj_LowBalList = obj_LowBalList.Where(c => c.contact_person_name.Trim().ToLower().Contains(CPersonName.Trim().ToLower())).ToList();

                    if (!string.IsNullOrWhiteSpace(mobile_no))
                        obj_LowBalList = obj_LowBalList.Where(c => c.mobile_number == mobile_no).ToList();

                    if (salesPersonID != null && salesPersonID > 0)
                    {
                        obj_LowBalList = obj_LowBalList.Where(s => s.salesPersonID == salesPersonID).ToList();
                    }

                    TempData["LowBalSummary_report"] = obj_LowBalList;

                    obj_LowBalList = obj_LowBalList.ToPagedList(currentPageIndex, default_page_size);

                    if (Request.IsAjaxRequest())
                        return PartialView("_ajax_master_acc_lowBalance", obj_LowBalList);
                    else
                        return View(obj_LowBalList);
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return View(obj_LowBalList);
        }

        #region Master low balance report csv

        public void MasterAccountLowBalanceCSVlist()
        {
            try
            {
                if (TempData["LowBalSummary_report"] != null)
                {
                    List<MasterAccLowBalanceModel> ExpiryReport = new List<MasterAccLowBalanceModel>();

                    ExpiryReport = (List<MasterAccLowBalanceModel>)TempData["LowBalSummary_report"];

                    if (ExpiryReport != null && ExpiryReport.Count > 0)
                    {
                        #region Audit_track
                        long A_id = Convert.ToInt64(Session["admin_id"]);
                        int R_id = Convert.ToInt32(Session["role_id"]);
                        LogTrackingInfo(A_id, R_id, 6, 50);/*"MasterAccountLowBalanceCSVlist"*/
                        #endregion
                        TempData["LowBalSummary_report"] = ExpiryReport;

                        string filename = "CompanyAccountLowBalanceReport_" + DateTime.Now.Date.ToShortDateString();

                        Response.ClearContent();
                        Response.AddHeader("content-disposition", "attachment;filename=" + filename + ".csv");
                        Response.ContentType = "text/csv";


                        //string sw;

                        //sw = _uow.cug_TransCSV_repo.ToCsv<TransModelCSV>(",", TransReport);

                        //Response.Write(sw.ToString());
                        //Response.End();
                        StringWriter sw = new StringWriter();

                        sw.WriteLine("\"Company Name\",\"Contact Person Name\",\"Email Address\",\"Mobile Number\",\"Available Amount(K)\",\"Required Amount (K)\",\"Outstanding Balance (K)\",\"Sales Person\"");

                        foreach (var line in ExpiryReport)
                        {
                            sw.WriteLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\"",

                                line.company_name,
                                line.contact_person_name,
                                line.email,
                                line.mobile_number,
                                line.available_amount,
                                line.required_amount,
                                line.outstanding_amount,
                                line.salesPerson

                                ));
                        }
                        Response.Write(sw.ToString());
                        Response.End();
                    }
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
        }

        #endregion

        #endregion

        #region Invoice Report

        public ActionResult InvoiceReport(long id)
        {

            if (Session["admin_id"] == null && Session["role_id"] == null)
                return RedirectToAction("Login");


            InvoiceModel inv_details = new InvoiceModel();
            #region Audit_track
            long A_id = Convert.ToInt64(Session["admin_id"]);
            int R_id = Convert.ToInt32(Session["role_id"]);

            #endregion
            try
            {
                tbl_company_info obj_compy = new tbl_company_info();
                obj_compy = _uow.company_info_repo.Get(filter: x => x.isactive == true && x.isdeleted == false && x.id == id).FirstOrDefault();

                tbl_si_ccp_invoice obj_inv = new tbl_si_ccp_invoice();
                obj_inv = _uow.ccp_invoice_Repo.Get(filter: x => x.company_id == id).OrderByDescending(o => o.id).FirstOrDefault();

                if (obj_inv != null)
                {
                    #region Audit_track
                    LogTrackingInfo(A_id, R_id, 2, 51, obj_compy.mobile_number);/*"InvoiceReport"*/
                    #endregion
                    inv_details.invoicedetail = obj_inv;
                    inv_details.id = obj_compy.id;
                    inv_details.company_name = obj_compy.company_name;
                    inv_details.contact_person_name = obj_compy.contact_person_name;
                    inv_details.mobile_number = obj_compy.mobile_number;
                    inv_details.phone_number = obj_compy.phone_number;
                    inv_details.email = obj_compy.email;

                    TempData["invoice_html"] = inv_details;

                    return View(inv_details);
                }


            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }


            return View();
        }


        public ActionResult InvoiceInPdf()
        {
            if (TempData["invoice_html"] != null)
            {
                InvoiceModel _data = new InvoiceModel();
                _data = (InvoiceModel)TempData["invoice_html"];
                #region Audit_track
                long A_id = Convert.ToInt64(Session["admin_id"]);
                int R_id = Convert.ToInt32(Session["role_id"]);
                LogTrackingInfo(A_id, R_id, 2, 48, _data.mobile_number);/*"InvoiceReport"*/
                #endregion
                if (_data != null)
                {
                    var pdf = new PdfResult(_data, "InvoiceInPdf");
                    //pdf.ViewBag.Title = "Report Title";

                    return pdf;

                }
                else
                    return null;
            }
            else
                return null;
        }

        #endregion




        #region Audit trail

        public ActionResult Audit_log(int? page, int? roleid, string username, int? cat_id, int? act_id, string ip_add, string aces_from, string sdate, string edate, string msisdn)
        {
            IList<ccp_AuditModel> objaudit = new List<ccp_AuditModel>();
            try
            {
                if (Session["admin_id"] == null || Session["role_id"] == null)
                    return RedirectToAction("Login");

                long admin_id = Convert.ToInt64(Session["admin_id"]);
                int roleId = Convert.ToInt32(Session["role_id"]);

                #region Audit_track
                LogTrackingInfo(admin_id, roleId, 5, 52);/*"Audit_log"*/
                #endregion

                if (roleId != 1)
                {
                    return RedirectToAction("Logout");
                }
                int currentPageIndex = page.HasValue ? page.Value : 1;

                ViewData["roleid"] = roleid;
                ViewData["cat_id"] = cat_id;
                ViewData["username"] = username;
                ViewData["act_id"] = act_id;
                ViewData["ip_add"] = ip_add;
                ViewData["aces_from"] = aces_from;
                ViewData["sdate"] = sdate;
                ViewData["edate"] = edate;
                ViewData["msisdn"] = msisdn;
                if (aces_from == null)
                {
                    aces_from = "Admin";
                }

                objaudit = _ccp_repo.get_auditlog(aces_from, sdate, edate);

                if (objaudit != null || objaudit.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(username))
                        objaudit = objaudit.Where(x => x.user_name.ToLower().Contains(username.ToLower())).ToList();

                    if (roleid != null && roleid > 0)
                        objaudit = objaudit.Where(x => x.role_id == roleid).ToList();

                    if (cat_id > 0)
                        objaudit = objaudit.Where(x => x.rpt_cat_id == cat_id).ToList();

                    if (act_id > 0)
                        objaudit = objaudit.Where(x => x.action_id == act_id).ToList();

                    if (!string.IsNullOrWhiteSpace(ip_add))
                        objaudit = objaudit.Where(x => x.ip_address == ip_add).ToList();

                    if (!string.IsNullOrWhiteSpace(msisdn))
                    {
                        objaudit = objaudit.Where(x => x.msisdn != null && x.msisdn.Contains(msisdn)).ToList();
                    }

                }
                ViewBag.catlist = _uow.ccp_rptcat_Repo.Get(filter: x => x.is_active == true && x.is_deleted == false).ToList();
                ViewBag.actlist = _uow.ccp_action_Repo.Get(filter: x => x.is_active == true && x.is_deleted == false).ToList();
                ViewBag.rolelist = _uow.role_repo.Get().ToList();

                TempData["Audit_Log"] = objaudit.AsEnumerable();

                objaudit = objaudit.ToPagedList(currentPageIndex, default_page_size);

                if (Request.IsAjaxRequest())
                    return PartialView("_Ajax_ccp_AuditLog", objaudit);
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View(objaudit);
        }

        public ActionResult AuditExcel()
        {
            if (Session["admin_id"] == null)
                return RedirectToAction("Login");

            try
            {

                long user_id = Convert.ToInt64(Session["Admin_Id"]);
                int role_id = Convert.ToInt32(Session["Role_Id"]);

                #region Audit_track
                LogTrackingInfo(user_id, role_id, 6, 53);/*"AuditExcel"*/
                #endregion

                if (TempData["Audit_Log"] != null)
                {
                    IEnumerable<ccp_AuditModel> objlog = (IEnumerable<ccp_AuditModel>)TempData["Audit_Log"];
                    TempData.Keep("Audit_Log");
                    if (objlog != null)
                    {
                        //Export to Excel
                        var grid = new System.Web.UI.WebControls.GridView();
                        grid.DataSource = from d in objlog
                                              //join r in _context.role on d.role_id equals r.id
                                          select new
                                          {
                                              User = d.user_name,
                                              ReportCatagory = d.rpt_cat_name,
                                              Action = d.action_name,
                                              IpAddress = d.ip_address,
                                              //URLAccesses = d.url_accesses,
                                              //URLReferral = d.url_referal,
                                              Role = d.role_name,
                                              Createdon = d.created_on,
                                              Accessfrom = d.access_from,
                                              msisdn = string.IsNullOrEmpty(d.msisdn) ? "NA" : d.msisdn,
                                          };
                        grid.DataBind();

                        Response.ClearContent();
                        Response.Buffer = true;
                        Response.AddHeader("content-disposition", "attachment; filename=Audit_Excel.xls");
                        Response.ContentType = "application/ms-excel";
                        Response.Charset = "";
                        StringWriter sw = new StringWriter();
                        System.Web.UI.HtmlTextWriter htw = new System.Web.UI.HtmlTextWriter(sw);
                        grid.RenderControl(htw);
                        Response.Write(sw.ToString());
                        Response.Flush();
                        Response.End();
                    }
                    else
                        return Content("records not found");
                }
                else
                    return Content("Permission denied");
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return Content("Permission denied");
        }

        #endregion
        #region LogTrackingInfo
        private void LogTrackingInfo(long user_id, int role_id, int track_action_id, int track_cat_id, string msisdn = null)
        {
            string url_accesses = Request.Url.AbsoluteUri;
            string url_referal = (Request.UrlReferrer != null) ? Request.UrlReferrer.ToString() : null;

            ccp_AuditModel obj_track = new ccp_AuditModel();
            obj_track.user_id = user_id;
            obj_track.role_id = role_id;
            //obj_track.action_name = track_action_name;
            //obj_track.rpt_cat_name = track_cat_name;
            obj_track.action_id = track_action_id;
            obj_track.rpt_cat_id = track_cat_id;
            obj_track.url_accesses = url_accesses;
            obj_track.access_from = Access_from;
            obj_track.url_referal = url_referal;
            obj_track.msisdn = msisdn;
            _ccp_repo.Ccp_Tracking(obj_track);
        }
        #endregion

        #region check_mobile_exist

        public JsonResult check_mobile_exist(string mobile_number, int? id)
        {

            bool bRet = false;

            bRet = _ccp_repo.check_mobile_exist(mobile_number, id);

            return Json(bRet);
        }

        #endregion
        #region delete staff msisdn

        [HttpPost]
        public JsonResult deleteStaffMsisdn(long id)
        {
            string Res = "false";
            try
            {
                if (Session["admin_id"] != null && id > 0)
                {

                    tbl_staff_topup topuplist = new tbl_staff_topup();
                    #region Audit_track
                    long admin_id = Convert.ToInt64(Session["admin_id"].ToString());
                    int role_id = Convert.ToInt32(Session["Role_Id"]);
                    LogTrackingInfo(admin_id, role_id, 4, 44);/*"delete_staff_msisdn"*/
                    #endregion
                    topuplist = _ccp_repo.get_staff_topupby_id(id);
                    if (topuplist != null)
                    {
                        topuplist.isdeleted = true;
                        topuplist.user_id = Convert.ToInt64(Session["admin_id"].ToString());

                        bool bRes = _ccp_repo.update_staff_topup(topuplist);
                        if (bRes)
                            Res = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return Json(new { Status = Res });
        }

        #endregion

        public ActionResult GetCompanyNameAutoComplete(string name)
        {
            List<CompanyAutoModel> obj = new List<CompanyAutoModel>();
            obj = _ccp_repo.GetCompanyNameAutoComplete(name);

            return Json(obj,JsonRequestBehavior.AllowGet);
        }

        
        #region Dispose Objects

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _util_repo.Dispose();
                _ccp_repo.Dispose();
                _admin_repo.Dispose();
                _uow.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
