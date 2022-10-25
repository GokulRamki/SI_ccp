using SI_ccp.DAL;
using SI_ccp.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using SI_ccp.Models;
using System.Configuration;
using Newtonsoft.Json;

namespace SI_ccp.Service
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "BundlePlanSI" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select BundlePlanSI.svc or BundlePlanSI.svc.cs at the Solution Explorer and start debugging.
    public class BundlePlanSI : IBundlePlanSI, IDisposable
    {
        private UnitOfWork _uow;
        private IUtilityRepo _util_repo;
        private IAdmin_Repo _admin_repo;

        public BundlePlanSI()
        {
            this._uow = new UnitOfWork();
            this._util_repo = new UtilityRepo();
            this._admin_repo = new Admin_Repo();
        }

        public string CreateorEdit_BunldePlanSICc(string sUname, string sPwd, string sData)
        {
            ReturnModel obj = new ReturnModel();
            obj.resultCode = "-555";

            try
            {
                bool Res = AuthenticateService(sUname, sPwd);
                if (Res)
                {
                    if (!string.IsNullOrEmpty(sData))
                    {
                        BundlePlanServiceModel objPlan = JsonConvert.DeserializeObject<BundlePlanServiceModel>(sData);

                        if (objPlan != null)
                        {
                            tbl_bundle_plan objBundle = _uow.bundle_plan_repo.Get(filter: x => x.bundle_id == objPlan.bundle_id && x.is_deleted == false).FirstOrDefault();

                            if (objBundle == null)
                            {
                                obj.resultCode = _admin_repo.CreateBundlePlanService(objPlan);
                            }
                            else 
                            {
                                obj.resultCode = _admin_repo.UpdateBunldePlanService(objPlan, objBundle);
                            }
                        }
                    }
                }
                else
                    obj.resultCode = "-222";
            }
            catch (System.Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex.Message, ex.StackTrace);
            }

            string data = JsonConvert.SerializeObject(obj);

            return data;
        }

        //public string UpdateBunldePlanSICc(string sUname, string sPwd, string sData)
        //{
        //    ReturnModel obj = new ReturnModel();
        //    obj.resultCode = "-555";

        //    try
        //    {
        //        bool Res = AuthenticateService(sUname, sPwd);
        //        if (Res)
        //        {
        //            //obj.resultCode = _admin_repo.UpdateBunldePlanService(sData);
        //        }
        //        else
        //            obj.resultCode = "-222";
        //    }
        //    catch (System.Exception ex)
        //    {
        //        _util_repo.ErrorLog_Txt(ex.Message, ex.StackTrace);
        //    }

        //    string data = JsonConvert.SerializeObject(obj);

        //    return data;
        //}

        public string DeleteBunldePlanSICc(string sUname, string sPwd, string sData)
        {
            ReturnModel obj = new ReturnModel();
            obj.resultCode = "-555";

            try
            {
                bool Res = AuthenticateService(sUname, sPwd);
                if (Res)
                {
                    obj.resultCode = _admin_repo.DeleteBunldePlanService(sData);
                }
                else
                    obj.resultCode = "-222";
            }
            catch (System.Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex.Message, ex.StackTrace);
            }

            string data = JsonConvert.SerializeObject(obj);

            return data;
        }

        public string GetBundleType(string sUname, string sPwd, string sData)
        {
            getBundleTypeModel obj = new getBundleTypeModel();
            obj.resultCode = "-555";

            try
            {
                bool Res = AuthenticateService(sUname, sPwd);
                if (Res)
                {
                    if (sData != null)
                    {
                        long bid = JsonConvert.DeserializeObject<long>(sData);
                        if (bid > 0)
                        {
                            obj = _admin_repo.getBundleType(bid);
                        }
                    }
                }
                else
                    obj.resultCode = "-222";
            }
            catch (System.Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex.Message, ex.StackTrace);
            }

            string data = JsonConvert.SerializeObject(obj);

            return data;
        }

        #region Service Authentication

        private bool AuthenticateService(string sUname, string sPwd)
        {
            bool bResult = false;
            try
            {
                string _Uname = _util_repo.AES_JENC(ConfigurationManager.AppSettings["sf_svc_uid"]);
                string _Pwd = _util_repo.AES_JENC(ConfigurationManager.AppSettings["sf_svc_pwd"]);
                if (_Uname == sUname && _Pwd == sPwd)
                    bResult = true;
            }
            catch (System.Exception ex)
            {
                _util_repo.ErrorLog_Txt(ex.Message, ex.StackTrace);
            }
            return bResult;
        }

        #endregion

        #region Dispose Objects

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _uow.Dispose();
                _util_repo.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
