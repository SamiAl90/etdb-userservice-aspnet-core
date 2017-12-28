﻿using Etdb.ServiceBase.Domain.Abstractions.Base;

namespace Etdb.UserService.Domain.Entities
{
    public class Securityrole : TrackedEntity
    {
        public string Designation
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public bool IsSystem
        {
            get;
            set;
        }
    }
}
