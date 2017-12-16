﻿using Etdb.UserService.EventSourcing.Commands;
using Etdb.UserService.Repositories.Abstractions;

namespace Etdb.UserService.EventSourcing.Validation
{
    public abstract class UserCommandValidation<TTransactionCommand, TResponse> : UserValidationDefinition<TTransactionCommand, TResponse>
        where TTransactionCommand : UserCommand<TResponse>
        where TResponse : class
    {
        protected UserCommandValidation(IUserRepository userRepository) : base(userRepository)
        {
            this.RegisterUserNameRule();
            this.RegisterNameRule();
            this.RegisterLastNameRule();
            this.RegisterEmailRule();
            this.RegisterPasswordRule();
        }
    }
}