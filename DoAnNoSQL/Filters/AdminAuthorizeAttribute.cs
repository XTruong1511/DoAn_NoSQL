using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DoAnNoSQL.Filters
{
    /// <summary>
    /// Custom Authorization Attribute for Role-Based Access Control (RBAC)
    /// Checks if user has required role(s) to access specific admin actions
    /// </summary>
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Comma-separated role IDs (e.g., "1" for Admin only, "1,2" for Admin and Staff)
        /// Role 1 = Admin (Full Access)
        /// Role 2 = Staff (Limited Access)
        /// </summary>
        public string Roles { get; set; }

        /// <summary>
        /// Executes before the action method is invoked
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var httpContext = filterContext.HttpContext;

            // 1. Check if user is logged in
            if (httpContext.Session["User"] == null)
            {
                // User not logged in - redirect to login page with return URL
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "controller", "Admin" },
                        { "action", "Login" },
                        { "returnUrl", httpContext.Request.RawUrl }
                    }
                );
                return;
            }

            // 2. Check if user has role assigned
            var userRole = httpContext.Session["Role"];
            
            // Debug logging
            System.Diagnostics.Debug.WriteLine($"[AdminAuthorize] User: {httpContext.Session["UserName"]}, Role: {userRole}, Required: {Roles}");
            
            if (userRole == null)
            {
                // Try to get from IsAdmin flag as fallback
                var isAdmin = httpContext.Session["IsAdmin"];
                if (isAdmin != null && (bool)isAdmin)
                {
                    // Admin user without role - assign role 1
                    userRole = 1;
                    httpContext.Session["Role"] = 1;
                    System.Diagnostics.Debug.WriteLine("[AdminAuthorize] Fallback: Set role to 1 (Admin)");
                }
                else
                {
                    // No role assigned and not admin - redirect to access denied page
                    System.Diagnostics.Debug.WriteLine("[AdminAuthorize] DENIED: No role assigned");
                    filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary
                        {
                            { "controller", "Account" },
                            { "action", "AccessDenied" }
                        }
                    );
                    return;
                }
            }

            // 3. Check if Roles property is set
            if (string.IsNullOrEmpty(Roles))
            {
                // No role restriction - allow access
                base.OnActionExecuting(filterContext);
                return;
            }

            // 4. Check if user's role is in the allowed roles list
            var allowedRoles = Roles.Split(',').Select(r => r.Trim()).ToList();
            var userRoleId = userRole.ToString();

            if (!allowedRoles.Contains(userRoleId))
            {
                // User doesn't have required role - redirect to access denied
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "AccessDenied" }
                    }
                );
                return;
            }

            // User has required role - allow access
            base.OnActionExecuting(filterContext);
        }
    }
}
