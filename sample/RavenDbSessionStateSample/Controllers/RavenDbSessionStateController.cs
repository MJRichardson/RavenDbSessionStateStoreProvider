using System.Web.Mvc;

namespace RavenDbSessionStateSample.Controllers
{
    public class RavenDbSessionStateController : Controller
    {
        private const string SessionStateKey = "__key";

         public ViewResult Index()
         {
             ViewBag.Value = Session[SessionStateKey];

             return View();
         }

        public ActionResult SetValue( string value)
        {
            Session[SessionStateKey] = value;

            return RedirectToAction("Index");
        }
    }
}