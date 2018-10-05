﻿using Etdb.UserService.Cqrs.Abstractions.Commands;
using Etdb.UserService.Cqrs.Validation.Base;
using Etdb.UserService.Services.Abstractions;

namespace Etdb.UserService.Cqrs.Validation.Emails
{
    public class EmailAddCommandValidation : EmailCommandValidation<EmailAddCommand>
    {
        public EmailAddCommandValidation(IUsersService usersService) : base(usersService)
        {
            this.RegisterEmailRules();
        }
    }
}