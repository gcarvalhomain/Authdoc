using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Authdoc.Application.DTOs;
using Authdoc.Data;
using Authdoc.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using LoginRequest = Authdoc.Application.DTOs.LoginRequest;

namespace Authdoc.Application.Services;

public class AuthService
{
    private readonly ManagerDbContext _context;
    private readonly IPasswordHasher<User>  _passwordHasher;
    private readonly IConfiguration  _configuration;

    public AuthService(ManagerDbContext context, IPasswordHasher<User> passwordHasher,  IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }
    

    public async Task<UserResponse?> RegisterAsync(RegisterUserRequest  request)
    {
        var emailExists = await _context.Users.AnyAsync(user => user.Email == request.Email );
        if (emailExists)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Age = request.Age,
            Gender = request.Gender,
            CreatedAt = now,
            UpdatedAt = now
        };
        
        user.Password = _passwordHasher.HashPassword(user, request.Password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Age = user.Age,
            Gender =  user.Gender,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Email == request.Email);
        if (user is null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return await CreateSessionAsync(user);
    }

    public async Task<LoginResponse?> RefreshAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await _context.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);

        if (storedToken is null || !storedToken.IsActive(DateTime.UtcNow))
        {
            return null;
        }

        storedToken.RevokedAt = DateTime.UtcNow;

        return await CreateSessionAsync(storedToken.User);
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == userId);
        if (user is null)
        {
            return ChangePasswordResult.UserNotFound;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, request.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
        {
            return ChangePasswordResult.WrongCurrentPassword;
        }

        var now = DateTime.UtcNow;
        user.Password = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = now;

        var activeTokens = await _context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await _context.SaveChangesAsync();
        return ChangePasswordResult.Success;
    }

    private async Task<LoginResponse> CreateSessionAsync(User user)
    {
        var refreshToken = GenerateRefreshToken();
        var refreshExpirationInDays = _configuration.GetValue("Jwt:RefreshTokenExpirationInDays", 7);
        var now = DateTime.UtcNow;

        var storedToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(refreshExpirationInDays)
        };

        _context.RefreshTokens.Add(storedToken);
        await _context.SaveChangesAsync();

        var (accessToken, accessExpiresAt) = GenerateJwtToken(user);

        return new LoginResponse
        {
            Token = accessToken,
            ExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = storedToken.ExpiresAt,
            User = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Age = user.Age,
                Gender = user.Gender,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            }
        };
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private (string Token, DateTime ExpiresAt) GenerateJwtToken(User user)
    {
        var key = _configuration["Jwt:Key"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:audience"];
        var expirationInMinutes = int.Parse(_configuration["Jwt:ExpirationInMinutes"]!);

        var expiresAt = DateTime.UtcNow.AddMinutes(expirationInMinutes);

        var claims = new List<Claim>
        {
            new  Claim(ClaimTypes.Role, user.Role.ToString()),
            new (ClaimTypes.NameIdentifier, user.Id.ToString()),
            new (ClaimTypes.Name, user.Name),
            new (ClaimTypes.Email, user.Email)
        };
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials:  credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

public enum ChangePasswordResult
{
    Success,
    UserNotFound,
    WrongCurrentPassword
}
