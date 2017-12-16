﻿using System.Collections.Generic;
using Etdb.ServiceBase.Repositories.Abstractions.Generics;
using Etdb.UserService.Domain.Entities;

namespace Etdb.UserService.Repositories.Abstractions
{
    public interface IUserSecurityRoleRepository : IEntityRepository<UserSecurityrole>
    {
        UserSecurityrole Find(string roleName, bool includeUser = false);

        IEnumerable<UserSecurityrole> Find(string roleName, string userName, bool includeUser = false);
    }
}