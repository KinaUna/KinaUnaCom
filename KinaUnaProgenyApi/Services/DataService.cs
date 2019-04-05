using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public List<Progeny> GetProgenyUserIsAdmin(string email)
        {
            List<Progeny> progenyList;
            string cachedProgenyList = _cache.GetString("progenywhereadmin" + email);
            if (!string.IsNullOrEmpty(cachedProgenyList))
            {
                progenyList = JsonConvert.DeserializeObject<List<Progeny>>(cachedProgenyList);
            }
            else
            {
                progenyList = _context.ProgenyDb.AsNoTracking().Where(p => p.Admins.Contains(email)).ToList();
                _cache.SetString("progenywhereadmin" + email, JsonConvert.SerializeObject(progenyList), _cacheOptions);
            }

            return progenyList;
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
            string cachedUserInfo = _cache.GetString("userinfobymail" + userEmail);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
                _cache.SetString("userinfobymail" + userEmail, JsonConvert.SerializeObject(userinfo), _cacheOptions);
            }

            return userinfo;
        }

        public UserInfo GetUserInfoById(int id)
        {
            UserInfo userinfo;
            string cachedUserInfo = _cache.GetString("userinfobyid" + id);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = _context.UserInfoDb.SingleOrDefault(u => u.Id == id);
                _cache.SetString("userinfobyid" + id, JsonConvert.SerializeObject(userinfo), _cacheOptions);
            }

            return userinfo;
        }

        public UserInfo GetUserInfoByUserId(string id)
        {
            UserInfo userinfo;
            string cachedUserInfo = _cache.GetString("userinfobyuserid" + id);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserId.ToUpper() == id.ToUpper());
                _cache.SetString("userinfobyuserid" + id, JsonConvert.SerializeObject(userinfo), _cacheOptions);
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

        public Location GetLocation(int id)
        {
            Location location;
            string cachedLocation = _cache.GetString("location" + id);
            if (!string.IsNullOrEmpty(cachedLocation))
            {
                location = JsonConvert.DeserializeObject<Location>(cachedLocation);
            }
            else
            {
                location = _context.LocationsDb.AsNoTracking().SingleOrDefault(l => l.LocationId == id);
                _cache.SetString("location" + id, JsonConvert.SerializeObject(location), _cacheOptions);
            }

            return location;
        }

        public List<Location> GetLocationsList(int progenyId)
        {
            List<Location> locationsList;
            string cachedLocationsList = _cache.GetString("locationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedLocationsList))
            {
                locationsList = JsonConvert.DeserializeObject<List<Location>>(cachedLocationsList);
            }
            else
            {
                locationsList = _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == progenyId).ToList();
                _cache.SetString("locationslist" + progenyId, JsonConvert.SerializeObject(locationsList), _cacheOptions);
            }

            return locationsList;
        }

        public TimeLineItem GetTimeLineItem(int id)
        {
            TimeLineItem timeLineItem;
            string cachedTimeLineItem = _cache.GetString("timelineitem" + id);
            if (!string.IsNullOrEmpty(cachedTimeLineItem))
            {
                timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(cachedTimeLineItem);
            }
            else
            {
                timeLineItem = _context.TimeLineDb.AsNoTracking().SingleOrDefault(t => t.TimeLineId == id);
                _cache.SetString("timelineitem" + id, JsonConvert.SerializeObject(timeLineItem), _cacheOptions);
            }

            return timeLineItem;
        }

        public TimeLineItem GetTimeLineItemByItemId(string itemId, int itemType)
        {
            TimeLineItem timeLineItem;
            string cachedTimeLineItem = _cache.GetString("timelineitembyid" + itemId + itemType);
            if (!string.IsNullOrEmpty(cachedTimeLineItem))
            {
                timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(cachedTimeLineItem);
            }
            else
            {
                timeLineItem = _context.TimeLineDb.SingleOrDefault(t => t.ItemId == itemId && t.ItemType == itemType);
                _cache.SetString("timelineitembyid" + itemId + itemType, JsonConvert.SerializeObject(timeLineItem), _cacheOptions);
            }

            return timeLineItem;
        }

        public List<TimeLineItem> GetTimeLineList(int progenyId)
        {
            List<TimeLineItem> timeLineList;
            string cachedTimeLineList = _cache.GetString("timelinelist" + progenyId);
            if (!string.IsNullOrEmpty(cachedTimeLineList))
            {
                timeLineList = JsonConvert.DeserializeObject<List<TimeLineItem>>(cachedTimeLineList);
            }
            else
            {
                timeLineList = _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == progenyId).ToList();
                _cache.SetString("timelinelist" + progenyId, JsonConvert.SerializeObject(timeLineList), _cacheOptions);
            }

            return timeLineList;
        }

        public Measurement GetMeasurement(int id)
        {
            Measurement measurement;
            string cachedMeasurement = _cache.GetString("measurement" + id);
            if (!string.IsNullOrEmpty(cachedMeasurement))
            {
                measurement = JsonConvert.DeserializeObject<Measurement>(cachedMeasurement);
            }
            else
            {
                measurement = _context.MeasurementsDb.AsNoTracking().SingleOrDefault(m => m.MeasurementId == id);
                _cache.SetString("measurement" + id, JsonConvert.SerializeObject(measurement), _cacheOptions);
            }

            return measurement;
        }

        public List<Measurement> GetMeasurementsList(int progenyId)
        {
            List<Measurement> measurementsList;
            string cachedMeasurementsList = _cache.GetString("measurementslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedMeasurementsList))
            {
                measurementsList = JsonConvert.DeserializeObject<List<Measurement>>(cachedMeasurementsList);
            }
            else
            {
                measurementsList = _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == progenyId).ToList();
                _cache.SetString("measurementslist" + progenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptions);
            }

            return measurementsList;
        }

        public Note GetNote(int id)
        {
            Note note;
            string cachedNote = _cache.GetString("note" + id);
            if (!string.IsNullOrEmpty(cachedNote))
            {
                note = JsonConvert.DeserializeObject<Note>(cachedNote);
            }
            else
            {
                note = _context.NotesDb.AsNoTracking().SingleOrDefault(n => n.NoteId == id);
                _cache.SetString("note" + id, JsonConvert.SerializeObject(note), _cacheOptions);
            }

            return note;
        }

        public List<Note> GetNotesList(int progenyId)
        {
            List<Note> notesList;
            string cachedNotesList = _cache.GetString("noteslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedNotesList))
            {
                notesList = JsonConvert.DeserializeObject<List<Note>>(cachedNotesList);
            }
            else
            {
                notesList = _context.NotesDb.AsNoTracking().Where(n => n.ProgenyId == progenyId).ToList();
                _cache.SetString("noteslist" + progenyId, JsonConvert.SerializeObject(notesList), _cacheOptions);
            }

            return notesList;
        }

        public Skill GetSkill(int id)
        {
            Skill skill;
            string cachedSkill = _cache.GetString("skill" + id);
            if (!string.IsNullOrEmpty(cachedSkill))
            {
                skill = JsonConvert.DeserializeObject<Skill>(cachedSkill);
            }
            else
            {
                skill = _context.SkillsDb.AsNoTracking().SingleOrDefault(s => s.SkillId == id);
                _cache.SetString("skill" + id, JsonConvert.SerializeObject(skill), _cacheOptions);
            }

            return skill;
        }

        public List<Skill> GetSkillsList(int progenyId)
        {
            List<Skill> skillsList;
            string cachedSkillsList = _cache.GetString("skillslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSkillsList))
            {
                skillsList = JsonConvert.DeserializeObject<List<Skill>>(cachedSkillsList);
            }
            else
            {
                skillsList = _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToList();
                _cache.SetString("skillslist" + progenyId, JsonConvert.SerializeObject(skillsList), _cacheOptions);
            }

            return skillsList;
        }

        public Sleep GetSleep(int id)
        {
            Sleep sleep;
            string cachedSleep = _cache.GetString("sleep" + id);
            if (!string.IsNullOrEmpty(cachedSleep))
            {
                sleep = JsonConvert.DeserializeObject<Sleep>(cachedSleep);
            }
            else
            {
                sleep = _context.SleepDb.AsNoTracking().SingleOrDefault(s => s.SleepId == id);
                _cache.SetString("sleep" + id, JsonConvert.SerializeObject(sleep), _cacheOptions);
            }

            return sleep;
        }

        public List<Sleep> GetSleepList(int progenyId)
        {
            List<Sleep> sleepList;
            string cachedSleepList = _cache.GetString("sleeplist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSleepList))
            {
                sleepList = JsonConvert.DeserializeObject<List<Sleep>>(cachedSleepList);
            }
            else
            {
                sleepList = _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToList();
                _cache.SetString("sleeplist" + progenyId, JsonConvert.SerializeObject(sleepList), _cacheOptions);
            }

            return sleepList;
        }

        public Vaccination GetVaccination(int id)
        {
            Vaccination vaccination;
            string cachedVaccination = _cache.GetString("vaccination" + id);
            if (!string.IsNullOrEmpty(cachedVaccination))
            {
                vaccination = JsonConvert.DeserializeObject<Vaccination>(cachedVaccination);
            }
            else
            {
                vaccination = _context.VaccinationsDb.AsNoTracking().SingleOrDefault(v => v.VaccinationId == id);
                _cache.SetString("vaccination" + id, JsonConvert.SerializeObject(vaccination), _cacheOptions);
            }

            return vaccination;
        }

        public List<Vaccination> GetVaccinationsList(int progenyId)
        {
            List<Vaccination> vaccinationsList;
            string cachedVaccinationsList = _cache.GetString("vaccinationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVaccinationsList))
            {
                vaccinationsList = JsonConvert.DeserializeObject<List<Vaccination>>(cachedVaccinationsList);
            }
            else
            {
                vaccinationsList = _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToList();
                _cache.SetString("vaccinationslist" + progenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptions);
            }

            return vaccinationsList;
        }

        public VocabularyItem GetVocabularyItem(int id)
        {
            VocabularyItem vocabularyItem;
            string cachedVocabularyItem = _cache.GetString("vocabularyitem" + id);
            if (!string.IsNullOrEmpty(cachedVocabularyItem))
            {
                vocabularyItem = JsonConvert.DeserializeObject<VocabularyItem>(cachedVocabularyItem);
            }
            else
            {
                vocabularyItem = _context.VocabularyDb.AsNoTracking().SingleOrDefault(v => v.WordId == id);
                _cache.SetString("vocabularyitem" + id, JsonConvert.SerializeObject(vocabularyItem), _cacheOptions);
            }

            return vocabularyItem;
        }

        public List<VocabularyItem> GetVocabularyList(int progenyId)
        {
            List<VocabularyItem> vocabularyList;
            string cachedVocabularyList = _cache.GetString("vocabularylist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVocabularyList))
            {
                vocabularyList = JsonConvert.DeserializeObject<List<VocabularyItem>>(cachedVocabularyList);
            }
            else
            {
                vocabularyList = _context.VocabularyDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToList();
                _cache.SetString("vocabularylist" + progenyId, JsonConvert.SerializeObject(vocabularyList), _cacheOptions);
            }

            return vocabularyList;
        }
    }
}
