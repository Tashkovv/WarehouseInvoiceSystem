namespace WarehouseInvoiceSystem.Tests.Services.UserService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.User;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class LoginTests : UserServiceTestBase
{
    [Fact]
    public async Task Login_ValidCredentials_ReturnsSuccessWithUser()
    {
        var user = CreateUser(username: "admin", password: "Secret123!");
        UserRepo.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        var service = CreateService();

        var result = await service.LoginAsync(new LoginDto { Username = "admin", Password = "Secret123!" });

        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("admin");
        result.User.Role.Should().Be(UserRole.User);
        result.ErrorMessage.Should().BeNull();
        await UserRepo.Received(1).UpdateAsync(Arg.Is<User>(u => u.LastLogin != null));
    }

    [Fact]
    public async Task Login_UserNotFound_ReturnsFailure()
    {
        UserRepo.GetByUsernameAsync("ghost", Arg.Any<CancellationToken>()).Returns((User?)null);
        var service = CreateService();

        var result = await service.LoginAsync(new LoginDto { Username = "ghost", Password = "anything" });

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("InvalidCredentials");
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsFailure()
    {
        var user = CreateUser(password: "CorrectPassword");
        UserRepo.GetByUsernameAsync(user.Username, Arg.Any<CancellationToken>()).Returns(user);
        var service = CreateService();

        var result = await service.LoginAsync(new LoginDto { Username = user.Username, Password = "WrongPassword" });

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("InvalidCredentials");
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsFailure()
    {
        var user = CreateUser(isActive: false, password: "Secret123!");
        UserRepo.GetByUsernameAsync(user.Username, Arg.Any<CancellationToken>()).Returns(user);
        var service = CreateService();

        var result = await service.LoginAsync(new LoginDto { Username = user.Username, Password = "Secret123!" });

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("InvalidCredentials");
    }

    [Fact]
    public async Task Login_Success_UpdatesLastLogin()
    {
        var user = CreateUser(password: "Secret123!");
        user.LastLogin = null;
        UserRepo.GetByUsernameAsync(user.Username, Arg.Any<CancellationToken>()).Returns(user);
        var service = CreateService();

        await service.LoginAsync(new LoginDto { Username = user.Username, Password = "Secret123!" });

        user.LastLogin.Should().NotBeNull();
        user.LastLogin.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
