using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace HeThong_BanVeMayBay.Attributes
{
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string _permission;

        public AdminAuthorizeAttribute(string permission = "")
        {
            _permission = permission;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userRole = context.HttpContext.Session.GetString("UserRole");
            var userPermissions = context.HttpContext.Session.GetString("UserPermissions") ?? "";

            // Chấp nhận cả "Staff" và "Nhân viên"
            bool isAuthorizedRole = userRole == "Admin" || userRole == "Staff" || userRole == "Nhân viên";

            // Nếu chưa đăng nhập hoặc không đúng vai trò thì đá ra trang Login
            if (string.IsNullOrEmpty(userRole) || !isAuthorizedRole)
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
                return;
            }

            // Nếu là Admin thì có toàn quyền (ALL)
            if (userPermissions == "ALL")
            {
                base.OnActionExecuting(context);
                return;
            }

            // Nếu yêu cầu quyền cụ thể
            if (!string.IsNullOrEmpty(_permission))
            {
                var list = userPermissions.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
                if (!list.Contains(_permission))
                {
                    // Nếu không có quyền, chuyển hướng về trang Thông tin cá nhân (trang an toàn cho mọi nhân viên)
                    // Tránh chuyển về Dashboard vì có thể gây vòng lặp nếu họ cũng không có quyền Dashboard
                    context.Result = new RedirectToActionResult("ThongTinCaNhan", "Profile", new { area = "Admin" });
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
