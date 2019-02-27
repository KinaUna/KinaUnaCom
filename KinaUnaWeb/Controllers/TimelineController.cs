﻿using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class TimelineController : Controller
    {
        private WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;
        private int _progId = 2;
        private readonly string _defaultUser = "testuser@niviaq.com";

        public TimelineController(WebDbContext context, IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, ImageStore imageStore)
        {
            _context = context;
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int sortBy = 1, int items = 0)
        {
            _progId = childId;
            ViewBag.SortBy = sortBy;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            if (_progId == 0)
            {
                _progId = 2;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
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

            TimeLineViewModel model = new TimeLineViewModel();
            model.TimeLineItems = new List<TimeLineItem>();
            model.TimeLineItems = await _context.TimeLineDb.Where(t => t.ProgenyId == _progId && t.AccessLevel >= userAccessLevel && t.ProgenyTime < DateTime.UtcNow).ToListAsync();
            if (sortBy == 1)
            {
                model.TimeLineItems = model.TimeLineItems.OrderByDescending(t => t.ProgenyTime).ToList();
            }
            else
            {
                model.TimeLineItems = model.TimeLineItems.OrderBy(t => t.ProgenyTime).ToList();
            }
            
            ViewBag.ProgenyName = progeny.NickName;
            ViewBag.Items = items;
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLinePhotoPartial(PictureViewModel model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVideoPartial(VideoViewModel model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineEventPartial(CalendarItem model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVocabularyPartial(VocabularyItem model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineSkillPartial(Skill model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineFriendPartial(Friend model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineMeasurementPartial(Measurement model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineSleepPartial(Sleep model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineNotePartial(Note model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineContactPartial(Contact model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVaccinationPartial(Vaccination model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineLocationPartial(Location model)
        {

            return View(model);
        }

        [AllowAnonymous]
        public async Task<ActionResult> GetTimeLineItem(TimeLineItemViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            
            string id = model.ItemId.ToString();
            int type = model.TypeId;
            int itemId;
            bool idParse = int.TryParse(id, out itemId);
            if (type == 1)
            {
                if (idParse)
                {
                    PictureViewModel picture = await _mediaHttpClient.GetPictureViewModel(itemId, 0, 1, userinfo.Timezone);
                    if (picture != null)
                    {
                        if (!picture.PictureLink.StartsWith("https://"))
                        {
                            picture.PictureLink = _imageStore.UriFor(picture.PictureLink);
                        }
                        picture.CommentsCount = picture.CommentsList.Count;
                        return PartialView("TimeLinePhotoPartial", picture);
                    }
                }
            }

            if (type == 2)
            {
                if (idParse)
                {
                    VideoViewModel video = await _mediaHttpClient.GetVideoViewModel(itemId, 0, 1, userinfo.Timezone);
                    if (video != null)
                    {
                        video.CommentsCount = video.CommentsList.Count;
                        return PartialView("TimeLineVideoPartial", video);
                    }
                }
            }

            if (type == 3)
            {
                if (idParse)
                {
                    CalendarItem evt = _context.CalendarDb.SingleOrDefault(e => e.EventId == itemId);
                    if (evt != null && evt.StartTime.HasValue && evt.EndTime.HasValue)
                    {
                        evt.StartTime = TimeZoneInfo.ConvertTimeFromUtc(evt.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                        evt.EndTime = TimeZoneInfo.ConvertTimeFromUtc(evt.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                        return PartialView("TimeLineEventPartial", evt);
                    }
                }
            }

            if (type == 4)
            {
                if (idParse)
                {
                    VocabularyItem voc = _context.VocabularyDb.SingleOrDefault(v => v.WordId == itemId);
                    if (voc != null)
                    {
                        if (voc.Date != null)
                        {
                            voc.Date = TimeZoneInfo.ConvertTimeFromUtc(voc.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));

                        }
                        return PartialView("TimeLineVocabularyPartial", voc);
                    }
                }
            }

            if (type == 5)
            {
                if (idParse)
                {
                    Skill skl = _context.SkillsDb.SingleOrDefault(s => s.SkillId == itemId);
                    if (skl != null)
                    {
                        return PartialView("TimeLineSkillPartial", skl);
                    }
                }
            }

            if (type == 6)
            {
                if (idParse)
                {
                    Friend frn = _context.FriendsDb.SingleOrDefault(f => f.FriendId == itemId);
                    if (frn != null)
                    {
                        if (!frn.PictureLink.StartsWith("https://"))
                        {
                            frn.PictureLink = _imageStore.UriFor(frn.PictureLink, "friends");
                        }
                        return PartialView("TimeLineFriendPartial", frn);
                    }
                }
            }

            if (type == 7)
            {
                if (idParse)
                {
                    Measurement mes = _context.MeasurementsDb.SingleOrDefault(m => m.MeasurementId == itemId);
                    if (mes != null)
                    {
                        return PartialView("TimeLineMeasurementPartial", mes);
                    }
                }
            }

            if (type == 8)
            {
                if (idParse)
                {
                    Sleep slp = _context.SleepDb.SingleOrDefault(s => s.SleepId == itemId);
                    if (slp != null)
                    {
                        slp.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(slp.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                        slp.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(slp.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                        DateTimeOffset sOffset = new DateTimeOffset(slp.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone).GetUtcOffset(slp.SleepStart));
                        DateTimeOffset eOffset = new DateTimeOffset(slp.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone).GetUtcOffset(slp.SleepEnd));
                        slp.SleepDuration = eOffset - sOffset;
                        return PartialView("TimeLineSleepPartial", slp);
                    }
                }
            }

            if (type == 9)
            {
                if (idParse)
                {
                    Note nte = _context.NotesDb.SingleOrDefault(n => n.NoteId == itemId);
                    if (nte != null)
                    {
                        nte.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(nte.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                        return PartialView("TimeLineNotePartial", nte);
                    }
                }
            }

            if (type == 10)
            {
                if (idParse)
                {
                    Contact cnt = _context.ContactsDb.SingleOrDefault(c => c.ContactId == itemId);
                    if (cnt != null)
                    {
                        if (!cnt.PictureLink.StartsWith("https://"))
                        {
                            cnt.PictureLink = _imageStore.UriFor(cnt.PictureLink, "contacts");
                        }
                        if (cnt.DateAdded == null)
                        {
                            cnt.DateAdded = new DateTime(1900, 1, 1);
                        }
                        return PartialView("TimeLineContactPartial", cnt);
                    }
                }
            }

            if (type == 11)
            {
                if (idParse)
                {
                    Vaccination vac = _context.VaccinationsDb.SingleOrDefault(v => v.VaccinationId == itemId);
                    if (vac != null)
                    {
                        return PartialView("TimeLineVaccinationPartial", vac);
                    }
                }
            }

            if (type == 12)
            {
                if (idParse)
                {
                    Location loc = _context.LocationsDb.SingleOrDefault(l => l.LocationId == itemId);
                    if (loc != null)
                    {
                        return PartialView("TimeLineLocationPartial", loc);
                    }
                }
            }

            Note failNote = new Note();
            failNote.CreatedDate = DateTime.UtcNow;
            failNote.Title = "Error, content not found.";

            return PartialView("TimeLineNotePartial", failNote);
        }
    }
}