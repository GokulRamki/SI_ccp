using SI_ccp.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace SI_ccp.DAL
{
    public class UnitOfWork : DbContext, IDisposable
    {
        private SI_CCPDBEntities context = new SI_CCPDBEntities();

        #region company info

        private GenericRepository<tbl_company_info> company_info;
        public GenericRepository<tbl_company_info> company_info_repo
        {
            get
            {
                if (this.company_info == null)
                    this.company_info = new GenericRepository<tbl_company_info>(context);

                return company_info;
            }
        }

        #endregion

        #region role

        private GenericRepository<tbl_role> role;
        public GenericRepository<tbl_role> role_repo
        {
            get
            {
                if (this.role == null)
                    this.role = new GenericRepository<tbl_role>(context);

                return role;
            }
        }

        #endregion

        #region company topup report

        private GenericRepository<tbl_company_topup_report> company_topup_report;
        public GenericRepository<tbl_company_topup_report> company_topup_report_repo
        {
            get
            {
                if (this.company_topup_report == null)
                    this.company_topup_report = new GenericRepository<tbl_company_topup_report>(context);

                return company_topup_report;
            }
        }

        #endregion

        #region company_topup_temp

        private GenericRepository<tbl_company_topup_temp> company_topup_temp;
        public GenericRepository<tbl_company_topup_temp> company_topup_temp_repo
        {
            get
            {
                if (this.company_topup_temp == null)
                    this.company_topup_temp = new GenericRepository<tbl_company_topup_temp>(context);

                return company_topup_temp;
            }
        }

        #endregion

        #region user

        private GenericRepository<tbl_user> user;
        public GenericRepository<tbl_user> user_repo
        {
            get
            {
                if (this.user == null)
                    this.user = new GenericRepository<tbl_user>(context);

                return user;
            }
        }

        #endregion

        #region staff topup

        private GenericRepository<tbl_staff_topup> staff_topup;
        public GenericRepository<tbl_staff_topup> staff_topup_repo
        {
            get
            {
                if (this.staff_topup == null)
                    this.staff_topup = new GenericRepository<tbl_staff_topup>(context);

                return staff_topup;
            }
        }

        #endregion

        #region staff topup bundle

        private GenericRepository<tbl_staff_topup_bundle> staff_topup_bundle;
        public GenericRepository<tbl_staff_topup_bundle> staff_topup_bundle_repo
        {
            get
            {
                if (this.staff_topup_bundle == null)
                    this.staff_topup_bundle = new GenericRepository<tbl_staff_topup_bundle>(context);

                return staff_topup_bundle;
            }
        }

        #endregion

        #region staff topup trans

        private GenericRepository<tbl_staff_topup_trans> staff_topup_trans;
        public GenericRepository<tbl_staff_topup_trans> staff_topup_trans_repo
        {
            get
            {
                if (this.staff_topup_trans == null)
                    this.staff_topup_trans = new GenericRepository<tbl_staff_topup_trans>(context);

                return staff_topup_trans;
            }
        }

        #endregion

        #region payment type

        private GenericRepository<tbl_payment_type> payment_type;
        public GenericRepository<tbl_payment_type> payment_type_repo
        {
            get
            {
                if (this.payment_type == null)
                    this.payment_type = new GenericRepository<tbl_payment_type>(context);

                return payment_type;
            }
        }

        #endregion

        #region menu

        private GenericRepository<tbl_menu> menu;
        public GenericRepository<tbl_menu> menu_repo
        {
            get
            {
                if (this.menu == null)
                    this.menu = new GenericRepository<tbl_menu>(context);

                return menu;
            }
        }

        #endregion

        #region user access

        private GenericRepository<tbl_user_access> user_access;
        public GenericRepository<tbl_user_access> user_access_repo
        {
            get
            {
                if (this.user_access == null)
                    this.user_access = new GenericRepository<tbl_user_access>(context);

                return user_access;
            }
        }

        #endregion

        #region admin user 

        private GenericRepository<tbl_admin_user> admin_user;
        public GenericRepository<tbl_admin_user> admin_user_repo
        {
            get
            {
                if (this.admin_user == null)
                    this.admin_user = new GenericRepository<tbl_admin_user>(context);

                return admin_user;
            }
        }

        #endregion

        #region bundle_type

        private GenericRepository<tbl_bundle_type> bundle_type;
        public GenericRepository<tbl_bundle_type> bundle_type_repo
        {
            get
            {
                if (this.bundle_type == null)
                    this.bundle_type = new GenericRepository<tbl_bundle_type>(context);

                return bundle_type;
            }
        }

        #endregion

        #region bundle_plan

        private GenericRepository<tbl_bundle_plan> bundle_plan;
        public GenericRepository<tbl_bundle_plan> bundle_plan_repo
        {
            get
            {
                if (this.bundle_plan == null)
                    this.bundle_plan = new GenericRepository<tbl_bundle_plan>(context);

                return bundle_plan;
            }
        }

        #endregion

        #region approve_status

        private GenericRepository<tbl_approve_status> approve_status;
        public GenericRepository<tbl_approve_status> approve_status_repo
        {
            get
            {
                if (this.approve_status == null)
                    this.approve_status = new GenericRepository<tbl_approve_status>(context);

                return approve_status;
            }
        }

        #endregion

        #region sales person
        private GenericRepository<tbl_sales_person> sales_person_repo;

        public GenericRepository<tbl_sales_person> sales_person_Repo
        {
            get
            {
                if (this.sales_person_repo == null)
                    this.sales_person_repo = new GenericRepository<tbl_sales_person>(context);

                return this.sales_person_repo;
            }
        }
        #endregion

        #region table invocie

        private GenericRepository<tbl_si_ccp_invoice> ccp_invoice;
        public GenericRepository<tbl_si_ccp_invoice> ccp_invoice_Repo
        {
            get
            {
                if (this.ccp_invoice == null)
                    this.ccp_invoice = new GenericRepository<tbl_si_ccp_invoice>(context);


                return this.ccp_invoice;
            }
        }

        #endregion

        #region db save

        public void Save()
        {
            context.SaveChanges();
        }

        #endregion


        #region ccp_track
        private GenericRepository<tbl_ccp_track> ccp_track;

        public GenericRepository<tbl_ccp_track> ccp_track_Repo
        {
            get
            {
                if (this.ccp_track == null)
                    this.ccp_track = new GenericRepository<tbl_ccp_track>(context);

                return this.ccp_track;
            }
        }
        #endregion

        #region ccp_action
        private GenericRepository<tbl_ccp_track_action> ccp_action;

        public GenericRepository<tbl_ccp_track_action> ccp_action_Repo
        {
            get
            {
                if (this.ccp_action == null)
                    this.ccp_action = new GenericRepository<tbl_ccp_track_action>(context);

                return this.ccp_action;
            }
        }
        #endregion

        #region ccp_rptcat
        private GenericRepository<tbl_ccp_report_cat> ccp_rptcat;

        public GenericRepository<tbl_ccp_report_cat> ccp_rptcat_Repo
        {
            get
            {
                if (this.ccp_rptcat == null)
                    this.ccp_rptcat = new GenericRepository<tbl_ccp_report_cat>(context);

                return this.ccp_rptcat;
            }
        }
        #endregion


        #region User Login History

        private GenericRepository<tbl_user_login_his> user_login_his;
        public GenericRepository<tbl_user_login_his> user_login_his_repo
        {
            get
            {

                if (this.user_login_his == null)
                {
                    this.user_login_his = new GenericRepository<tbl_user_login_his>(context);
                }
                return user_login_his;
            }
        }

        #endregion

        #region Company Login History

        private GenericRepository<tbl_company_login_his> company_login_his;
        public GenericRepository<tbl_company_login_his> company_login_his_repo
        {
            get
            {

                if (this.company_login_his == null)
                {
                    this.company_login_his = new GenericRepository<tbl_company_login_his>(context);
                }
                return company_login_his;
            }
        }

        #endregion

        #region Disposal

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    context.Dispose();
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