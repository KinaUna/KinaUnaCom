﻿using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.HomeViewModels;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace KinaUnaWeb.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private int _progId = 2;
        private readonly WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ImageStore _imageStore;
        private readonly IHostingEnvironment _env;
        private readonly string _defaultUser = "testuser@niviaq.com";
        
        public HomeController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, WebDbContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ImageStore imageStore, IHostingEnvironment env)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _imageStore = imageStore;
            _env = env;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int id = 0)
        {
            int childId = id;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            string userTimeZone = HttpContext.User.FindFirst("timezone")?.Value ?? "Romance Standard Time";
            if (string.IsNullOrEmpty(userTimeZone))
            {
                userTimeZone = "Romance Standard Time";
            }
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            if (progeny.Name == "401")
            {
                var returnUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                return RedirectToAction("CheckOut", "Account", new{returnUrl});
            }
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = 5;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                userAccessLevel = 0;
            }

            if (progeny.BirthDay.HasValue)
            {
                progeny.BirthDay = DateTime.SpecifyKind(progeny.BirthDay.Value, DateTimeKind.Unspecified);
            }
            
            HomeFeedViewModel feedModel = new HomeFeedViewModel();
            feedModel.UserAccessLevel = 5;
            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    feedModel.UserAccessLevel = userAccess.AccessLevel;
                }
                else
                {
                    ViewBag.OriginalProgeny = progeny;
                    progeny = await _progenyHttpClient.GetProgeny(2);
                }
            }
            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                feedModel.UserAccessLevel = 0;
            }
            
            BirthTime progBirthTime;
            if (!String.IsNullOrEmpty(progeny.NickName) && progeny.BirthDay.HasValue && feedModel.UserAccessLevel < 5)
            {
                progBirthTime = new BirthTime(progeny.BirthDay.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
            }
            else
            {
                progBirthTime = new BirthTime(new DateTime(2018, 02, 18, 18, 02, 00), TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
            }

            feedModel.CurrentTime = progBirthTime.CurrentTime;
            feedModel.Years = progBirthTime.CalcYears();
            feedModel.Months = progBirthTime.CalcMonths();
            feedModel.Weeks = progBirthTime.CalcWeeks();
            feedModel.Days = progBirthTime.CalcDays();
            feedModel.Hours = progBirthTime.CalcHours();
            feedModel.Minutes = progBirthTime.CalcMinutes();
            feedModel.NextBirthday = progBirthTime.CalcNextBirthday();
            feedModel.MinutesMileStone = progBirthTime.CalcMileStoneMinutes();
            feedModel.HoursMileStone = progBirthTime.CalcMileStoneHours();
            feedModel.DaysMileStone = progBirthTime.CalcMileStoneDays();
            feedModel.WeeksMileStone = progBirthTime.CalcMileStoneWeeks();

            
            Picture tempPicture = new Picture();
            tempPicture.ProgenyId = 0;
            tempPicture.Progeny = progeny;
            tempPicture.AccessLevel = 5;
            tempPicture.PictureLink600 = $"https://{Request.Host}{Request.PathBase}" + "/photodb/0/default_temp.jpg";
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);

            Picture displayPicture = tempPicture;

            if (feedModel.UserAccessLevel < 5)
            {
                displayPicture = await _mediaHttpClient.GetRandomPicture(progeny.Id, feedModel.UserAccessLevel, userTimeZone);
            }
            PictureTime picTime = new PictureTime(new DateTime(2018, 02, 18, 20, 18, 00), new DateTime(2018, 02, 18, 20, 18, 00), TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
            if (feedModel.UserAccessLevel == 5 || displayPicture == null)
            {
                displayPicture = await _mediaHttpClient.GetRandomPicture(2, feedModel.UserAccessLevel, userTimeZone);
                if (!displayPicture.PictureLink600.StartsWith("https://"))
                {
                    displayPicture.PictureLink600 = _imageStore.UriFor(displayPicture.PictureLink600);
                }

                feedModel.ImageLink600 = displayPicture.PictureLink600;
                feedModel.ImageId = displayPicture.PictureId;
                picTime = new PictureTime(new DateTime(2018, 02, 18, 20, 18, 00), displayPicture.PictureTime, TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
                feedModel.Tags = displayPicture.Tags;
                feedModel.Location = displayPicture.Location;
                feedModel.PicTimeValid = false;
            }
            else
            {
                if (!displayPicture.PictureLink600.StartsWith("https://"))
                {
                    displayPicture.PictureLink600 = _imageStore.UriFor(displayPicture.PictureLink600);
                }

                feedModel.ImageLink600 = displayPicture.PictureLink600;
                feedModel.ImageId = displayPicture.PictureId;
                if (displayPicture.PictureTime != null && progeny.BirthDay.HasValue)
                {
                    picTime = new PictureTime(progeny.BirthDay.Value,
                        displayPicture.PictureTime,
                        TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
                    feedModel.PicTimeValid = true;
                }

                feedModel.Tags = displayPicture.Tags;
                feedModel.Location = displayPicture.Location;
            }
            feedModel.PicTime = picTime.PictureDateTime;
            feedModel.PicYears = picTime.CalcYears();
            feedModel.PicMonths = picTime.CalcMonths();
            feedModel.PicWeeks = picTime.CalcWeeks();
            feedModel.PicDays = picTime.CalcDays();
            feedModel.PicHours = picTime.CalcHours();
            feedModel.PicMinutes = picTime.CalcMinutes();
            

            feedModel.Progeny = progeny;
            feedModel.EventsList = new List<CalendarItem>();
            feedModel.EventsList = await _context.CalendarDb
                .Where(e => e.ProgenyId == progeny.Id && e.EndTime > DateTime.UtcNow && e.AccessLevel >= userAccessLevel).ToListAsync();
            feedModel.EventsList = feedModel.EventsList.OrderBy(e => e.StartTime).ToList();
            feedModel.EventsList = feedModel.EventsList.Take(5).ToList();
            foreach (CalendarItem ev in feedModel.EventsList)
            {
                if (ev.StartTime.HasValue && ev.EndTime.HasValue)
                {
                    ev.StartTime = TimeZoneInfo.ConvertTimeFromUtc(ev.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                    ev.EndTime = TimeZoneInfo.ConvertTimeFromUtc(ev.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                }
            }

            feedModel.LatestPosts = new TimeLineViewModel();
            feedModel.LatestPosts.TimeLineItems = new List<TimeLineItem>();
            feedModel.LatestPosts.TimeLineItems = await _context.TimeLineDb.Where(t => t.ProgenyId == _progId && t.AccessLevel >= userAccessLevel && t.ProgenyTime < DateTime.UtcNow).ToListAsync();
            if (feedModel.LatestPosts.TimeLineItems.Any())
            {
                feedModel.LatestPosts.TimeLineItems = feedModel.LatestPosts.TimeLineItems.OrderByDescending(t => t.ProgenyTime).Take(5).ToList();
            }


            return View(feedModel);
        }

        [Authorize]
        public IActionResult RequestAccess(int childId)
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult About()
        {
            return View();
        }

        public async Task<IActionResult> Contact()
        {
            var claims = ((ClaimsIdentity)User.Identity).Claims;
            ViewBag.TokenInfo = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);
            ViewBag.TimeZone = claims.FirstOrDefault(x => x.Type == "timezone")?.Value;
            ViewBag.ViewChild = HttpContext.User.FindFirst("viewchild")?.Value;
            return View(claims);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Terms()
        {

            return View();
        }

        [AllowAnonymous]
        public IActionResult Test()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetViewChild(int childId, string userId, string userEmail, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                HttpClient httpClient = new HttpClient();
                string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
                var currentContext = _httpContextAccessor.HttpContext;
                string accessToken = await AuthenticationHttpContextExtensions.GetTokenAsync(currentContext, OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    httpClient.SetBearerToken(accessToken);
                }
                httpClient.BaseAddress = new Uri(clientUri);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                
                UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
                userinfo.ViewChild = childId;
                
                string setChildApiPath = "/api/userinfo/" + userId;
                var setChildUri = clientUri + setChildApiPath;
                await httpClient.PutAsJsonAsync(setChildUri, userinfo);
            }

            return Redirect(returnUrl);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            if (_env.IsDevelopment())
            {
                Response.Cookies.Append(
                    "KinaUnaLanguage",
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );
            }
            else
            {
                Response.Cookies.Append(
                    "KinaUnaLanguage",
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Domain = ".kinauna.com" }
                );
            }
            return Redirect(returnUrl);
        }

        [AllowAnonymous]
        public IActionResult Start()
        {
            return Redirect("https://web.kinauna.com/");
        }
        
    }
}
