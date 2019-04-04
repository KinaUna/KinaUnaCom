using System.Collections.Generic;
using System.Linq;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class DataService : IDataService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();

        public DataService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
        }

        public Progeny GetProgeny(int id)
        {
            Progeny progeny;
            string cachedProgeny = _cache.GetString("progeny" + id);
            if (!string.IsNullOrEmpty(cachedProgeny))
            {
                progeny = JsonConvert.DeserializeObject<Progeny>(cachedProgeny);
            }
            else
            {
                progeny = _context.ProgenyDb.AsNoTracking().SingleOrDefault(p => p.Id == id);
                _cache.SetString("progeny" + id, JsonConvert.SerializeObject(progeny), _cacheOptions);
            }

            return progeny;
        }

        public List<UserAccess> GetProgenyUserAccessList(int id)
        {
            List<UserAccess> accessList;
            string cachedAccessList = _cache.GetString("accessList" + id);
            if (!string.IsNullOrEmpty(cachedAccessList))
            {
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(cachedAccessList);
            }
            else
            {
                accessList = _context.UserAccessDb.AsNoTracking().Where(u => u.ProgenyId == id).ToList();
                _cache.SetString("accessList" + id, JsonConvert.SerializeObject(accessList), _cacheOptions);
            }

            return accessList;
        }

        public List<UserAccess> GetUsersUserAccessList(string id)
        {
            List<UserAccess> accessList;
            string cachedAccessList = _cache.GetString("usersaccesslist" + id);
            if (!string.IsNullOrEmpty(cachedAccessList))
            {
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(cachedAccessList);
            }
            else
            {
                accessList = _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == id.ToUpper()).ToList();
            }

            return accessList;
        }

        public UserInfo GetUserInfoByEmail(string userEmail)
        {
            UserInfo userinfo;
            string cachedUserInfo = _cache.GetString("userinfo" + userEmail);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
                _cache.SetString("userinfo" + userEmail, JsonConvert.SerializeObject(userinfo), _cacheOptions);
            }

            return userinfo;
        }

        public UserAccess GetUserAccess(int id)
        {
            UserAccess userAccess;
            string cachedUserAccess = _cache.GetString("useraccess" + id);
            if (!string.IsNullOrEmpty(cachedUserAccess))
            {
                userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
            }
            else
            {
                userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.AccessId == id);
                _cache.SetString("useraccess" + id, JsonConvert.SerializeObject(userAccess), _cacheOptions);
            }

            return userAccess;
        }

        public UserAccess GetProgenyUserAccessForUser(int progenyId, string userEmail)
        {
            UserAccess userAccess;
            string cachedUserAccess = _cache.GetString("progenyuseraccess" + progenyId + userEmail);
            if (!string.IsNullOrEmpty(cachedUserAccess))
            {
                userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
            }
            else
            {
                userAccess = _context.UserAccessDb.SingleOrDefault(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());
                _cache.SetString("progenyuseraccess" + progenyId + userEmail, JsonConvert.SerializeObject(userAccess), _cacheOptions);
            }

            return userAccess;
        }

        public Address GetAddressItem(int id)
        {
            Address address;
            string cachedAddress = _cache.GetString("address" + id);
            if (!string.IsNullOrEmpty(cachedAddress))
            {
                address = JsonConvert.DeserializeObject<Address>(cachedAddress);
            }
            else
            {
                address = _context.AddressDb.AsNoTracking().SingleOrDefault(a => a.AddressId == id);
                _cache.SetString("address" + id, JsonConvert.SerializeObject(address), _cacheOptions);

            }

            return address;
        }

        public CalendarItem GetCalendarItem(int id)
        {
            CalendarItem calendarItem;
            string cachedCalendarItem = _cache.GetString("calendaritem" + id);
            if (!string.IsNullOrEmpty(cachedCalendarItem))
            {
                calendarItem = JsonConvert.DeserializeObject<CalendarItem>(cachedCalendarItem);
            }
            else
            {
                calendarItem = _context.CalendarDb.AsNoTracking().SingleOrDefault(l => l.EventId == id);
                _cache.SetString("calendaritem" + id, JsonConvert.SerializeObject(calendarItem), _cacheOptions);
            }

            return calendarItem;
        }

        public List<CalendarItem> GetCalendarList(int progenyId)
        {
            List<CalendarItem> calendarList;
            string cachedCalendar = _cache.GetString("calendarlist" + progenyId);
            if (!string.IsNullOrEmpty(cachedCalendar))
            {
                calendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(cachedCalendar);
            }
            else
            {
                calendarList = _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToList();
                _cache.SetString("calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptions);
            }

            return calendarList;
        }

        public Contact GetContact(int id)
        {
            Contact contact;
            string cachedContact = _cache.GetString("contact" + id);
            if (!string.IsNullOrEmpty(cachedContact))
            {
                contact = JsonConvert.DeserializeObject<Contact>(cachedContact);
            }
            else
            {
                contact = _context.ContactsDb.AsNoTracking().SingleOrDefault(c => c.ContactId == id);
                _cache.SetString("contact" + id, JsonConvert.SerializeObject(contact), _cacheOptions);
            }

            return contact;
        }

        public List<Contact> GetContactsList(int progenyId)
        {
            List<Contact> contactsList;
            string cachedContactsList = _cache.GetString("contactslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedContactsList))
            {
                contactsList = JsonConvert.DeserializeObject<List<Contact>>(cachedContactsList);
            }
            else
            {
                contactsList = _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToList();
                _cache.SetString("contactslist" + progenyId, JsonConvert.SerializeObject(contactsList), _cacheOptions);
            }

            return contactsList;
        }

        public Friend GetFriend(int id)
        {
            Friend friend;
            string cachedFriend = _cache.GetString("friend" + id);
            if (!string.IsNullOrEmpty(cachedFriend))
            {
                friend = JsonConvert.DeserializeObject<Friend>(cachedFriend);
            }
            else
            {
                friend = _context.FriendsDb.AsNoTracking().SingleOrDefault(f => f.FriendId == id);
                _cache.SetString("friend" + id, JsonConvert.SerializeObject(friend), _cacheOptions);
            }

            return friend;
        }

        public List<Friend> GetFriendsList(int progenyId)
        {
            List<Friend> friendsList;
            string cachedFriendsList = _cache.GetString("friendslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedFriendsList))
            {
                friendsList = JsonConvert.DeserializeObject<List<Friend>>(cachedFriendsList);
            }
            else
            {
                friendsList = _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == progenyId).ToList();
                _cache.SetString("friendslist" + progenyId, JsonConvert.SerializeObject(friendsList), _cacheOptions);
            }

            return friendsList;
        }
    }
}
