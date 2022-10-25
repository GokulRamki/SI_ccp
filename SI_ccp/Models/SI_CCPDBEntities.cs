using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace SI_ccp.Models
{
    public class SI_CCPDBEntities :  DbContext
    {

        public DbSet<tbl_company_info> company_info { get; set; }

        public DbSet<tbl_company_topup_temp> company_topup_temp { get; set; }

        public DbSet<tbl_company_topup_report> company_topup_report { get; set; }

        public DbSet<tbl_role> role { get; set; }

        public DbSet<tbl_user> user { get; set; }

        public DbSet<tbl_staff_topup> staff_topup {get; set;}

        public DbSet<tbl_staff_topup_bundle> staff_topup_bundle { get; set; }

        public DbSet<tbl_staff_topup_trans> staff_topup_trans { get; set; }

        public DbSet<tbl_payment_type> payment_type { get; set; }

        public DbSet<tbl_menu> menu { get; set; }

        public DbSet<tbl_user_access> user_access { get; set; }

        public DbSet<tbl_admin_user> admin_user { get; set; }        

        public DbSet<tbl_bundle_type> bundle_type { get; set; }

        public DbSet<tbl_bundle_plan> bundle_plan { get; set; }

        public DbSet<tbl_approve_status> approve_status { get; set; }

        public DbSet<tbl_sales_person> sales_person { get; set; }

        public DbSet<tbl_si_ccp_invoice> si_ccp_invoice { get; set; }


        //newly added
        public DbSet<tbl_ccp_report_cat> ccp_rptcat { get; set; }

        public DbSet<tbl_ccp_track> ccp_track { get; set; }

        public DbSet<tbl_ccp_track_action> ccp_action { get; set; }
        public DbSet<tbl_user_login_his> user_login_his { get; set; }

        public DbSet<tbl_company_login_his> company_login_his { get; set; }
    }
}