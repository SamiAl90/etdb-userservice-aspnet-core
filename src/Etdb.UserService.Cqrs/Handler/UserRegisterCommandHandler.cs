﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Etdb.ServiceBase.Cqrs.Abstractions.Handler;
using Etdb.ServiceBase.Cqrs.Abstractions.Validation;
using Etdb.ServiceBase.ErrorHandling.Abstractions.Exceptions;
using Etdb.UserService.Cqrs.Abstractions.Base;
using Etdb.UserService.Cqrs.Abstractions.Commands;
using Etdb.UserService.Domain;
using Etdb.UserService.Extensions;
using Etdb.UserService.Services.Abstractions;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Etdb.UserService.Cqrs.Handler
{
    public class UserRegisterCommandHandler : IVoidCommandHandler<UserRegisterCommand>
    {
        private readonly IMapper mapper;
        private readonly IAuthService authService;
        private readonly ICommandValidation<UserRegisterCommand> userRegisterCommandValidation;
        private readonly ICommandValidation<EmailAddCommand> emailAddCommandValidation;
        private readonly ICommandValidation<PasswordAddCommand> passwordCommandValidation;

        public UserRegisterCommandHandler(IMapper mapper, IAuthService authService,
            ICommandValidation<UserRegisterCommand> userRegisterCommandValidation,
            ICommandValidation<EmailAddCommand> emailAddCommandValidation,
            ICommandValidation<PasswordAddCommand> passwordCommandValidation)
        {
            
            this.mapper = mapper;
            this.authService = authService;
            this.userRegisterCommandValidation = userRegisterCommandValidation;
            this.emailAddCommandValidation = emailAddCommandValidation;
            this.passwordCommandValidation = passwordCommandValidation;
        }

        public async Task Handle(UserRegisterCommand request, CancellationToken cancellationToken)
        {
            var validationResults =
                new List<ValidationResult>
                {
                    await this.userRegisterCommandValidation.ValidateCommandAsync(request)
                };

            foreach (var emailAddCommand in request.Emails)
            {
                validationResults.Add(await this.emailAddCommandValidation.ValidateCommandAsync(emailAddCommand));
            }

            var passwordAddCommand = new PasswordAddCommand
            {
                Password = request.Password
            };
            
            validationResults.Add(await this.passwordCommandValidation.ValidateCommandAsync(passwordAddCommand));

            if (validationResults.Any(result => !result.IsValid))
            {
                var errors = validationResults
                    .Where(result => !result.IsValid)
                    .SelectMany(result => result.Errors)
                    .Select(result => result.ErrorMessage)
                    .ToArray();
                
                throw new GeneralValidationException("Error validating user registration!", errors);
            }
            
            var user = this.mapper.Map<User>(request);

            await this.authService.RegisterAsync(user);
        }
    }
}