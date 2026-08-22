using Identity.Application.DTOs;
using Identity.Application.Exceptions;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Moq;

namespace Identity.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userService = new UserService(_userRepo.Object);
    }

    // ── PASS TESTS ──

    [Fact]
    public async Task GetProfile_WithValidId_ReturnsUserProfile()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId, FullName = "John", Email = "john@test.com",
            PhoneNumber = "9876543210", IsActive = true, CreatedAt = DateTime.UtcNow,
            Role = new Role { Name = "User" }
        };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _userService.GetProfileAsync(userId);

        Assert.Equal("John", result.FullName);
        Assert.Equal("User", result.Role);
    }

    [Fact]
    public async Task GetProfile_ReturnsCorrectEmail()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId, FullName = "Jane", Email = "jane@test.com",
            IsActive = true, Role = new Role { Name = "Admin" }
        };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _userService.GetProfileAsync(userId);

        Assert.Equal("jane@test.com", result.Email);
        Assert.Equal("Admin", result.Role);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsUpdatedProfile()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId, FullName = "Old Name", Email = "john@test.com",
            PhoneNumber = "1111111111", IsActive = true, Role = new Role { Name = "User" }
        };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var request = new UpdateProfileRequestDto { FullName = "New Name", PhoneNumber = "9999999999" };
        var result = await _userService.UpdateProfileAsync(userId, request);

        Assert.Equal("New Name", result.FullName);
        Assert.Equal("9999999999", result.PhoneNumber);
    }

    [Fact]
    public async Task UpdateProfile_CallsUpdateAsync_OnRepository()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId, FullName = "Test", Email = "test@test.com",
            IsActive = true, Role = new Role { Name = "User" }
        };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        await _userService.UpdateProfileAsync(userId, new UpdateProfileRequestDto { FullName = "Updated" });

        _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_TrimsFullName()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId, FullName = "Test", Email = "test@test.com",
            IsActive = true, Role = new Role { Name = "User" }
        };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _userService.UpdateProfileAsync(userId, new UpdateProfileRequestDto { FullName = "  Trimmed Name  " });

        Assert.Equal("Trimmed Name", result.FullName);
    }

    // ── FAIL TESTS ──

    [Fact]
    public async Task GetProfile_WithInvalidId_ThrowsNotFound()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetProfileAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidId_ThrowsNotFound()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _userService.UpdateProfileAsync(Guid.NewGuid(), new UpdateProfileRequestDto { FullName = "Test" }));
    }

    [Fact]
    public async Task GetProfile_NeverCallsAddAsync()
    {
        var userId = Guid.NewGuid();
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User
        {
            Id = userId, FullName = "X", Email = "x@x.com", IsActive = true, Role = new Role { Name = "User" }
        });

        await _userService.GetProfileAsync(userId);

        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfile_DoesNotChangeEmail()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId, FullName = "Test", Email = "original@test.com",
            IsActive = true, Role = new Role { Name = "User" }
        };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _userService.UpdateProfileAsync(userId, new UpdateProfileRequestDto { FullName = "Changed" });

        Assert.Equal("original@test.com", result.Email); // Email should never change via update
    }

    [Fact]
    public async Task GetProfile_ReturnsIsActiveStatus()
    {
        var userId = Guid.NewGuid();
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User
        {
            Id = userId, FullName = "Active", Email = "a@a.com", IsActive = true, Role = new Role { Name = "User" }
        });

        var result = await _userService.GetProfileAsync(userId);

        Assert.True(result.IsActive);
    }
}
