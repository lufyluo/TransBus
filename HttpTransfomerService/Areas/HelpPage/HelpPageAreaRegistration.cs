using System;
using System.Web.Http;
using System.Web.Mvc;

namespace HttpTransfomerService.Areas.HelpPage
{
    public class HelpPageAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "HelpPage";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            try
            {
                context.MapRoute(
                    "HelpPage_Default",
                    "Help/{action}/{apiId}",
                    new { controller = "Help", action = "Index", apiId = UrlParameter.Optional });

                HelpPageConfig.Register(GlobalConfiguration.Configuration);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
        }
    }
}