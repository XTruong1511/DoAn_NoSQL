using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DoAnNoSQL
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "KhuyenMaiPage",
                url: "khuyen-mai",
                defaults: new { controller = "KhuyenMai", action = "Index" },
                namespaces: new[] { "DoAnNoSQL.Controllers" }
            );

            routes.MapRoute(
                name: "SaleCuoiNamPage",
                url: "sale-cuoi-nam",
                defaults: new { controller = "KhuyenMai", action = "Index" },
                namespaces: new[] { "DoAnNoSQL.Controllers" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "DoAnNoSQL.Controllers" }
            );
        }
    }
}
