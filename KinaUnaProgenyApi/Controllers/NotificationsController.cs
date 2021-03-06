﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using Microsoft.AspNetCore.Mvc;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly AzureNotifications _azureNotifications;
        private readonly ProgenyDbContext _context;
        private readonly ImageStore _imageStore;

        public NotificationsController(AzureNotifications azureNotifications, ProgenyDbContext context, ImageStore imageStore)
        {
            _azureNotifications = azureNotifications;
            _context = context;
            _imageStore = imageStore;
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post(string pns, [FromBody] string message, string to_tag)
        {
            var user = User.GetEmail();
            if (user == null)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            string[] userTag = new string[2];
            userTag[0] = "username:" + to_tag;
            userTag[1] = "from:" + user;

            Microsoft.Azure.NotificationHubs.NotificationOutcome outcome = null;
            HttpStatusCode ret = HttpStatusCode.InternalServerError;

            switch (pns.ToLower())
            {
                case "wns":
                    // Windows 8.1 / Windows Phone 8.1
                    var toast = @"<toast><visual><binding template=""ToastText01""><text id=""1"">" +
                                "From " + user + ": " + message + "</text></binding></visual></toast>";
                    outcome = await _azureNotifications.Hub.SendWindowsNativeNotificationAsync(toast, userTag);
                    break;
                case "apns":
                    // iOS
                    var alert = "{\"aps\":{\"alert\":\"" + "From " + user + ": " + message + "\"}}";
                    outcome = await _azureNotifications.Hub.SendAppleNativeNotificationAsync(alert, userTag);
                    break;
                case "fcm":
                    // Android
                    var notif = "{ \"data\" : {\"message\":\"" + "From " + user + ": " + message + "\"}}";
                    outcome = await _azureNotifications.Hub.SendFcmNativeNotificationAsync(notif, userTag);
                    break;
            }

            if (outcome != null)
            {
                if (!((outcome.State == Microsoft.Azure.NotificationHubs.NotificationOutcomeState.Abandoned) ||
                      (outcome.State == Microsoft.Azure.NotificationHubs.NotificationOutcomeState.Unknown)))
                {
                    ret = HttpStatusCode.OK;
                }
            }

            return new HttpResponseMessage(ret);
        }

        [HttpGet]
        [Route("[action]/{count}/{start}/{language}")]
        public async Task<IActionResult> Latest(int count, int start = 0, string language = "EN")
        {
            string userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            List<MobileNotification> notifications = await _context.MobileNotificationsDb
                .Where(n => n.UserId == userId && n.Language.ToUpper() == language.ToUpper()).ToListAsync();
            if (notifications.Any())
            {
                if (start > notifications.Count)
                {
                    return Ok(new List<MobileNotification>());
                }
                notifications = notifications.OrderByDescending(n => n.Time).ToList();
                notifications = notifications.Skip(start).Take(count).ToList();
                foreach (MobileNotification notif in notifications)
                {
                    if (!string.IsNullOrEmpty(notif.IconLink) && !notif.IconLink.ToLower().StartsWith("http"))
                    {
                        notif.IconLink = _imageStore.UriFor(notif.IconLink, BlobContainers.Profiles);
                    }
                    else
                    {
                        notif.IconLink = Constants.ProfilePictureUrl;
                    }
                }
            }

            return Ok(notifications);
        }

        [HttpGet]
        [Route("[action]/{count}/{start}/{language}")]
        public async Task<IActionResult> Unread(int count, int start = 0, string language = "EN")
        {
            string userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            List<MobileNotification> notifications = await _context.MobileNotificationsDb
                .Where(n => n.UserId == userId && n.Read == false && n.Language.ToUpper() == language.ToUpper()).ToListAsync();
            if (notifications.Any())
            {
                if (start > notifications.Count)
                {
                    return Ok(new List<MobileNotification>());
                }
                notifications = notifications.OrderByDescending(n => n.Time).ToList();
                notifications = notifications.Skip(start).Take(count).ToList();
                foreach (MobileNotification notif in notifications)
                {
                    if (!string.IsNullOrEmpty(notif.IconLink) && !notif.IconLink.ToLower().StartsWith("http"))
                    {
                        notif.IconLink = _imageStore.UriFor(notif.IconLink, BlobContainers.Profiles);
                    }
                    else
                    {
                        notif.IconLink = Constants.ProfilePictureUrl;
                    }
                }
            }

            return Ok(notifications);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] MobileNotification value)
        {
            string userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            MobileNotification mobileNotification =
                _context.MobileNotificationsDb.SingleOrDefault(m => m.NotificationId == id);
            if (mobileNotification != null && mobileNotification.UserId == userId)
            {
                mobileNotification.Read = value.Read;
                _context.MobileNotificationsDb.Update(mobileNotification);
                await _context.SaveChangesAsync();
                return Ok(mobileNotification);
            }

            return Ok(value);
        }

        // DELETE api/notifications/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            MobileNotification mobileNotification = await _context.MobileNotificationsDb.SingleOrDefaultAsync(m => m.NotificationId == id);
            if (mobileNotification != null)
            {
                string userEmail = User.GetEmail();
                if (mobileNotification.UserId == User.GetUserId())
                {
                    _context.MobileNotificationsDb.Remove(mobileNotification);
                    await _context.SaveChangesAsync();
                }

                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }
    }
}