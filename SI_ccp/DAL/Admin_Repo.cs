using bmdoku.bmkuRef;
using Newtonsoft.Json;
using SI_ccp.BundleRef;
using SI_ccp.Models;
using SI_ccp.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace SI_ccp.DAL
{
    public class Admin_Repo : IAdmin_Repo, IDisposable
    {
        #region Repo

        private SI_CCPDBEntities _context;
        private UnitOfWork _uow;
        private IUtilityRepo _util_repo;
        private EasyRecharge _easyRecharge;
        private IBundlePlan _scBPlan;
        private string sf_encry_pwd;
        private string sf_plain_pwd;
        private string sf_merchantid;
        private string sf_username;
        private string sf_keycode;

        public Admin_Repo()
        {
            this._context = new SI_CCPDBEntities();
            this._uow = new UnitOfWork();
            this._util_repo = new UtilityRepo();
            this._scBPlan = new BundlePlanClient();
            this._easyRecharge = new EasyRecharge();

            this.sf_merchantid = ConfigurationManager.AppSettings["sf_merchantid"].ToString();
            this.sf_username = ConfigurationManager.AppSettings["sf_username"].ToString();
            this.sf_plain_pwd = ConfigurationManager.AppSettings["sf_plain_pwd"].ToString();
            this.sf_encry_pwd = ConfigurationManager.AppSettings["sf_encry_pwd"].ToString();
            this.sf_keycode = ConfigurationManager.AppSettings["sf_keycode"].ToString();
        }

        #endregion

        #region Admin Login

        public tbl_admin_user check_admin_login(admin_login_model obj_login)
        {
            tbl_admin_user obj_user = new tbl_admin_user();

            if (obj_login != null && !string.IsNullOrEmpty(obj_login.username) && !string.IsNullOrEmpty(obj_login.password))
            {
                string pwd = _util_repo.AES_ENC(obj_login.password.Trim());
                //string pwsd = _util_repo.AES_DEC("ClylN3hyds2FsSpLDQ5ofQ==");
                obj_user = _uow.admin_user_repo.Get(filter: u => u.isactive == true && u.isdeleted == false && u.email == obj_login.username && u.password == pwd).FirstOrDefault();
            }

            return obj_user;
        }


        #endregion

        #region user menus list

        public List<user_access_model> get_user_access_list(long? user_id)
        {
            var objAccess = (from acc in _context.menu
                             where acc.isactive == true && acc.isdeleted == false
                             select new user_access_model
                             {
                                 access_id = acc.id,
                                 level_name = acc.menu_name,
                                 selected = false
                             }).ToList();

            if (user_id != null)
            {
                var user_access = _context.user_access.Where(U => U.user_id == user_id).ToList();
                foreach (var acc in objAccess)
                {
                    foreach (var useracc in user_access)
                    {
                        if (acc.access_id == useracc.access_id)
                            acc.selected = true;
                    }
                }
            }

            return objAccess;
        }

        public List<tbl_menu> get_user_menus(long user_id)
        {
            var user_access = (from useracc in _context.user_access
                               join acc in _context.menu on useracc.access_id equals acc.id
                               where useracc.user_id == user_id && acc.isactive == true && acc.isdeleted == false && acc.id != 3
                               select acc).ToList();

            return user_access;
        }

        public bool check_menu_access(long user_id, int role_id, string viewname)
        {
            bool ret_val = false;
            var _obj = (from U in _context.admin_user
                        join R in _context.role on U.role_id equals R.id
                        join A in _context.user_access on U.id equals A.user_id
                        join M in _context.menu on A.access_id equals M.id
                        where U.id == user_id && R.id == role_id && U.isdeleted == false && U.isactive == true && M.isactive == true && M.isdeleted == false
                        select M).ToList();
            if (_obj.Count > 0)
            {
                foreach (var item in _obj)
                {
                    if (item.menu == viewname)
                        ret_val = true;
                }
            }

            return ret_val;

        }

        #endregion

        #region get user

        public List<userlist_model> get_admin_users()
        {
            List<userlist_model> _obj = new List<userlist_model>();

            var temp_obj = (from U in _context.admin_user
                            join R in _context.role on U.role_id equals R.id
                            where U.isdeleted == false && U.role_id != 1
                            select new userlist_model
                            {
                                Id = U.id,
                                role_id = R.id,
                                role_name = R.role_name,
                                username = U.first_name + " " + U.last_name,
                                emailid = U.email,
                                is_active = U.isactive
                            }).ToList();

            _obj = temp_obj;

            return _obj;
        }

        #endregion

        #region get roles

        public List<tbl_role> get_admin_user_roles()
        {
            List<tbl_role> obj_role = new List<tbl_role>();

            obj_role = _uow.role_repo.Get(filter: n => n.id != 1).ToList();

            return obj_role;
        }

        #endregion

        #region user by id

        public user_model userby_id(long id)
        {
            var _obj = (from U in _context.admin_user
                        join R in _context.role on U.role_id equals R.id
                        where U.id == id
                        select new user_model
                        {
                            Id = U.id,
                            first_name = U.first_name,
                            last_name = U.last_name,
                            role_id = R.id,
                            email = U.email,
                            is_active = U.isactive,
                            mobile_number = U.msisdn_number
                        }).FirstOrDefault();

            return _obj;
        }

        #endregion

        public bool check_admin_user_email_exist(string email, long? id)
        {
            bool bRet = false;

            tbl_admin_user alreadyExist = new tbl_admin_user();

            if (id == 0 || id == null)
            {
                alreadyExist = _uow.admin_user_repo.Get(filter: (m => m.email == email && m.isdeleted == false)).FirstOrDefault();
                if (alreadyExist == null)
                    bRet = true;
            }
            else if (id > 0)
            {
                alreadyExist = _uow.admin_user_repo.Get(filter: (m => m.email == email && m.isdeleted == false && m.id != id)).FirstOrDefault();
                if (alreadyExist == null)
                    bRet = true;
            }

            return bRet;
        }

        public bool insert_admin_user(tbl_admin_user obj_user)
        {
            bool bRes = false;

            _uow.admin_user_repo.Insert(obj_user);
            _uow.Save();

            if (obj_user.id > 0)
                bRes = true;

            return bRes;
        }

        public tbl_admin_user get_admin_userdetails(long id)
        {
            tbl_admin_user obj_newuser = new tbl_admin_user();

            obj_newuser = _uow.admin_user_repo.Get(filter: m => m.id == id && m.isdeleted == false).FirstOrDefault();

            return obj_newuser;
        }

        public bool update_admin_user(tbl_admin_user obj_user)
        {
            bool bRes = false;

            tbl_admin_user obj_newuser = _uow.admin_user_repo.GetByID(obj_user.id);

            obj_newuser.role_id = obj_user.role_id;
            obj_newuser.first_name = obj_user.first_name;
            obj_newuser.last_name = obj_user.last_name;
            obj_newuser.email = obj_user.email;
            obj_newuser.isactive = obj_user.isactive;
            obj_newuser.modified_on = DateTime.Now;
            obj_newuser.msisdn_number = obj_user.msisdn_number;

            _uow.admin_user_repo.Update(obj_newuser);
            _uow.Save();

            bRes = true;

            return bRes;
        }

        public bool delete_admin_user(long id)
        {
            bool bRes = false;

            tbl_admin_user _obj = _uow.admin_user_repo.GetByID(id);

            if (_obj != null)
            {
                _obj.isactive = false;
                _obj.isdeleted = true;

                _uow.admin_user_repo.Update(_obj);
                _uow.Save();

                bRes = true;

            }
            return bRes;
        }

        public void delete_user_accessby_userid(long id)
        {
            var user_acc = _uow.user_access_repo.Get(filter: U => U.user_id == id).ToList();
            foreach (var item in user_acc)
            {
                _uow.user_access_repo.Delete(item);
                _uow.Save();
            }
        }

        public bool insert_user_access(tbl_user_access obj_user)
        {
            bool bRes = false;

            _uow.user_access_repo.Insert(obj_user);
            _uow.Save();

            if (obj_user.id > 0)
                bRes = true;

            return bRes;
        }


        #region GetSalesPerson
        public IList<SalesPersonModel> GetSalesPerson()
        {
            List<SalesPersonModel> objSalesList = (from s in _context.sales_person
                                                   where s.isDeleted == false 
                                                   select new SalesPersonModel
                                                   {
                                                       contact_address = s.contact_address,
                                                       contact_number = s.contact_number,
                                                       createdOn = s.createdOn,
                                                       email = s.email,
                                                       first_name = s.first_name,
                                                       fullName = (!string.IsNullOrEmpty(s.last_name)) ? s.first_name + " " + s.last_name : s.first_name,
                                                       Id = s.Id,
                                                       isActive = s.isActive,
                                                       isDeleted = s.isDeleted,
                                                       last_name = s.last_name,
                                                       modifiedOn = s.modifiedOn
                                                   }).ToList();

            return objSalesList;
        }
        #endregion

        #region GetBundlePlans
        public IList<BundlePlanModel> GetBundlePlans()
        {
            List<BundlePlanModel> objBundlePlans = (from B in _context.bundle_plan
                                                    join BT in _context.bundle_type on B.bundle_type_id equals BT.id
                                                    where   B.is_deleted == false 
                                                    select new BundlePlanModel
                                                    {
                                                        bundlePlan = B,
                                                        bundleType = BT.bundle_type
                                                    }).ToList();

            return objBundlePlans;
        }
        #endregion

        #region create bundle plan

        public bool CreateBundlePlan(BundlePlan_Model objBPlan, long userId, out string Msg)
        {
            Msg = "Failed to create bundle!";
            bool Res = false;

            if (objBPlan != null)
            {
                #region cctopup BundlePlan
                tbl_bundle_plan obj = new tbl_bundle_plan();
                obj.bundle_id = objBPlan.bundleId;
                obj.bundle_name = objBPlan.planName;
                obj.description = objBPlan.Description;
                obj.bundle_type_id = objBPlan.bundle_type_id;
                obj.price = Convert.ToDecimal(objBPlan.Price);
                obj.validity = objBPlan.Validity.ToString();
                obj.is_selfcare = objBPlan.isSelfcare;
                
                obj.created_on = DateTime.Now;
                obj.is_deleted = false;
                obj.is_active = true;
                obj.updated_by = userId;
                obj.is_api = false;

                if (!string.IsNullOrEmpty(objBPlan.btype))//newly added 
                    obj.btype = objBPlan.btype.ToUpper();
             
                obj.company_id = objBPlan.comp_id;
                _uow.bundle_plan_repo.Insert(obj);
                _uow.Save();

                if (obj.id > 0)
                {
                    Res = true;
                    Msg = "Bundle Plan created successfully";
                }
                #endregion
            }
            if (objBPlan.isSelfcare)
            {
                #region Selfcare Bundleplan
                string sMsg = string.Empty;
                string enc_sfSvcUid = _util_repo.AES_JENC(ConfigurationManager.AppSettings["sf_svc_uid"]);
                string enc_sfSvcPwd = _util_repo.AES_JENC(ConfigurationManager.AppSettings["sf_svc_pwd"]);

                string sData = JsonConvert.SerializeObject(objBPlan);
                string sRes = _scBPlan.CreateBunldePlan(enc_sfSvcUid, enc_sfSvcPwd, sData);
                if (!string.IsNullOrEmpty(sRes))
                {
                    BundleServiceOPModel bundleOP = JsonConvert.DeserializeObject<BundleServiceOPModel>(sRes);
                    if (bundleOP != null)
                    {
                        if (bundleOP.resultCode == "0")
                        {
                            Res = true;
                            sMsg = "The Selfcare Bundle Plan created successfully";
                            Msg = sMsg;
                        }
                        else
                        {
                            sMsg = "ErrorCode:(" + bundleOP.resultCode + "), create failed!";
                            Msg = sMsg;
                        }
                    }

                }
                #endregion
            }
            return Res;
        }

        #endregion

        #region Edit bundle plan

        public bool EditBundlePlan(BundlePlan_Model objBPlan, long userId, out string Msg)
        {
            Msg = "Failed to Update bundle!";
            bool Res = false;
            var obj = _uow.bundle_plan_repo.Get(filter: m => m.id == objBPlan.id).FirstOrDefault();
            if (obj != null)
            {
                #region cctopup BundlePlan
                obj.bundle_id = objBPlan.bundleId;
                obj.bundle_name = objBPlan.planName;
                obj.description = objBPlan.Description;
                obj.bundle_type_id = objBPlan.bundle_type_id;
                obj.price = Convert.ToDecimal(objBPlan.Price);
                obj.validity = objBPlan.Validity.ToString();
                obj.is_selfcare = objBPlan.isSelfcare;
                obj.modified_on = DateTime.Now;
                obj.is_active = objBPlan.isActive;
                obj.is_deleted = false;
                obj.updated_by = userId;
                obj.is_api = false;
                if (!string.IsNullOrEmpty(objBPlan.btype))//newly added 
                    obj.btype = objBPlan.btype.ToUpper();

                _uow.bundle_plan_repo.Update(obj);
                _uow.Save();

                Res = true;
                Msg = "Bundle Plan Updated successfully";
                #endregion
            }
            if (objBPlan.isSelfcare == true)
            {
                #region Selfcare Bundleplan
                string sMsg = string.Empty;
            string enc_sfSvcUid = _util_repo.AES_JENC(ConfigurationManager.AppSettings["sf_svc_uid"]);
            string enc_sfSvcPwd = _util_repo.AES_JENC(ConfigurationManager.AppSettings["sf_svc_pwd"]);

            string sData = JsonConvert.SerializeObject(objBPlan);
            string sRes = _scBPlan.EditBunldePlan(enc_sfSvcUid, enc_sfSvcPwd, sData);
            if (!string.IsNullOrEmpty(sRes))
            {
                BundleServiceOPModel bundleOP = JsonConvert.DeserializeObject<BundleServiceOPModel>(sRes);
                if (bundleOP != null)
                {
                    if (bundleOP.resultCode == "0")
                    {
                        Res = true;
                        sMsg = "The Selfcare Bundle Plan updated successfully";
                        Msg = sMsg;
                    }
                    else
                    {
                        sMsg = "ErrorCode:(" + bundleOP.resultCode + "), Update Failed!";
                        Msg = sMsg;
                    }
                }
            }
                #endregion
            }
            return Res;
        }

        #endregion

        #region Delete bundle plan
        public bool DeleteBundlePlan(long id, long A_id)
        {
            bool res = false;
            if (id > 0)
            {
                tbl_bundle_plan obj = _uow.bundle_plan_repo.GetByID(id);
                if (obj != null)
                {
                    #region cctopup BundlePlan
                    obj.is_deleted = true;
                    obj.is_active = false;
                    obj.updated_by = A_id;
                    _uow.bundle_plan_repo.Update(obj);
                    _uow.Save();

                    res = true;
                    #endregion
                }
                #region selfcare BundlePlan

                //string enc_sfSvcUid = _util_repo.AES_JENC(ConfigurationManager.AppSettings["sf_svc_uid"]);
                //string enc_sfSvcPwd = _util_repo.AES_JENC(ConfigurationManager.AppSettings["sf_svc_pwd"]);

                //string sData = JsonConvert.SerializeObject(obj.bundle_id);
                //string sRes = _scBPlan.DeleteBunldePlan(enc_sfSvcUid, enc_sfSvcPwd, sData);
                //if (!string.IsNullOrEmpty(sRes))
                //{
                //    BundleServiceOPModel bundleOP = JsonConvert.DeserializeObject<BundleServiceOPModel>(sRes);
                //    if (bundleOP == null && bundleOP.resultCode == "1")
                //    {
                //        res = false;
                //    }

                //}
                #endregion

            }
            return res;
        }
        #endregion

        #region Sales by sales person model

        public List<SalesBySalesPersonModel> getSalesDetailsBySalesPerson(DateTime sdate, DateTime edate)
        {

            DateTime dtend = edate.AddDays(1);
            List<SalesBySalesPersonModel> objSales = new List<SalesBySalesPersonModel>();
            objSales = (from CR in _context.company_topup_report
                        join a in _context.company_topup_temp on CR.temp_topup_id equals a.id
                        join c in _context.company_info on CR.company_id equals c.id
                        join s in _context.sales_person on a.sales_person_id equals s.Id into ss
                        from s in ss.DefaultIfEmpty()
                        where c.isdeleted == false && s.isDeleted == false && CR.credited_on >= sdate && CR.credited_on < dtend
                        select new SalesBySalesPersonModel
                        {
                            amount = CR.credit_amount,
                            salesPerson = (a.sales_person_id != null) ? ((s.last_name != null) ? s.first_name + " " + s.last_name : s.first_name) : "",
                            masterName = c.company_name,
                            masterNumber = c.mobile_number,
                            topupDate = CR.credited_on,
                            salesPersonID = a.sales_person_id
                        }).ToList();
            return objSales;
        }

        #endregion

        #region getbundlebyid
        public BundlePlan_Model getbundlebyid(long id)
        {
            BundlePlan_Model obj = new BundlePlan_Model();
            var bdl = _uow.bundle_plan_repo.Get(x => x.id == id && x.is_deleted == false).FirstOrDefault();

            if (bdl != null)
            {
                obj.id = bdl.id;
                obj.bundleId = bdl.bundle_id;
                obj.planName = bdl.bundle_name;
                obj.Price = Convert.ToDouble(bdl.price);
                obj.Validity = Convert.ToInt32(bdl.validity);
                obj.Description = bdl.description;
                obj.bundle_type_id = bdl.bundle_type_id;
                obj.isActive = bdl.is_active;
                obj.isSelfcare = bdl.is_selfcare;
                obj.isApi = bdl.is_api;
                obj.btype = bdl.btype;
            }
            if (obj.isSelfcare == true || obj.isApi == true)
            {
                #region selfcare bundle plan using web service
                string sMsg = string.Empty;
                string enc_sfSvcUid = _util_repo.AES_JENC(ConfigurationManager.AppSettings["sf_svc_uid"]);
                string enc_sfSvcPwd = _util_repo.AES_JENC(ConfigurationManager.AppSettings["sf_svc_pwd"]);

                string sData = JsonConvert.SerializeObject(obj.bundleId);
                string sRes = _scBPlan.getbundleplan_details(enc_sfSvcUid, enc_sfSvcPwd, sData);
                if (!string.IsNullOrEmpty(sRes))
                {
                    getBundlePlanModel bundleOP = JsonConvert.DeserializeObject<getBundlePlanModel>(sRes);
                    if (bundleOP != null && bundleOP.resultcode == "0")
                    {
                        var bund = bundleOP.bundles;

                        obj.isPostpaid = bund.isPostpaid ?? false;
                        obj.orderby = bund.orderby;
                        obj.isVoice = bund.isVoice;
                        obj.Size = bund.Size;
                        obj.AccountType = bund.AccountType;
                        obj.SmsAccountType = bund.SmsAccountType;
                        obj.VoiceAccountType = bund.VoiceAccountType;
                        obj.IddAccountType = bund.IddAccountType;
                        obj.UlAccountType = bund.UlAccountType;
                        obj.voiceSize = bund.voiceSize;
                        obj.smsCount = bund.smsCount;
                        obj.isOnlyData = bund.isOnlyData ?? false;
                    }
                }
                #endregion
            }
            return obj;
        }
        #endregion

        #region create bundle plan using selfcare service

        public string CreateBundlePlanService(BundlePlanServiceModel objPlan)
        {
            string Res = "1";

            //if (!string.IsNullOrEmpty(sData))
            //{
            //    BundlePlanServiceModel objPlan = JsonConvert.DeserializeObject<BundlePlanServiceModel>(sData);

            if (objPlan != null)
            {
                //tbl_bundle_plan objBundle = _uow.bundle_plan_repo.Get(filter: x => x.bundle_id == objPlan.bundle_id && x.is_deleted == false).FirstOrDefault();

                //if (objBundle == null)
                //{
                tbl_bundle_plan obj = new tbl_bundle_plan();
                obj.bundle_id = objPlan.bundle_id;
                obj.bundle_name = objPlan.bundle_Name;
                obj.description = objPlan.desc;
                obj.validity = objPlan.Validity;
                obj.price = objPlan.Price;
                obj.bundle_type_id = objPlan.bundle_type_id;
                obj.is_active = objPlan.isActive;
                obj.is_deleted = false;
                obj.created_on = DateTime.Now;
                obj.is_selfcare = false;
                obj.is_api = true;

                _uow.bundle_plan_repo.Insert(obj);
                _uow.Save();

                if (obj.id > 0)
                {
                    Res = "0";
                }
            }
            else
            {
                Res = "-111";
            }
            //    }
            //}
            return Res;
        }

        #endregion

        #region update bundle plan using selfcare service
        public string UpdateBunldePlanService(BundlePlanServiceModel objPlan, tbl_bundle_plan objBundle)
        {
            string Res = "1";

            //if (!string.IsNullOrEmpty(sData))
            //{
            //    BundlePlanServiceModel objPlan = JsonConvert.DeserializeObject<BundlePlanServiceModel>(sData);
            if (objPlan != null && objBundle != null)
            {
                //tbl_bundle_plan obj = _uow.bundle_plan_repo.Get(x => x.bundle_id == objPlan.bundle_id && x.is_deleted==false).FirstOrDefault();
                //if (obj != null)
                //{
                //obj.Id = objPlan.bundleId;
                objBundle.bundle_name = objPlan.bundle_Name;
                objBundle.description = objPlan.desc;
                objBundle.price = objPlan.Price;
                objBundle.validity = objPlan.Validity;
                objBundle.is_active = objPlan.isActive;
                objBundle.bundle_type_id = objPlan.bundle_type_id;
                objBundle.is_deleted = false;
                objBundle.modified_on = DateTime.Now;
                objBundle.is_selfcare = false;
                objBundle.is_api = true;

                _uow.bundle_plan_repo.Update(objBundle);
                _uow.Save();

                Res = "0";
            }
            else
                Res = "-111";
            //    }
            //}

            return Res;
        }
        #endregion

        #region delete bundle plan using selfcare service
        public string DeleteBunldePlanService(string sData)
        {
            string Res = "1";

            if (!string.IsNullOrEmpty(sData))
            {
                long bid = JsonConvert.DeserializeObject<long>(sData);
                if (bid > 0)
                {
                    tbl_bundle_plan obj = _uow.bundle_plan_repo.Get(x => x.bundle_id == bid && x.is_deleted == false).FirstOrDefault();
                    if (obj != null)
                    {

                        obj.is_deleted = true;
                        obj.is_active = false;

                        _uow.bundle_plan_repo.Update(obj);
                        _uow.Save();

                        Res = "0";
                    }
                    else
                        Res = "-111";
                }
            }

            return Res;
        }
        #endregion

        #region Get Bundle Type Using selfcare service
        public getBundleTypeModel getBundleType(long id)
        {
            getBundleTypeModel bid = new getBundleTypeModel();
            if (id > 0)
            {
                bid.resultCode = "1";
                bid.bdleTypeId = _uow.bundle_plan_repo.Get(filter: x => x.is_deleted == false && x.bundle_id == id).Select(x => x.bundle_type_id).FirstOrDefault();
                if (bid.bdleTypeId > 0)
                {
                    bid.resultCode = "0";
                }

            }
            return bid;
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