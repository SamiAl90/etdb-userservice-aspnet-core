﻿using System;

namespace Etdb.UserService.Presentation
{
    public class AccessTokenDto
    {
        public string AccessToken { get; }
        public string RefreshToken { get; }
        public DateTime ExpiresAt { get; }
        public string TokenType { get; }

        public AccessTokenDto(string accessToken, string refreshToken, DateTime expiresAt, string tokenType)
        {
            this.AccessToken = accessToken;
            this.RefreshToken = refreshToken;
            this.ExpiresAt = expiresAt;
            this.TokenType = tokenType;
        }
    }
}