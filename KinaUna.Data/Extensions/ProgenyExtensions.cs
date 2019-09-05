using System;
using System.Collections.Generic;
using System.Text;
using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    public static class ProgenyExtensions
    {
        /// <summary>
        /// Verifies if an email address is in the admin list.
        /// </summary>
        /// <param name="email">string: The email address to verify.</param>
        /// <returns>bool: True if the email address is in the admin list, otherwise false.</returns>
        public static bool IsInAdminList(this Progeny progeny, string email)
        {
            if (progeny == null || string.IsNullOrEmpty(progeny.Admins) || string.IsNullOrEmpty(email))
            {
                return false;
            }

            string[] adminList = progeny.Admins.Split(',');
            foreach (string adminItem in adminList)
            {
                if (adminItem.Trim().ToUpper().Equals(email.Trim().ToUpper()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
