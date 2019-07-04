using Joanna.Ocelot.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Joanna.Ocelot.Controllers
{
    public class AuthenticationAttribute : ActionFilterAttribute
    {
#if DEBUG
        private string passportApiUrl = "http://192.168.1.179:8088/";
#else
        private string passportApiUrl = "http://passport.lsmaps.com/";
#endif

        /// <inheritdoc />
        /// <summary>
        /// 每一次Action执行前，执行
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                var user = context.HttpContext.User;
                if (user != null && user.Identity.IsAuthenticated && !string.IsNullOrEmpty(user.Identity.Name))
                {
                    var verifyTicketUrl = string.Format(passportApiUrl + "api/VerifyToken?token={0}", user.Identity.Name);
                    var web = new WebClient { Encoding = Encoding.UTF8 };
                    var data = web.DownloadString(verifyTicketUrl);
                    var result = JsonConvert.DeserializeObject<ResParameter>(data);
                    if (result.code != "200")
                    {
                        //本地退出
                        Task.Run(async () =>
                        {
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }).Wait();
                        //跳转提示页面（提示页面不受权限控制）
                        RouteValueDictionary dictionary = new RouteValueDictionary
                        (new
                        {
                            controller = "Prompt",
                            action = "Index",
                            returnUrl = context.HttpContext.Request
                        });
                        context.Result = new RedirectToRouteResult(dictionary);
                    }
                }
                base.OnActionExecuting(context);
            }
            catch (Exception e)
            {
                Task.Run(async () =>
                {
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }).Wait();
            }
        }

    }
}