using SI_ccp.DAL;
using SI_ccp.Models;
using SI_ccp.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcPaging;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace SI_ccp.Controllers
{
    public class UserController : Controller
    {
        #region Repo

        //private UnitOfWork _uow;
        private IUtilityRepo _util_repo;
        private ICCPRepo _ccp_repo;
        private int default_page_size = Convert.ToInt32(ConfigurationManager.AppSettings["pgSize"]);
        private string Access_from = "User";
        public UserController()
        {
            //this._uow = new UnitOfWork();
            this._util_repo = new UtilityRepo();
            this._ccp_repo = new CCPRepo();
        }

        #endregion

        #region Login

        [HttpGet]
        public ActionResult Login()
        {
            try
            {

                Session.Clear();
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
                Session.Clear();

                if (ModelState.IsValid)
                {

                    tbl_user obj_user = new tbl_user();

                    obj_user = _ccp_repo.check_user_login(obj_login);
                    if (obj_user != null)
                    {
                        Session["user_id"] = obj_user.id;
                        Session["username"] = obj_user.first_name + " " + obj_user.last_name;
                        long user_id = Convert.ToInt64(Session["user_id"]);
                        import_staff_model obj_new_topup = new import_staff_model();
                        obj_new_topup.company_info = _ccp_repo.get_activecompanyby_id(obj_user.company_id);
                        Session["import_staff"] = obj_new_topup.company_info.is_import_topup.ToString();
                        var im = Session["import_staff"].ToString();
                        #region Audit_track
                        LogTrackingInfo(user_id, 8, 1);/*"Login"*/
                        #endregion
                        if(im == "False")
                        {
                            return RedirectToAction("import_staff_topup");
                        }
                        else
                        {
                            return RedirectToAction("staffs_msisdn");
                        }
                       
                    }
                    else
                        ViewBag.Msg = "Invalid Username & Password";

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
                long user_id = Convert.ToInt64(Session["user_id"]);
                #region Audit_track
                LogTrackingInfo(user_id, 9, 2);/*"Logout"*/
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

        #region Import Staffs Topup
        [HttpGet]
        public ActionResult import_staff_topup()
        {
            if (Session["user_id"] == null)
                return RedirectToAction("Login");

            import_staff_model obj_new_topup = new import_staff_model();
            obj_new_topup.staff_topup_list = new List<import_topup_model>();
            try
            {
                ViewBag.BundleList = _ccp_repo.GetBundle_Plans();

                long user_id = Convert.ToInt64(Session["user_id"]);

                #region Audit_track
                LogTrackingInfo(user_id, 2, 40);/*"import_staff_topup"*/
                #endregion
                tbl_user obj_user = _ccp_repo.get_userby_id(user_id);
                if (obj_user != null)
                {
                    obj_new_topup.company_info = _ccp_repo.get_activecompanyby_id(obj_user.company_id);

                    //if (obj_new_topup != null && obj_new_topup.company_info != null && obj_new_topup.company_info.is_import_topup == true)
                    //{
                    //    obj_new_topup.staff_topup_list = new List<import_topup_model>();

                    //    if (Session["user_staffs_data"] != null)
                    //    {
                    //        obj_new_topup.staff_topup_list = (List<import_topup_model>)Session["user_staffs_data"];
                    //        Session["user_staffs_data"] = null;
                    //    }
                    //}

                    //else
                    //    return RedirectToAction("staffs_msisdn");

                    //if (TempData["staffs_data"] != null)
                    //{
                    //    obj_new_topup.staff_topup_list = (List<import_topup_model>)TempData["staffs_data"];
                    //    TempData["staffs_data"] = null;
                    //}
                }
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

            if (Session["user_id"] == null)
                return RedirectToAction("Login");

            List<import_topup_model> topup_list = new List<import_topup_model>();
            List<tbl_bundle_plan> objBundle_plans = new List<tbl_bundle_plan>();
            decimal? tot_bundle_amt = 0;
            try
            {
                var bdlpln = _ccp_repo.GetBundle_Plans();

                if (bdlpln != null && bdlpln.Count > 0)
                {
                    objBundle_plans = bdlpln;
                    ViewBag.BundleList = bdlpln;
                }
                decimal required_amount = 0;
                long user_id = Convert.ToInt64(Session["user_id"]);
                #region Audit_track
                LogTrackingInfo(user_id, 1, 40);/*"import_staff_topup"*/
                #endregion

                import_staff_model objdata = new import_staff_model();
                tbl_company_info company_info = new tbl_company_info();

                tbl_user user = new tbl_user();
                user = _ccp_repo.get_activeuserby_id(user_id);
                if (user != null)
                {
                    company_info = _ccp_repo.get_activecompanyby_id(user.company_id);

                    if (company_info != null)
                        obj_staff.company_info = company_info;
                }

                if (obj_staff.cmd == "Export" || cmd == "Export")
                {
                    #region Export_csv

                    import_staff_model obj_newstaff = new import_staff_model();

                    if (obj_staff != null && obj_staff.staff_csv != null)
                    {
                        obj_staff.staff_topup_list = new List<import_topup_model>();

                        var extension = Path.GetExtension(obj_staff.staff_csv.FileName ?? string.Empty);

                        if (extension.ToLower() == ".csv")
                        {
                            List<import_topup_model> first_topup_list = new List<import_topup_model>();
                            bool checkMsisdn = false;
                            if (objBundle_plans.Count > 0)
                            {
                                using (var reader = new StreamReader(obj_staff.staff_csv.InputStream))
                                {
                                    var SkipColumnName = 0;
                                    var csv_items = reader.ReadToEnd().Split('\n');

                                    int cnt = csv_items.Count();
                                    if (csv_items.Length > 1)
                                    {


                                        if (csv_items.Last() == "")
                                        {
                                            csv_items = csv_items.Take(csv_items.Count() - 1).ToArray(); //Remove last
                                            cnt = --cnt;
                                        }
                                        var vp_item = from line in csv_items select line.Replace("\r", "").Split(',').ToList();
                                        var res = vp_item.ToList();
                                        if (res != null && res.Count > 0)
                                        {

                                            if (res[0].Contains("MSISDN Number"))
                                            {
                                                foreach (var item in vp_item)
                                                {
                                                    if (!string.IsNullOrEmpty(item[0]) || !string.IsNullOrEmpty(item[1]) || !string.IsNullOrEmpty(item[2]) || !string.IsNullOrEmpty(item[3]) || !string.IsNullOrEmpty(item[4]) || !string.IsNullOrEmpty(item[5]))
                                                    {

                                                        import_topup_model obj_first_topup = new import_topup_model();
                                                        obj_first_topup.bundle_ids = new List<long>();

                                                        if (SkipColumnName > 0 && SkipColumnName <= (cnt - 1))
                                                        {

                                                            #region Topup_bundle_MSISDN
                                                            if (!string.IsNullOrEmpty(item[0]))
                                                            {
                                                                obj_first_topup.msisdn_number = item[0];
                                                                obj_first_topup.first_name = item[1];
                                                                obj_first_topup.last_name = item[2];
                                                                obj_first_topup.amount = string.IsNullOrEmpty(item[3]) ? 0 : Convert.ToInt32(item[3]);
                                                                obj_first_topup.invoice = item[4];
                                                                obj_first_topup.email = item[5];
                                                                checkMsisdn = true;
                                                                first_topup_list.Add(obj_first_topup);
                                                            }

                                                            else
                                                            {
                                                                obj_first_topup.description += "<div class='ifail'>MSISDN Number Required!</div>";
                                                                ViewBag.Msg = "MSISDN Number Required!";
                                                            }

                                                            #endregion

                                                           

                                                        }

                                                        SkipColumnName++;

                                                    }
                                                }
                                            }
                                            else
                                            {
                                                ViewBag.Msg = "Choose correct csv file!";
                                            }

                                        }
                                    }
                                    else
                                    {
                                        ViewBag.Msg = "MSISDN Number Required!";
                                    }
                                }
                                if (checkMsisdn)
                                {


                                    if (first_topup_list.Count > 0)
                                    {
                                        bool is_imported = false;

                                        #region import staff topup                               

                                        foreach (var item in first_topup_list)
                                        {
                                            DateTime dt = DateTime.Now;
                                            bool check_alreadyAvailable = true;
                                            long res;
                                            var msisdn = long.TryParse(item.msisdn_number, out res);
                                            if (msisdn)
                                            {


                                                if (!string.IsNullOrEmpty(item.msisdn_number))
                                                {
                                                    item.description = "<div class='isuccess'>Imported Successfully</div>";
                                                    item.is_valid = false;
                                                    is_imported = true;
                                                }

                                                //    check_alreadyAvailable = _ccp_repo.get_staff_topup(item.msisdn_number.Trim());

                                                //if (check_alreadyAvailable == true)
                                                //{
                                                //    item.is_valid = true;
                                                //    item.description = "<div class='ifail'>Data already imported for this MSISDN number!</div>";
                                                //}
                                                //else if (check_alreadyAvailable == false)
                                                //{
                                                //    item.description = "<div class='isuccess'>Imported Successfully</div>";
                                                //    item.is_valid = false;
                                                //    is_imported = true;
                                                //}

                                                topup_list.Add(item);
                                            }
                                            else
                                            {
                                                ViewBag.Msg = "MSISDN number accepts only numbers.";

                                            }
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

                    _ccp_repo.TrucateTempStaffTopup();

                    objdata = obj_staff;
                    if (!string.IsNullOrEmpty(obj_staff.staffjson))
                    {
                        var staffDes = JsonConvert.DeserializeObject<List<import_topup_model>>(obj_staff.staffjson);
                        objdata.staff_topup_list = staffDes;
                    }
                    bool bCheck = true;

                    if (objdata.staff_topup_list != null && objdata.staff_topup_list.Count > 0)
                    {
                        #region Level0 Validation
                        var lev0B = false;
                        string msg = "";
                        objdata = _ccp_repo.StaffTopupAllAmountVal(objdata, out lev0B, out msg);

                        #endregion

                        if (lev0B)
                        {


                            var lev1B = false;
                            #region Level1 Validation 

                            objdata = _ccp_repo.StaffTopupAmountVal(objdata, out lev1B);

                            #endregion

                            if (lev1B)
                            {
                                var lev2B = false;

                                #region Level2 Validation 
                                objdata = _ccp_repo.StaffTopupBundleIdVal(objdata, out lev2B);
                                #endregion

                                if (lev2B)
                                {
                                    #region Save StaffTopup Temp table 
                                    foreach (var itm in objdata.staff_topup_list)
                                    {
                                        foreach (long bundle_id in itm.bundle_ids)
                                        {
                                            tbl_bundle_plan objBundle = objBundle_plans.Where(x => x.bundle_id == bundle_id).FirstOrDefault();
                                            if (bundle_id == 0)
                                            {
                                                _ccp_repo.SaveTempStaffTopup(bundle_id, "NA", itm.msisdn_number);
                                            }
                                            else
                                            {
                                                _ccp_repo.SaveTempStaffTopup(objBundle.bundle_id, objBundle.bundle_name, itm.msisdn_number);
                                            }

                                        }
                                    }
                                    #endregion

                                    #region Level3 Validation
                                    List<temp_staff_topup> res = new List<temp_staff_topup>();
                                    res = _ccp_repo.GetStaffTopupDuplicate();

                                    if (res != null && res.Count > 0)
                                    {
                                        bCheck = false;
                                        ViewData["tempTable"] = res;
                                    }

                                    #endregion
                                }
                                else
                                {
                                    bCheck = false;
                                }


                            }
                            else
                            {
                                bCheck = false;
                            }
                        }
                        else
                        {
                            bCheck = false;
                            ViewBag.Msg = msg;
                        }

                        if (bCheck)
                        {
                            #region Final Save

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
                                            bool check_alreadyAvailable = false;
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
                                            if (item.amount >= tot_bundle_amt)
                                            {


                                                if (string.IsNullOrEmpty(item.description))
                                                {
                                                    tbl_staff_topup stopup = new tbl_staff_topup();
                                                    stopup.first_name = item.first_name.Trim();
                                                    stopup.last_name = item.last_name.Trim();
                                                    stopup.msisdn_number = current_msisdn;
                                                    stopup.topup_amount = (decimal)item.amount;
                                                    stopup.invoice_number = item.invoice;
                                                    stopup.email = item.email;
                                                    stopup.created_on = DateTime.Now;
                                                    stopup.is_recharged = false;
                                                    stopup.isactive = true;
                                                    stopup.isdeleted = false;
                                                    stopup.user_id = user_id;
                                                    stopup.company_id = company_info.id;
                                                    stopup.sales_person_id = company_info.sales_person_id;

                                                    bool Res = _ccp_repo.insert_staff_topup(stopup, item.bundle_ids, objBundle_plans);
                                                    if (Res)
                                                    {
                                                        success_count++;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                ViewBag.Msg = "Insufficient Amount for the msisdn " + current_msisdn + "!";
                                                tot_bundle_amt = 0;
                                                item.description = "<div class='ifail'>Insufficient Amount!</div>";
                                                // item.is_valid = false;
                                                break;
                                            }
                                        }
                                    }

                                    #endregion

                                    if (success_count > 0 && company_info != null)
                                        send_importtopup_notify_email(company_info.email, company_info.company_name);

                                    obj_staff.staff_topup_list = objdata.staff_topup_list;

                                    if (success_count > 0 && company_info != null && objdata.staff_topup_list.Count > 0)
                                    {
                                        Session["user_staffs_data"] = objdata.staff_topup_list;
                                        ViewBag.SuccessMessage = "Data Inserted Successfully!";
                                        return RedirectToAction("staffs_msisdn", "User");
                                    }
                                }
                                else
                                    ViewBag.Msg = "Import Staff Topup terminated due to insufficient balance!";
                            }
                            else
                                ViewBag.Msg = "Invalid, Data not insert!";
                            #endregion
                        }
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return View(obj_staff);
        }

        #region import_staff_topup_backup
        //public ActionResult import_staff_topup(import_staff_model obj_staff)
        //{

        //    if (Session["user_id"] == null)
        //        return RedirectToAction("Login");

        //    List<import_topup_model> topup_list = new List<import_topup_model>();

        //    try
        //    {

        //        if (obj_staff.staff_topup_list == null)
        //            obj_staff.staff_topup_list = new List<import_topup_model>();

        //        long user_id = Convert.ToInt64(Session["user_id"]);

        //        import_staff_model obj_newstaff = new import_staff_model();
        //        tbl_company_info company_info = new tbl_company_info();
        //        tbl_user user = new tbl_user();
        //        user = _ccp_repo.get_activeuserby_id(user_id);
        //        if (user != null)
        //        {
        //            company_info = _ccp_repo.get_activecompanyby_id(user.company_id);

        //            if (company_info != null)
        //                obj_staff.company_info = company_info;
        //        }

        //        if (ModelState.IsValid && obj_staff != null && obj_staff.staff_csv != null)
        //        {
        //            if (obj_staff.staff_topup_list == null)
        //                obj_staff.staff_topup_list = new List<import_topup_model>();

        //            var extension = Path.GetExtension(obj_staff.staff_csv.FileName ?? string.Empty);

        //            if (extension.ToLower() == ".csv")
        //            {
        //                decimal required_amount = 0;
        //                //string sRes = "";
        //                List<import_topup_model> first_topup_list = new List<import_topup_model>();

        //                List<tbl_bundle_plan> objBundle_plans = _ccp_repo.GetBundle_Plans();
        //                if (objBundle_plans.Count > 0)
        //                {
        //                    using (var reader = new StreamReader(obj_staff.staff_csv.InputStream))
        //                    {
        //                        var SkipColumnName = 0;
        //                        var csv_items = reader.ReadToEnd().Split('\n');
        //                        var vp_item = from line in csv_items select line.Replace("\r", "").Split(',').ToList();

        //                        foreach (var item in vp_item)
        //                        {
        //                            import_topup_model obj_first_topup = new import_topup_model();
        //                            obj_first_topup.bunlde_ids = new List<long>();

        //                            if (SkipColumnName > 0 && item.Count == 9)
        //                            {
        //                                #region Topup_bundle_Fn
        //                                if (!string.IsNullOrEmpty(item[0]))
        //                                    obj_first_topup.first_name = item[0];
        //                                else
        //                                    obj_first_topup.description = "<div class='ifail'>First Name Required!</div>";
        //                                #endregion

        //                                #region Topup_bundle_Ln
        //                                if (!string.IsNullOrEmpty(item[1]))
        //                                    obj_first_topup.last_name = item[1];
        //                                else
        //                                    obj_first_topup.description += "<div class='ifail'>Last Name Required!</div>";
        //                                #endregion

        //                                #region Topup_bundle_MSISDN
        //                                if (!string.IsNullOrEmpty(item[2]))
        //                                    obj_first_topup.msisdn_number = item[2];
        //                                else
        //                                    obj_first_topup.description += "<div class='ifail'>MSISDN No Required!</div>";
        //                                #endregion

        //                                #region Topup_bundle_Amount
        //                                decimal amt = 0;
        //                                if (decimal.TryParse(item[3], out amt))
        //                                {
        //                                    if (amt >= company_info.min_trans_amount && amt <= company_info.max_trans_amount)//amt <= 20 && 
        //                                    {
        //                                        obj_first_topup.amount = amt;
        //                                        required_amount += amt;
        //                                    }
        //                                    else
        //                                        obj_first_topup.description += "<div class='ifail'>Amount limit exceeded!<br/>Min Amount:" + company_info.min_trans_amount + "<br/>Max Amount:" + company_info.max_trans_amount + "</div>";
        //                                }
        //                                else
        //                                    obj_first_topup.description += "<div class='ifail'>Invalid Amount!</div>";
        //                                #endregion

        //                                #region Topup_bundle_Email
        //                                string valid_email = item[5];
        //                                if (!string.IsNullOrEmpty(valid_email))
        //                                {
        //                                    bool bValEmail = IsValidEmailAddress(valid_email);
        //                                    if (bValEmail == false)
        //                                    {
        //                                        valid_email = "";
        //                                        obj_first_topup.description += "<div class='ifail'>Invalid Email!</div>";
        //                                    }
        //                                    obj_first_topup.email = valid_email;
        //                                }
        //                                #endregion

        //                                #region Topup_bundle_Invoice

        //                                obj_first_topup.invoice = item[4];

        //                                #endregion

        //                                #region Topup_bundle_Bundle_1
        //                                decimal tot_bundle_amt = 0;
        //                                decimal Bundle1_amt;
        //                                //if (decimal.TryParse(item[6], out Bundle1_amt))
        //                                //{
        //                                //    obj_first_topup.bundle1_amt = Bundle1_amt;
        //                                //    tbl_bundle_plan objBundle = objBundle_plans.Where(x => x.price == Bundle1_amt).FirstOrDefault();
        //                                //    if (objBundle != null)
        //                                //    {
        //                                //        tot_bundle_amt += objBundle.price;
        //                                //        obj_first_topup.bunlde_ids.Add(objBundle.bundle_id);
        //                                //    }
        //                                //    else
        //                                //        obj_first_topup.description += "<div class='ifail'>Bundle1 Plan Not Available!</div>";
        //                                //}

        //                                if (!string.IsNullOrEmpty(item[6]))
        //                                {
        //                                    string b1_alpha = string.Empty;
        //                                    string b1_number = string.Empty;
        //                                    _util_repo.SplitLetterAndNumber(item[6], out b1_alpha, out b1_number);
        //                                    obj_first_topup.bType = b1_alpha;
        //                                    if (decimal.TryParse(b1_number, out Bundle1_amt))
        //                                    {
        //                                        obj_first_topup.bundle1_amt = Bundle1_amt;
        //                                        tbl_bundle_plan objBundle;
        //                                        if (string.IsNullOrEmpty(b1_alpha))
        //                                            b1_alpha = null;
        //                                        else
        //                                            b1_alpha = b1_alpha.ToUpper();

        //                                        objBundle = objBundle_plans.Where(x => x.price == Bundle1_amt && x.btype == b1_alpha).FirstOrDefault();
        //                                        if (objBundle != null)
        //                                        {
        //                                            tot_bundle_amt += objBundle.price;
        //                                            obj_first_topup.bunlde_ids.Add(objBundle.bundle_id);
        //                                        }
        //                                        else
        //                                            obj_first_topup.description += "<div class='ifail'>Bundle1 Plan Not Available!</div>";
        //                                    }
        //                                }
        //                                #endregion

        //                                #region Topup_bundle_Bundle_2
        //                                decimal Bundle2_amt;
        //                                //if (decimal.TryParse(item[7], out Bundle2_amt))
        //                                //{
        //                                //    obj_first_topup.bundle2_amt = Bundle2_amt;
        //                                //    tbl_bundle_plan objBundle = objBundle_plans.Where(x => x.price == Bundle2_amt).FirstOrDefault();
        //                                //    if (objBundle != null)
        //                                //    {
        //                                //        tot_bundle_amt += objBundle.price;
        //                                //        obj_first_topup.bunlde_ids.Add(objBundle.bundle_id);
        //                                //    }
        //                                //    else
        //                                //        obj_first_topup.description += "<div class='ifail'>Bundle2 Plan Not Available!</div>";
        //                                //}

        //                                if (!string.IsNullOrEmpty(item[7]))
        //                                {
        //                                    string b2_alpha = string.Empty;
        //                                    string b2_number = string.Empty;
        //                                    _util_repo.SplitLetterAndNumber(item[7], out b2_alpha, out b2_number);
        //                                    obj_first_topup.bType2 = b2_alpha;
        //                                    if (decimal.TryParse(b2_number, out Bundle2_amt))
        //                                    {
        //                                        obj_first_topup.bundle2_amt = Bundle2_amt;
        //                                        tbl_bundle_plan objBundle;
        //                                        if (string.IsNullOrEmpty(b2_alpha))
        //                                            b2_alpha = null;
        //                                        else
        //                                            b2_alpha = b2_alpha.ToUpper();

        //                                        objBundle = objBundle_plans.Where(x => x.price == Bundle2_amt && x.btype == b2_alpha).FirstOrDefault();
        //                                        if (objBundle != null)
        //                                        {
        //                                            tot_bundle_amt += objBundle.price;
        //                                            obj_first_topup.bunlde_ids.Add(objBundle.bundle_id);
        //                                        }
        //                                        else
        //                                            obj_first_topup.description += "<div class='ifail'>Bundle2 Plan Not Available!</div>";
        //                                    }
        //                                }
        //                                #endregion

        //                                #region Topup_bundle_Bundle_3
        //                                decimal Bundle3_amt;
        //                                //if (decimal.TryParse(item[8], out Bundle3_amt))
        //                                //{
        //                                //    obj_first_topup.bundle3_amt = Bundle3_amt;
        //                                //    tbl_bundle_plan objBundle = objBundle_plans.Where(x => x.price == Bundle3_amt).FirstOrDefault();
        //                                //    if (objBundle != null)
        //                                //    {
        //                                //        tot_bundle_amt += objBundle.price;
        //                                //        obj_first_topup.bunlde_ids.Add(objBundle.bundle_id);
        //                                //    }
        //                                //    else
        //                                //        obj_first_topup.description += "<div class='ifail'>Bundle3 Plan Not Available!</div>";
        //                                //}

        //                                if (!string.IsNullOrEmpty(item[8]))
        //                                {
        //                                    string b3_alpha = string.Empty;
        //                                    string b3_number = string.Empty;
        //                                    _util_repo.SplitLetterAndNumber(item[8], out b3_alpha, out b3_number);
        //                                    obj_first_topup.bType3 = b3_alpha;
        //                                    if (decimal.TryParse(b3_number, out Bundle3_amt))
        //                                    {
        //                                        obj_first_topup.bundle3_amt = Bundle3_amt;
        //                                        tbl_bundle_plan objBundle;
        //                                        if (string.IsNullOrEmpty(b3_alpha))
        //                                            b3_alpha = null;
        //                                        else
        //                                            b3_alpha = b3_alpha.ToUpper();

        //                                        objBundle = objBundle_plans.Where(x => x.price == Bundle3_amt && x.btype == b3_alpha).FirstOrDefault();
        //                                        if (objBundle != null)
        //                                        {
        //                                            tot_bundle_amt += objBundle.price;
        //                                            obj_first_topup.bunlde_ids.Add(objBundle.bundle_id);
        //                                        }
        //                                        else
        //                                            obj_first_topup.description += "<div class='ifail'>Bundle3 Plan Not Available!</div>";
        //                                    }
        //                                }


        //                                if (obj_first_topup.amount < tot_bundle_amt)
        //                                    obj_first_topup.description += "<div class='ifail'>Insufficient Amount!</div>";

        //                                first_topup_list.Add(obj_first_topup);

        //                                #endregion
        //                            }

        //                            SkipColumnName++;
        //                        }
        //                    }

        //                    if (first_topup_list.Count > 0)
        //                    {
        //                        if (required_amount <= company_info.credit_amount)
        //                        {
        //                            bool is_imported = false;

        //                            #region import staff topup

        //                            int success_count = 0;

        //                            foreach (var item in first_topup_list)
        //                            {
        //                                string current_msisdn = "";

        //                                if (!string.IsNullOrEmpty(item.msisdn_number))
        //                                    current_msisdn = item.msisdn_number.Trim();

        //                                DateTime dt = DateTime.Now;
        //                                tbl_staff_topup check_alreadyAvailable = _ccp_repo.get_staff_topupby_userid(user_id, current_msisdn);
        //                                if (check_alreadyAvailable == null)
        //                                {
        //                                    item.created_on = DateTime.Now;
        //                                    item.is_recharged = false;

        //                                    if (string.IsNullOrEmpty(item.description))
        //                                    {
        //                                        tbl_staff_topup stopup = new tbl_staff_topup();
        //                                        stopup.first_name = item.first_name;
        //                                        stopup.last_name = item.last_name;
        //                                        stopup.msisdn_number = item.msisdn_number;
        //                                        stopup.topup_amount = (decimal)item.amount;
        //                                        stopup.invoice_number = item.invoice;
        //                                        stopup.email = item.email;
        //                                        stopup.created_on = DateTime.Now;
        //                                        stopup.is_recharged = false;
        //                                        stopup.isactive = true;
        //                                        stopup.user_id = user_id;
        //                                        stopup.company_id = company_info.id;
        //                                        stopup.sales_person_id = company_info.sales_person_id;

        //                                        bool bRes = _ccp_repo.insert_staff_topup(stopup, item.bunlde_ids, objBundle_plans);
        //                                        if (bRes)
        //                                        {
        //                                            item.description = "<div class='isuccess'>Imported Successfully</div>";
        //                                            is_imported = true;
        //                                            success_count++;
        //                                        }
        //                                    }
        //                                }
        //                                else
        //                                {
        //                                    item.description = "<div class='ifail'>Data already imported for this MSISDN number!</div>";
        //                                }

        //                                topup_list.Add(item);
        //                            }

        //                            #endregion


        //                            if (is_imported)
        //                            {
        //                                ViewBag.SuccessMsg = "Import Successful";
        //                            }
        //                            else if (ViewBag.Msg == null)
        //                            {
        //                                ViewBag.Msg = "No data imported. Please verify the file.";
        //                            }

        //                            if (success_count > 0 && company_info != null)
        //                                send_importtopup_notify_email(company_info.email, company_info.company_name);

        //                            obj_staff.staff_topup_list = topup_list;

        //                            if (topup_list.Count > 0)
        //                                Session["user_staffs_data"] = topup_list;

        //                        }
        //                        else
        //                            ViewBag.Msg = "Import Staff Topup terminated due to insufficient balance!"; // Current balance: K" + obj_staff.company_info.credit_amount + ", Required balance: K" + required_amount;
        //                    }
        //                    else
        //                        ViewBag.Msg = "Invalid data!";
        //                }
        //                else
        //                    ViewBag.Msg = "Bundles not available!";
        //            }
        //            else
        //            {
        //                ViewBag.Msg = "File should be .csv format";
        //            }
        //        }
        //        else
        //        {
        //            ViewBag.Msg = "Please upload file";
        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        _util_repo.ErrorLog_Txt(ex);
        //    }

        //    return View(obj_staff);
        //}
        #endregion

        #region send email import staff topup notify(Method)

        private void send_importtopup_notify_email(string email, string company_name)
        {
            XElement doc = XElement.Load(Server.MapPath("~/EmailTemplate/email_template.xml"));
            XElement emailsubj = doc.Element("SendImportTopupNotifyEmail_Subj");

            XElement emailBody = doc.Element("SendUserImportTopupNotifyEmail_Body");

            string today_date = DateTime.Now.Date.ToString("d");

            string sData = emailBody.Value;
            //sData = sData.Replace("#CompanyName#", company_name).Replace("#UserName#", user_name).Replace("#Email#", email).Replace("#Password#", password);

            sData = sData.Replace("#CompanyName#", company_name).Replace("#CurrentDate#", today_date);
            string hostURL = Request.Url.AbsoluteUri.Replace(Request.Url.AbsolutePath, "");
            sData = sData.Replace("#LoginURL#", hostURL + "/Company/Login");

            _util_repo.SendEmailMessage(email, emailsubj.Value.Trim(), sData);
            //_util_repo.SendEmailMessageFROMGMAIL(email, emailsubj.Value.Trim(), sData);
        }

        #endregion

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
            long user_id = Convert.ToInt64(Session["user_id"]);

            #region Audit_track
            LogTrackingInfo(user_id, 6, 41);/*"download_stafftemplate"*/
            #endregion
            string csv = "MSISDN Number,First Name,Last Name,Amount($),Invoice,Email";
            //string csv = "MSISDN Number";
            return File(new System.Text.UTF8Encoding().GetBytes(csv), "text/csv", "StaffTopupTemplate.csv");
        }

        #endregion

        #region Staffs MSISDN

        public ActionResult staffs_msisdn(int? page, string staffs, string msisdn, string invoice, string sdate, string edate)
        {
            if (Session["user_id"] == null)
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

                long user_id = Convert.ToInt64(Session["user_id"].ToString());
                #region Audit_track
                LogTrackingInfo(user_id, 5, 43);/*"download_stafftemplate"*/
                #endregion
                IList<StaffTopupNewModel> obj_staff = new List<StaffTopupNewModel>();

                obj_staff = _ccp_repo.get_staff_topupby_company(sdate,edate,0, user_id);
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

        #endregion

        #region delete staff msisdn

        [HttpPost]
        public JsonResult delete_staff_msisdn(long id)
        {
            string Res = "false";
            try
            {
                if (Session["user_id"] != null && id > 0)
                {

                    tbl_staff_topup topuplist = new tbl_staff_topup();
                    #region Audit_track
                    long user_id = Convert.ToInt64(Session["user_id"].ToString());
                    LogTrackingInfo(user_id, 4, 44);/*"delete_staff_msisdn"*/
                    #endregion
                    topuplist = _ccp_repo.get_staff_topupby_id(id);
                    if (topuplist != null)
                    {
                        topuplist.isdeleted = true;
                        topuplist.user_id = Convert.ToInt64(Session["user_id"].ToString());

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

        #region profile


        [HttpGet]
        public ActionResult profile()
        {
            if (Session["user_id"] == null)
                return RedirectToAction("Login");
            try
            {
                long id = Convert.ToInt64(Session["user_id"].ToString());

                if (id > 0)
                {
                    tbl_user user = new tbl_user();

                    user = _ccp_repo.getuser(id);


                    if (user != null)
                    {
                        #region Audit_track
                        LogTrackingInfo(id, 2, 38, user.msisdn_number);/*"profile"*/
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
        public ActionResult profile(tbl_user obj_user)
        {
            if (Session["user_id"] == null)
                return RedirectToAction("Login");
            try
            {

                if (ModelState.IsValid)
                {
                    long user_id = Convert.ToInt64(Session["user_id"]);
                    obj_user.password = _util_repo.AES_ENC(obj_user.password);

                    bool bRes = _ccp_repo.update_user(obj_user);

                    if (bRes)
                    {
                        #region Audit_track
                        LogTrackingInfo(user_id, 3, 38, obj_user.msisdn_number);/*"profile"*/
                        #endregion
                        TempData["successmsg"] = "Profile updated successfully.";
                        obj_user.password = _util_repo.AES_DEC(obj_user.password);
                        return View(obj_user);
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

        #region cctopupinsert
        public ActionResult cctopupinsert(List<import_topup_model> jdata)
        {
            bool Res = false;
            decimal required_amount = 0;
            tbl_company_info company_info = new tbl_company_info();
            tbl_user user = new tbl_user();
            import_staff_model objdata = new import_staff_model();
            List<import_topup_model> topup_list = new List<import_topup_model>();
            List<tbl_bundle_plan> objBundle_plans = new List<tbl_bundle_plan>();
            long user_id = Convert.ToInt64(Session["user_id"]);
            try
            {
                topup_list = jdata;
                objBundle_plans = _ccp_repo.GetBundle_Plans();

                user = _ccp_repo.get_activeuserby_id(user_id);
                if (user != null)
                {
                    company_info = _ccp_repo.get_activecompanyby_id(user.company_id);

                    if (company_info != null)
                        objdata.company_info = company_info;
                }
                foreach (var itm in topup_list)
                {
                    if (itm.amount >= company_info.min_trans_amount && itm.amount <= company_info.max_trans_amount)
                    {
                        required_amount += itm.amount ?? 0;
                    }

                }
                if (topup_list.Count > 0)
                {
                    if (required_amount <= company_info.credit_amount)
                    {

                        #region import staff topup

                        int success_count = 0;

                        foreach (var item in topup_list)
                        {
                            if (item.bid_list != null)
                            {
                                item.bundle_ids = objBundle_plans.Where(x => x.is_active == true && item.bid_list.Contains(x.id)).Select(x => x.bundle_id).ToList();
                            }
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
                                stopup.user_id = user_id;
                                stopup.company_id = company_info.id;
                                stopup.sales_person_id = company_info.sales_person_id;

                                Res = _ccp_repo.insert_staff_topup(stopup, item.bundle_ids, objBundle_plans);
                                if (Res)
                                {
                                    success_count++;
                                }
                            }
                        }
                        #endregion

                        if (success_count > 0 && company_info != null)
                            // send_importtopup_notify_email(company_info.email, company_info.company_name);

                            objdata.staff_topup_list = topup_list;

                        if (topup_list.Count > 0)
                            Session["user_staffs_data"] = topup_list;
                        ViewBag.SuccessMessage = "Data Inserted Successfully!";
                    }
                    else
                        ViewBag.Msg = "Import Staff Topup terminated due to insufficient balance!";
                }
                else
                    ViewBag.Msg = "Invalid, Data not insert!";
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            return View();
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

            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
