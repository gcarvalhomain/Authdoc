using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authdoc.Application.DTOs;
using Authdoc.Data;
using Authdoc.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Age = request.Age,
            Gender = request.Gender,
            PasswordHash = request.PasswordHash,
            CreatedAt = DateTime.UtcNow,
            Role = "User"
        };
        
        user.PasswordHash = _passwordHasher.HashPassword(user, request.PasswordHash);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Age = user.Age,
            CreatedAt = DateTime.UtcNow,
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
        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }
        return GenerateJwtToken(user);
    }

    private LoginResponse GenerateJwtToken(User user)
    {
        var key = _configuration["Jwt:Key"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:audience"];
        var expirationInMinutes = int.Parse(_configuration["Jwt:ExpirationInMinutes"]!);

        var expiresAt = DateTime.UtcNow.AddMinutes(expirationInMinutes);

        var claims = new List<Claim>
        {
            new (ClaimTypes.NameIdentifier, user.Id.ToString()),
            new (ClaimTypes.Name, user.Name),
            new (ClaimTypes.Email, user.Email),
            new (ClaimTypes.Role, user.Role),
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
        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            User = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Age = user.Age,
                Gender = user.Gender,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            }
        };
    }
}