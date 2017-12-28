﻿using System;
using Etdb.UserService.EventSourcing.Abstractions.Events;

namespace Etdb.UserService.EventSourcing.Events
{
    public class UserRegisterEvent : UserEvent
    {
        public UserRegisterEvent(Guid id, string name, string lastName, string email, string userName) : base(id, name, lastName, email, userName)
        {
        }
    }
}
