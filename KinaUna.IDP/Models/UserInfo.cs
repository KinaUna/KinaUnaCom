﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUna.IDP.Models
{
    public class UserInfo
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public int ViewChild { get; set; }
        public string Timezone { get; set; }
        public string ProfilePicture { get; set; }

        [NotMapped]
        public bool CanUserAddItems { get; set; }
        [NotMapped]
        public List<UserAccess> AccessList { get; set; }
    }
}