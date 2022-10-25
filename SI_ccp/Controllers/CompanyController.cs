using SI_ccp.DAL;
using SI_ccp.Models;
using SI_ccp.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcPaging;
using System.IO;
using System.Xml.Linq;
using RazorPDF;


namespace SI_ccp.Controllers
{
    public class CompanyController : Controller
    {

        #region Repo

        private UnitOfWork _uow;
        private IUtilityRepo _util_repo;
        private ICCPRepo _ccp_repo;
        private int default_page_size = Convert.ToInt32(ConfigurationManager.AppSettings["pgSize"]);
        private string Access_from = "Company";
        private string PP_Trigger;
        public CompanyController()
        {
            this._uow = new UnitOfWork();
            this._util_repo = new UtilityRepo();
            this._ccp_repo = new CCPRepo();
            this.PP_Trigger = ConfigurationManager.AppSettings["PP_Trigger"];
        }

        #endregion

        #region Login

        [HttpGet]
        public ActionResult Login()
        {
            try
            {
                string pwd = _util_repo.AES_DEC("DOoz5iC+GwSyRCcRB0MSBA==");

                Session.Clear();
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }

        [HttpPost]
        public ActionResult Login(company_login_model obj_login)
        {
            try
            {
                Session.Clear();
                int failed_log_count = 0;
                XElement doc = XElement.Load(Server.MapPath("~/EmailTemplate/email_template.xml"));
                XElement pwd_exp_msg = doc.Element("PwdExpied_Msg");
                XElement AccLocked_Msg = doc.Element("User_AccLocked_Msg");
                XElement InvalidLogin_Msg = doc.Element("User_InvalidLogin_Msg");
                //string dec = _util_repo.AES_DEC("6QBYsqZYOVNXPsogCvnFEQ==");
                if (ModelState.IsValid)
                {
                    if (PP_Trigger.ToLower() == "true")
                    {
                        tbl_company_info obj_company = new tbl_company_info();

                        //obj_company = _ccp_repo.check_company_login(obj_login);
                        obj_company = _ccp_repo.Check_Company_Login(obj_login, out failed_log_count);
                        if (obj_company != null)
                        {
                            if (obj_company != null && failed_log_count <= 2)
                            {
                                Session["company_id"] = obj_company.id;
                                Session["username"] = obj_company.company_name;

                                long com_id = Convert.ToInt64(Session["company_id"]);
                                #region Audit_track
                                LogTrackingInfo(com_id, 8, 1);/*"Login"*/
                                #endregion
                                if (obj_company.is_import_topup == true)
                                    Session["import_access"] = obj_company.is_import_topup;

                                return RedirectToAction("dashboard");
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
                        tbl_company_info obj_company = new tbl_company_info();

                        obj_company = _ccp_repo.check_company_login(obj_login);

                        if (obj_company != null)
                        {
                            Session["company_id"] = obj_company.id;
                            Session["username"] = obj_company.company_name;
                            long com_id = Convert.ToInt64(Session["company_id"]);
                            #region Audit_track
                            LogTrackingInfo(com_id, 8, 1);/*"Login"*/
                            #endregion
                            if (obj_company.is_import_topup == true)
                                Session["import_access"] = obj_company.is_import_topup;

                            return RedirectToAction("dashboard");
                        }
                        else
                            ViewBag.Msg = InvalidLogin_Msg.Value;
                    }

                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }


        [HttpGet]
        public ActionResult Logout()
        {
            try
            {
                long com_id = Convert.ToInt64(Session["company_id"]);
                #region Audit_track
                LogTrackingInfo(com_id, 9, 2);/*"Logout"*/
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

        #region profile

        [HttpGet]
        public ActionResult profile()
        {
            if (Session["company_id"] == null)
                return RedirectToAction("Login");
            try
            {
                tbl_company_info company = new tbl_company_info();

                long id = Convert.ToInt64(Session["company_id"].ToString());

                if (id > 0)
                {
                    company = _ccp_repo.get_activecompanyby_id(id);
                    if (company != null)
                    {
                        #region Audit_track
                        LogTrackingInfo(id, 2, 38, company.mobile_number);/*"Login"*/
                        #endregion
                        company.password = _util_repo.AES_DEC(company.password);
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
        public ActionResult profile(tbl_company_info company)
        {
            if (Session["company_id"] == null)
                return RedirectToAction("Login");

            try
            {
                long com_id = Convert.ToInt64(Session["company_id"]);

                if (ModelState.IsValid)
                {
                    bool bRes = false;
                    tbl_company_info obj = _uow.company_info_repo.GetByID(company.id);
                    if (obj != null)
                    {
                        obj.contact_person_name = company.contact_person_name;
                        obj.phone_number = company.phone_number;
                        obj.mobile_number = company.mobile_number;
                        obj.address1 = company.address1;
                        obj.address2 = company.address2;
                        obj.city = company.city;

                        obj.password = _util_repo.AES_ENC(company.password);
                        obj.modified_on = DateTime.Now;

                        bRes = _ccp_repo.update_company(obj);
                        #region Audit_track
                        LogTrackingInfo(com_id, 3, 38, obj.mobile_number);/*"Login"*/
                        #endregion
                    }

                    //company.password = _util_repo.AES_ENC(company.password);
                    //company.modified_on = DateTime.Now;

                    //bool bRes = _ccp_repo.update_company(company);

                    if (bRes)
                    {
                        TempData["successmsg"] = "Profile Updated Successfully.";
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

        #endregion

        #region dashboard

        public ActionResult dashboard()
        {
            if (Session["company_id"] == null)
                return RedirectToAction("Login");

            staff_process_model obj_s_process = new staff_process_model();
            obj_s_process.company = new tbl_company_info();
            try
            {
                long company_id = Convert.ToInt64(Session["company_id"]);
                #region Audit_track
                LogTrackingInfo(company_id, 2, 39);/*"dashboard"*/
                #endregion
                obj_s_process.company = _ccp_repo.get_activecompanyby_id(company_id);

                List<tbl_staff_topup> obj_staff_topup = _ccp_repo.get_staff_topupby_comp(company_id).Where(S => S.is_recharged == false && S.trans_desc == null && S.isactive == true).ToList();

                decimal required_amount = 0;

                if (obj_staff_topup.Count > 0)
                    required_amount = obj_staff_topup.Sum(m => m.topup_amount);

                obj_s_process.required_amt = required_amount;

            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View(obj_s_process);
        }

        #endregion

        #region users

        public ActionResult users(int? page, string email, string name, string mobile_no, bool? ddlstatus)
        {
            if (Session["company_id"] == null)
                return RedirectToAction("Login");

            IList<tbl_user> obj_user = new List<tbl_user>();
            try
            {

                ViewData["email"] = email;
                ViewData["name"] = name;
                ViewData["mobile_no"] = mobile_no;
                ViewData["ddlstatus"] = ddlstatus;

                int current_page_index = page.HasValue ? page.Value : 1;

                long company_id = Convert.ToInt64(Session["company_id"].ToString());
                #region Audit_track
                LogTrackingInfo(company_id, 5, 15);/*"users"*/
                #endregion
                obj_user = _ccp_repo.getuserby_compid(company_id);
              
                if (obj_user.Count > 0)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        name = name.Trim().ToLower();

                        obj_user = (from n in obj_user
                                    let _name = (n.first_name + " " + n.last_name).Trim().ToLower()
                                    where _name.Contains(name.Trim().ToLower()) 
                                    select n).ToList();

                    }

                    if (!string.IsNullOrEmpty(email))
                        obj_user = obj_user.Where(c => c.email != null && c.email == email.Trim()).ToList();

                    if (!string.IsNullOrEmpty(mobile_no))
                        obj_user = obj_user.Where(c => c.msisdn_number == mobile_no.Trim()).ToList();

                    if (ddlstatus != null)
                        obj_user = obj_user.Where(c => c.isactive == ddlstatus).ToList();

                }

                obj_user = obj_user.ToPagedList(current_page_index, default_page_size);

                if (Request.IsAjaxRequest())
                    return PartialView("_ajax_users", obj_user);
                else
                    return View(obj_user);

            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View(obj_user);
        }

        #region create user

        [HttpGet]
        public ActionResult create_user()
        {
            if (Session["company_id"] == null)
                return RedirectToAction("Login");
            try
            {
                long company_id = Convert.ToInt64(Session["company_id"].ToString());
                #region Audit_track
                LogTrackingInfo(company_id, 2, 16);/*"create_user"*/
                #endregion
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }

        [HttpPost]
        public ActionResult create_user(tbl_user obj_user, bool? send_email)
        {
            if (Session["company_id"] == null)
                return RedirectToAction("Login");
            try
            {
                if (obj_user != null)
                {
                    #region Audit_track
                    long company_id = Convert.ToInt64(Session["company_id"].ToString());
                    LogTrackingInfo(company_id, 1, 16, obj_user.msisdn_number);/*"create_user"*/
                    #endregion
                    obj_user.password = _util_repo.AES_ENC(_util_repo.GetUniqueKey(6));

                    obj_user.created_on = DateTime.Now;
                    obj_user.company_id = Convert.ToInt64(Session["company_id"].ToString());
                    obj_user.isdeleted = false;

                    bool bRes = _ccp_repo.insert_user(obj_user);

                    if (bRes)
                    {
                        //tbl_company_info com = _ccp_repo.get_company_id(obj_user.company_id);

                        if (send_email == true)
                            send_user_email(obj_user.email, obj_user.first_name + " " + obj_user.last_name, obj_user.password);
                        //send_user_email(obj_user.email, obj_user.first_name + " " + obj_user.last_name, com.company_name, obj_user.password);

                        TempData["successmsg"] = "User details inserted successfully.";
                        return RedirectToAction("users");
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

        #region update user


        [HttpGet]
        public ActionResult update_user(long id)
        {
            if (Session["company_id"] == null)
                return RedirectToAction("Login");
            try
            {
                if (id > 0)
                {
                    tbl_user user = new tbl_user();
                    long company_id = Convert.ToInt64(Session["company_id"].ToString());

                    user = _ccp_repo.get_userby_id(id);

                    if (user != null)
                    {
                        #region Audit_track
                        LogTrackingInfo(company_id, 2, 4, user.msisdn_number);/*"update_user"*/
                        #endregion
                        user.password = _util_repo.AES_DEC(user.password);
                        return View(user);
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
        public ActionResult update_user(tbl_user obj_user, bool? send_email)
        {
            if (Session["company_id"] == null)
                return RedirectToAction("Login");
            try
            {
                if (obj_user != null)
                {
                    tbl_user user = _ccp_repo.get_userby_id(obj_user.id);
                    long company_id = Convert.ToInt64(Session["company_id"].ToString());

                    if (user != null)
                    {
                        user.first_name = obj_user.first_name;
                        user.email = obj_user.email;
                        user.isactive = obj_user.isactive;
                        user.last_name = obj_user.last_name;
                        user.msisdn_number = obj_user.msisdn_number;

                        obj_user.company_id = Convert.ToInt64(Session["company_id"].ToString());


                        bool bRes = _ccp_repo.update_user(user);
                        #region Audit_track
                        LogTrackingInfo(company_id, 3, 4, user.msisdn_number);/*"update_user"*/
                        #endregion
                        if (bRes)
                        {
                            //tbl_company_info com = _ccp_repo.get_activecompanyby_id(obj_user.company_id);

                            if (send_email == true)
                                send_user_email(user.email, user.first_name + " " + user.last_name, user.password);

                            TempData["successmsg"] = "User details updated successfully.";
                            return RedirectToAction("users");
                        }
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

        #region delete user

        public JsonResult delete_user(long id)
        {
            string Res = "false";
            try
            {
                long company_id = Convert.ToInt64(Session["company_id"].ToString());

                if (Session["company_id"] != null && id > 0)
                {
                    tbl_user user = new tbl_user();
                    user = _ccp_repo.get_userby_id(id);
                    if (user != null)
                    {
                        #region Audit_track
                        LogTrackingInfo(company_id, 4, 17, user.msisdn_number);/*"delete_user"*/
                        #endregion
                        user.isdeleted = true;
                        user.company_id = Convert.ToInt64(Session["company_id"].ToString());
                        user.modified_on = DateTime.Now;

                        bool bRes = _ccp_repo.update_user(user);

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

        #region Check user mail exist

        public JsonResult check_user_email_exist(string email, int? id)
        {
            bool bRet = false;

            bRet = _ccp_repo.check_user_email_exist(email, id);

            return Json(bRet);
        }

        #endregion

        #region send_company_email(Method)

        public void send_user_email(string email, string user_name, string pwd)
        {
            long company_id = Convert.ToInt64(Session["company_id"].ToString());
            #region Audit_track
            LogTrackingInfo(company_id, 10, 18);/*"update_user"*/
            #endregion
            string password = _util_repo.AES_DEC(pwd);
            XElement doc = XElement.Load(Server.MapPath("~/EmailTemplate/email_template.xml"));
            XElement emailsubj = doc.Element("SendUserEmail_Subj");
            XElement emailBody = doc.Element("SendUserEmail_Body");
            string sData = emailBody.Value;
            //sData = sData.Replace("#CompanyName#", company_name).Replace("#UserName#", user_name).Replace("#Email#", email).Replace("#Password#", password);
            sData = sData.Replace("#UserName#", user_name).Replace("#Email#", email).Replace("#Password#", password);
            string hostURL = Request.Url.AbsoluteUri.Replace(Request.Url.AbsolutePath, "");
            sData = sData.Replace("#LoginURL#", hostURL + "/User/Login");

            _util_repo.SendEmailMessage(email, emailsubj.Value.Trim(), sData);
        }

        #endregion


        #endregion

        #region import staffs topup

        public ActionResult import_staff_topup()
        {
            if (Session["company_id"] == null)
                return RedirectToAction("Login");

            import_staff_model obj_new_topup = new import_staff_model();
            try
            {
                long company_id = Convert.ToInt64(Session["company_id"]);

                #region Audit_track
                LogTrackingInfo(company_id, 2, 40);/*"import_staff_topup"*/
                #endregion
                obj_new_topup.company_info = _ccp_repo.get_activecompanyby_id(company_id);

                if (obj_new_topup.company_info != null && obj_new_topup.company_info.is_import_topup == true)
                {
                    if (obj_new_topup != null && obj_new_topup.company_info != null)
                    {
                        obj_new_topup.staff_topup_list = new List<import_topup_model>();

                        if (Session["staffs_data"] != null)
                        {
                            obj_new_topup.staff_topup_list = (List<import_topup_model>)Session["staffs_data"];
                            Session["staffs_data"] = null;
                        }
                    }
                }
                else
                    return RedirectToAction("Login");

            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View(obj_new_topup);
        }

        [HttpPost]
        public ActionResult import_staff_topup(import_staff_model obj_staff, string cmd)
        {

            if (Session["company_id"] == null)
                return RedirectToAction("Login");

            List<import_topup_model> topup_list = new List<import_topup_model>();
            try
            {
                ViewBag.BundleList = _ccp_repo.GetBundle_Plans();
                decimal required_amount = 0;
                import_staff_model objdata = new import_staff_model();
                long company_id = Convert.ToInt64(Session["company_id"]);
                #region Audit_track
                LogTrackingInfo(company_id, 1, 40);/*"import_staff_topup"*/
                #endregion

                tbl_company_info company_info = new tbl_company_info();

                if (company_id > 0 && company_info.is_import_topup == true)
                {
                    company_info = _ccp_repo.get_activecompanyby_id(company_id);

                    if (company_info != null)
                        obj_staff.company_info = company_info;
                }
                List<tbl_bundle_plan> objBundle_plans = _ccp_repo.GetBundle_Plans();

                if (cmd == "Export")
                {
                    #region Export_csv
                    if (obj_staff.staff_topup_list == null)
                        obj_staff.staff_topup_list = new List<import_topup_model>();

                    import_staff_model obj_newstaff = new import_staff_model();


                    if (ModelState.IsValid && obj_staff != null && obj_staff.staff_csv != null)
                    {
                        if (obj_staff.staff_topup_list == null)
                            obj_staff.staff_topup_list = new List<import_topup_model>();

                        var extension = Path.GetExtension(obj_staff.staff_csv.FileName ?? string.Empty);

                        if (extension.ToLower() == ".csv")
                        {
                            List<import_topup_model> first_topup_list = new List<import_topup_model>();

                            if (objBundle_plans.Count > 0)
                            {
                                using (var reader = new StreamReader(obj_staff.staff_csv.InputStream))
                                {
                                    var SkipColumnName = 0;
                                    var csv_items = reader.ReadToEnd().Split('\n');
                                    var vp_item = from line in csv_items select line.Replace("\r", "").Split(',').ToList();
                                    int cnt = csv_items.Count();

                                    foreach (var item in vp_item)
                                    {
                                        import_topup_model obj_first_topup = new import_topup_model();
                                        obj_first_topup.bundle_ids = new List<long>();

                                        if (SkipColumnName > 0 && SkipColumnName < (cnt - 1))
                                        {

                                            #region Topup_bundle_MSISDN
                                            if (!string.IsNullOrEmpty(item[0]))
                                                obj_first_topup.msisdn_number = item[0];
                                            else
                                                obj_first_topup.description += "<div class='ifail'>MSISDN No Required!</div>";
                                            #endregion

                                            first_topup_list.Add(obj_first_topup);

                                        }

                                        SkipColumnName++;
                                    }
                                }

                                if (first_topup_list.Count > 0)
                                {
                                    bool is_imported = false;

                                    #region import staff topup                               

                                    foreach (var item in first_topup_list)
                                    {
                                        string current_msisdn = "";

                                        if (!string.IsNullOrEmpty(item.msisdn_number))
                                            current_msisdn = item.msisdn_number.Trim();

                                        DateTime dt = DateTime.Now;
                                        bool check_alreadyAvailable = _ccp_repo.get_staff_topup(current_msisdn);

                                        if (check_alreadyAvailable == true)
                                        {
                                            item.is_valid = true;
                                            item.description = "<div class='ifail'>Data already imported for this MSISDN number!</div>";
                                        }
                                        else if (check_alreadyAvailable == false)
                                        {
                                            item.description = "<div class='isuccess'>Imported Successfully</div>";
                                            item.is_valid = false;
                                            is_imported = true;
                                        }

                                        topup_list.Add(item);
                                    }

                                    #endregion


                                    if (is_imported)
                                    {
                                        ViewBag.SuccessMsg = "Import Successfully";
                                    }
                                    else if (ViewBag.Msg == null)
                                    {
                                        ViewBag.Msg = "No data imported. Please verify the file.";
                                    }

                                    obj_staff.staff_topup_list = topup_list;

                                }
                                else
                                    ViewBag.Msg = "Invalid data!";
                            }
                            else
                                ViewBag.Msg = "Bundles not available!";
                        }
                        else
                            ViewBag.Msg = "File should be .csv format";
                    }
                    else
                        ViewBag.Msg = "Please upload file";

                    #endregion
                }
                else if (cmd == "Save")
                {
                    #region Save_staff_topup

                    objdata = obj_staff;

                    foreach (var itm in objdata.staff_topup_list)
                    {
                        if (itm.amount >= company_info.min_trans_amount && itm.amount <= company_info.max_trans_amount)
                        {
                            required_amount += itm.amount ?? 0;
                        }

                        bool check_alreadyAvailable = _ccp_repo.get_staff_topup(itm.msisdn_number);
                        if (check_alreadyAvailable == true)
                        {
                            itm.is_valid = true;
                        }

                    }
                    if (objdata.staff_topup_list.Count > 0)
                    {
                        if (required_amount <= company_info.credit_amount)
                        {
                            #region insert staff topup

                            int success_count = 0;

                            foreach (var item in objdata.staff_topup_list)
                            {
                                if (item.is_valid != true)
                                {
                                    #region bundle IDs

                                    long bid = 0;
                                    List<long> b_id = new List<long>();
                                    if (item.bType != null)
                                    {
                                        if (long.TryParse(item.bType, out bid))
                                        {
                                            bid = objBundle_plans.Where(x => x.is_active == true && x.id == bid).Select(x => x.bundle_id).FirstOrDefault();
                                            b_id.Add(bid);
                                            bid = 0;
                                        }
                                    }
                                    if (item.bType2 != null)
                                    {
                                        if (long.TryParse(item.bType2, out bid))
                                        {
                                            bid = objBundle_plans.Where(x => x.is_active == true && x.id == bid).Select(x => x.bundle_id).FirstOrDefault();
                                            b_id.Add(bid);
                                            bid = 0;
                                        }
                                    }
                                    if (item.bType3 != null)
                                    {
                                        if (long.TryParse(item.bType3, out bid))
                                        {
                                            bid = objBundle_plans.Where(x => x.is_active == true && x.id == bid).Select(x => x.bundle_id).FirstOrDefault();
                                            b_id.Add(bid);
                                            bid = 0;
                                        }
                                    }
                                    if (b_id.Count > 0)
                                    {
                                        item.bundle_ids = b_id;
                                    }
                                    #endregion

                                    string current_msisdn = "";

                                    if (!string.IsNullOrEmpty(item.msisdn_number))
                                        current_msisdn = item.msisdn_number.Trim();

                                    DateTime dt = DateTime.Now;
                                    item.created_on = DateTime.Now;
                                    item.is_recharged = false;

                                    if (string.IsNullOrEmpty(item.description))
                                    {
                                        tbl_staff_topup stopup = new tbl_staff_topup();
                                        stopup.first_name = item.first_name;
                                        stopup.last_name = item.last_name;
                                        stopup.msisdn_number = item.msisdn_number;
                                        stopup.topup_amount = (decimal)item.amount;
                                        stopup.invoice_number = item.invoice;
                                        stopup.email = item.email;
                                        stopup.created_on = DateTime.Now;
                                        stopup.is_recharged = false;
                                        stopup.isactive = true;

                                        bool Res = _ccp_repo.insert_staff_topup(stopup, item.bundle_ids, objBundle_plans);
                                        if (Res)
                                        {
                                            success_count++;
                                        }
                                    }
                                }
                            }
                            #endregion

                            //if (success_count > 0 && company_info != null)
                            //    send_importtopup_notify_email(company_info.email, company_info.company_name);

                            obj_staff.staff_topup_list = objdata.staff_topup_list;

                            if (objdata.staff_topup_list.Count > 0)
                                Session["staff_data"] = objdata.staff_topup_list;
                            ViewBag.SuccessMessage = "Data Inserted Successfully!";
                        }
                        else
                            ViewBag.Msg = "Import Staff Topup terminated due to insufficient balance!";
                    }
                    else
                        ViewBag.Msg = "Invalid, Data not insert!";

                    #endregion
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return View(obj_staff);
        }

        //public ActionResult import_staff_topup(import_staff_model obj_staff)
        //{

        //    if (Session["company_id"] == null)
        //        return RedirectToAction("Login");

        //    List<import_topup_model> topup_list = new List<import_topup_model>();

        //    try
        //    {
        //        long company_id = Convert.ToInt64(Session["company_id"]);

        //        import_staff_model obj_newstaff = new import_staff_model();

        //        tbl_company_info company_info = _ccp_repo.get_activecompanyby_id(company_id);

        //        if (company_info != null && company_info.is_import_topup == true)
        //        {
        //            obj_staff.company_info = company_info;

        //            #region import functions

        //            if (ModelState.IsValid && obj_staff != null && obj_staff.staff_csv != null)
        //            {
        //                if (obj_staff.staff_topup_list == null)
        //                    obj_staff.staff_topup_list = new List<import_topup_model>();

        //                var extension = Path.GetExtension(obj_staff.staff_csv.FileName ?? string.Empty);

        //                if (extension.ToLower() == ".csv")
        //                {
        //                    decimal required_amount = 0;
        //                    //string sRes = "";
        //                    List<import_topup_model> first_topup_list = new List<import_topup_model>();

        //                    List<tbl_bundle_plan> objBundle_plans = _ccp_repo.GetBundle_Plans();
        //                    if (objBundle_plans.Count > 0)
        //                    {
        //                        using (var reader = new StreamReader(obj_staff.staff_csv.InputStream))
        //                        {
        //                            var SkipColumnName = 0;
        //                            var csv_items = reader.ReadToEnd().Split('\n');
        //                            var vp_item = from line in csv_items select line.Replace("\r", "").Split(',').ToList();

        //                            foreach (var item in vp_item)
        //                            {
        //                                import_topup_model obj_first_topup = new import_topup_model();
        //                                obj_first_topup.bundle_ids = new List<long>();

        //                                if (SkipColumnName > 0 && item.Count == 9)
        //                                {
        //                                    if (!string.IsNullOrEmpty(item[0]))
        //                                        obj_first_topup.first_name = item[0];
        //                                    else
        //                                        obj_first_topup.description = "<div class='ifail'>First Name Required!</div>";

        //                                    if (!string.IsNullOrEmpty(item[1]))
        //                                        obj_first_topup.last_name = item[1];
        //                                    else
        //                                        obj_first_topup.description += "<div class='ifail'>Last Name Required!</div>";

        //                                    if (!string.IsNullOrEmpty(item[2]))
        //                                        obj_first_topup.msisdn_number = item[2];
        //                                    else
        //                                        obj_first_topup.description += "<div class='ifail'>MSISDN No Required!</div>";

        //                                    decimal amt = 0;
        //                                    if (decimal.TryParse(item[3], out amt))
        //                                    {
        //                                        if (amt >= obj_staff.company_info.min_trans_amount && amt <= obj_staff.company_info.max_trans_amount)//amt <= 20 &&
        //                                        {
        //                                            obj_first_topup.amount = amt;
        //                                            required_amount = required_amount + amt;
        //                                        }
        //                                        else
        //                                            obj_first_topup.description += "<div class='ifail'>Amount limit exceeded!<br/>Min Amount:" + obj_staff.company_info.min_trans_amount + "<br/>Max Amount:" + obj_staff.company_info.max_trans_amount + "</div>";
        //                                    }
        //                                    else
        //                                        obj_first_topup.description += "<div class='ifail'>Invalid Amount!</div>";

        //                                    string valid_email = item[5];
        //                                    if (!string.IsNullOrEmpty(valid_email))
        //                                    {
        //                                        bool bValEmail = IsValidEmailAddress(valid_email);
        //                                        if (bValEmail == false)
        //                                        {
        //                                            valid_email = "";
        //                                            obj_first_topup.description += "<div class='ifail'>Invalid Email!</div>";
        //                                        }
        //                                    }

        //                                    obj_first_topup.invoice = item[4];
        //                                    obj_first_topup.email = valid_email;

        //                                    decimal tot_bundle_amt = 0;
        //                                    decimal Bundle1_amt;
        //                                    if (decimal.TryParse(item[6], out Bundle1_amt))
        //                                    {
        //                                        obj_first_topup.bundle1_amt = Bundle1_amt;
        //                                        tbl_bundle_plan objBundle = objBundle_plans.Where(x => x.price == Bundle1_amt).FirstOrDefault();
        //                                        if (objBundle != null)
        //                                        {
        //                                            tot_bundle_amt += objBundle.price;
        //                                            obj_first_topup.bundle_ids.Add(objBundle.bundle_id);
        //                                        }
        //                                        else
        //                                            obj_first_topup.description += "<div class='ifail'>Bundle1 Plan Not Available!</div>";
        //                                    }

        //                                    decimal Bundle2_amt;
        //                                    if (decimal.TryParse(item[7], out Bundle2_amt))
        //                                    {
        //                                        obj_first_topup.bundle2_amt = Bundle2_amt;
        //                                        tbl_bundle_plan objBundle = objBundle_plans.Where(x => x.price == Bundle2_amt).FirstOrDefault();
        //                                        if (objBundle != null)
        //                                        {
        //                                            tot_bundle_amt += objBundle.price;
        //                                            obj_first_topup.bundle_ids.Add(objBundle.bundle_id);
        //                                        }
        //                                        else
        //                                            obj_first_topup.description += "<div class='ifail'>Bundle2 Plan Not Available!</div>";
        //                                    }

        //                                    decimal Bundle3_amt;
        //                                    if (decimal.TryParse(item[8], out Bundle3_amt))
        //                                    {
        //                                        obj_first_topup.bundle3_amt = Bundle3_amt;
        //                                        tbl_bundle_plan objBundle = objBundle_plans.Where(x => x.price == Bundle3_amt).FirstOrDefault();
        //                                        if (objBundle != null)
        //                                        {
        //                                            tot_bundle_amt += objBundle.price;
        //                                            obj_first_topup.bundle_ids.Add(objBundle.bundle_id);
        //                                        }
        //                                        else
        //                                            obj_first_topup.description += "<div class='ifail'>Bundle3 Plan Not Available!</div>";
        //                                    }

        //                                    if (obj_first_topup.amount < tot_bundle_amt)
        //                                        obj_first_topup.description += "<div class='ifail'>Insufficient Amount!</div>";

        //                                    first_topup_list.Add(obj_first_topup);
        //                                }

        //                                SkipColumnName++;
        //                            }
        //                        }

        //                        if (first_topup_list.Count > 0)
        //                        {
        //                            if (required_amount <= obj_staff.company_info.credit_amount && required_amount > 0)
        //                            {
        //                                bool is_imported = false;

        //                                #region import staff topup

        //                                int success_count = 0;

        //                                foreach (var item in first_topup_list)
        //                                {
        //                                    string current_msisdn = "";

        //                                    if (!string.IsNullOrEmpty(item.msisdn_number))
        //                                        current_msisdn = item.msisdn_number.Trim();

        //                                    tbl_staff_topup check_alreadyAvailable = new tbl_staff_topup();

        //                                    check_alreadyAvailable = _ccp_repo.get_staff_topupby_comp(company_id).Where(T => T.msisdn_number == current_msisdn).LastOrDefault();

        //                                    if (check_alreadyAvailable != null)
        //                                    {
        //                                        DateTime check_date = Convert.ToDateTime(check_alreadyAvailable.created_on);

        //                                        if (check_date.Date != DateTime.Now.Date)
        //                                            check_alreadyAvailable = null;
        //                                    }

        //                                    if (check_alreadyAvailable == null)
        //                                    {
        //                                        item.created_on = DateTime.Now;
        //                                        item.is_recharged = false;

        //                                        if (string.IsNullOrEmpty(item.description))
        //                                        {
        //                                            tbl_staff_topup stopup = new tbl_staff_topup();
        //                                            stopup.first_name = item.first_name;
        //                                            stopup.last_name = item.last_name;
        //                                            stopup.msisdn_number = item.msisdn_number;
        //                                            stopup.topup_amount = (decimal)item.amount;
        //                                            stopup.invoice_number = item.invoice;
        //                                            stopup.email = item.email;
        //                                            stopup.created_on = DateTime.Now;
        //                                            stopup.is_recharged = false;
        //                                            stopup.isactive = true;

        //                                            stopup.company_id = company_id;

        //                                            bool bRes = _ccp_repo.insert_staff_topup(stopup, item.bundle_ids, objBundle_plans);

        //                                            if (bRes)
        //                                            {
        //                                                item.description = "<div class='isuccess'>Imported Successfully</div>";
        //                                                is_imported = true;
        //                                                success_count++;
        //                                            }
        //                                        }
        //                                    }
        //                                    else
        //                                    {
        //                                        item.description = "<div class='ifail'>Data already imported for this MSISDN number!</div>";
        //                                    }

        //                                    topup_list.Add(item);
        //                                }

        //                                #endregion


        //                                if (is_imported)
        //                                {
        //                                    ViewBag.SuccessMsg = "Import Successful";
        //                                }
        //                                else if (ViewBag.Msg == null)
        //                                {
        //                                    ViewBag.Msg = "No data imported. Please verify the file.";
        //                                }

        //                                //if (success_count > 0 && company_info != null)
        //                                //    send_importtopup_notify_email(company_info.email,company_info.company_name);

        //                                obj_staff.staff_topup_list = topup_list;

        //                                if (topup_list.Count > 0)
        //                                    Session["staffs_data"] = topup_list;

        //                            }
        //                            else
        //                                ViewBag.Msg = "Import Staff Topup terminated due to insufficient balance! Current balance: K" + obj_staff.company_info.credit_amount + ", Required balance: K" + required_amount;
        //                        }
        //                        else
        //                            ViewBag.Msg = "Invalid file format!";
        //                    }
        //                    else
        //                        ViewBag.Msg = "Bundles not available!";
        //                }
        //                else                      
        //                    ViewBag.Msg = "File should be .csv format";                       
        //            }
        //            else                 
        //                ViewBag.Msg = "Please upload file";


        //            #endregion

        //        }
        //        else
        //            return RedirectToAction("Login");

        //    }
        //    catch (Exception ex)
        //    {
        //        _util_repo.ErrorLog_Txt(ex);
        //    }

        //    return View(obj_staff);
        //}

        //#region send email import staff topup notify(Method)

        //public void send_importtopup_notify_email(string email, string company_name)
        //{
        //    XElement doc = XElement.Load(Server.MapPath("~/EmailTemplate/email_template.xml"));
        //    XElement emailsubj = doc.Element("SendImportTopupNotifyEmail_Subj");

        //    XElement emailBody ;

        //        emailBody = doc.Element("SendUserImportTopupNotifyEmail_Body");

        //    string sData = emailBody.Value;

        //    sData = sData.Replace("#CompanyName#", company_name);
        //    //string hostURL = Request.Url.AbsoluteUri.Replace(Request.Url.AbsolutePath, "");
        //    //sData = sData.Replace("#LoginURL#", hostURL + "/User/Login");

        //    //_util_repo.SendEmailMessage(email, emailsubj.Value.Trim(), sData);
        //    _util_repo.SendEmailMessageFROMGMAIL(email, emailsubj.Value.Trim(), sData);
        //}

        //#endregion


        private bool IsValidEmailAddress(string emailAddress)
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

        #region download staff template

        public ActionResult download_stafftemplate()
        {
            #region Audit_track
            long company_id = Convert.ToInt64(Session["company_id"]);
            LogTrackingInfo(company_id, 2, 41);/*"download_stafftemplate"*/
            #endregion
            string csv = "First Name,Last Name,MSISDN Number,Amount,Invoice,Email";
            return File(new System.Text.UTF8Encoding().GetBytes(csv), "text/csv", "StaffTopupTemplate.csv");
        }


        #endregion

        #region staffs msisdn

        public ActionResult staffs_msisdn(int? page, string staffs, string msisdn, string invoice, string sdate, string edate)
        {
            if (Session["company_id"] == null)
                return RedirectToAction("Login");

            try
            {
                int currentPageIndex = page.HasValue ? page.Value : 1;
                sdate = string.IsNullOrEmpty(sdate) ? DateTime.Now.Date.ToString("dd/MM/yyyy") : sdate;
                edate = string.IsNullOrEmpty(edate) ? DateTime.Now.Date.ToString("dd/MM/yyyy") : edate;
                ViewData["staffs"] = staffs;
                ViewData["msisdn"] = msisdn;
                ViewData["invoice"] = invoice;
                ViewData["sdate"] = sdate;
                ViewData["edate"] = edate;

                long company_id = Convert.ToInt64(Session["company_id"].ToString());
                #region Audit_track
                LogTrackingInfo(company_id, 5, 43);/*"download_stafftemplate"*/
                #endregion
                IList<StaffTopupNewModel> obj_staff = new List<StaffTopupNewModel>();

                obj_staff = _ccp_repo.get_staff_topupby_company(sdate, edate, company_id);

                if (obj_staff.Count() > 0)
                {
                    if (!string.IsNullOrEmpty(staffs))
                    {
                        obj_staff = (from n in obj_staff
                                     let _name = (n.first_name + " " + n.last_name).Trim().ToLower()
                                     where _name.Contains(staffs.Trim().ToLower())
                                     select n).ToList();
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
                    if (!string.IsNullOrEmpty(msisdn))
                        obj_staff = obj_staff.Where(S => S.msisdn_number != null && S.msisdn_number.Trim() == msisdn.Trim()).ToList();

                    if (!string.IsNullOrEmpty(invoice))
                        obj_staff = obj_staff.Where(S => S.invoice_number != null && S.invoice_number.Trim() == invoice.Trim()).ToList();
                }

                obj_staff = obj_staff.ToPagedList(currentPageIndex, default_page_size);

                if (Request.IsAjaxRequest())
                    return PartialView("_ajax_staff_topup", obj_staff);
                else
                    return View(obj_staff);
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return View();
        }


        #region delete staff msisdn

        [HttpPost]
        public JsonResult delete_staff_msisdn(long id)
        {
            string Res = "false";
            try
            {
                if (Session["company_id"] != null && id > 0)
                {

                    tbl_staff_topup topuplist = new tbl_staff_topup();

                    topuplist = _ccp_repo.get_staff_topupby_id(id);
                    #region Audit_track
                    long company_id = Convert.ToInt64(Session["company_id"].ToString());
                    LogTrackingInfo(company_id, 4, 44);/*"delete_staff_msisdn"*/
                    #endregion
                    if (topuplist != null)
                    {
                        topuplist.isdeleted = true;
                        topuplist.company_id = Convert.ToInt64(Session["company_id"].ToString());

                        bool bRes = _ccp_repo.update_staff_topup(topuplist);
                        //_uow.staff_topup_repo.Update(topuplist);
                        //_uow.Save();
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

        #endregion

        #region staff process

        public ActionResult staff_process()
        {
            if (Session["company_id"] == null)
                return RedirectToAction("Login");

            staff_process_model obj_s_process = new staff_process_model();
            obj_s_process.company = new tbl_company_info();
            try
            {
                long company_id = Convert.ToInt64(Session["company_id"]);

                #region Audit_track
                LogTrackingInfo(company_id, 2, 45);/*"staff_process"*/
                #endregion
                List<tbl_bundle_plan> objBundle_plans = _ccp_repo.GetBundle_Plans();

                obj_s_process.company = _ccp_repo.get_activecompanyby_id(company_id);

                List<tbl_staff_topup> obj_staff_topup = _ccp_repo.get_staff_topupby_comp(company_id).Where(S => S.is_recharged == false && S.trans_desc == null && S.isactive == true && S.isdeleted == false).ToList();

                obj_s_process.required_amt = 0;

                if (obj_staff_topup.Count > 0)
                    obj_s_process.required_amt = obj_staff_topup.Sum(m => m.topup_amount);

                //if (obj_staff_topup.Count > 0)
                //{                   
                //    foreach (var item in obj_staff_topup)
                //    {
                //        required_amount += item.topup_amount;

                //        List<tbl_staff_topup_bundle> objStaff_Bundles = _ccp_repo.GetStaffs_Bunlde_Topup(item.id);
                //        if(objStaff_Bundles.Count > 0)                        
                //            required_amount += objStaff_Bundles.Sum(x => x.bundle_amt);

                //        //if (item.bundle_id1 != null)
                //        //    required_amount += objBundle_plans.Where(x => x.bundle_id == item.bundle_id1).Select(b => b.price).FirstOrDefault();

                //        //if (item.bundle_id2 != null)
                //        //    required_amount += objBundle_plans.Where(x => x.bundle_id == item.bundle_id2).Select(b => b.price).FirstOrDefault();

                //        //if (item.bundle_id3 != null)
                //        //    required_amount += objBundle_plans.Where(x => x.bundle_id == item.bundle_id3).Select(b => b.price).FirstOrDefault();
                //    }
                //}

                //obj_s_process.required_amt = required_amount;

            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View(obj_s_process);
        }

        [HttpPost]
        public ActionResult staff_process(string staff)
        {
            string sRet = "Failed";

            try
            {
                if (Session["company_id"] != null)
                {
                    long company_id = Convert.ToInt64(Session["company_id"].ToString());

                    #region Audit_track
                    LogTrackingInfo(company_id, 3, 45);/*"staff_process"*/
                    #endregion
                    if (company_id > 0)
                    {
                        string sResult = "";

                        bool success = _ccp_repo.SendStaffsTopupEmail(company_id, out sResult);

                        if (!string.IsNullOrEmpty(sResult))
                            sRet = sResult;

                        if (success == true)
                            sRet = "Staffs Process Completed successfully";
                    }
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return Json(sRet, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Staffs Transactions

        public ActionResult staffs_transactions(int? page, string sTransFrom, string sTransTo, string sMsisdn, string sInvoice, string sEmail)
        {
            if (Session["company_id"] == null)
                return RedirectToAction("Login");
            try
            {
                int currentPageIndex = page.HasValue ? page.Value : 1;

                sTransFrom = string.IsNullOrEmpty(sTransFrom) ? DateTime.Now.Date.ToString("dd/MM/yyyy") : sTransFrom;
                sTransTo = string.IsNullOrEmpty(sTransTo) ? DateTime.Now.Date.ToString("dd/MM/yyyy") : sTransTo;

                DateTime sdt = string.IsNullOrEmpty(sTransFrom) ? Convert.ToDateTime(sTransFrom) : DateTime.ParseExact(sTransFrom, @"dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                DateTime edt = string.IsNullOrEmpty(sTransTo) ? Convert.ToDateTime(sTransTo) : DateTime.ParseExact(sTransTo, @"dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);

                ViewData["sTransFrom"] = sTransFrom;
                ViewData["sTransTo"] = sTransTo;
                //ViewData["sName"] = sName;
                ViewData["sInvoice"] = sInvoice;
                ViewData["sEmail"] = sEmail;
                ViewData["sMsisdn"] = sMsisdn;

                IList<tbl_staff_topup_trans> staffs_trans = new List<tbl_staff_topup_trans>();

                long company_id = Convert.ToInt64(Session["company_id"]);
                #region Audit_track
                LogTrackingInfo(company_id, 5, 46);/*"staffs_transactions"*/
                #endregion
                staffs_trans = _ccp_repo.get_staff_topup_transby_comp(sdt, edt, company_id);

                if (staffs_trans.Count() > 0)
                {
                    if (!string.IsNullOrEmpty(sMsisdn))
                        staffs_trans = staffs_trans.Where(S => S.msisdn_number != null && S.msisdn_number.Trim() == sMsisdn.Trim()).ToList();

                    if (!string.IsNullOrEmpty(sInvoice))
                        staffs_trans = staffs_trans.Where(S => S.invoice_number != null && S.invoice_number.Trim() == sInvoice.Trim()).ToList();

                    if (!string.IsNullOrEmpty(sEmail))
                        staffs_trans = staffs_trans.Where(S => S.email != null && S.email.Trim() == sEmail.Trim()).ToList();

                    //if (!string.IsNullOrEmpty(sTransFrom))
                    //{
                    //    DateTime from_date = Convert.ToDateTime(sTransFrom);
                    //    staffs_trans = staffs_trans.Where(S => S.trans_date.Date >= from_date.Date).ToList();
                    //}

                    //if (!string.IsNullOrEmpty(sTransTo))
                    //{
                    //    DateTime to_date = Convert.ToDateTime(sTransTo);
                    //    staffs_trans = staffs_trans.Where(S => S.trans_date.Date <= to_date.Date).ToList();
                    //}

                }

                staffs_trans = staffs_trans.ToPagedList(currentPageIndex, default_page_size);

                if (Request.IsAjaxRequest())
                    return PartialView("_ajax_staffs_transactions", staffs_trans);
                else
                    return View(staffs_trans);
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return View();
        }

        #endregion

        #region Create invoice

        public ActionResult create_invoice()
        {
            //long company_id = Convert.ToInt64(Session["company_id"].ToString());
            InvoiceModel obj = new InvoiceModel();
            if (Session["company_id"] == null)
                return RedirectToAction("Login");
            try
            {
                long company_id = Convert.ToInt64(Session["company_id"]);
                #region Audit_track
                LogTrackingInfo(company_id, 2, 47);/*"staffs_transactions"*/
                #endregion
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return View(obj);
        }

        #endregion

        #region Generate Invoice

        [HttpPost]
        public ActionResult Generate_Invoice(InvoiceModel obj_invoice_details)
        {
            if (Session["company_id"] == null)
                return RedirectToAction("Login");

            try
            {
                InvoiceModel _objIM = new InvoiceModel();

                long company_id = Convert.ToInt64(Session["company_id"]);

                #region Audit_track
                LogTrackingInfo(company_id, 1, 47);/*"create_invoice"*/
                #endregion
                _objIM = _ccp_repo.getInvoice(obj_invoice_details, company_id);

                TempData["inv_data"] = _objIM;

                return View(_objIM);

            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
        }

        #endregion

        #region PDF Conversion

        public ActionResult InvoiceInPdf()
        {
            if (TempData["inv_data"] != null)
            {
                InvoiceModel _data = new InvoiceModel();
                _data = (InvoiceModel)TempData["inv_data"];
                long company_id = Convert.ToInt64(Session["company_id"].ToString());

                if (_data != null)
                {
                    #region Audit_track
                    LogTrackingInfo(company_id, 2, 48, _data.mobile_number);/*"InvoiceInPdf"*/
                    #endregion
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



        #region LogTrackingInfo
        private void LogTrackingInfo(long user_id, int track_action_id, int track_cat_id, string msisdn = null)
        {
            string url_accesses = Request.Url.AbsoluteUri;
            string url_referal = (Request.UrlReferrer != null) ? Request.UrlReferrer.ToString() : null;

            ccp_AuditModel obj_track = new ccp_AuditModel();
            obj_track.user_id = user_id;
            obj_track.role_id = 0;
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

        #region Dispose Objects

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _util_repo.Dispose();
                _ccp_repo.Dispose();
                _uow.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

    }
}
