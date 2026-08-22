using Identity.Application.DTOs;
using Identity.Application.Exceptions;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRoleRepository> _roleRepo = new();
    private readonly IOptions<JwtSettings> _jwtOptions;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _jwtOptions = Options.Create(new JwtSettings
        {
            SecretKey = "CredVault-Super-Secret-Key-That-Should-Be-At-Least-32-Characters-Long!",
            Issuer = "CredVault.IdentityService",
            Audience = "CredVault.Services",
            ExpiryInMinutes = 60
        });
        _authService = new AuthService(_userRepo.Object, _roleRepo.Object, _jwtOptions);
    }

    // ── PASS TESTS ──

    [Fact]
    public async Task Register_WithValidData_ReturnsAuthResponse()
    {
        var request = new RegisterRequestDto { FullName = "John", Email = "john@test.com", Password = "Test@123" };
        _userRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);
        _roleRepo.Setup(r => r.GetByNameAsync("User")).ReturnsAsync(new Role { Id = Guid.NewGuid(), Name = "User" });

        var result = await _authService.RegisterAsync(request);

        Assert.NotNull(result);
        Assert.Equal("john@test.com", result.Email);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task Register_ReturnsToken_ThatIsNotEmpty()
    {
        var request = new RegisterRequestDto { FullName = "Jane", Email = "jane@test.com", Password = "Test@123" };
        _userRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);
        _roleRepo.Setup(r => r.GetByNameAsync("User")).ReturnsAsync(new Role { Id = Guid.NewGuid(), Name = "User" });

        var result = await _authService.RegisterAsync(request);

        Assert.Contains(".", result.Token); // JWT tokens have 3 parts separated by dots
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAuthResponse()
    {
        var password = "Test@123";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "john@test.com", FullName = "John",
            PasswordHash = hashedPassword, IsActive = true,
            Role = new Role { Id = Guid.NewGuid(), Name = "User" }
        };
        _userRepo.Setup(r => r.GetByEmailAsync("john@test.com")).ReturnsAsync(user);

        var result = await _authService.LoginAsync(new LoginRequestDto { Email = "john@test.com", Password = password });

        Assert.NotNull(result);
        Assert.Equal("john@test.com", result.Email);
    }

    [Fact]
    public async Task Register_CallsAddAsync_OnRepository()
    {
        var request = new RegisterRequestDto { FullName = "Test", Email = "test@test.com", Password = "Test@123" };
        _userRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);
        _roleRepo.Setup(r => r.GetByNameAsync("User")).ReturnsAsync(new Role { Id = Guid.NewGuid(), Name = "User" });

        await _authService.RegisterAsync(request);

        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Register_HashesPassword_NotStoredAsPlainText()
    {
        User? savedUser = null;
        var request = new RegisterRequestDto { FullName = "Test", Email = "hash@test.com", Password = "MySecret123" };
        _userRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);
        _roleRepo.Setup(r => r.GetByNameAsync("User")).ReturnsAsync(new Role { Id = Guid.NewGuid(), Name = "User" });
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Callback<User>(u => savedUser = u);

        await _authService.RegisterAsync(request);

        Assert.NotNull(savedUser);
        Assert.NotEqual("MySecret123", savedUser!.PasswordHash); // Must be hashed
    }

    // ── FAIL TESTS (expected exceptions) ──

    [Fact]
    public async Task Register_WithDuplicateEmail_ThrowsConflict()
    {
        var request = new RegisterRequestDto { FullName = "Dup", Email = "dup@test.com", Password = "Test@123" };
        _userRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(new User { Email = "dup@test.com" });

        await Assert.ThrowsAsync<ConflictException>(() => _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorized()
    {
        var user = new User
        {
            Email = "john@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct@123"),
            IsActive = true, Role = new Role { Name = "User" }
        };
        _userRepo.Setup(r => r.GetByEmailAsync("john@test.com")).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _authService.LoginAsync(new LoginRequestDto { Email = "john@test.com", Password = "Wrong@123" }));
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ThrowsUnauthorized()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("ghost@test.com")).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _authService.LoginAsync(new LoginRequestDto { Email = "ghost@test.com", Password = "Test@123" }));
    }

    [Fact]
    public async Task Login_WithDeactivatedAccount_ThrowsUnauthorized()
    {
        var user = new User
        {
            Email = "inactive@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            IsActive = false, Role = new Role { Name = "User" }
        };
        _userRepo.Setup(r => r.GetByEmailAsync("inactive@test.com")).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _authService.LoginAsync(new LoginRequestDto { Email = "inactive@test.com", Password = "Test@123" }));
    }

    [Fact]
    public async Task Register_WhenRoleNotFound_ThrowsException()
    {
        var request = new RegisterRequestDto { FullName = "Test", Email = "norole@test.com", Password = "Test@123" };
        _userRepo.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);
        _roleRepo.Setup(r => r.GetByNameAsync("User")).ReturnsAsync((Role?)null);

        await Assert.ThrowsAsync<NullReferenceException>(() => _authService.RegisterAsync(request));
    }
}
