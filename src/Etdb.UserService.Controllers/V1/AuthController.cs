﻿using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Etdb.ServiceBase.Cqrs.Abstractions.Bus;
using Etdb.ServiceBase.Extensions;
using Etdb.UserService.Cqrs.Abstractions.Commands.Authentication;
using Etdb.UserService.Cqrs.Abstractions.Commands.Users;
using Etdb.UserService.Presentation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Etdb.UserService.Controllers.V1
{
    [Route("api/v1/[controller]")]
    public class AuthController : Controller
    {
        private readonly IMapper mapper;
        private readonly IBus bus;

        public AuthController(IBus bus, IMapper mapper)
        {
            this.bus = bus;
            this.mapper = mapper;
        }

        [AllowAnonymous]
        [HttpPost("registration")]
        public async Task<IActionResult> RegistrationAsync([FromBody] UserRegisterDto dto,
            CancellationToken cancellationToken)
        {
            if (!this.ModelState.IsValid)
            {
                throw this.ModelState.GenerateValidationException("User register request is invalid!");
            }

            var command = this.mapper.Map<UserRegisterCommand>(dto);

            await this.bus.SendCommandAsync<UserRegisterCommand, UserDto>(command, cancellationToken);

            return this.NoContent();
        }

        [AllowAnonymous]
        [HttpPost("authentication")]
        public Task<AccessTokenDto> AuthenticationAsync([FromBody] UserInternalAuthenticationDto authenticationDto)
        {
            var command = this.mapper.Map<InternalAuthenticationCommand>(authenticationDto);

            return this.bus.SendCommandAsync<InternalAuthenticationCommand, AccessTokenDto>(command);
        }

        [AllowAnonymous]
        [HttpPost("external-authentication")]
        public Task<AccessTokenDto> ExternalAuthenticationAsync(
            [FromBody] UserExternalAuthenticationDto authenticationDto)
        {
            var command = this.mapper.Map<ExternalAuthenticationCommand>(authenticationDto);

            return this.bus.SendCommandAsync<ExternalAuthenticationCommand, AccessTokenDto>(command);
        }

        [AllowAnonymous]
        [HttpGet("refresh-authentication/{refreshToken}/{clientId}")]
        public Task<AccessTokenDto> RefreshAuthenticationAsync(string refreshToken, string clientId)
            => this.bus.SendCommandAsync<RefreshAuthenticationCommand, AccessTokenDto>(
                new RefreshAuthenticationCommand(refreshToken, clientId));

        [HttpGet("user-identity/{accessToken}")]
        public Task<IdentityUserDto> UserIdentityAsync(string accessToken)
            => this.bus.SendCommandAsync<IdentityUserLoadCommand, IdentityUserDto>(
                new IdentityUserLoadCommand(accessToken));
    }
}