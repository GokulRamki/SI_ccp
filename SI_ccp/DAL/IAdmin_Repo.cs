using SI_ccp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SI_ccp.DAL
{
    interface IAdmin_Repo : IDisposable
    {

        tbl_admin_user check_admin_login(admin_login_model obj_login);

        List<user_access_model> get_user_access_list(long? user_id);

        List<tbl_menu> get_user_menus(long user_id);

        bool check_menu_access(long user_id, int role_id, string viewname);

        List<userlist_model> get_admin_users();

        List<tbl_role> get_admin_user_roles();

        user_model userby_id(long id);

        bool check_admin_user_email_exist(string email, long? id);

        tbl_admin_user get_admin_userdetails(long id);

        bool insert_admin_user(tbl_admin_user obj_user);

        bool update_admin_user(tbl_admin_user obj_user);

        bool delete_admin_user(long id);

        void delete_user_accessby_userid(long id);

        bool insert_user_access(tbl_user_access obj_user);

        IList<SalesPersonModel> GetSalesPerson();

        IList<BundlePlanModel> GetBundlePlans();

        bool CreateBundlePlan(BundlePlan_Model objBPlan, long userId, out string Msg);

        List<SalesBySalesPersonModel> getSalesDetailsBySalesPerson(DateTime sdate, DateTime edate);

        string CreateBundlePlanService(BundlePlanServiceModel objplan);

        string UpdateBunldePlanService(BundlePlanServiceModel objplan, tbl_bundle_plan objBundle);

        string DeleteBunldePlanService(string sData);

        BundlePlan_Model getbundlebyid(long id);

        bool EditBundlePlan(BundlePlan_Model objBPlan, long userId, out string Msg);

        getBundleTypeModel getBundleType(long id);

        bool DeleteBundlePlan(long id, long A_id);
    }
}
