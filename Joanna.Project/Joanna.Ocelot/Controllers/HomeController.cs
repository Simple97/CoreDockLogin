using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Joanna.Ocelot.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace Joanna.Ocelot.Controllers
{
    public class HomeController : Controller
    {
        private IHttpContextAccessor _accessor;
        public HomeController(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }
#if DEBUG
        private string idUrl = "http://192.168.1.179:8087/";
        //private string idUrl = " http://localhost:54459/";
        private string passportApiUrl = "http://192.168.1.179:8088/";
#else
        private string idUrl = "http://id.lsmaps.com/";
        private string passportApiUrl = "http://passport.lsmaps.com/";
#endif

        /// <summary>
        /// 判断当前登陆
        /// </summary>
        /// <param name="ReturnUrl"></param>
        /// <returns></returns>
        public IActionResult Index(string ReturnUrl = "")
        {
            var user = _accessor.HttpContext.User;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                var loginUrl = idUrl + $"Login?ReturnUrl=http://{_accessor.HttpContext.Request.Host.Host}:{HttpContext.Request.Host.Port}{ReturnUrl}";
                Response.Redirect(loginUrl);
            }
            return View();
        }

        /// <summary>
        /// 验证登录
        /// </summary>
        /// <param name="ticket"></param>
        /// <param name="returnUrl"></param>
        public IActionResult VerifyLoginAsync(string ticket, string returnUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(ticket) && string.IsNullOrEmpty(returnUrl))
                {
                    return Ok("验证Ticket失败");
                }
                //验证Ticket
                var verifyTicketUrl = string.Format(passportApiUrl + "api/VerifyTicket?ticket={0}", ticket);
                var web = new WebClient { Encoding = Encoding.UTF8 };
                var data = web.DownloadString(verifyTicketUrl);
                var result = JsonConvert.DeserializeObject<ResParameter>(data);
                if (result.code != "200")
                {
                    return Ok(result);
                }
                //设置当前登录状态为 true
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, result.data.ToString()),
                    new Claim("FullName", "Joanna.Zhang")
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2),
                    IsPersistent = true,
                    IssuedUtc = DateTimeOffset.Now,
                    RedirectUri = "../Home/Index",
                };
                //await HttpContext.SignInAsync(
                //     CookieAuthenticationDefaults.AuthenticationScheme,
                //     new ClaimsPrincipal(claimsIdentity),
                //     //, authProperties
                //     new AuthenticationProperties
                //     {
                //         IsPersistent = true //  永久Cookie
                //     }
                //     );

                Task.Run(async () =>
                {
                    await HttpContext.SignInAsync(
                     CookieAuthenticationDefaults.AuthenticationScheme,
                     new ClaimsPrincipal(claimsIdentity),
                     //, authProperties
                     new AuthenticationProperties
                     {
                         IsPersistent = true //  永久Cookie
                     }
                     );
                }).Wait();


                return RedirectToAction("Index", "Home");
            }
            catch (Exception e)
            {
                return Ok(e.Message.ToString());
            }
        }

        /// <summary>
        /// 退出
        /// </summary>
        /// <returns></returns>
        public async Task ExitAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var loginUrl = idUrl + $"Login/Exit?ReturnUrl=http://{_accessor.HttpContext.Request.Host.Host}:{HttpContext.Request.Host.Port}";
            Response.Redirect(loginUrl);
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        //[Authorize]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }



    }
}
