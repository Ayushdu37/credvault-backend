using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application.DTOs;
using Identity.Application.Exceptions;
using Identity.Application.Interfaces;
using Identity.Domain.Interfaces;
using Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;

namespace Identity.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IOptions<JwtSettings> jwtSettings)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            // 1. Check if email already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser is not null)
                throw new ConflictException($"A user with email '{request.Email}' already exists.");

            // 2. Get the default "User" role
            var UserRole = await _roleRepository.GetByNameAsync("User")
                ?? throw new NotFoundException("Default role 'User' not found. Ensure roles are seeded");

            // 3. Create the user entity
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email.ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                RoleId = UserRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            // 4. Generate JWT and return
            var token = GenerateJwtToken(user, UserRole.Name);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Token = token
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            // 1. Find user by email
            var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant())
                ?? throw new UnauthorizedException("Invalid email or password.");

            // 2. Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedException("Wrong password.");

            // 3. Check if account is active
            if (!user.IsActive)
                throw new UnauthorizedException("Account is deactivated.");

            // 4. Generate JWT and return
            var token = GenerateJwtToken(user, user.Role.Name);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Token = token
            };
        }

        private string GenerateJwtToken(User user, string roleName)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("UserId", user.Id.ToString())
            };

            var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes),
            signingCredentials: credentials
        );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
