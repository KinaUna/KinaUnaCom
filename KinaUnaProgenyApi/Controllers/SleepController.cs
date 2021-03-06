﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Constants = KinaUna.Data.Constants;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class SleepController : ControllerBase
    {
        private readonly ProgenyDbContext _context;
        private readonly IDataService _dataService;
        private readonly AzureNotifications _azureNotifications;

        public SleepController(ProgenyDbContext context, IDataService dataService, AzureNotifications azureNotifications)
        {
            _context = context;
            _dataService = dataService;
            _azureNotifications = azureNotifications;
        }

        // GET api/sleep/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Sleep> sleepList = await _dataService.GetSleepList(id);
                sleepList = sleepList.Where(s => s.AccessLevel >= accessLevel).ToList();
                if (sleepList.Any())
                {
                    return Ok(sleepList);
                }
            }

            return NotFound();
        }

        // GET api/sleep/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSleepItem(int id)
        {

            Sleep result = await _dataService.GetSleep(id);
            if (result.AccessLevel == (int)AccessLevel.Public || result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/sleep
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Sleep value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail();
            if (prog != null)
            {
                // Check if user is allowed to add sleep for this child.

                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Sleep sleepItem = new Sleep();
            sleepItem.AccessLevel = value.AccessLevel;
            sleepItem.Author = value.Author;
            sleepItem.SleepNotes = value.SleepNotes;
            sleepItem.SleepRating = value.SleepRating;
            sleepItem.ProgenyId = value.ProgenyId;
            sleepItem.SleepStart = value.SleepStart;
            sleepItem.SleepEnd = value.SleepEnd;
            sleepItem.CreatedDate = DateTime.UtcNow;

            _context.SleepDb.Add(sleepItem);
            await _context.SaveChangesAsync();
            await _dataService.SetSleep(sleepItem.SleepId);

            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = sleepItem.ProgenyId;
            tItem.AccessLevel = sleepItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Sleep;
            tItem.ItemId = sleepItem.SleepId.ToString();
            UserInfo userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            tItem.CreatedBy = userinfo?.UserId ?? "Unknown";
            tItem.CreatedTime = DateTime.UtcNow;
            tItem.ProgenyTime = sleepItem.SleepStart;

            await _context.TimeLineDb.AddAsync(tItem);
            await _context.SaveChangesAsync();
            await _dataService.SetTimeLineItem(tItem.TimeLineId);

            string title = "Sleep added for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " added a new sleep item for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

            return Ok(sleepItem);
        }

        // PUT api/sleep/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Sleep value)
        {
            Sleep sleepItem = await _context.SleepDb.SingleOrDefaultAsync(s => s.SleepId == id);
            if (sleepItem == null)
            {
                return NotFound();
            }

            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail();
            if (prog != null)
            {
                // Check if user is allowed to edit sleep for this child.

                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            sleepItem.AccessLevel = value.AccessLevel;
            sleepItem.Author = value.Author;
            sleepItem.SleepNotes = value.SleepNotes;
            sleepItem.SleepRating = value.SleepRating;
            sleepItem.ProgenyId = value.ProgenyId;
            sleepItem.SleepStart = value.SleepStart;
            sleepItem.SleepEnd = value.SleepEnd;
            sleepItem.CreatedDate = value.CreatedDate;

            _context.SleepDb.Update(sleepItem);
            await _context.SaveChangesAsync();
            await _dataService.SetSleep(sleepItem.SleepId);

            TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                t.ItemId == sleepItem.SleepId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Sleep);
            if (tItem != null)
            {
                tItem.ProgenyTime = sleepItem.SleepStart;
                tItem.AccessLevel = sleepItem.AccessLevel;
                _context.TimeLineDb.Update(tItem);
                await _context.SaveChangesAsync();
                await _dataService.SetTimeLineItem(tItem.TimeLineId);
            }

            string title = "Sleep for " + prog.NickName + " edited";
            UserInfo userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " edited a sleep item for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

            return Ok(sleepItem);
        }

        // DELETE api/sleep/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Sleep sleepItem = await _context.SleepDb.SingleOrDefaultAsync(s => s.SleepId == id);
            if (sleepItem != null)
            {
                string userEmail = User.GetEmail();
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == sleepItem.ProgenyId);
                if (prog != null)
                {
                    // Check if user is allowed to delete sleep for this child.

                    if (!prog.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                    t.ItemId == sleepItem.SleepId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Sleep);
                if (tItem != null)
                {
                    _context.TimeLineDb.Remove(tItem);
                    await _context.SaveChangesAsync();
                    await _dataService.RemoveTimeLineItem(tItem.TimeLineId, tItem.ItemType, tItem.ProgenyId);
                }

                _context.SleepDb.Remove(sleepItem);
                await _context.SaveChangesAsync();
                await _dataService.RemoveSleep(sleepItem.SleepId, sleepItem.ProgenyId);

                string title = "Sleep for " + prog.NickName + " deleted";
                UserInfo userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " deleted a sleep item for " + prog.NickName + ". Sleep start: " + sleepItem.SleepStart.ToString("dd-MMM-yyyy HH:mm");
                await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetSleepMobile(int id)
        {

            Sleep result = await _dataService.GetSleep(id);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetSleepListPage([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Sleep> allItems = await _dataService.GetSleepList(progenyId);
            allItems = allItems.OrderBy(s => s.SleepStart).ToList();

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int sleepCounter = 1;
            int slpCount = allItems.Count;
            foreach (Sleep slp in allItems)
            {
                if (sortBy == 1)
                {
                    slp.SleepNumber = slpCount - sleepCounter + 1;
                }
                else
                {
                    slp.SleepNumber = sleepCounter;
                }

                sleepCounter++;
            }

            var itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            SleepListPage model = new SleepListPage();
            model.SleepList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;

            return Ok(model);
        }

        [HttpGet("[action]/{progenyId}/{accessLevel}/{start}")]
        public async Task<IActionResult> GetSleepListMobile(int progenyId, int accessLevel, int start = 0)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(progenyId, userEmail);
            if (userAccess != null)
            {
                List<Sleep> result = await _dataService.GetSleepList(progenyId);
                result = result.Where(s => s.AccessLevel >= accessLevel).ToList();
                result = result.OrderByDescending(s => s.SleepStart).ToList();
                if (start != -1)
                {
                    result = result.Skip(start).Take(25).ToList();
                }

                return Ok(result);
            }

            return Unauthorized();
        }

        [HttpGet("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> GetSleepStatsMobile(int progenyId, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(progenyId, userEmail);
            if (userAccess != null)
            {
                string userTimeZone = Constants.DefaultTimezone;
                SleepStatsModel model = new SleepStatsModel();
                model.SleepTotal = TimeSpan.Zero;
                model.SleepLastYear = TimeSpan.Zero;
                model.SleepLastMonth = TimeSpan.Zero;
                List<Sleep> sList = await _dataService.GetSleepList(progenyId);
                List<Sleep> sleepList = new List<Sleep>();
                DateTime yearAgo = new DateTime(DateTime.UtcNow.Year - 1, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);
                DateTime monthAgo = DateTime.UtcNow - TimeSpan.FromDays(30);
                if (sList.Count != 0)
                {
                    foreach (Sleep s in sList)
                    {

                        bool isLessThanYear = s.SleepEnd > yearAgo;
                        bool isLessThanMonth = s.SleepEnd > monthAgo;
                        s.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(s.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                        s.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(s.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                        DateTimeOffset sOffset = new DateTimeOffset(s.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepStart));
                        DateTimeOffset eOffset = new DateTimeOffset(s.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepEnd));
                        s.SleepDuration = eOffset - sOffset;

                        model.SleepTotal = model.SleepTotal + s.SleepDuration;
                        if (isLessThanYear)
                        {
                            model.SleepLastYear = model.SleepLastYear + s.SleepDuration;
                        }

                        if (isLessThanMonth)
                        {
                            model.SleepLastMonth = model.SleepLastMonth + s.SleepDuration;
                        }

                        if (s.AccessLevel >= accessLevel)
                        {
                            sleepList.Add(s);
                        }
                    }
                    sleepList = sleepList.OrderBy(s => s.SleepStart).ToList();

                    model.TotalAverage = model.SleepTotal / (DateTime.UtcNow - sleepList.First().SleepStart).TotalDays;
                    model.LastYearAverage = model.SleepLastYear / (DateTime.UtcNow - yearAgo).TotalDays;
                    model.LastMonthAverage = model.SleepLastMonth / 30;

                }
                else
                {
                    model.TotalAverage = TimeSpan.Zero;
                    model.LastYearAverage = TimeSpan.Zero;
                    model.LastMonthAverage = TimeSpan.Zero;
                }

                return Ok(model);
            }

            return Unauthorized();
        }

        [HttpGet("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> GetSleepChartDataMobile(int progenyId, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(progenyId, userEmail);
            if (userAccess != null)
            {
                string userTimeZone = Constants.DefaultTimezone;
                List<Sleep> sList = await _dataService.GetSleepList(progenyId);
                List<Sleep> sleepList = new List<Sleep>();
                foreach (Sleep s in sList)
                {
                    s.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(s.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    s.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(s.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    DateTimeOffset sOffset = new DateTimeOffset(s.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepStart));
                    DateTimeOffset eOffset = new DateTimeOffset(s.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepEnd));
                    s.SleepDuration = eOffset - sOffset;

                    if (s.AccessLevel >= accessLevel)
                    {
                        sleepList.Add(s);
                    }
                }
                sleepList = sleepList.OrderBy(s => s.SleepStart).ToList();

                List<Sleep> chartList = new List<Sleep>();
                foreach (Sleep chartItem in sleepList)
                {
                    double durationStartDate = 0.0;
                    if (chartItem.SleepStart.Date == chartItem.SleepEnd.Date)
                    {
                        durationStartDate = durationStartDate + chartItem.SleepDuration.TotalMinutes;
                        Sleep slpItem = chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepStart.Date);
                        if (slpItem != null)
                        {
                            slpItem.SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                        }
                        else
                        {
                            Sleep newSleep = new Sleep();
                            newSleep.SleepStart = chartItem.SleepStart;
                            newSleep.SleepDuration = TimeSpan.FromMinutes(durationStartDate);
                            chartList.Add(newSleep);
                        }
                    }
                    else
                    {
                        DateTimeOffset sOffset = new DateTimeOffset(chartItem.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(chartItem.SleepStart));
                        DateTimeOffset s2Offset = new DateTimeOffset(chartItem.SleepStart.Date + TimeSpan.FromDays(1),
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone)
                                .GetUtcOffset(chartItem.SleepStart.Date + TimeSpan.FromDays(1)));
                        DateTimeOffset eOffset = new DateTimeOffset(chartItem.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(chartItem.SleepEnd));
                        DateTimeOffset e2Offset = new DateTimeOffset(chartItem.SleepEnd.Date,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone)
                                .GetUtcOffset(chartItem.SleepEnd.Date));
                        TimeSpan sDateDuration = s2Offset - sOffset;
                        TimeSpan eDateDuration = eOffset - e2Offset;
                        durationStartDate = chartItem.SleepDuration.TotalMinutes - (eDateDuration.TotalMinutes);
                        var durationEndDate = chartItem.SleepDuration.TotalMinutes - sDateDuration.TotalMinutes;
                        Sleep slpItem = chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepStart.Date);
                        if (slpItem != null)
                        {
                            slpItem.SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                        }
                        else
                        {
                            Sleep newSleep = new Sleep();
                            newSleep.SleepStart = chartItem.SleepStart;
                            newSleep.SleepDuration = TimeSpan.FromMinutes(durationStartDate);
                            chartList.Add(newSleep);
                        }

                        Sleep slpItem2 = chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepEnd.Date);
                        if (slpItem2 != null)
                        {
                            slpItem2.SleepDuration += TimeSpan.FromMinutes(durationEndDate);
                        }
                        else
                        {
                            Sleep newSleep = new Sleep();
                            newSleep.SleepStart = chartItem.SleepEnd;
                            newSleep.SleepDuration = TimeSpan.FromMinutes(durationEndDate);
                            chartList.Add(newSleep);
                        }
                    }
                }

                List<Sleep> model = chartList.OrderBy(s => s.SleepStart).ToList();

                return Ok(model);
            }

            return Unauthorized();
        }

        [HttpGet("[action]/{sleepId}/{accessLevel}/{sortOrder}")]
        public async Task<IActionResult> GetSleepDetails(int sleepId, int accessLevel, int sortOrder)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            Sleep currentSleep = await _dataService.GetSleep(sleepId);
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(currentSleep.ProgenyId, userEmail);
            if (userAccess != null)
            {
                string userTimeZone = Constants.DefaultTimezone;
                List<Sleep> sList = await _dataService.GetSleepList(currentSleep.ProgenyId);
                List<Sleep> sleepList = new List<Sleep>();
                foreach (Sleep s in sList)
                {
                    if (s.AccessLevel >= accessLevel)
                    {
                        sleepList.Add(s);
                    }
                }

                if (sortOrder == 0)
                {
                    sleepList = sleepList.OrderBy(s => s.SleepStart).ToList();
                }
                else
                {
                    sleepList = sleepList.OrderByDescending(s => s.SleepStart).ToList();
                }

                List<Sleep> model = new List<Sleep>();

                if (currentSleep != null)
                {
                    model.Add(currentSleep);
                    int currentSleepIndex = sleepList.IndexOf(currentSleep);
                    if (currentSleepIndex > 0)
                    {
                        model.Add(sleepList[currentSleepIndex - 1]);
                    }
                    else
                    {
                        model.Add(sleepList[sleepList.Count - 1]);
                    }

                    if (sleepList.Count < currentSleepIndex + 1)
                    {
                        model.Add(sleepList[currentSleepIndex + 1]);
                    }
                    else
                    {
                        model.Add(sleepList[0]);
                    }

                    foreach (Sleep s in model)
                    {
                        DateTimeOffset sOffset = new DateTimeOffset(s.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepStart));
                        DateTimeOffset eOffset = new DateTimeOffset(s.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepEnd));
                        s.SleepDuration = eOffset - sOffset;
                    }
                }

                return Ok(model);
            }

            return Unauthorized();
        }
    }
}
