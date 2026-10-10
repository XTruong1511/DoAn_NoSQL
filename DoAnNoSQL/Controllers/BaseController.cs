using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAnNoSQL.Models;

namespace DoAnNoSQL.Controllers
{
    /// <summary>
    /// Base Controller cho cc controller c?n authentication
    /// </summary>
    public class BaseController : Controller
    {
        protected MongoDbContext db = new MongoDbContext();

        /// <summary>
        /// Kiểm tra user đã đăng nhập chưa
        /// </summary>
        protected bool IsAuthenticated
        {
            get { return Session["User"] != null; }
        }

        /// <summary>
        /// Lấy thông tin user hiện tại
        /// </summary>
        protected UserSessionViewModel CurrentUser
        {
            get { return Session["User"] as UserSessionViewModel; }
        }

        /// <summary>
        /// Kiểm tra user có phải admin không
        /// </summary>
        protected bool IsAdmin
        {
            get
            {
                var user = CurrentUser;
                return user != null && user.LaQuanTri;
            }
        }

        /// <summary>
        /// Action filter: Yêu cầu đăng nhập
        /// </summary>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Lưu thông tin user vào ViewBag để dùng trong View
            if (IsAuthenticated)
            {
                ViewBag.CurrentUser = CurrentUser;
                ViewBag.IsAuthenticated = true;
                ViewBag.IsAdmin = IsAdmin;
            }
            else
            {
                ViewBag.IsAuthenticated = false;
                ViewBag.IsAdmin = false;
            }

            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// Dispose DbContext
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Attribute yêu cầu đăng nhập
    /// </summary>
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Session["User"] == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "Login" },
                        { "returnUrl", filterContext.HttpContext.Request.Url.PathAndQuery }
                    });
            }

            base.OnActionExecuting(filterContext);
        }
    }

    /// <summary>
    /// Attribute yêu cầu quyền admin
    /// </summary>
    public class RequireAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var user = filterContext.HttpContext.Session["User"] as UserSessionViewModel;

            if (user == null)
            {
                // Chưa đăng nhập
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "Login" },
                        { "returnUrl", filterContext.HttpContext.Request.Url.PathAndQuery }
                    });
            }
            else if (!user.LaQuanTri)
            {
                // Không có quyền admin
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Home" },
                        { "action", "Index" }
                    });

                filterContext.Controller.TempData["ErrorMessage"] = "B?n khng c quy?n truy c?p trang ny!";
            }

            base.OnActionExecuting(filterContext);
        }
    }
}



