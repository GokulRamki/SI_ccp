using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SI_ccp.Models;
using bmdoku.bmkuRef;
using SI_ccp.Utility;
using System.Configuration;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Net;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Data.Entity.Core.Objects;

namespace SI_ccp.DAL
{
    public class CCPRepo : ICCPRepo, IDisposable
    {
        #region Repo

        private SI_CCPDBEntities _context;
        private UnitOfWork _uow;
        private IUtilityRepo _util_repo;
        private EasyRecharge _easyRecharge;

        private string sf_encry_pwd;
        private string sf_plain_pwd;
        private string sf_merchantid;
        private string sf_username;
        private string sf_keycode;
        private string sendEmail_failed_details_To;
        private string SendInvoiceMailTo;
        private string invoice_path;
        private string img_path;

        public CCPRepo()
        {
            this._context = new SI_CCPDBEntities();
            this._uow = new UnitOfWork();
            this._util_repo = new UtilityRepo();
            this._easyRecharge = new EasyRecharge();

            this.sf_merchantid = ConfigurationManager.AppSettings["sf_merchantid"].ToString();
            this.sf_username = ConfigurationManager.AppSettings["sf_username"].ToString();
            this.sf_plain_pwd = ConfigurationManager.AppSettings["sf_plain_pwd"].ToString();
            this.sf_encry_pwd = ConfigurationManager.AppSettings["sf_encry_pwd"].ToString();
            this.sf_keycode = ConfigurationManager.AppSettings["sf_keycode"].ToString();
            this.sendEmail_failed_details_To = ConfigurationManager.AppSettings["SendFailedDetails"].ToString();
            this.SendInvoiceMailTo = ConfigurationManager.AppSettings["send_invoice_mail_to"].ToString();
            this.invoice_path = ConfigurationManager.AppSettings["InvoicePDF"].ToString();
            this.img_path = ConfigurationManager.AppSettings["img_path"].ToString();
        }

        #endregion

        #region Get Topup Reports

        public List<topup_report_model> get_topup_reports(DateTime sdate, DateTime edate)
        {
            List<topup_report_model> topup_report = new List<topup_report_model>();
            DateTime dtend = edate.AddDays(1);
            var query = (from c in _context.company_info
                         join ct in _context.company_topup_report on c.id equals ct.company_id
                         join p in _context.payment_type on ct.payment_type_id equals p.Id
                         join sp in _context.sales_person on c.sales_person_id equals sp.Id into sp2
                         from sp in sp2.DefaultIfEmpty()
                         where c.isdeleted == false && ct.credited_on >= sdate && ct.credited_on < dtend
                         select new topup_report_model
                         {
                             comapny_id = c.id,
                             company_name = c.company_name,
                             credit_amount = ct.credit_amount,
                             credited_on = ct.credited_on,
                             email = c.email,
                             invoice = ct.invoice,
                             is_recharged = ct.is_recharged,
                             mobile_number = c.mobile_number,
                             trans_desc = ct.trans_desc,
                             account_order_no = ct.account_order_no,
                             internal_ref_no = ct.internal_ref_no,
                             payment_type_id = ct.payment_type_id,
                             payment_type_name = p.payment_type,
                             sales_person_id = c.sales_person_id,
                             sales_person = sp != null ? sp.first_name + " " + sp.last_name : ""
                         });
            //DateTime dtstart = sdate.Date;
            //DateTime dtend = edate.AddDays(1).Date;


            //query = query.Where(x => (x.credited_on.Date >= dtstart.Date && x.credited_on.Date < dtend.Date) || (x.credited_on >= dtstart && x.credited_on < dtend));

            topup_report = query.ToList();

            return topup_report;
        }


        public List<topup_report_model> get_topup_reportsby_comp(long comp_id)
        {
            List<topup_report_model> topup_report = new List<topup_report_model>();

            topup_report = (from c in _context.company_info
                            join ct in _context.company_topup_report on c.id equals ct.company_id
                            join p in _context.payment_type on ct.payment_type_id equals p.Id
                            where c.isdeleted == false && c.id == comp_id && c.isdeleted == false
                            select new topup_report_model
                            {
                                comapny_id = c.id,
                                company_name = c.company_name,
                                credit_amount = ct.credit_amount,
                                credited_on = ct.credited_on,
                                email = c.email,
                                invoice = ct.invoice,
                                is_recharged = ct.is_recharged,
                                mobile_number = c.mobile_number,
                                trans_desc = ct.trans_desc,
                                account_order_no = ct.account_order_no,
                                internal_ref_no = ct.internal_ref_no,
                                payment_type_id = ct.payment_type_id,
                                payment_type_name = p.payment_type
                            }).ToList();



            return topup_report;
        }

        #endregion

        #region get_temp_topup

        public List<topup_report_model> get_temp_topup(DateTime sdate, DateTime edate)
        {
            DateTime dtend = edate.AddDays(1);
            List<topup_report_model> topup_report = (from c in _context.company_info
                                                     join ct in _context.company_topup_temp on c.id equals ct.company_id
                                                     join p in _context.payment_type on ct.payment_type_id equals p.Id
                                                     join a in _context.approve_status on ct.approval_status_id equals a.id
                                                     join cr in _context.company_topup_report on ct.id equals cr.temp_topup_id into tempCmpyinfo
                                                     from cr in tempCmpyinfo.DefaultIfEmpty()
                                                     where c.isdeleted == false && c.isdeleted == false && ct.credited_on >= sdate && ct.credited_on < dtend
                                                     orderby ct.credited_on descending
                                                     select new topup_report_model
                                                     {
                                                         temp_topup_id = ct.id,
                                                         comapny_id = c.id,
                                                         company_name = c.company_name,
                                                         credit_amount = ct.credit_amount,
                                                         credited_on = ct.credited_on,
                                                         email = c.email,
                                                         invoice = ct.invoice,
                                                         is_recharged = ct.is_recharged,
                                                         mobile_number = c.mobile_number,
                                                         trans_desc = ct.trans_desc,
                                                         account_order_no = ct.account_order_no,
                                                         internal_ref_no = ct.internal_ref_no,
                                                         payment_type_id = ct.payment_type_id,
                                                         payment_type_name = p.payment_type,
                                                         approval_status = a.status,
                                                         approval_status_id = a.id,
                                                         approved_on = cr != null ? cr.credited_on : (DateTime?)null
                                                     }).ToList();



            return topup_report;
        }

        public List<topup_report_model> get_temp_topup_by_comp(long comp_id)
        {
            List<topup_report_model> topup_report = new List<topup_report_model>();

            topup_report = (from c in _context.company_info
                            join ct in _context.company_topup_temp on c.id equals ct.company_id
                            join p in _context.payment_type on ct.payment_type_id equals p.Id
                            join a in _context.approve_status on ct.approval_status_id equals a.id
                            where c.isdeleted == false && c.id == comp_id && c.isdeleted == false
                            select new topup_report_model
                            {
                                comapny_id = c.id,
                                company_name = c.company_name,
                                credit_amount = ct.credit_amount,
                                credited_on = ct.credited_on,
                                email = c.email,
                                invoice = ct.invoice,
                                is_recharged = ct.is_recharged,
                                mobile_number = c.mobile_number,
                                trans_desc = ct.trans_desc,
                                account_order_no = ct.account_order_no,
                                internal_ref_no = ct.internal_ref_no,
                                payment_type_id = ct.payment_type_id,
                                payment_type_name = p.payment_type,
                                approval_status = a.status,
                                approval_status_id = a.id
                            }).ToList();



            return topup_report;
        }

        #endregion

        #region approve_topup

        public bool approve_topup(long id, int status_id, string reason, long updated_by)
        {
            bool Res = false;

            tbl_company_topup_temp objTempTopup = _uow.company_topup_temp_repo.Get(filter: x => x.id == id && x.approval_status_id == 1).FirstOrDefault();
            if (objTempTopup != null)
            {
                objTempTopup.updated_by = updated_by;

                tbl_company_info objCompany = _uow.company_info_repo.GetByID(objTempTopup.company_id);
                if (objCompany != null)
                {
                    if (status_id == 2)
                    {
                        objTempTopup.approval_status_id = status_id;

                        _uow.company_topup_temp_repo.Update(objTempTopup);
                        _uow.Save();

                        tbl_company_topup_report obj_com = new tbl_company_topup_report();
                        obj_com.company_id = objTempTopup.company_id;
                        obj_com.credit_amount = objTempTopup.credit_amount;
                        obj_com.credited_on = DateTime.Now;
                        obj_com.email = objTempTopup.email;
                        obj_com.invoice = objTempTopup.invoice;
                        obj_com.is_recharged = true;
                        obj_com.trans_desc = objTempTopup.trans_desc;
                        obj_com.internal_ref_no = objTempTopup.internal_ref_no;
                        obj_com.account_order_no = objTempTopup.account_order_no;
                        obj_com.payment_type_id = objTempTopup.payment_type_id;
                        obj_com.temp_topup_id = objTempTopup.id;

                        _uow.company_topup_report_repo.Insert(obj_com);
                        _uow.Save();

                        objCompany.credit_amount += objTempTopup.credit_amount;

                        _uow.company_info_repo.Update(objCompany);
                        _uow.Save();

                        Res = true;
                    }
                    else if (status_id == 3)
                    {
                        objTempTopup.approval_status_id = status_id;
                        objTempTopup.trans_desc = reason;

                        _uow.company_topup_temp_repo.Update(objTempTopup);
                        _uow.Save();

                        Res = true;
                    }
                }
            }

            return Res;
        }
        #endregion

        #region User Login

        public tbl_user check_user_login(admin_login_model obj_login)
        {
            tbl_user obj_user = new tbl_user();

            if (obj_login != null && !string.IsNullOrEmpty(obj_login.username) && !string.IsNullOrEmpty(obj_login.password))
            {
                string pwd = _util_repo.AES_ENC(obj_login.password.Trim());

                obj_user = (from u in _context.user
                            join c in _context.company_info on u.company_id equals c.id
                            where u.isactive == true && u.isdeleted == false && c.isactive == true && c.isdeleted == false
                            && u.email == obj_login.username.Trim() && u.password == pwd
                            select u).FirstOrDefault();
            }

            return obj_user;
        }


        #endregion



        #region Company Login

        public tbl_company_info check_company_login(company_login_model obj_login)
        {
            tbl_company_info obj_company = new tbl_company_info();

            if (obj_login != null && !string.IsNullOrEmpty(obj_login.username) && !string.IsNullOrEmpty(obj_login.password))
            {
                string pwd = _util_repo.AES_ENC(obj_login.password.Trim());

                obj_company = _uow.company_info_repo.Get(filter: u => u.isactive == true && u.isdeleted == false && u.email == obj_login.username && u.password == pwd).FirstOrDefault();
            }

            return obj_company;
        }


        #endregion

        #region SendScheduledEmail

