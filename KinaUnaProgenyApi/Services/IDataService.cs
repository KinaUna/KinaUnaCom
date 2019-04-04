using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IDataService
    {
        List<UserAccess> GetProgenyUserAccessList(int id);
        List<UserAccess> GetUsersUserAccessList(string id);
        Progeny GetProgeny(int id);
        UserInfo GetUserInfoByEmail(string userEmail);
        UserAccess GetUserAccess(int id);
        UserAccess GetProgenyUserAccessForUser(int progenyId, string userEmail);
        Address GetAddressItem(int id);
        CalendarItem GetCalendarItem(int id);
        List<CalendarItem> GetCalendarList(int progenyId);
        Contact GetContact(int id);
        List<Contact> GetContactsList(int progenyId);
        Friend GetFriend(int id);
        List<Friend> GetFriendsList(int progenyId);
    }
}
