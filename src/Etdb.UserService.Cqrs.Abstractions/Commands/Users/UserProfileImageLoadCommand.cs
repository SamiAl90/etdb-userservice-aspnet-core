﻿using System;
using Etdb.ServiceBase.Cqrs.Abstractions.Commands;
using Etdb.UserService.Presentation;

namespace Etdb.UserService.Cqrs.Abstractions.Commands.Users
{
    public class UserProfileImageLoadCommand : IResponseCommand<FileDownloadInfoDto>
    {
        public UserProfileImageLoadCommand(Guid id)
        {
            this.Id = id;
        }

        public Guid Id { get; }
    }
}