        public bool SendStaffsTopupEmail(long company_id, out string msg)
        {
            bool bRes = false;
            string sRes = "";

            try
            {
                List<tbl_staff_topup> objStaffs = new List<tbl_staff_topup>();
                objStaffs = _uow.staff_topup_repo.Get(filter: S => S.company_id == company_id && S.is_recharged == false && S.is_processed == false && S.trans_desc == null && S.isactive == true && S.isdeleted == false).ToList();
                if (objStaffs.Count > 0)
                {
                    List<tbl_bundle_plan> objBundle_plans = GetBundle_Plans();

                    decimal required_amount = (from staffs in objStaffs
                                               select new
                                               {
                                                   amount = staffs.topup_amount //Calculate_Amt(staffs, objBundle_plans)
                                               }).Sum(x => x.amount);

                    tbl_company_info obj_company = _uow.company_info_repo.GetByID(company_id);
                    if (obj_company != null)
                    {
                        if (required_amount <= obj_company.credit_amount && required_amount > 0)
                        {
                            //_context.Database.SqlQuery<int>("EXEC Update_StaffTopup_Process @company_id", new System.Data.SqlClient.SqlParameter("@company_id", company_id)).FirstOrDefault<int>();
                            //objStaffs.ForEach(x => x.is_processed = true);

                            #region staff recharge proceed

                            //_util_repo.ErrorLog_Txt("Begin", "Data Loaded");
                            string EmailTempFilePath = ConfigurationSettings.AppSettings["EmailTempPath"];
                            XElement doc = XElement.Load(@EmailTempFilePath + "email_template.xml");
                            XElement emailsubj = doc.Element("ReminderEmail_Subj");
                            XElement emailBody = doc.Element("ReminderEmail_Body");
                            XElement emailBudleTbl_Head = doc.Element("ReminderEmail_BudleTbl_Head");
                            XElement emailBudleTbl_Row = doc.Element("ReminderEmail_BudleTbl_Row");
                            XElement emailBudleTbl_Foot = doc.Element("ReminderEmail_BudleTbl_Foot");


                            #region for failed bundle email template

                            string EmailTempFilePath1 = ConfigurationManager.AppSettings["EmailTempPath"];
                            XElement doc1 = XElement.Load(@EmailTempFilePath1 + "email_template.xml");
                            XElement emailsubj1 = doc1.Element("SendFailedTopupBundleEmail_Subj");
                            XElement emailBody1 = doc1.Element("SendFailedTopupBundleEmail_Body");
                            XElement emailFdeatilsTbl_Row = doc1.Element("SendFailedTopupBundleEmail_Row");
                            #endregion

                            #region for failed topup email template

                            string EmailTempFilePath2 = ConfigurationManager.AppSettings["EmailTempPath"];
                            XElement doc2 = XElement.Load(@EmailTempFilePath2 + "email_template.xml");
                            XElement emailsubj2 = doc2.Element("SendFailedTopupEmail_Subj");
                            XElement emailBody2 = doc2.Element("SendFailedTopupEmail_Body");
                            XElement emailFTopupdeatilsTbl_Row = doc2.Element("SendFailedTopupEmail_Row");

                            #endregion

                            List<long> staffTopupIDs = objStaffs.Select(x => x.id).ToList();
                            List<tbl_staff_topup_bundle> Staff_BundlesList = (from s_bundle in _context.staff_topup_bundle
                                                                              where staffTopupIDs.Contains(s_bundle.staff_topup_id) && s_bundle.is_deleted == false && s_bundle.is_active == true && s_bundle.is_recharged == false
                                                                              select s_bundle).ToList();

                            //List<tbl_staff_topup_bundle> Staff_BundlesList = (from s_topup in objStaffs.AsEnumerable()
                            //                                                  join s_bundle in _context.staff_topup_bundle.AsEnumerable() on s_topup.id equals s_bundle.staff_topup_id
                            //                                                  where s_bundle.is_deleted == false && s_bundle.is_active == true && s_bundle.is_recharged == false
                            //                                                  select s_bundle).ToList();


                            StringBuilder sb_failedBundleDetails = new StringBuilder();
                            StringBuilder sb_failedTopupDetails = new StringBuilder();

                            foreach (var item in objStaffs)
                            {
                                //decimal total_amt = item.topup_amount;
                                tbl_company_info obj_each_company = _uow.company_info_repo.GetByID(company_id);
                                decimal currCompanyBal = obj_each_company.credit_amount;

                                bool res = false;
                                tbl_staff_topup_trans obj_sf_trans = new tbl_staff_topup_trans();
                                obj_sf_trans.trans_date = DateTime.Now;
                                obj_sf_trans.ip_address = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();
                                obj_sf_trans.amount = item.topup_amount;
                                obj_sf_trans.email = item.email;
                                obj_sf_trans.invoice_number = item.invoice_number;
                                obj_sf_trans.msisdn_number = item.msisdn_number;
                                obj_sf_trans.company_id = item.company_id;
                                obj_sf_trans.user_id = item.user_id;
                                obj_sf_trans.is_recharged = false;
                                obj_sf_trans.curr_bal = currCompanyBal;
                                _uow.staff_topup_trans_repo.Insert(obj_sf_trans);
                                _uow.Save();
                                item.is_processed = true;
                                if (item.topup_amount > 0)
                                {
                                    if (obj_each_company.credit_amount >= item.topup_amount && item.topup_amount >= obj_each_company.min_trans_amount && item.topup_amount <= obj_each_company.max_trans_amount)
                                    {
                                        // Deduct amount from company balance
                                        obj_each_company.credit_amount -= item.topup_amount;
                                        _uow.company_info_repo.Update(obj_each_company);
                                        _uow.Save();

                                        List<tbl_staff_topup_bundle> objStaff_Bundles = Staff_BundlesList.Where(x => x.staff_topup_id == item.id).ToList(); //_uow.staff_topup_bundle_repo.Get(filter: x => x.staff_topup_id == item.id && x.is_recharged == false && x.is_active == true && x.is_deleted == false).ToList();

                                        List<recharge_bundle_model> BundleIDs = new List<recharge_bundle_model>();

                                        StringBuilder sb = new StringBuilder("");

                                        decimal tot_bundle_Amt = 0;
                                        #region Append Staff Bundle IDs & Calculate total bundle amount
                                        if (objStaff_Bundles.Count > 0)
                                        {
                                            sb.Append(emailBudleTbl_Head.Value);

                                            foreach (var staff_bunlde in objStaff_Bundles)
                                            {
                                                tbl_bundle_plan objBundle = objBundle_plans.Where(x => x.bundle_id == staff_bunlde.bundle_id).FirstOrDefault();
                                                if (objBundle != null)
                                                {
                                                    staff_bunlde.bundle_amt = objBundle.price;
                                                    tot_bundle_Amt += objBundle.price;

                                                    recharge_bundle_model objbundle = new recharge_bundle_model();
                                                    objbundle.bundlePlanId = objBundle.bundle_id.ToString();
                                                    objbundle.bundleName = objBundle.bundle_name.ToString();
                                                    objbundle.bundlePrice = objBundle.price.ToString();
                                                    BundleIDs.Add(objbundle);

                                                    sb.Append(emailBudleTbl_Row.Value.Replace("#BundleName#", objBundle.bundle_name).Replace("#BundlePrice#", string.Format("{0:0.##}", objBundle.price)));
                                                }
                                            }

                                            sb.Append(emailBudleTbl_Foot.Value);
                                        }
                                        #endregion

                                        if (item.topup_amount >= tot_bundle_Amt)
                                        {
                                            //obj_sf_trans.trans_date = DateTime.Now;
                                            //obj_sf_trans.ip_address = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();
                                            //obj_sf_trans.amount = total_amt;
                                            //obj_sf_trans.email = item.email;
                                            //obj_sf_trans.invoice_number = item.invoice_number;
                                            //obj_sf_trans.msisdn_number = item.msisdn_number;
                                            //obj_sf_trans.company_id = item.company_id;
                                            //obj_sf_trans.user_id = item.user_id;                                

                                            //Actual Code 
                                            string ValidateMerch = validate_merchant(item.msisdn_number, item.topup_amount.ToString());
                                            if (!string.IsNullOrEmpty(ValidateMerch))
                                            {
                                                recharge_OP_model opRes = new recharge_OP_model();
                                                //Actual Code 
                                                string Recharge = recharge_msisdn(item.msisdn_number, item.topup_amount.ToString(), item.invoice_number, ValidateMerch, BundleIDs, item.id.ToString());
                                                opRes = JsonConvert.DeserializeObject<recharge_OP_model>(Recharge);

                                                ////testing purpose
                                                // opRes.resultCode = 0;
                                                if (opRes.resultCode == 0)
                                                {
                                                    item.is_recharged = true;

                                                    item.trans_desc = "Topup Success";
                                                    item.trans_date = DateTime.Now;
                                                    item.curr_bal = currCompanyBal;
                                                    //item.sales_person_id = obj_each_company.sales_person_id;
                                                    _uow.staff_topup_repo.Update(item);
                                                    _uow.Save();

                                                    obj_sf_trans.is_recharged = true;
                                                    obj_sf_trans.trans_desc = "Topup Success";
                                                    obj_sf_trans.trans_date = DateTime.Now;
                                                    obj_sf_trans.curr_bal = currCompanyBal;
                                                    _uow.staff_topup_trans_repo.Update(obj_sf_trans); //_uow.staff_topup_trans_repo.Insert(obj_sf_trans);
                                                    _uow.Save();

                                                    #region Bundle Log
                                                    if (objStaff_Bundles.Count > 0)
                                                    {
                                                        foreach (var staff_bunlde in objStaff_Bundles)
                                                        {
                                                            staff_bunlde.is_processed = true;
                                                            string bundleId = staff_bunlde.bundle_id.ToString();
                                                            ReponseBundleModel BundleRes = opRes.bundleResponse.Where(x => x.bundleId != null && x.bundleId.Trim() == bundleId).FirstOrDefault();

                                                            ////For Only Testing 
                                                            ////string BundleRes = "test";
                                                            //ReponseBundleModel BundleRes = new ReponseBundleModel();
                                                            //BundleRes.bundleId = "1232216982";

                                                            if (BundleRes != null)
                                                            {
                                                                ////For Only Testing 
                                                                //staff_bunlde.result_code = "405000000";
                                                                //staff_bunlde.result_desc = "test success";

                                                                staff_bunlde.result_code = BundleRes.resultCode;
                                                                staff_bunlde.result_desc = BundleRes.resultDesc;
                                                                if (!string.IsNullOrEmpty(BundleRes.resultCode) && BundleRes.resultCode.Trim() == "405000000")
                                                                {
                                                                    staff_bunlde.is_recharged = true;

                                                                }
                                                                else
                                                                {
                                                                    staff_bunlde.is_recharged = false;

                                                                    #region To Get Top up bundle failed Result(Details)

                                                                    sb_failedBundleDetails.Append(emailFdeatilsTbl_Row.Value.Replace("#msisdn#", item.msisdn_number).Replace("#BundleId#", Convert.ToString(staff_bunlde.bundle_id)).Replace("#BundleName#", staff_bunlde.bundle_name).Replace("#amount#", string.Format("{0:0.##}", staff_bunlde.bundle_amt)).Replace("#resultcode#", staff_bunlde.result_code).Replace("#Desc#", staff_bunlde.result_desc).Replace("#transDate#", DateTime.Now.ToString()));
                                                                    #endregion
                                                                }

                                                                //if (BundleRes.resultCode == "405000000")
                                                                //    staff_bunlde.is_recharged = true; 
                                                                //else
                                                                //    staff_bunlde.is_recharged = false;

                                                                _uow.staff_topup_bundle_repo.Update(staff_bunlde);
                                                                _uow.Save();
                                                            }
                                                        }
                                                    }
                                                    #endregion

                                                    //obj_each_company.credit_amount -= total_amt;
                                                    //_uow.company_info_repo.Update(obj_each_company);
                                                    //_uow.Save();                                                  

                                                    res = true;

                                                    if (!string.IsNullOrEmpty(item.email))
                                                    {
                                                        string sBody = emailBody.Value.Replace("#StaffName#", item.first_name + " " + item.last_name).Replace("#TotalAmount#", string.Format("{0:0.##}", item.topup_amount)).Replace("#TopupAmount#", string.Format("{0:0.##}", (item.topup_amount - tot_bundle_Amt)));
                                                        sBody = sBody.Replace("#MSISDN#", item.msisdn_number).Replace("#Date#", DateTime.Now.ToString("dd/MM/yyyy")).Replace("#CompanyName#", obj_each_company.company_name).Replace("#Bundles#", sb.ToString());

                                                        _util_repo.SendEmailMessage(item.email, emailsubj.Value.Trim(), sBody);
                                                    }
                                                }
                                                else
                                                {
                                                    item.trans_desc = opRes.desc;
                                                    item.result_code = opRes.resultCode.ToString();
                                                    res = false;

                                                    obj_sf_trans.trans_desc = "Failed and Result Code: " + opRes.resultCode.ToString();

                                                    #region To Get topup failed Details
                                                    sb_failedTopupDetails.Append(emailFTopupdeatilsTbl_Row.Value.Replace("#msisdn#", item.msisdn_number).Replace("#resultcode#", item.trans_desc).Replace("#transDate#", DateTime.Now.ToString()));
                                                    #endregion
                                                }
                                            }
                                            else
                                            {
                                                item.trans_desc = "Validate Merchant Failed";
                                                res = false;

                                                obj_sf_trans.trans_desc = "Validate Merchant Failed";
                                            }
                                        }
                                        else
                                        {
                                            item.trans_desc = "Bundle limit exceeded";
                                            item.isactive = false;
                                            res = false;

                                            obj_sf_trans.trans_desc = "Bundle limit exceeded";
                                        }
                                    }
                                    else
                                    {
                                        item.trans_desc = "Amount limit exceeded";
                                        item.isactive = false;
                                        res = false;

                                        obj_sf_trans.trans_desc = "Amount limit exceeded";
                                    }
                                }
                                else
                                {
                                    item.trans_desc = "Invalid amount";
                                    item.isactive = false;
                                    res = false;

                                    obj_sf_trans.trans_desc = "Invalid amount";
                                }

                                if (!res)
                                {
                                    item.is_recharged = false;
                                    item.trans_date = DateTime.Now;
                                    item.curr_bal = currCompanyBal;
                                    _uow.staff_topup_repo.Update(item);
                                    _uow.Save();

                                    obj_sf_trans.is_recharged = false;
                                    obj_sf_trans.trans_date = DateTime.Now;
                                    obj_sf_trans.curr_bal = currCompanyBal;
                                    _uow.staff_topup_trans_repo.Update(obj_sf_trans); //_uow.staff_topup_trans_repo.Insert(obj_sf_trans);
                                    _uow.Save();
                                }
                            }

                            #region To send email (on failed topup bundle details)
                            if (!string.IsNullOrEmpty(sb_failedBundleDetails.ToString()))
                            {
                                string sData = emailBody1.Value.Replace("#TableRow#", sb_failedBundleDetails.ToString());
                                string sSubj = emailsubj1.Value.Trim().Replace("#Date#", DateTime.Now.ToString("dd/MM/yyyy"));

                                _util_repo.SendEmailMessage(sendEmail_failed_details_To, sSubj, sData);
                            }
                            #endregion

                            #region To Send email (on failed topup details)
                            if (!string.IsNullOrEmpty(sb_failedTopupDetails.ToString()))
                            {
                                string sData = emailBody2.Value.Replace("#TableRow#", sb_failedTopupDetails.ToString());
                                string sSubj = emailsubj2.Value.Trim().Replace("#Date#", DateTime.Now.ToString("dd/MM/yyyy"));

                                _util_repo.SendEmailMessage(sendEmail_failed_details_To, sSubj, sData);
                            }
                            #endregion

                            bRes = true;

                            #endregion
                        }
                        else
                            sRes = "Process terminated due to insufficient balance! <br/> Current balance: K" + obj_company.credit_amount + ", <br/> Required balance: K " + required_amount;
                    }
                    else
                        sRes = "Process terminated!";
                }
                else
                    sRes = "No records found to process!";
            }
            catch (System.Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
            msg = sRes;

            return bRes;
        }

        //public bool SendStaffsTopupEmail(long company_id, out string msg)
        //{
        //    bool bRes = false;
        //    string sRes = "";

        //    try
        //    {
        //        List<tbl_staff_topup> objStaffs = new List<tbl_staff_topup>();
        //        objStaffs = _uow.staff_topup_repo.Get(filter: S => S.company_id == company_id && S.is_recharged == false && S.trans_desc == null && S.isactive == true && S.isdeleted == false).ToList();

        //        if (objStaffs.Count > 0)
        //        {
        //            List<tbl_bundle_plan> objBundle_plans = GetBundle_Plans();

        //            decimal required_amount = (from staffs in objStaffs
        //                                       select new
        //                                       {
        //                                           amount = staffs.topup_amount //Calculate_Amt(staffs, objBundle_plans)
        //                                       }).Sum(x => x.amount);

        //            tbl_company_info obj_company = _uow.company_info_repo.GetByID(company_id);
        //            if (obj_company != null)
        //            {
        //                if (required_amount <= obj_company.credit_amount && required_amount > 0)
        //                {

        //                    #region staff recharge proceed

        //                    //_util_repo.ErrorLog_Txt("Begin", "Data Loaded");
        //                    string EmailTempFilePath = ConfigurationSettings.AppSettings["EmailTempPath"];
        //                    XElement doc = XElement.Load(@EmailTempFilePath + "email_template.xml");
        //                    XElement emailsubj = doc.Element("ReminderEmail_Subj");
        //                    XElement emailBody = doc.Element("ReminderEmail_Body");
        //                    XElement emailBudleTbl_Head = doc.Element("ReminderEmail_BudleTbl_Head");
        //                    XElement emailBudleTbl_Row = doc.Element("ReminderEmail_BudleTbl_Row");
        //                    XElement emailBudleTbl_Foot = doc.Element("ReminderEmail_BudleTbl_Foot");

        //                    #region for failed bundle email list

        //                    string EmailTempFilePath1 = ConfigurationManager.AppSettings["EmailTempPath"];
        //                    XElement doc1 = XElement.Load(@EmailTempFilePath1 + "email_template.xml");
        //                    XElement emailsubj1 = doc1.Element("SendFailedTopupBundleEmail_Subj");
        //                    XElement emailBody1 = doc1.Element("SendFailedTopupBundleEmail_Body");
        //                    XElement emailFdeatilsTbl_Row = doc1.Element("SendFailedTopupBundleEmail_Row");


        //                    #endregion

        //                    #region for failed topup email list

        //                    string EmailTempFilePath2 = ConfigurationManager.AppSettings["EmailTempPath"];
        //                    XElement doc2 = XElement.Load(@EmailTempFilePath2 + "email_template.xml");
        //                    XElement emailsubj2 = doc2.Element("SendFailedTopupEmail_Subj");
        //                    XElement emailBody2 = doc2.Element("SendFailedTopupEmail_Body");
        //                    XElement emailFTopupdeatilsTbl_Row = doc2.Element("SendFailedTopupEmail_Row");

        //                    #endregion

        //                    List<tbl_staff_topup_bundle> Staff_BundlesList = (from s_topup in objStaffs.AsEnumerable()
        //                                                                      join s_bundle in _context.staff_topup_bundle.AsEnumerable() on s_topup.id equals s_bundle.staff_topup_id
        //                                                                      where s_bundle.is_deleted == false && s_bundle.is_active == true && s_bundle.is_processed == false
        //                                                                      select s_bundle).ToList();

        //                    StringBuilder sb1 = new StringBuilder();
        //                    StringBuilder failedtopupDetails_sb = new StringBuilder();


        //                    foreach (var item in objStaffs)
        //                    {
        //                        List<tbl_staff_topup_bundle> objStaff_Bundles = Staff_BundlesList.Where(x => x.staff_topup_id == item.id).ToList(); //_uow.staff_topup_bundle_repo.Get(filter: x => x.staff_topup_id == item.id && x.is_recharged == false && x.is_active == true && x.is_deleted == false).ToList();

        //                        List<recharge_bundle_model> BundleIDs = new List<recharge_bundle_model>();

        //                        tbl_staff_topup_trans obj_sf_trans = new tbl_staff_topup_trans();
        //                        bool res = false;

        //                        StringBuilder sb = new StringBuilder("");

        //                        decimal total_amt = item.topup_amount;

        //                        decimal tot_bundle_Amt = 0;
        //                        if (objStaff_Bundles.Count > 0)
        //                        {
        //                            sb.Append(emailBudleTbl_Head.Value);

        //                            foreach (var staff_bunlde in objStaff_Bundles)
        //                            {
        //                                tbl_bundle_plan objBundle = objBundle_plans.Where(x => x.bundle_id == staff_bunlde.bundle_id).FirstOrDefault();
        //                                if (objBundle != null)
        //                                {
        //                                    staff_bunlde.bundle_amt = objBundle.price;
        //                                    tot_bundle_Amt += objBundle.price;

        //                                    recharge_bundle_model objbundle = new recharge_bundle_model();
        //                                    objbundle.bundlePlanId = objBundle.bundle_id.ToString();
        //                                    objbundle.bundleName = objBundle.bundle_name.ToString();
        //                                    objbundle.bundlePrice = objBundle.price.ToString();
        //                                    BundleIDs.Add(objbundle);

        //                                    sb.Append(emailBudleTbl_Row.Value.Replace("#BundleName#", objBundle.bundle_name).Replace("#BundlePrice#", string.Format("{0:0.##}", objBundle.price)));
        //                                }
        //                            }

        //                            sb.Append(emailBudleTbl_Foot.Value);
        //                        }                               

        //                        obj_sf_trans.trans_date = DateTime.Now;
        //                        obj_sf_trans.ip_address = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();
        //                        obj_sf_trans.amount = total_amt;
        //                        obj_sf_trans.email = item.email;
        //                        obj_sf_trans.invoice_number = item.invoice_number;
        //                        obj_sf_trans.msisdn_number = item.msisdn_number;
        //                        obj_sf_trans.company_id = item.company_id;
        //                        obj_sf_trans.user_id = item.user_id;

        //                        tbl_company_info obj_each_company = _uow.company_info_repo.GetByID(company_id);
        //                        if (obj_each_company.credit_amount >= total_amt && item.topup_amount >= obj_each_company.min_trans_amount && item.topup_amount <= obj_each_company.max_trans_amount)
        //                        {
        //                            //Actual Code 
        //                            string ValidateMerch = validate_merchant(item.msisdn_number, item.topup_amount.ToString());
        //                            //For Only Testing 
        //                            //string ValidateMerch = "success";
        //                            if (ValidateMerch != "")
        //                            {
        //                                recharge_OP_model opRes = new recharge_OP_model();
        //                                //For Only Testing 
        //                                //opRes.resultCode = 0;

        //                                //Actual Code 
        //                                string Recharge = recharge_msisdn(item.msisdn_number, item.topup_amount.ToString(), item.invoice_number, ValidateMerch, BundleIDs);
        //                                opRes = JsonConvert.DeserializeObject<recharge_OP_model>(Recharge);
        //                                if (opRes.resultCode == 0)
        //                                {
        //                                    item.is_recharged = true;
        //                                    item.trans_desc = "Topup Success";
        //                                    item.trans_date = DateTime.Now;
        //                                    _uow.staff_topup_repo.Update(item);
        //                                    _uow.Save();

        //                                    if (objStaff_Bundles.Count > 0)
        //                                    {
        //                                        //foreach (var staff_bunlde in objStaff_Bundles)
        //                                        //{
        //                                        //    staff_bunlde.is_recharged = true;
        //                                        //    _uow.staff_topup_bundle_repo.Update(staff_bunlde);
        //                                        //    _uow.Save();
        //                                        //}

        //                                        foreach (var staff_bunlde in objStaff_Bundles)
        //                                        {      
        //                                            string bundleId = staff_bunlde.bundle_id.ToString();
        //                                            ReponseBundleModel BundleRes = opRes.bundleResponse.Where(x => x.bundleId != null && x.bundleId.Trim() == bundleId).FirstOrDefault();
        //                                            if (BundleRes != null)
        //                                            {
        //                                                staff_bunlde.result_code = BundleRes.resultCode;
        //                                                staff_bunlde.result_desc = BundleRes.resultDesc;                                                        
        //                                                if (!string.IsNullOrEmpty(BundleRes.resultCode) && BundleRes.resultCode.Trim() == "405000000")
        //                                                    staff_bunlde.is_recharged = true;
        //                                                else
        //                                                {
        //                                                    staff_bunlde.is_recharged = false;

        //                                                    #region To Get Top up bundle failed Result(Details)
        //                                                    // lstfailedtopup.Add(staff_bunlde);   
        //                                                    sb1.Append(emailFdeatilsTbl_Row.Value.Replace("#msisdn#", item.msisdn_number).Replace("#BundleId#", Convert.ToString(staff_bunlde.bundle_id)).Replace("#BundleName#", staff_bunlde.bundle_name).Replace("#amount#", string.Format("{0:0.##}", staff_bunlde.bundle_amt)).Replace("#resultcode#", staff_bunlde.result_code).Replace("#Desc#", staff_bunlde.result_desc).Replace("#transDate#", DateTime.Now.ToString()));
        //                                                    #endregion
        //                                                }

        //                                            }

        //                                            staff_bunlde.is_processed = true;
        //                                            _uow.staff_topup_bundle_repo.Update(staff_bunlde);
        //                                            _uow.Save();
        //                                        }
        //                                    }

        //                                    obj_each_company.credit_amount -= total_amt;
        //                                    _uow.company_info_repo.Update(obj_each_company);
        //                                    _uow.Save();

        //                                    obj_sf_trans.is_recharged = true;
        //                                    obj_sf_trans.trans_desc = "Topup Success";
        //                                    _uow.staff_topup_trans_repo.Insert(obj_sf_trans);
        //                                    _uow.Save();


        //                                    res = true;

        //                                    if (!string.IsNullOrEmpty(item.email))
        //                                    {
        //                                        string sBody = emailBody.Value.Replace("#StaffName#", item.first_name + " " + item.last_name).Replace("#TotalAmount#", string.Format("{0:0.##}", total_amt)).Replace("#TopupAmount#", string.Format("{0:0.##}", (total_amt - tot_bundle_Amt)));
        //                                        sBody = sBody.Replace("#MSISDN#", item.msisdn_number).Replace("#Date#", DateTime.Now.ToString("dd/MM/yyyy")).Replace("#CompanyName#", obj_each_company.company_name).Replace("#Bundles#", sb.ToString());

        //                                        _util_repo.SendEmailMessage(item.email, emailsubj.Value.Trim(), sBody);
        //                                    }
        //                                }
        //                                else
        //                                {
        //                                    item.trans_desc = "Result Code: " + opRes.resultCode.ToString();
        //                                    res = false;

        //                                    obj_sf_trans.trans_desc = "Failed and Result Code: " + opRes.resultCode.ToString();

        //                                    #region To Get topup failed Details
        //                                    failedtopupDetails_sb.Append(emailFTopupdeatilsTbl_Row.Value.Replace("#msisdn#", item.msisdn_number).Replace("#resultcode#", item.trans_desc).Replace("#transDate#", DateTime.Now.ToString()));
        //                                    #endregion

        //                                }
        //                            }
        //                            else
        //                            {
        //                                item.trans_desc = "Validate Merchant Failed";
        //                                res = false;

        //                                obj_sf_trans.trans_desc = "Validate Merchant Failed";
        //                            }
        //                        }
        //                        else
        //                        {
        //                            item.trans_desc = "Amount limit exceeded";
        //                            item.isactive = false;
        //                            res = false;

        //                            obj_sf_trans.trans_desc = "Amount limit exceeded";
        //                        }

        //                        if (!res)
        //                        {                                    
        //                            item.is_recharged = false;
        //                            item.trans_date = DateTime.Now;
        //                            _uow.staff_topup_repo.Update(item);
        //                            _uow.Save();

        //                            obj_sf_trans.is_recharged = false;
        //                            _uow.staff_topup_trans_repo.Insert(obj_sf_trans);
        //                            _uow.Save();
        //                        }
        //                    }

        //                    #region To send email (on failed topup bundle details)
        //                    if (!string.IsNullOrEmpty(sb1.ToString()))
        //                    {
        //                        string sData = emailBody1.Value.Replace("#TableRow#", sb1.ToString());
        //                        string sSubj = emailsubj1.Value.Trim().Replace("#Date#", DateTime.Now.ToString("dd/MM/yyyy"));

        //                        _util_repo.SendEmailMessage(sendEmail_failed_details_To, sSubj, sData);
        //                    }
        //                    #endregion

        //                    #region To Send email (on failed topup details)
        //                    if (!string.IsNullOrEmpty(failedtopupDetails_sb.ToString()))
        //                    {
        //                        string sData = emailBody2.Value.Replace("#TableRow#", failedtopupDetails_sb.ToString());
        //                        string sSubj = emailsubj2.Value.Trim().Replace("#Date#", DateTime.Now.ToString("dd/MM/yyyy"));

        //                        _util_repo.SendEmailMessage(sendEmail_failed_details_To, sSubj, sData);
        //                    }
        //                    #endregion

        //                    bRes = true;

        //                    #endregion

        //                }
        //                else
        //                    sRes = "Process terminated due to insufficient balance! <br/> Current balance: K" + obj_company.credit_amount + ", <br/> Required balance: K " + required_amount;
        //            }
        //            else
        //                sRes = "Process terminated!";

        //        }
        //        else
        //            sRes = "No records found to process!";


        //    }
        //    catch (System.Exception ex)
        //    {
        //        _util_repo.ErrorLog_Txt(ex);
        //    }
        //    msg = sRes;

        //    return bRes;
        //}

        private decimal Calculate_Amt(tbl_staff_topup objStaffs, List<BundlePlansModel> objBundles)
        {
            decimal req_amount = objStaffs.topup_amount;

            List<tbl_staff_topup_bundle> objStaff_Bundles = _uow.staff_topup_bundle_repo.Get(filter: x => x.staff_topup_id == objStaffs.id && x.is_recharged == false && x.is_active == true && x.is_deleted == false).ToList();
            if (objStaff_Bundles.Count > 0)
            {
                foreach (var item in objStaff_Bundles)
                {
                    req_amount += objBundles.Where(x => x.bundle_id == item.bundle_id).Select(b => b.price).FirstOrDefault();
                }
            }

            //if (objStaffs.bundle_id1 != null)
            //    req_amount += objBundles.Where(x => x.bundle_id == objStaffs.bundle_id1).Select(b => b.price).FirstOrDefault();

            //if (objStaffs.bundle_id2 != null)
            //    req_amount += objBundles.Where(x => x.bundle_id == objStaffs.bundle_id2).Select(b => b.price).FirstOrDefault();

            //if (objStaffs.bundle_id3 != null)
            //    req_amount += objBundles.Where(x => x.bundle_id == objStaffs.bundle_id3).Select(b => b.price).FirstOrDefault();

            return req_amount;
        }

        #endregion

        #region For Recharge APIs

        /// Validate Merchant using the config details
        public string validate_merchant(string msisdn, string amount)
        {
            string sRes = "";
            RechargeModel obj_regcharge = new RechargeModel();

            obj_regcharge.keycode = sf_keycode;
            obj_regcharge.username = sf_username;
            obj_regcharge.password = sf_plain_pwd;

            obj_regcharge.msisdn = msisdn;
            obj_regcharge.amount = amount;

            var data = JsonConvert.SerializeObject(obj_regcharge, Formatting.Indented);

            var resultmerchant = _util_repo.AES_JDEC(_easyRecharge.ValidateMerchant(sf_merchantid, _util_repo.AES_JENC(data)));

            if (!string.IsNullOrEmpty(resultmerchant))
                sRes = resultmerchant;

            return sRes;
        }


        /// Recharge MSISDN w.r.t amount
        public string recharge_msisdn(string msisdn, string amount, string invoice, string data, List<recharge_bundle_model> bundle_ids, string staff_topup_id)
        {
            string sRes = "";

            if (data != null)
            {
                var opRes = JsonConvert.DeserializeObject<validate_output_model>(data);

                if (opRes.resultcode == 0)
                {
                    recharge_msisdn_model obj_recharge = new recharge_msisdn_model();
                    obj_recharge.amount = amount;
                    obj_recharge.msisdn = msisdn;
                    obj_recharge.reference = opRes.reference;
                    obj_recharge.invoice = invoice;
                    obj_recharge.bundleIds = bundle_ids;
                    obj_recharge.ocsRef = staff_topup_id + "DTU" + opRes.reference;

                    string sRec_res = JsonConvert.SerializeObject(obj_recharge, Formatting.Indented);
                    string encry_data = _util_repo.AES_JENC(sRec_res);

                    var resultmerchant = _util_repo.AES_JDEC(_easyRecharge.RechargeMsisdn(sf_merchantid, encry_data));

                    if (!string.IsNullOrEmpty(resultmerchant))
                        sRes = resultmerchant;
                }
                else
                {
                    recharge_OP_model obj = new recharge_OP_model();
                    obj.resultCode = opRes.resultcode;
                    obj.reference = opRes.reference;
                    obj.desc = opRes.desc;
                    sRes = JsonConvert.SerializeObject(obj);
                    //sRes = data;
                }
            }

            return sRes;
        }

        #endregion

        #region insert company

        public bool insert_company(tbl_company_info obj_comp)
        {
            bool bRes = false;

            _uow.company_info_repo.Insert(obj_comp);
            _uow.Save();

            if (obj_comp.id > 0)
                bRes = true;

            return bRes;
        }


        #endregion

        #region update company

        public bool update_company(tbl_company_info obj_comp)
        {
            bool bRes = false;

            _uow.company_info_repo.Update(obj_comp);
            _uow.Save();

            bRes = true;

            return bRes;
        }


        #endregion

        #region companies

        public List<tbl_company_info> get_companies()
        {
            List<tbl_company_info> obj_company = new List<tbl_company_info>();

            obj_company = _uow.company_info_repo.Get(filter: c => c.isdeleted == false).ToList();


            return obj_company;
        }

        #endregion

        #region company by id

        public tbl_company_info get_activecompanyby_id(long id)
        {
            tbl_company_info obj_company = new tbl_company_info();

            obj_company = _uow.company_info_repo.Get(filter: m => m.id == id && m.isdeleted == false && m.isactive == true).FirstOrDefault();


            return obj_company;
        }

        public tbl_company_info get_company_id(long id)
        {
            tbl_company_info obj_company = new tbl_company_info();

            obj_company = _uow.company_info_repo.Get(filter: m => m.id == id && m.isdeleted == false).FirstOrDefault();


            return obj_company;
        }

        #endregion


        public bool check_email_exist(string email, int? id)
        {
            bool bRet = false;
            if (id == 0 || id == null)
            {
                var alreadyExist = _uow.company_info_repo.Get(filter: (m => m.email == email && m.isdeleted == false)).FirstOrDefault();
                if (alreadyExist == null)
                    bRet = true;
            }
            else if (id > 0)
            {
                var alreadyExist = _uow.company_info_repo.Get(filter: (m => m.email == email && m.isdeleted == false && m.id != id)).FirstOrDefault();
                if (alreadyExist == null)
                    bRet = true;
            }
            return bRet;
        }

        public bool insert_company_topup_temp(tbl_company_topup_temp obj_com)
        {
            bool bRes = false;

            _uow.company_topup_temp_repo.Insert(obj_com);
            _uow.Save();

            if (obj_com.id > 0)
                bRes = true;

            return bRes;
        }

        public bool insert_company_topup_trans(tbl_company_topup_report obj_com)
        {
            bool bRes = false;

            _uow.company_topup_report_repo.Insert(obj_com);
            _uow.Save();

            if (obj_com.id > 0)
                bRes = true;

            return bRes;
        }

        public List<tbl_company_topup_report> company_topup_report(long com_id)
        {
            List<tbl_company_topup_report> obj_comp = new List<tbl_company_topup_report>();

            obj_comp = _uow.company_topup_report_repo.Get(filter: m => m.company_id == com_id).ToList();

            return obj_comp;

        }

        public bool check_user_email_exist(string email, int? id)
        {
            bool bRet = false;

            tbl_user alreadyExist = new tbl_user();
            if (id == 0 || id == null)
            {
                //tbl_user alreadyExist = _uow.user_repo.Get(filter: (m => m.email == email && m.isdeleted == false)).FirstOrDefault();
                alreadyExist = (from u in _context.user
                                join c in _context.company_info on u.company_id equals c.id
                                where u.isactive == true && u.isdeleted == false && c.isactive == true && c.isdeleted == false
                                && u.email == email
                                select u).FirstOrDefault();
                if (alreadyExist == null)
                    bRet = true;
            }
            else if (id > 0)
            {
                alreadyExist = (from u in _context.user
                                join c in _context.company_info on u.company_id equals c.id
                                where u.isactive == true && u.isdeleted == false && c.isactive == true && c.isdeleted == false
                                && u.email == email && u.id != id
                                select u).FirstOrDefault();
                //tbl_user alreadyExist = _uow.user_repo.Get(filter: (m => m.email == email && m.isdeleted == false && m.id != id)).FirstOrDefault();
                if (alreadyExist == null)
                    bRet = true;
            }
            return bRet;
        }

        public tbl_user get_activeuserby_id(long id)
        {
            tbl_user obj_user = new tbl_user();

            obj_user = _uow.user_repo.Get(filter: c => c.isdeleted == false && c.id == id && c.isactive == true).FirstOrDefault();

            return obj_user;
        }

        public tbl_user get_userby_id(long id)
        {
            tbl_user obj_user = new tbl_user();

            obj_user = _uow.user_repo.Get(filter: c => c.isdeleted == false && c.id == id).FirstOrDefault();

            return obj_user;
        }

        public List<tbl_user> getuserby_compid(long company_id)
        {
            List<tbl_user> obj_user = new List<tbl_user>();

            obj_user = _uow.user_repo.Get(filter: c => c.company_id == company_id && c.isdeleted == false).ToList();

            return obj_user;
        }

        public bool insert_user(tbl_user obj_user)
        {
            bool bRes = false;

            _uow.user_repo.Insert(obj_user);
            _uow.Save();

            if (obj_user.id > 0)
                bRes = true;

            return bRes;
        }

        public bool update_user(tbl_user obj_user)
        {
            bool bRes = false;

            _uow.user_repo.Update(obj_user);
            _uow.Save();

            bRes = true;

            return bRes;
        }

        public tbl_staff_topup get_staff_topupby_userid(long user_id, string msisdn_no)
        {
            DateTime dt = DateTime.Now;
            tbl_staff_topup obj_staff = _uow.staff_topup_repo.Get(filter: S => S.isdeleted == false && S.user_id == user_id && S.msisdn_number == msisdn_no && S.created_on >= dt.Date).FirstOrDefault();

            return obj_staff;
        }

        public List<StaffTopupNewModel> get_staff_topup_by_userid(long user_id)
        {
            List<StaffTopupNewModel> obj_staff = new List<StaffTopupNewModel>();

            //obj_staff = (from staff_topup in _context.staff_topup.AsEnumerable()
            //             where staff_topup.isdeleted == false && staff_topup.user_id == user_id
            //             orderby staff_topup.created_on descending
            //             select new StaffsTopupModel
            //             {
            //                 staff_topup = staff_topup,
            //                 staff_topup_bundle = _context.staff_topup_bundle.Where(x => x.staff_topup_id == staff_topup.id && x.is_deleted == false).ToList()
            //             }).ToList();

            var dum_lst = (from staff_topup in _context.staff_topup
                           join comp in _context.company_info on staff_topup.company_id equals comp.id
                           join spl in _context.sales_person on staff_topup.sales_person_id equals spl.Id into spl2
                           from spl in spl2.DefaultIfEmpty()
                           join bl in _context.staff_topup_bundle on staff_topup.id equals bl.staff_topup_id into bl2
                           from bl in bl2.DefaultIfEmpty()
                           where staff_topup.isdeleted == false && comp.isdeleted == false && staff_topup.user_id == user_id

                           group new { staff_topup, spl, comp, bl } by new
                           {
                               staff_topup.id,
                               staff_topup.first_name,
                               staff_topup.last_name,
                               staff_topup.msisdn_number,
                               staff_topup.topup_amount,
                               staff_topup.invoice_number,
                               staff_topup.is_recharged,
                               staff_topup.isactive,
                               staff_topup.isdeleted,
                               staff_topup.trans_desc,
                               staff_topup.trans_date,
                               staff_topup.sales_person_id,
                               salesF = spl.first_name,
                               salesL = spl.last_name,
                               comp.company_name,
                               staff_topup.is_processed,
                               staff_topup.email,
                           } into g
                           orderby g.Key.id descending
                           select new StaffTopupNewModel
                           {
                               staff_topup_id = g.Key.id,
                               company_name = g.Key.company_name,
                               first_name = g.Key.first_name,
                               last_name = g.Key.last_name,
                               msisdn_number = g.Key.msisdn_number,
                               topup_amount = g.Key.topup_amount,
                               invoice_number = g.Key.invoice_number,
                               is_recharged = g.Key.is_recharged,
                               isactive = g.Key.isactive,
                               isdeleted = g.Key.isdeleted,
                               trans_desc = g.Key.trans_desc,
                               trans_date = g.Key.trans_date,
                               sales_person_id = g.Key.sales_person_id,
                               email = g.Key.email,
                               is_processed = g.Key.is_processed,
                               sales_person = g.Key.sales_person_id != null ? g.Key.salesF == null ? "" : g.Key.salesL == null ? "" : g.Key.salesF + " " + g.Key.salesL : ""
                           }).ToList();
            obj_staff = dum_lst;
            var bundle_lst = _context.staff_topup_bundle.Select(x => new { x.staff_topup_id, x.bundle_name, x.bundle_amt }).ToList();
            obj_staff.ForEach(x => x.bundle_name = String.Join(";", bundle_lst.AsEnumerable().Where(c => c.staff_topup_id == x.staff_topup_id).Select(v => v.bundle_name + " : $" + v.bundle_amt.ToString() + "(" + (x.is_recharged == true ? "Success" : "Failed") + ")")));
            obj_staff.ForEach(z => z.balance = bundle_lst.Where(c => c.staff_topup_id == z.staff_topup_id).Sum(x => x.bundle_amt));


            return obj_staff;
        }

        public tbl_staff_topup get_staff_topupby_id(long id)
        {
            tbl_staff_topup obj_staff = new tbl_staff_topup();

            obj_staff = _uow.staff_topup_repo.Get(filter: S => S.isdeleted == false && S.id == id).FirstOrDefault();

            return obj_staff;
        }

        public bool insert_staff_topup(tbl_staff_topup topuplist, List<long> bundle_ids, List<tbl_bundle_plan> objBundle_plans)
        {
            bool bRes = false;

            _uow.staff_topup_repo.Insert(topuplist);
            _uow.Save();

            if (topuplist.id > 0)
            {
                if (bundle_ids != null && bundle_ids.Count > 0)
                {
                    foreach (long bundle_id in bundle_ids)
                    {
                        tbl_bundle_plan objBundle = objBundle_plans.Where(x => x.bundle_id == bundle_id).FirstOrDefault();
                        if (objBundle != null)
                        {
                            tbl_staff_topup_bundle obj = new tbl_staff_topup_bundle();
                            obj.id = 0;
                            obj.staff_topup_id = topuplist.id;
                            obj.bundle_id = objBundle.bundle_id;
                            obj.bundle_name = objBundle.bundle_name;
                            obj.bundle_amt = objBundle.price;
                            obj.is_recharged = false;
                            obj.is_active = true;
                            obj.is_deleted = false;
                            obj.is_processed = false;

                            _uow.staff_topup_bundle_repo.Insert(obj);
                            _uow.Save();
                        }
                    }
                }
                bRes = true;
            }


            return bRes;
        }

        public bool update_staff_topup(tbl_staff_topup topuplist)
        {
            bool bRes = false;

            _uow.staff_topup_repo.Update(topuplist);
            _uow.Save();
            bRes = true;


            return bRes;
        }

        public List<tbl_staff_topup> get_staff_topupby_comp(long comp_id)
        {
            List<tbl_staff_topup> obj_staff = new List<tbl_staff_topup>();

            obj_staff = _uow.staff_topup_repo.Get(filter: S => S.isdeleted == false && S.company_id == comp_id).ToList();

            return obj_staff;
        }

        public List<StaffTopupNewModel> get_staff_topupby_company(string sdate, string edate, long comp_id = 0, long user_id = 0)
        {
            List<StaffTopupNewModel> obj_staff = new List<StaffTopupNewModel>();

            //obj_staff = (from staff_topup in _context.staff_topup.AsEnumerable()
            //             where staff_topup.isdeleted == false && staff_topup.company_id == comp_id
            //             orderby staff_topup.created_on descending
            //             select new StaffsTopupModel
            //             {
            //                 staff_topup = staff_topup,
            //                 staff_topup_bundle = _context.staff_topup_bundle.Where(x => x.staff_topup_id == staff_topup.id && x.is_deleted == false).ToList()
            //             }).ToList();



            var query = (from staff_topup in _context.staff_topup
                         join comp in _context.company_info on staff_topup.company_id equals comp.id
                         join spl in _context.sales_person on staff_topup.sales_person_id equals spl.Id into spl2
                         from spl in spl2.DefaultIfEmpty()
                         join bl in _context.staff_topup_bundle on staff_topup.id equals bl.staff_topup_id into bl2
                         from bl in bl2.DefaultIfEmpty()
                         where staff_topup.isdeleted == false && comp.isdeleted == false
                         select new
                         {
                             staff_topup = staff_topup,
                             comp = comp,
                             spl = spl,
                             bl = bl
                         });

            sdate = string.IsNullOrEmpty(sdate) ? DateTime.Now.Date.ToString("dd/MM/yyyy") : sdate;
            edate = string.IsNullOrEmpty(edate) ? DateTime.Now.Date.ToString("dd/MM/yyyy") : edate;

            DateTime dtstart = DateTime.ParseExact(sdate, @"dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            DateTime dtend = DateTime.ParseExact(edate, @"dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            dtend = dtend.AddDays(1);
            if (comp_id > 0)
                query = query.Where(x => x.staff_topup.company_id == comp_id);
            if (user_id > 0)
                query = query.Where(x => x.staff_topup.user_id == user_id);
            if (dtstart!=null && dtend!=null)
            {
                query = query.Where(x => (x.staff_topup.created_on >= dtstart && x.staff_topup.created_on < dtend) || (x.staff_topup.trans_date.Value >= dtstart && x.staff_topup.trans_date.Value < dtend));
            }
            else
            {
                dtstart = Convert.ToDateTime(sdate).Date;
                dtend = Convert.ToDateTime(edate).AddDays(1).Date;
                query = query.Where(x => x.staff_topup.trans_date.Value >= dtstart && x.staff_topup.trans_date.Value < dtend);
            }

            var qres = query.ToList();

            obj_staff = (from r in qres

                         group new { r.staff_topup, r.spl, r.comp, r.bl } by new
                         {
                             r.staff_topup.id,
                             r.staff_topup.first_name,
                             r.staff_topup.last_name,
                             r.staff_topup.msisdn_number,
                             r.staff_topup.topup_amount,
                             r.staff_topup.invoice_number,
                             r.staff_topup.is_recharged,
                             r.staff_topup.isactive,
                             r.staff_topup.isdeleted,
                             r.staff_topup.trans_desc,
                             r.staff_topup.trans_date,
                             r.staff_topup.sales_person_id,
                             //r.spl.first_name,
                             //r.spl.last_name,
                             r.comp.company_name,
                             r.staff_topup.is_processed,
                             r.staff_topup.email,
                         } into g
                         orderby g.Key.id descending
                         select new StaffTopupNewModel
                         {
                             staff_topup_id = g.Key.id,
                             company_name = !string.IsNullOrEmpty(g.Key.company_name)? g.Key.company_name:"",
                             first_name = !string.IsNullOrEmpty(g.Key.first_name)? g.Key.first_name:"",
                             last_name = !string.IsNullOrEmpty(g.Key.last_name)? g.Key.last_name:"",
                             msisdn_number = !string.IsNullOrEmpty(g.Key.msisdn_number)? g.Key.msisdn_number:"",
                             topup_amount = g.Key.topup_amount!=null? g.Key.topup_amount:0,
                             invoice_number = !string.IsNullOrEmpty(g.Key.invoice_number)? g.Key.invoice_number:"",
                             is_recharged = g.Key.is_recharged==null? false: g.Key.is_recharged,
                             isactive = g.Key.isactive == null ? false : g.Key.isactive,
                             isdeleted = g.Key.isdeleted == null ? false : g.Key.isdeleted,
                             trans_desc = !string.IsNullOrEmpty(g.Key.trans_desc)? g.Key.trans_desc:"",
                             trans_date = g.Key.trans_date,
                             sales_person_id = g.Key.sales_person_id!=null ? g.Key.sales_person_id:0,
                             email = !string.IsNullOrEmpty(g.Key.email)? g.Key.email:"",
                             is_processed = g.Key.is_processed,
                             //sales_person=""
                             //sales_person = g.Key.sales_person_id != null ? g.Key.salesF == null ? "" : g.Key.salesL == null ? "" : g.Key.salesF + " " + g.Key.salesL : ""
                         }).ToList();
            var sp_lst = qres.Where(s => s.spl != null && s.staff_topup.sales_person_id != null).Select(x => new { name = x.spl.first_name + " " + x.spl.last_name, x.spl.Id }).ToList();
            var bundle_lst = qres.Where(b => b.bl != null).Select(x => new { x.bl.staff_topup_id, x.bl.bundle_name, x.bl.bundle_amt, x.bl.is_processed }).ToList();
            obj_staff.ForEach(x => x.bundle_name = String.Join(";", bundle_lst.AsEnumerable().Where(c => c.staff_topup_id == x.staff_topup_id).Select(v => v.bundle_name + " : $" + v.bundle_amt.ToString() + "(" + (x.is_recharged == true ? "Success" : "Pending") + ")")));
            obj_staff.ForEach(z => z.balance = bundle_lst.Where(c => c.staff_topup_id == z.staff_topup_id).Sum(x => x.bundle_amt!=null? x.bundle_amt:0));
            obj_staff.ForEach(y => y.sales_person = y.sales_person_id != null ? (sp_lst.Where(x => x.Id == y.sales_person_id).FirstOrDefault() != null ? sp_lst.Where(x => x.Id == y.sales_person_id).FirstOrDefault().name : "NA") : "NA");


            return obj_staff;
        }

        public List<tbl_staff_topup_trans> get_staff_topup_transby_comp(DateTime sTransFrom, DateTime sTransTo, long comp_id)
        {
            List<tbl_staff_topup_trans> obj_staff = new List<tbl_staff_topup_trans>();
            DateTime dtend = sTransTo.AddDays(1);
            obj_staff = _uow.staff_topup_trans_repo.Get(filter: S => S.company_id == comp_id && S.trans_date >= sTransFrom && S.trans_date < dtend).OrderByDescending(x => x.trans_date).ToList();

            return obj_staff;
        }

        public List<tbl_payment_type> get_paytype_list()
        {
            List<tbl_payment_type> obj_pay = new List<tbl_payment_type>();

            obj_pay = _uow.payment_type_repo.Get(filter: m => m.is_active == true).ToList();

            return obj_pay;
        }

        #region GetBundle_Plans

        //public List<BundlePlansModel> GetBundle_Plans()
        //{
        //    List<BundlePlansModel> BundlePlansList = new List<BundlePlansModel>();

        //    object[] objBundlePlans = _selfcare.getBundlePlans(_util_repo.AES_ENC("1"), "1", false, true);
        //    foreach (beLib.beRef.AbstractBundles items in objBundlePlans)
        //    {
        //        if (items.id != null && items.price != null)
        //        {
        //            BundlePlansModel obj = new BundlePlansModel();
        //            obj.bundle_id = (long)items.id;
        //            obj.bundle_name = items.planName;
        //            obj.price = Convert.ToDecimal(items.price);

        //            BundlePlansList.Add(obj);
        //        }
        //    }

        //    return BundlePlansList;
        //}

        public List<tbl_bundle_plan> GetBundle_Plans()
        {
            List<tbl_bundle_plan> objBundlePlan = _uow.bundle_plan_repo.Get(filter: x => x.is_active == true && x.is_deleted == false).ToList();
            objBundlePlan.ForEach(x => x.description = x.bundle_name + " | $" + x.price + " | Days:" + x.validity);

            return objBundlePlan;
        }
        #endregion

        public List<tbl_staff_topup_bundle> GetStaffs_Bunlde_Topup(long staff_topup_id)
        {
            List<tbl_staff_topup_bundle> objStaff_Bundles = _uow.staff_topup_bundle_repo.Get(filter: x => x.staff_topup_id == staff_topup_id && x.is_recharged == false && x.is_active == true && x.is_deleted == false).ToList();

            return objStaff_Bundles;
        }

        #region get_staff_topup

        #region oldcode
        //public List<StaffsTopupModel> get_staff_topup()
        //{
        //    List<StaffsTopupModel> obj_staff = new List<StaffsTopupModel>();

        //    obj_staff = (from staff_topup in _context.staff_topup.AsEnumerable()
        //                 join comp in _context.company_info.AsEnumerable() on staff_topup.company_id equals comp.id
        //                 join sp in _context.sales_person on staff_topup.sales_person_id equals sp.Id into sp2
        //                 from sp in sp2.DefaultIfEmpty()
        //                 where staff_topup.isdeleted == false && comp.isdeleted == false
        //                 orderby staff_topup.created_on descending
        //                 select new StaffsTopupModel
        //                 {
        //                     company_name = comp.company_name,
        //                     staff_topup = staff_topup,
        //                     staff_topup_bundle = _context.staff_topup_bundle.Where(x => x.staff_topup_id == staff_topup.id && x.is_deleted == false).ToList(),
        //                     sales_person = staff_topup.sales_person_id != null ? sp.first_name + " " + sp.last_name : ""
        //                 }).ToList();

        //    return obj_staff;
        //}
        //public List<StaffsTopupModel> get_staff_topup(long comp_id)
        //{
        //    List<StaffsTopupModel> obj_staff = new List<StaffsTopupModel>();

        //    obj_staff = (from staff_topup in _context.staff_topup.AsEnumerable()
        //                 join comp in _context.company_info.AsEnumerable() on staff_topup.company_id equals comp.id
        //                 join sp in _context.sales_person on staff_topup.sales_person_id equals sp.Id into sp2
        //                 from sp in sp2.DefaultIfEmpty()
        //                 where staff_topup.isdeleted == false && comp.isdeleted == false && staff_topup.company_id == comp_id
        //                 orderby staff_topup.created_on descending
        //                 select new StaffsTopupModel
        //                 {
        //                     company_name = comp.company_name,
        //                     staff_topup = staff_topup,
        //                     staff_topup_bundle = _context.staff_topup_bundle.Where(x => x.staff_topup_id == staff_topup.id && x.is_deleted == false).ToList(),
        //                     sales_person = staff_topup.sales_person_id != null ? sp.first_name + " " + sp.last_name : ""
        //                 }).ToList();

        //    return obj_staff;
        //}
        #endregion
        public List<StaffTopupNewModel> get_staff_topup()
        {
            List<StaffTopupNewModel> obj_staff = new List<StaffTopupNewModel>();

            var dum_lst = (from staff_topup in _context.staff_topup
                           join comp in _context.company_info on staff_topup.company_id equals comp.id
                           join spl in _context.sales_person on staff_topup.sales_person_id equals spl.Id into spl2
                           from spl in spl2.DefaultIfEmpty()
                           join bl in _context.staff_topup_bundle on staff_topup.id equals bl.staff_topup_id into bl2
                           from bl in bl2.DefaultIfEmpty()
                           where staff_topup.isdeleted == false && comp.isdeleted == false

                           group new { staff_topup, spl, comp, bl } by new
                           {
                               staff_topup.id,
                               staff_topup.first_name,
                               staff_topup.last_name,
                               staff_topup.msisdn_number,
                               staff_topup.topup_amount,
                               staff_topup.invoice_number,
                               staff_topup.is_recharged,
                               staff_topup.isactive,
                               staff_topup.isdeleted,
                               staff_topup.trans_desc,
                               staff_topup.trans_date,
                               staff_topup.sales_person_id,
                               salesF = spl.first_name,
                               salesL = spl.last_name,
                               comp.company_name,
                           } into g
                           orderby g.Key.id descending
                           select new StaffTopupNewModel
                           {
                               staff_topup_id = g.Key.id,
                               company_name = g.Key.company_name,
                               first_name = g.Key.first_name,
                               last_name = g.Key.last_name,
                               msisdn_number = g.Key.msisdn_number,
                               topup_amount = g.Key.topup_amount,
                               invoice_number = g.Key.invoice_number,
                               is_recharged = g.Key.is_recharged,
                               isactive = g.Key.isactive,
                               isdeleted = g.Key.isdeleted,
                               trans_desc = g.Key.trans_desc,
                               trans_date = g.Key.trans_date,
                               sales_person_id = g.Key.sales_person_id,
                               sales_person = g.Key.sales_person_id != null ? g.Key.salesF == null ? "" : g.Key.salesL == null ? "" : g.Key.salesF + " " + g.Key.salesL : ""
                           }).ToList();
            obj_staff = dum_lst;
            var bundle_lst = _context.staff_topup_bundle.Select(x => new { x.staff_topup_id, x.bundle_name, x.bundle_amt }).ToList();
            obj_staff.ForEach(x => x.bundle_name = String.Join(";", bundle_lst.AsEnumerable().Where(c => c.staff_topup_id == x.staff_topup_id).Select(v => v.bundle_name + " : $" + v.bundle_amt.ToString())));
            obj_staff.ForEach(z => z.balance = bundle_lst.Where(c => c.staff_topup_id == z.staff_topup_id).Sum(x => x.bundle_amt));


            return obj_staff;
        }
        public List<StaffTopupNewModel> get_staff_topup(long comp_id)
        {
            List<StaffTopupNewModel> obj_staff = new List<StaffTopupNewModel>();

            var dum_lst = (from staff_topup in _context.staff_topup
                           join comp in _context.company_info on staff_topup.company_id equals comp.id
                           join spl in _context.sales_person on staff_topup.sales_person_id equals spl.Id into spl2
                           from spl in spl2.DefaultIfEmpty()
                           join bl in _context.staff_topup_bundle on staff_topup.id equals bl.staff_topup_id into bl2
                           from bl in bl2.DefaultIfEmpty()
                           where staff_topup.isdeleted == false && comp.isdeleted == false && staff_topup.company_id == comp_id

                           group new { staff_topup, spl, comp, bl } by new
                           {
                               staff_topup.id,
                               staff_topup.first_name,
                               staff_topup.last_name,
                               staff_topup.msisdn_number,
                               staff_topup.topup_amount,
                               staff_topup.invoice_number,
                               staff_topup.is_recharged,
                               staff_topup.isactive,
                               staff_topup.isdeleted,
                               staff_topup.trans_desc,
                               staff_topup.trans_date,
                               staff_topup.sales_person_id,
                               salesF = spl.first_name,
                               salesL = spl.last_name,
                               comp.company_name,
                           } into g
                           orderby g.Key.id descending
                           select new StaffTopupNewModel
                           {
                               staff_topup_id = g.Key.id,
                               company_name = g.Key.company_name,
                               first_name = g.Key.first_name,
                               last_name = g.Key.last_name,
                               msisdn_number = g.Key.msisdn_number,
                               topup_amount = g.Key.topup_amount,
                               invoice_number = g.Key.invoice_number,
                               is_recharged = g.Key.is_recharged,
                               isactive = g.Key.isactive,
                               isdeleted = g.Key.isdeleted,
                               trans_desc = g.Key.trans_desc,
                               trans_date = g.Key.trans_date,
                               sales_person_id = g.Key.sales_person_id,
                               sales_person = g.Key.sales_person_id != null ? g.Key.salesF == null ? "" : g.Key.salesL == null ? "" : g.Key.salesF + " " + g.Key.salesL : ""
                           }).ToList();
            obj_staff = dum_lst;
            var bundle_lst = _context.staff_topup_bundle.Select(x => new { x.staff_topup_id, x.bundle_name, x.bundle_amt }).ToList();
            obj_staff.ForEach(x => x.bundle_name = String.Join(";", bundle_lst.AsEnumerable().Where(c => c.staff_topup_id == x.staff_topup_id).Select(v => v.bundle_name + " : $" + v.bundle_amt.ToString())));
            obj_staff.ForEach(z => z.balance = bundle_lst.Where(c => c.staff_topup_id == z.staff_topup_id).Sum(x => x.bundle_amt));

            return obj_staff;
        }


        #endregion

        #region GetApprove_Status

        public List<tbl_approve_status> GetApprove_Status()
        {
            List<tbl_approve_status> objApproveStatus = _uow.approve_status_repo.Get().ToList();

            return objApproveStatus;
        }
        #endregion

        #region GetCompanies

        public List<CompanyModel> GetCompanies()
        {
            List<CompanyModel> obj_company = new List<CompanyModel>();
            try
            {

                obj_company = (from C in _context.company_info
                                                  join SP in _context.sales_person on C.sales_person_id equals SP.Id into SP2
                                                  from SP in SP2.DefaultIfEmpty()
                                                  where C.isdeleted == false
                                                  select new CompanyModel
                                                  {
                                                      company = C,
                                                      sales_person = SP != null ? SP.first_name + " " + SP.last_name : ""
                                                  }).ToList();
            }catch(Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }

            return obj_company;
        }

        #endregion

        #region Bundle Plan detials

        public List<BundlePlanDetailsModel> get_bundleplanDetails()
        {
            List<BundlePlanDetailsModel> obj_bundlePlan = new List<BundlePlanDetailsModel>();

            var data = _uow.bundle_plan_repo.Get(filter: x => x.is_active == true && x.is_deleted == false).ToList();

            obj_bundlePlan = (from b in data
                              select new BundlePlanDetailsModel
                              {
                                  bundle_id = b.bundle_id,
                                  bundle_name = b.bundle_name
                              }).ToList();

            return obj_bundlePlan;
        }

        #endregion

        #region for Master Account Expiry Report

        public List<MasterAccExpriyReportModel> masterAccExpiry()
        {
            List<MasterAccExpriyReportModel> obj_ExpList = new List<MasterAccExpriyReportModel>();

            obj_ExpList = (from c in _context.company_info
                           where c.isdeleted == false
                           select new MasterAccExpriyReportModel
                           {
                               company_name = c.company_name,
                               contact_person_name = c.contact_person_name,
                               email = c.email,
                               mobile_number = c.mobile_number,
                               ContractExpiryDate = c.contract_exp_date,
                               status = c.isactive
                           }).ToList();

            return obj_ExpList;
        }

        #endregion

        #region for Master Account Low Balance Report

        public List<MasterAccLowBalanceModel> get_MasterAccLowBalanceDetails()
        {
            List<MasterAccLowBalanceModel> obj_LowBalList = new List<MasterAccLowBalanceModel>();

            obj_LowBalList = (from c in _context.company_info
                              join s in _context.sales_person on c.sales_person_id equals s.Id into tempComp
                              from s in tempComp.DefaultIfEmpty()
                              where c.isactive == true && c.isdeleted == false
                              select new MasterAccLowBalanceModel
                              {
                                  company_name = c.company_name,
                                  contact_person_name = c.contact_person_name,
                                  email = c.email,
                                  mobile_number = c.mobile_number,
                                  available_amount = c.credit_amount,
                                  required_amount = c.min_mthly_spend,
                                  outstanding_amount = ((c.min_mthly_spend) - (c.credit_amount)),
                                  salesPerson = (c.sales_person_id != null) ? ((s.last_name != null) ? s.first_name + " " + s.last_name : s.first_name) : "NA",
                                  salesPersonID = c.sales_person_id
                              }).Where(x => x.available_amount < x.required_amount).ToList();

            return obj_LowBalList;
        }

        #endregion

        #region create invoice

        public InvoiceModel getInvoice(InvoiceModel obj_invoice_details, long company_id)
        {
            InvoiceModel _objIM = new InvoiceModel();

            try
            {




                if (company_id > 0)
                {
                    tbl_si_ccp_invoice inv_obj = new tbl_si_ccp_invoice();

                    inv_obj = invoice_number_generate();


                    //inv_obj.Gst_percentage = Convert.ToDecimal(_utils_repo.CalcGST((double)mas_obj.plan_amount));//  Convert.ToDecimal(gstvalue);
                    //inv_obj.total_val = inv_obj.total_val;// - gstvalue;

                    //inv_obj.Gst_total_percentage = Math.Round((decimal)(inv_obj.Gst_percentage * mem_obj.Count()), 2, MidpointRounding.AwayFromZero);

                    if (inv_obj != null)
                    {
                        inv_obj.company_id = company_id;
                        inv_obj.total_quantity = obj_invoice_details.invoicedetail.total_quantity;
                        inv_obj.unit_price = obj_invoice_details.invoicedetail.unit_price;

                        // double gstvalue = _util_repo.CalcGST(Convert.ToDouble(inv_obj.total_amt) / 1.1);

                        decimal gstvalue = Convert.ToDecimal(_util_repo.CalcGST((double)obj_invoice_details.invoicedetail.total_amt)); //  Convert.ToDecimal(gstvalue);

                        inv_obj.total_gst_val = Convert.ToDecimal(gstvalue);
                        inv_obj.item_val = obj_invoice_details.invoicedetail.total_amt;
                        inv_obj.total_amt = Convert.ToDecimal(obj_invoice_details.invoicedetail.total_amt + gstvalue);
                        inv_obj.created_on = DateTime.Now;

                        _uow.ccp_invoice_Repo.Insert(inv_obj);
                        _uow.Save();
                    }



                    tbl_company_info obj_cmpy_detials = new tbl_company_info();
                    obj_cmpy_detials = _uow.company_info_repo.Get(filter: x => x.id == company_id).FirstOrDefault();

                    _objIM.invoicedetail = inv_obj;
                    _objIM.id = obj_cmpy_detials.id;
                    _objIM.company_name = obj_cmpy_detials.company_name;
                    _objIM.email = obj_cmpy_detials.email;
                    _objIM.contact_person_name = obj_cmpy_detials.contact_person_name;
                    _objIM.phone_number = obj_cmpy_detials.phone_number;
                    _objIM.mobile_number = obj_cmpy_detials.mobile_number;


                    string filepath = generate_invoice_pdf(_objIM);

                    string EmailTempFilePath = ConfigurationManager.AppSettings["EmailTempPath"];
                    XElement doc = XElement.Load(@EmailTempFilePath + "email_template.xml");
                    XElement emailsubj = doc.Element("Invoice_Subj");
                    XElement emailBody = doc.Element("Invoice_Body");
                    string sBody = emailBody.Value.Replace("#Date_time#", DateTime.Now.ToString("dd/MM/yyyy")).Replace("#companyname#", _objIM.company_name).Replace("#invoicetxt#", _objIM.invoicedetail.invoice_txt);
                    sBody = sBody.Replace("#master_no#", _objIM.mobile_number).Replace("#contact_name#", _objIM.contact_person_name).Replace("#contactno#", _objIM.phone_number).Replace("#mas_emailid#", _objIM.email);
                    sBody = sBody.Replace("#plan_price#", _objIM.invoicedetail.unit_price.ToString()).Replace("#total_qnty#", _objIM.invoicedetail.total_quantity.ToString());
                    sBody = sBody.Replace("#tot_item_amt#", _objIM.invoicedetail.item_val.Value.ToString("0.00")).Replace("#final_tot_amt#", _objIM.invoicedetail.total_amt.Value.ToString("0.00"));
                    sBody = sBody.Replace("#gst_percentage#", _objIM.invoicedetail.total_gst_val.Value.ToString("0.00"));

                    if (_util_repo.IsValidEmailAddress(_objIM.email))
                    {
                        bool bRes = false;
                        _util_repo.SendEmailMessage(_objIM.email + "," + SendInvoiceMailTo, emailsubj.Value.Trim(), sBody, out bRes, filepath);
                    }


                }
            }

            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }


            return _objIM;
        }


        #endregion

        #region to genereate and save pdf

        private string generate_invoice_pdf(InvoiceModel obj_inv)
        {
            string filepath = "", filename = "";
            if (obj_inv != null)
            {
                filename = obj_inv.id.ToString() + "_" + DateTime.Now.Month + "_" + DateTime.Now.Year + "_" + obj_inv.invoicedetail.invoice_txt.ToString() + ".pdf";
                filepath = invoice_path + filename;
                if (!File.Exists(filepath))
                {
                    #region generate PDF

                    Document document = new Document();
                    PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filepath, FileMode.Create));

                    document.Open();

                    Font font_size = new Font(Font.HELVETICA, 9f, Font.NORMAL);
                    Font font_size_small = new Font(Font.HELVETICA, 7f, Font.NORMAL);
                    Font font_size_small_bold = new Font(Font.HELVETICA, 7f, Font.BOLD);
                    Font cat_name_font_size = new Font(Font.HELVETICA, 9f, Font.BOLD);
                    Font header_font_unbold = new Font(Font.HELVETICA, 11f, Font.NORMAL);
                    Font header_font = new Font(Font.HELVETICA, 11f, Font.BOLD);

                    int gen_table_cols = 5;
                    PdfPCell cell;


                    #region Tables
                    PdfPTable empty_row = new PdfPTable(gen_table_cols);
                    empty_row.WidthPercentage = 100;
                    empty_row.SetWidths(new int[] { 25, 20, 15, 15, 15 });

                    PdfPTable header = new PdfPTable(gen_table_cols);
                    header.WidthPercentage = 100;
                    header.SetWidths(new int[] { 25, 20, 15, 20, 25 });

                    PdfPTable header1 = new PdfPTable(gen_table_cols);
                    header1.WidthPercentage = 100;
                    header1.SetWidths(new int[] { 20, 20, 15, 15, 15 });

                    PdfPTable header2 = new PdfPTable(gen_table_cols);
                    header2.WidthPercentage = 100;
                    header2.SetWidths(new int[] { 25, 20, 15, 15, 15 });

                    PdfPTable invoice_info = new PdfPTable(gen_table_cols);
                    invoice_info.WidthPercentage = 100;
                    invoice_info.SetWidths(new int[] { 25, 20, 15, 15, 15 });

                    PdfPTable invoice_table = new PdfPTable(gen_table_cols);
                    invoice_table.WidthPercentage = 100;
                    invoice_table.SetWidths(new int[] { 25, 20, 15, 15, 15 });

                    PdfPTable invoice_table_footer = new PdfPTable(gen_table_cols);
                    invoice_table_footer.WidthPercentage = 100;
                    invoice_table_footer.SetWidths(new int[] { 25, 20, 15, 15, 15 });

                    PdfPTable payment_options = new PdfPTable(gen_table_cols);
                    payment_options.WidthPercentage = 100;
                    payment_options.SetWidths(new int[] { 20, 20, 15, 15, 15 });

                    PdfPTable terms_conditions = new PdfPTable(gen_table_cols);
                    terms_conditions.WidthPercentage = 100;
                    terms_conditions.SetWidths(new int[] { 25, 20, 15, 15, 15 });
                    #endregion

                    #region empty
                    cell = new PdfPCell(new Phrase(" "));
                    cell.Border = 0;
                    cell.Colspan = 5;
                    empty_row.AddCell(cell);
                    #endregion

                    #region Header 

                    iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(img_path + "bmobilelogo.png");
                    cell = new PdfPCell(logo);
                    cell.Border = 0;
                    cell.Colspan = 2;
                    header.AddCell(cell);

                    cell = new PdfPCell(new Phrase(" ", header_font_unbold));
                    cell.Border = 0;
                    header.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Date Time :\n\nPage :", header_font_unbold));
                    cell.Border = 0;
                    cell.HorizontalAlignment = PdfCell.ALIGN_RIGHT;
                    header.AddCell(cell);

                    cell = new PdfPCell(new Phrase(" " + DateTime.Now.ToString("dd/MM/yyyy hh:mm tt") + "\n\n Page 1 of 1", header_font_unbold));
                    cell.Border = 0;
                    cell.HorizontalAlignment = PdfCell.ALIGN_LEFT;
                    header.AddCell(cell);

                    #endregion

                    #region Header1
                    //cell = new PdfPCell(new Phrase("TIN : 500007351", cat_name_font_size));
                    //cell.Colspan = 5;
                    //cell.PaddingBottom = 2f;
                    //cell.Border = 0;
                    //header1.AddCell(cell);

                    cell = new PdfPCell(new Phrase("bmobile Solomon Islands", font_size));
                    cell.Colspan = 5;
                    cell.PaddingBottom = 2f;
                    cell.Border = 0;
                    header1.AddCell(cell);

                    cell = new PdfPCell(new Phrase("P.O. Box 2134,", font_size));
                    cell.Colspan = 5;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    header1.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Honiara", font_size));
                    cell.Colspan = 5;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    header1.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Email : support@bmobile.com.sb", font_size));
                    cell.Colspan = 5;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    header1.AddCell(cell);

                    //cell = new PdfPCell(new Phrase("Phone : 76003555", font_size));
                    //cell.Colspan = 5;
                    //cell.Border = 0;
                    //cell.PaddingBottom = 2f;
                    //header1.AddCell(cell);

                    #endregion

                    #region Header2
                    cell = new PdfPCell(new Phrase("INVOICE", header_font_unbold));
                    cell.Colspan = 5;
                    cell.Border = 0;
                    cell.HorizontalAlignment = PdfCell.ALIGN_CENTER;
                    header2.AddCell(cell);

                    #endregion

                    #region Invoice Info

                    cell = new PdfPCell(new Phrase("INVOICE No", cat_name_font_size));
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    invoice_info.AddCell(cell);
                    cell = new PdfPCell(new Phrase(": " + obj_inv.invoicedetail.invoice_txt, cat_name_font_size));
                    cell.Colspan = 4;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    invoice_info.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Company Name", cat_name_font_size));
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    invoice_info.AddCell(cell);
                    cell = new PdfPCell(new Phrase(": " + obj_inv.company_name, cat_name_font_size));
                    cell.Colspan = 4;
                    cell.PaddingBottom = 2f;
                    cell.Border = 0;
                    invoice_info.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Mobile Number", cat_name_font_size));
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    invoice_info.AddCell(cell);
                    cell = new PdfPCell(new Phrase(": " + obj_inv.mobile_number, cat_name_font_size));
                    cell.Colspan = 4;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    invoice_info.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Administrator Name", cat_name_font_size));
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    invoice_info.AddCell(cell);
                    cell = new PdfPCell(new Phrase(": " + obj_inv.contact_person_name, cat_name_font_size));
                    cell.Colspan = 4;
                    cell.PaddingBottom = 2f;
                    cell.Border = 0;
                    invoice_info.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Contact No ", cat_name_font_size));
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    invoice_info.AddCell(cell);
                    cell = new PdfPCell(new Phrase(": " + obj_inv.phone_number, cat_name_font_size));
                    cell.Colspan = 4;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    invoice_info.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Email ", cat_name_font_size));
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    invoice_info.AddCell(cell);
                    cell = new PdfPCell(new Phrase(": " + obj_inv.email, cat_name_font_size));
                    cell.Colspan = 4;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    invoice_info.AddCell(cell);


                    cell = new PdfPCell(new Phrase("  ", cat_name_font_size));
                    cell.Border = 0;
                    cell.Colspan = 5;
                    cell.PaddingBottom = 2f;
                    invoice_info.AddCell(cell);
                    #endregion

                    #region Invoice Table

                    #region table header

                    #region Row1
                    cell = new PdfPCell(new Phrase("Description ", header_font_unbold));
                    cell.Border = 0;
                    cell.Border = PdfCell.TOP_BORDER;
                    invoice_info.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Quantity ", header_font_unbold));
                    cell.Border = 0;
                    cell.Border = PdfCell.TOP_BORDER;
                    cell.HorizontalAlignment = PdfCell.ALIGN_CENTER;
                    invoice_info.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Unit Price ", header_font_unbold));
                    cell.Border = 0;
                    cell.Border = PdfCell.TOP_BORDER;
                    cell.HorizontalAlignment = PdfCell.ALIGN_CENTER;
                    invoice_info.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Sales Tax 10%(+)", header_font_unbold));
                    cell.Border = 0;
                    cell.Border = PdfCell.TOP_BORDER;
                    cell.HorizontalAlignment = PdfCell.ALIGN_CENTER;
                    invoice_info.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Total Payable(SBD)", header_font_unbold));
                    cell.Border = 0;
                    cell.Border = PdfCell.TOP_BORDER;
                    invoice_info.AddCell(cell);
                    #endregion

                    #region Row2
                    //cell = new PdfPCell(new Phrase("  ", header_font_unbold));
                    //cell.Colspan = 4;
                    //cell.Border = PdfCell.BOTTOM_BORDER;
                    //invoice_info.AddCell(cell);

                    //cell = new PdfPCell(new Phrase("(PG$)", header_font_unbold));
                    //cell.Border = PdfCell.BOTTOM_BORDER;
                    //cell.HorizontalAlignment = PdfCell.ALIGN_CENTER;
                    //invoice_info.AddCell(cell);
                    #endregion


                    #endregion

                    #region table data
                    cell = new PdfPCell(new Phrase("Direct Topup", header_font_unbold));
                    cell.Border = 0;
                    invoice_table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(obj_inv.invoicedetail.total_quantity.ToString(), header_font_unbold));
                    cell.Border = 0;
                    cell.HorizontalAlignment = PdfCell.ALIGN_CENTER;
                    invoice_table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(obj_inv.invoicedetail.unit_price.Value.ToString("0.00"), header_font_unbold));
                    cell.Border = 0;
                    cell.HorizontalAlignment = PdfCell.ALIGN_CENTER;
                    invoice_table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(obj_inv.invoicedetail.total_gst_val.Value.ToString("0.00"), header_font_unbold));
                    cell.Border = 0;
                    cell.HorizontalAlignment = PdfCell.ALIGN_CENTER;
                    invoice_table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("$ " + obj_inv.invoicedetail.item_val.Value.ToString("0.00"), header_font_unbold));
                    cell.Border = PdfCell.BOTTOM_BORDER;
                    cell.HorizontalAlignment = PdfCell.ALIGN_RIGHT;
                    cell.PaddingBottom = 10f;

                    invoice_table.AddCell(cell);
                    #endregion

                    #region table Total
                    cell = new PdfPCell(new Phrase(" ", header_font_unbold));
                    cell.Border = 0;
                    cell.Colspan = 3;
                    invoice_table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Total", header_font_unbold));
                    cell.Border = 0;
                    cell.HorizontalAlignment = PdfCell.ALIGN_RIGHT;
                    cell.PaddingBottom = 10f;
                    cell.PaddingTop = 10f;
                    invoice_table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("$ " + obj_inv.invoicedetail.item_val.Value.ToString("0.00"), header_font_unbold));
                    cell.Border = 0;
                    cell.HorizontalAlignment = PdfCell.ALIGN_RIGHT;
                    cell.PaddingBottom = 10f;
                    cell.PaddingTop = 10f;
                    invoice_table.AddCell(cell);

                    #endregion

                    #region table GST
                    cell = new PdfPCell(new Phrase(" ", header_font_unbold));
                    cell.Border = 0;
                    cell.Colspan = 3;
                    invoice_table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Sales Tax", header_font_unbold));
                    cell.Border = 0;
                    cell.HorizontalAlignment = PdfCell.ALIGN_RIGHT;
                    cell.PaddingBottom = 10f;
                    cell.PaddingTop = 10f;
                    invoice_table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("$ " + obj_inv.invoicedetail.total_gst_val.Value.ToString("0.00"), header_font_unbold));
                    cell.Border = PdfCell.BOTTOM_BORDER;
                    cell.HorizontalAlignment = PdfCell.ALIGN_RIGHT;
                    cell.PaddingBottom = 10f;
                    cell.PaddingTop = 10f;
                    invoice_table.AddCell(cell);

                    #endregion

                    #region table Grand Total
                    cell = new PdfPCell(new Phrase(" ", header_font_unbold));
                    cell.Border = 0;
                    cell.Colspan = 3;
                    invoice_table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Grand Total", header_font_unbold));
                    cell.Border = 0;
                    cell.HorizontalAlignment = PdfCell.ALIGN_RIGHT;
                    cell.PaddingBottom = 10f;
                    cell.PaddingTop = 10f;
                    invoice_table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("$ " + obj_inv.invoicedetail.total_amt.Value.ToString("0.00"), header_font_unbold));
                    cell.Border = PdfCell.BOTTOM_BORDER;
                    cell.HorizontalAlignment = PdfCell.ALIGN_RIGHT;
                    cell.PaddingBottom = 10f;
                    cell.PaddingTop = 10f;
                    invoice_table.AddCell(cell);

                    #endregion

                    #region Invoice Table Footer

                    cell = new PdfPCell(new Phrase(" ", font_size));
                    cell.Colspan = 2;
                    cell.Border = 0;
                    invoice_table_footer.AddCell(cell);
                    cell = new PdfPCell(new Phrase(" ", header_font_unbold));
                    cell.Border = 0;
                    invoice_table_footer.AddCell(cell);
                    cell = new PdfPCell(new Phrase(" ", header_font_unbold));
                    cell.Colspan = 2;
                    cell.Border = 0;
                    invoice_table_footer.AddCell(cell);

                    //cell = new PdfPCell(new Phrase(" ", font_size));
                    //cell.Colspan = 2;
                    //cell.Border = 0;
                    //invoice_table_footer.AddCell(cell);
                    //cell = new PdfPCell(new Phrase("Payment Due :", header_font_unbold));
                    //cell.Border = 0;
                    //cell.HorizontalAlignment = PdfCell.ALIGN_RIGHT;
                    //invoice_table_footer.AddCell(cell);
                    //cell = new PdfPCell(new Phrase(DueDate, header_font_unbold));
                    //cell.Colspan = 2;
                    //cell.Border = 0;
                    //cell.HorizontalAlignment = PdfCell.ALIGN_RIGHT;
                    //invoice_table_footer.AddCell(cell);



                    //cell = new PdfPCell(new Phrase(" ", font_size));
                    //cell.Colspan = 2;
                    //cell.Border = 0;
                    //invoice_table_footer.AddCell(cell);
                    //cell = new PdfPCell(new Phrase(" CUG Portal :", header_font_unbold));
                    //cell.Border = 0;
                    //cell.HorizontalAlignment = PdfCell.ALIGN_RIGHT;
                    //invoice_table_footer.AddCell(cell);
                    //cell = new PdfPCell(new Phrase("http://cug.bmobile.com.pg", header_font_unbold));
                    //cell.Colspan = 2;
                    //cell.Border = 0;
                    //cell.HorizontalAlignment = PdfCell.ALIGN_RIGHT;
                    //invoice_table_footer.AddCell(cell);

                    #endregion

                    #endregion

                    #region Payment options
                    cell = new PdfPCell(new Phrase("Payment Options:", cat_name_font_size));
                    cell.Colspan = 5;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    payment_options.AddCell(cell);

                    //cell = new PdfPCell(new Phrase("Online or Voucher Topup through the CUG Portal", font_size));
                    //cell.Colspan = 5;
                    //cell.Border = 0;
                    //cell.PaddingBottom = 2f;
                    //payment_options.AddCell(cell);

                    //cell = new PdfPCell(new Phrase("Electronic Voucher Distribution (EVD)", font_size));
                    //cell.Colspan = 5;
                    //cell.Border = 0;
                    //cell.PaddingBottom = 2f;
                    //payment_options.AddCell(cell);

                    cell = new PdfPCell(new Phrase("EFTPOS", font_size));
                    cell.Colspan = 5;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    payment_options.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Direct Deposit/Electronic Transfer: ", font_size));
                    cell.Colspan = 5;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    payment_options.AddCell(cell);

                    cell = new PdfPCell(new Phrase("BANK ", font_size));
                    cell.Colspan = 1;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    payment_options.AddCell(cell);
                    cell = new PdfPCell(new Phrase("Bank of South Pacific Limited", font_size));
                    cell.Colspan = 4;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    payment_options.AddCell(cell);

                    cell = new PdfPCell(new Phrase("BANK NAME", font_size));
                    cell.Colspan = 1;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    payment_options.AddCell(cell);
                    cell = new PdfPCell(new Phrase("Bemobile (Solomon Islands) Limited", font_size));
                    cell.Colspan = 4;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    payment_options.AddCell(cell);

                    cell = new PdfPCell(new Phrase("BANK ACCOUNT #", font_size));
                    cell.Colspan = 1;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    payment_options.AddCell(cell);
                    cell = new PdfPCell(new Phrase("4000173544", font_size));
                    cell.Colspan = 4;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    payment_options.AddCell(cell);

                    //cell = new PdfPCell(new Phrase("BSP # ", font_size));
                    //cell.Colspan = 1;
                    //cell.Border = 0;
                    //cell.PaddingBottom = 2f;
                    //payment_options.AddCell(cell);
                    //cell = new PdfPCell(new Phrase("088951", font_size));
                    //cell.Colspan = 4;
                    //cell.Border = 0;
                    //cell.PaddingBottom = 2f;
                    //payment_options.AddCell(cell);

                    cell = new PdfPCell(new Phrase("SWIFT CODE ", font_size));
                    cell.Colspan = 1;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    payment_options.AddCell(cell);
                    cell = new PdfPCell(new Phrase("BOSP SBSB", font_size));
                    cell.Colspan = 4;
                    cell.Border = 0;
                    cell.PaddingBottom = 2f;
                    payment_options.AddCell(cell);
                    #endregion

                    //#region Terms and Conditions
                    //cell = new PdfPCell(new Phrase("TERMS AND CONDITIONS", cat_name_font_size));
                    //cell.Colspan = 5;
                    //cell.Border = 0;
                    //terms_conditions.AddCell(cell);

                    //cell = new PdfPCell(new Phrase("This is a prepaid product service, payment will need to be received by the 1st of each month to ensure continuation of this service.", font_size));
                    //cell.Colspan = 5;
                    //cell.Border = 0;
                    //terms_conditions.AddCell(cell);

                    //#endregion

                    #region Bind to PDF
                    document.Add(empty_row);
                    document.Add(header);
                    document.Add(empty_row);

                    document.Add(header1);
                    document.Add(empty_row);
                    document.Add(header2);
                    document.Add(empty_row);
                    document.Add(invoice_info);
                    document.Add(empty_row);

                    document.Add(invoice_table);
                    document.Add(empty_row);
                    document.Add(invoice_table_footer);

                    document.Add(empty_row);
                    document.Add(payment_options);

                    document.Add(empty_row);
                    document.Add(empty_row);
                    document.Add(terms_conditions);
                    document.Close();
                    #endregion


                    #endregion
                }


            }


            return filepath;
        }

        #endregion

        public tbl_si_ccp_invoice invoice_number_generate()
        {
            tbl_si_ccp_invoice inv_obj = new tbl_si_ccp_invoice();
            long Invoicenumber = 10000;

            List<tbl_si_ccp_invoice> _obj = _uow.ccp_invoice_Repo.Get().ToList();
            if (_obj.Count > 0)
            {
                Invoicenumber = _obj.Max(f => f.invoice_no);
                Invoicenumber = Invoicenumber + 1;
            }

            tbl_si_ccp_invoice chk_invoice = _uow.ccp_invoice_Repo.Get(filter: f => f.invoice_no == Invoicenumber).FirstOrDefault();
            if (chk_invoice == null)
            {
                inv_obj.invoice_no = Invoicenumber;
                inv_obj.invoice_txt = "DTU" + String.Format("{0:yy}", DateTime.Now).ToString() + Invoicenumber.ToString();
            }
            else
            {
                invoice_number_generate();
            }

            return inv_obj;
        }

        public bool get_staff_topup(string msisdn_no)
        {
            bool res = false;
            DateTime dt = DateTime.Now;
            var obj_staff = _uow.staff_topup_repo.Get(filter: S => S.isdeleted == false && S.msisdn_number == msisdn_no && S.created_on >= dt.Date).FirstOrDefault();
            if (obj_staff != null)
            {
                res = true;
            }
            return res;
        }

        public tbl_user getuser(long id)
        {
            string pwd = null;
            tbl_user obj_user = new tbl_user();

            obj_user = _uow.user_repo.Get(filter: c => c.isdeleted == false && c.id == id).FirstOrDefault();

            return obj_user;
        }



        #region get_auditlog

        public List<ccp_AuditModel> get_auditlog(string aces_from, string sdate, string edate)
        {
            List<ccp_AuditModel> auditlog = new List<ccp_AuditModel>();

            int user_role;

            user_role = (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["user_role"])) ? Convert.ToInt32(ConfigurationManager.AppSettings["user_role"]) : 999;

            sdate = !string.IsNullOrEmpty(sdate) ? sdate : DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");
            edate = !string.IsNullOrEmpty(edate) ? edate : DateTime.Now.ToString("dd/MM/yyyy");

            DateTime sdt = Convert.ToDateTime(sdate);
            DateTime edt = Convert.ToDateTime(edate).AddDays(1);


            if (aces_from == "Admin")
            {
                auditlog = (from obj in _context.ccp_track
                            join u in _context.admin_user on obj.user_id equals u.id
                            join rc in _context.ccp_rptcat on obj.rpt_cat_id equals rc.id
                            join a in _context.ccp_action on obj.action_id equals a.id
                            join r in _context.role on obj.role_id equals r.id
                            where obj.access_from == aces_from
                            orderby obj.created_on descending
                            select new ccp_AuditModel
                            {
                                user_id = obj.user_id,
                                role_id = obj.role_id,
                                role_name = r.role_name,
                                ip_address = obj.ip_address,
                                access_from = obj.access_from,
                                url_accesses = obj.url_accesses,
                                url_referal = obj.url_referal,
                                created_on = obj.created_on,
                                action_id = obj.action_id,
                                action_name = a.action_name,
                                rpt_cat_name = rc.report_cat_name,
                                rpt_cat_id = obj.rpt_cat_id,
                                user_name = u.first_name + " " + u.last_name,
                                msisdn = obj.msisdn

                            }).ToList();

            }
            else if (aces_from == "Company")
            {
                auditlog = (from obj in _context.ccp_track
                            join c in _context.company_info on obj.user_id equals c.id
                            join rc in _context.ccp_rptcat on obj.rpt_cat_id equals rc.id
                            join a in _context.ccp_action on obj.action_id equals a.id
                            where obj.access_from == aces_from
                            orderby obj.created_on descending
                            select new ccp_AuditModel
                            {
                                user_id = obj.user_id,
                                role_id = obj.role_id,
                                ip_address = obj.ip_address,
                                access_from = obj.access_from,
                                url_accesses = obj.url_accesses,
                                url_referal = obj.url_referal,
                                created_on = obj.created_on,
                                action_id = obj.action_id,
                                action_name = a.action_name,
                                rpt_cat_name = rc.report_cat_name,
                                rpt_cat_id = obj.rpt_cat_id,
                                user_name = c.company_name,
                                msisdn = obj.msisdn
                            }).ToList();
            }
            else if (aces_from == "User")
            {
                auditlog = (from obj in _context.ccp_track
                            join c in _context.user on obj.user_id equals c.id
                            join rc in _context.ccp_rptcat on obj.rpt_cat_id equals rc.id
                            join a in _context.ccp_action on obj.action_id equals a.id
                            where obj.access_from == aces_from
                            orderby obj.created_on descending
                            select new ccp_AuditModel
                            {
                                user_id = obj.user_id,
                                role_id = user_role,
                                role_name = "User",
                                ip_address = obj.ip_address,
                                access_from = obj.access_from,
                                url_accesses = obj.url_accesses,
                                url_referal = obj.url_referal,
                                created_on = obj.created_on,
                                action_id = obj.action_id,
                                action_name = a.action_name,
                                rpt_cat_name = rc.report_cat_name,
                                rpt_cat_id = obj.rpt_cat_id,
                                user_name = c.first_name + " " + c.last_name,
                                msisdn = obj.msisdn
                            }).ToList();
            }
            //if (!string.IsNullOrWhiteSpace(aces_from))
            //    auditlog = auditlog.Where(x => x.access_from.ToLower().Contains(aces_from.ToLower())).ToList();

            if (sdate != null || edate != null)
                auditlog = auditlog.Where(x => x.created_on >= sdt && x.created_on <= edt).ToList();

            return auditlog;
        }

        #endregion
        #region Cug_Tracking
        public void Ccp_Tracking(ccp_AuditModel objtrack)
        {
            try
            {
                tbl_ccp_track objtrk = new tbl_ccp_track();
                if (objtrack != null)
                {
                    objtrk.user_id = objtrack.user_id;
                    objtrk.rpt_cat_id = objtrack.rpt_cat_id;//_uow.cug_rptcat_Repo.Get(filter: x => x.is_active == true && x.is_deleted == false && x.report_cat_name.ToLower() == objtrack.rpt_cat_name.ToLower()).Select(q=> q.id).FirstOrDefault();
                    objtrk.ip_address = GetIPAddress();
                    objtrk.action_id = objtrack.action_id;//_uow.cug_action_Repo.Get(filter: x => x.is_active == true && x.is_deleted == false && x.action_name.ToLower() == objtrack.action_name.ToLower()).Select(q => q.id).FirstOrDefault();
                    objtrk.url_accesses = objtrack.url_accesses;
                    objtrk.role_id = objtrack.role_id;
                    objtrk.access_from = objtrack.access_from;
                    objtrk.sdate = DateTime.Now.ToString("yyyyMMdd");
                    objtrk.stime = DateTime.Now.ToString("HHmmss");
                    objtrk.created_on = DateTime.Now;
                    objtrk.url_referal = objtrack.url_referal;
                    if (!string.IsNullOrEmpty(objtrack.msisdn))
                    {
                        objtrk.msisdn = objtrack.msisdn;
                    }
                    _uow.ccp_track_Repo.Insert(objtrk);
                    _uow.Save();
                }
            }
            catch (Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex);
            }
        }

        private String GetIPAddress()
        {
            String ipAddr = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ipAddr))
            {
                ipAddr = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            }

            return ipAddr;
        }

        #endregion




        #region Check_Admin_UserLogin

        public tbl_admin_user Check_Admin_UserLogin(admin_login_model objLoginModel, out int failed_his_count)
        {
            failed_his_count = 0;

            string pwd = _util_repo.AES_ENC(objLoginModel.password.Trim());

            List<int> objRoles = _context.role.Where(x => x.id != 0).Select(r => r.id).ToList();

            tbl_admin_user user_info = _context.admin_user.Where(x => x.email == objLoginModel.username && x.password == pwd && x.isactive == true && x.isdeleted == false && objRoles.Contains(x.role_id)).FirstOrDefault();

            tbl_admin_user user_detail = _context.admin_user.Where(M => M.email == objLoginModel.username && M.role_id != 1).FirstOrDefault();
            if (user_detail != null)
            {
                DateTime sdt = DateTime.Now.Date;
                DateTime edt = sdt.AddDays(1);

                List<tbl_user_login_his> LogHis = _context.user_login_his.Where(P => P.user_id == user_detail.id && P.log_from == "WEB" && P.created_on >= sdt && P.created_on < edt).ToList();
                failed_his_count = LogHis.Count();

                if (user_info != null && failed_his_count <= 2 && failed_his_count > 0)
                {
                    foreach (var ulog in LogHis)
                    {
                        _uow.user_login_his_repo.Delete(ulog);
                        _uow.Save();
                    }
                }
            }
            return user_info;
        }
        #endregion



        #region CreateLoginHistory_Admin_User

        public bool CreateLoginHistory_Admin_User(string email)
        {
            bool Res = false;
            tbl_admin_user objUser = new tbl_admin_user();
            objUser = _context.admin_user.Where(U => U.email == email && U.role_id != 1).FirstOrDefault();
            if (objUser != null)
            {
                tbl_user_login_his objLoginHis = new tbl_user_login_his();
                objLoginHis.Id = 0;
                objLoginHis.user_id = objUser.id;
                objLoginHis.created_on = DateTime.Now;
                objLoginHis.log_from = "WEB";
                _uow.user_login_his_repo.Insert(objLoginHis);
                _uow.Save();

                Res = true;
            }
            return Res;
        }
        #endregion




        #region Check_Company_Login

        public tbl_company_info Check_Company_Login(company_login_model objLoginModel, out int failed_his_count)
        {
            failed_his_count = 0;

            string pwd = _util_repo.AES_ENC(objLoginModel.password.Trim());

            List<int> objRoles = _context.role.Where(x => x.id != 0).Select(r => r.id).ToList();

            tbl_company_info company_info = _context.company_info.Where(x => x.email == objLoginModel.username && x.password == pwd && x.isactive == true && x.isdeleted == false).FirstOrDefault();

            tbl_company_info comapny_detail = _context.company_info.Where(M => M.email == objLoginModel.username).FirstOrDefault();
            if (comapny_detail != null)
            {
                DateTime sdt = DateTime.Now.Date;
                DateTime edt = sdt.AddDays(1);

                List<tbl_company_login_his> LogHis = _context.company_login_his.Where(P => P.company_id == comapny_detail.id && P.log_from == "WEB" && P.created_on >= sdt && P.created_on < edt).ToList();
                failed_his_count = LogHis.Count();

                if (company_info != null && failed_his_count <= 2 && failed_his_count > 0)
                {
                    foreach (var clog in LogHis)
                    {
                        _uow.company_login_his_repo.Delete(clog);
                        _uow.Save();
                    }
                }
            }
            return company_info;
        }
        #endregion



        #region Create Login History_Company_Login

        public bool Create_Login_History_Company_Login(string email)
        {
            bool Res = false;
            tbl_company_info obj_company = new tbl_company_info();
            obj_company = _context.company_info.Where(U => U.email == email).FirstOrDefault();
            if (obj_company != null)
            {
                tbl_company_login_his objLoginHis = new tbl_company_login_his();
                objLoginHis.Id = 0;
                objLoginHis.company_id = obj_company.id;
                objLoginHis.created_on = DateTime.Now;
                objLoginHis.log_from = "WEB";
                _uow.company_login_his_repo.Insert(objLoginHis);
                _uow.Save();
                Res = true;
            }
            return Res;
        }
        #endregion

        #region check_mobile_exist
        public bool check_mobile_exist(string msisdn, long? id)
        {
            bool bRet = false;
            if (id == 0 || id == null)
            {
                var alreadyExist = _uow.company_info_repo.Get(filter: (m => m.mobile_number == msisdn && m.isdeleted == false)).FirstOrDefault();
                if (alreadyExist == null)
                    bRet = true;
            }
            else if (id > 0)
            {
                var alreadyExist = _uow.company_info_repo.Get(filter: (m => m.mobile_number == msisdn && m.isdeleted == false && m.id != id)).FirstOrDefault();
                if (alreadyExist == null)
                    bRet = true;
            }
            return bRet;
        }

        #endregion

        public bool get_staff_topup_v1(string msisdn_no, long bundle_id)
        {
            bool res = false;
            DateTime dt = DateTime.Now;
            // var obj_staff = _uow.staff_topup_repo.Get(filter: S => S.isdeleted == false && S.msisdn_number == msisdn_no && S.created_on >= dt.Date).FirstOrDefault();

            var obj_staff = (from st in _context.staff_topup
                             join bl in _context.staff_topup_bundle on st.id equals bl.staff_topup_id into b1
                             from bl in b1.DefaultIfEmpty()
                             where st.isactive == true && st.isdeleted == false && st.msisdn_number == msisdn_no && bl.bundle_id == bundle_id && EntityFunctions.TruncateTime(st.created_on) == EntityFunctions.TruncateTime(dt)
                             select new
                             {
                                 st = st,
                                 bl = bl
                             }).FirstOrDefault();

            if (obj_staff != null)
            {
                res = true;
            }
            return res;
        }

        #region Truncate temp staff topup
        public void TrucateTempStaffTopup()
        {


            var res = _context.Database.ExecuteSqlCommand("EXEC trunc_temp_staff_topup");


        }
        #endregion

        #region save temp staff topup
        public void SaveTempStaffTopup(long bundle_id, string bunde_name, string msisdn)
        {
            object[] parameters = { msisdn, bundle_id, bunde_name };

            var res = _context.Database.SqlQuery<temp_staff_topup>("save_temp_staff_topup @msisdn={0},@bundle_id={1},@bundle_name={2}", parameters).ToList();

        }
        #endregion

        #region save temp staff topup
        public List<temp_staff_topup> GetStaffTopupDuplicate()
        {
            var res = _context.Database.SqlQuery<temp_staff_topup>("GetStaffTopupDuplicates ").ToList();

            return res;
        }
        #endregion

        #region Level0 validation for staff topup
        public import_staff_model StaffTopupAllAmountVal(import_staff_model objdata, out bool res,out string msg)
        {
            decimal required_amount = 0;
            int count = 0;
            res = true;
            msg = "";
            if (objdata.staff_topup_list != null && objdata.staff_topup_list.Count > 0)
            {


                var amt = objdata.staff_topup_list.Where(y=>y.amount !=null).Sum(x => x.amount);
                required_amount = (decimal)amt;
                if (required_amount > objdata.company_info.credit_amount)
                {
                    msg = "Insufficient Amount. Please topup company credit balance";
                    count += 1;
                    res = false;
                }
               
                
            }
            
            return objdata;
        }

        #endregion

        #region Level1 validation for staff topup
        public import_staff_model StaffTopupAmountVal(import_staff_model objdata, out bool res)
        {
            decimal required_amount = 0;
            bool bCheck = true;
            int count = 0;
            res = true;
            if (objdata.staff_topup_list != null && objdata.staff_topup_list.Count > 0)
            {


                foreach (var itm in objdata.staff_topup_list)
                {
                    if (itm.amount > objdata.company_info.credit_amount)
                    {
                        itm.description = "<div class='ifail'>Insufficient Amount</div>";
                        bCheck = false;
                        count += 1;
                    }
                    else if (itm.amount < objdata.company_info.min_trans_amount || itm.amount > objdata.company_info.max_trans_amount)
                    {
                        itm.description = "<div class='ifail'>Amount must be greater than minimum amount and Less than maximum amount</div>";

                        count += 1;
                        bCheck = false;

                    }
                    else
                    {
                        bCheck = true;
                    }
                }
            }
            if (count>0)
            {
                res = false;
            }
           

            return objdata;
        }

        #endregion

        #region Level2 validation for Staff Topup
        public import_staff_model StaffTopupBundleIdVal(import_staff_model objdata, out bool res)
        {
            decimal? tot_bundle_amt = 0;
            bool bCheck = true;
            res = true;
            int count = 0;
            List<tbl_bundle_plan> objBundle_plans = new List<tbl_bundle_plan>();

            var bdlpln = GetBundle_Plans();

            if (bdlpln != null && bdlpln.Count > 0)
            {
                objBundle_plans = bdlpln;
            }
            if (objdata.staff_topup_list != null && objdata.staff_topup_list.Count > 0)
            {


                foreach (var itm in objdata.staff_topup_list)
                {
                    bool check_alreadyAvailable = false;


                    #region bundle IDs
                    long bid = 0;
                    List<long> b_id = new List<long>();

                    if (!string.IsNullOrEmpty(itm.bType))
                    {
                        if (long.TryParse(itm.bType, out bid))
                        {
                            var bundle_id = objBundle_plans.Where(x => x.is_active == true && x.id == bid).Select(x => x.bundle_id).FirstOrDefault();

                            bid = bundle_id;
                            b_id.Add(bid);

                            check_alreadyAvailable = get_staff_topup_v1(itm.msisdn_number.Trim(), bid);
                            if (check_alreadyAvailable == true)
                            {
                                tot_bundle_amt = 0;
                                itm.description = "<div class='ifail'>Data already imported for this MSISDN number with same bundle !</div>";
                                count += 1;
                                bCheck = false;
                            }


                            bid = 0;
                        }
                    }
                    else
                    {
                        long bundle_id = 0;
                        b_id.Add(bundle_id);
                    }
                    if (!string.IsNullOrEmpty(itm.bType2))
                    {
                        if (long.TryParse(itm.bType2, out bid))
                        {
                            var bundle_id = objBundle_plans.Where(x => x.is_active == true && x.id == bid).Select(x => x.bundle_id).FirstOrDefault();
                            bid = bundle_id;
                            b_id.Add(bid);

                            check_alreadyAvailable = get_staff_topup_v1(itm.msisdn_number.Trim(), bid);
                            if (check_alreadyAvailable == true)
                            {
                                tot_bundle_amt = 0;
                                itm.description = "<div class='ifail'>Data already imported for this MSISDN number with same bundle !</div>";
                                count += 1;
                                bCheck = false;
                            }

                            bid = 0;
                        }
                    }
                    else
                    {
                        long bundle_id = 0;
                        b_id.Add(bundle_id);
                    }
                    if (!string.IsNullOrEmpty(itm.bType3))
                    {
                        if (long.TryParse(itm.bType3, out bid))
                        {
                            var bundle_id = objBundle_plans.Where(x => x.is_active == true && x.id == bid).Select(x => x.bundle_id).FirstOrDefault();
                            bid = bundle_id;
                            b_id.Add(bid);

                            check_alreadyAvailable = get_staff_topup_v1(itm.msisdn_number.Trim(), bid);
                            if (check_alreadyAvailable == true)
                            {
                                tot_bundle_amt = 0;
                                itm.description = "<div class='ifail'>Data already imported for this MSISDN number with same bundle !</div>";
                                count += 1;
                                bCheck = false;
                            }

                            bid = 0;
                        }
                    }
                    else
                    {
                        long bundle_id = 0;
                        b_id.Add(bundle_id);
                    }
                    if (b_id.Count > 0)
                    {
                        itm.bundle_ids = b_id;

                    }
                    if (itm.bundle_ids != null)
                    {
                        foreach (long bundle_id in itm.bundle_ids)
                        {
                            tbl_bundle_plan objBundle = objBundle_plans.Where(x => x.bundle_id == bundle_id).FirstOrDefault();

                            tot_bundle_amt += objBundle != null ? objBundle.price : 0;


                        }
                    }

                    if (itm.amount >= tot_bundle_amt)
                    {

                        tot_bundle_amt = 0;

                    }
                    else
                    {
                        tot_bundle_amt = 0;
                        itm.description = "<div class='ifail'>Insufficient Amount!</div>";
                        count += 1;
                        bCheck = false;
                    }

                    #endregion
                }
            }
            if (count>0)
            {
                res = bCheck;
            }
            

            return objdata;
        }
        #endregion

        #region autocomplete company name
        public List<CompanyAutoModel> GetCompanyNameAutoComplete(string name)
        {
            List<CompanyAutoModel> obj = new List<CompanyAutoModel>();
            obj = (from c in _context.company_info
                   where c.isactive == true && c.isdeleted == false && c.company_name.StartsWith(name.ToLower())
                  select new CompanyAutoModel
                  {
                      id = c.id,
                      company_name = c.company_name

                  }).ToList();

            return obj;
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
                    _context.Dispose();
                    _util_repo.Dispose();
                    _uow.Dispose();
                    _easyRecharge.Dispose();
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