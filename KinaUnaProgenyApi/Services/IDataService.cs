using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IDataService
    {
        List<Progeny> GetProgenyUserIsAdmin(string email);
        List<UserAccess> GetProgenyUserAccessList(int id);
        List<UserAccess> GetUsersUserAccessList(string id);
        Progeny GetProgeny(int id);
        UserInfo GetUserInfoByEmail(string userEmail);
        UserInfo GetUserInfoById(int id);
        UserInfo GetUserInfoByUserId(string id);
        UserAccess GetUserAccess(int id);
        UserAccess GetProgenyUserAccessForUser(int progenyId, string userEmail);
        Address GetAddressItem(int id);
        CalendarItem GetCalendarItem(int id);
        List<CalendarItem> GetCalendarList(int progenyId);
        Contact GetContact(int id);
        List<Contact> GetContactsList(int progenyId);
        Friend GetFriend(int id);
        List<Friend> GetFriendsList(int progenyId);
        Location GetLocation(int id);
        List<Location> GetLocationsList(int progenyId);
        TimeLineItem GetTimeLineItem(int id);
        TimeLineItem GetTimeLineItemByItemId(string itemId, int itemType);
        List<TimeLineItem> GetTimeLineList(int progenyId);
        Measurement GetMeasurement(int id);
        List<Measurement> GetMeasurementsList(int progenyId);
        Note GetNote(int id);
        List<Note> GetNotesList(int progenyId);
        Skill GetSkill(int id);
        List<Skill> GetSkillsList(int progenyId);
        Sleep GetSleep(int id);
        List<Sleep> GetSleepList(int progenyId);
        Vaccination GetVaccination(int id);
        List<Vaccination> GetVaccinationsList(int progenyId);
        VocabularyItem GetVocabularyItem(int id);
        List<VocabularyItem> GetVocabularyList(int progenyId);
    }
}
