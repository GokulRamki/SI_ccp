using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SI_ccp.Controllers
{
    public class HomeController : Controller
    {


        public ActionResult Index()
        {
            
            return RedirectToAction("Login", "Company");
        }

     
    }
}
