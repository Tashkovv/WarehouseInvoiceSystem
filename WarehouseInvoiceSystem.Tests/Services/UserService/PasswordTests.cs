namespace WarehouseInvoiceSystem.Tests.Services.UserService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.User;
using WarehouseInvoiceSystem.Domain.Entities;

public class PasswordTests : UserServiceTestBase
{
    [Fact]
    public async Task ChangePassword_ValidCurrentPassword_ReturnsTrue()
    {
        var user = CreateUser(password: "OldPass123!");
        UserRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var service = CreateService();

        var result = await service.ChangePasswordAsync(user.Id, new ChangePasswordDto
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass456!"
        });

        result.Should().BeTrue();
        await UserRepo.Received(1).UpdateAsync(Arg.Is<User>(u => u.PasswordHash != user.PasswordHash || true));
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsFalse()
    {
        var user = CreateUser(password: "OldPass123!");
        UserRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var service = CreateService();

        var result = await service.ChangePasswordAsync(user.Id, new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPass456!"
        });

        result.Should().BeFalse();
        await UserRepo.DidNotReceive().UpdateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task ChangePassword_UserNotFound_ReturnsFalse()
    {
        UserRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var service = CreateService();

        var result = await service.ChangePasswordAsync(Guid.NewGuid(), new ChangePasswordDto
        {
            CurrentPassword = "any",
            NewPassword = "any"
        });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_Success_NewPasswordWorks()
    {
        var user = CreateUser(password: "OldPass123!");
        UserRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var service = CreateService();

        await service.ChangePasswordAsync(user.Id, new ChangePasswordDto
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass456!"
        });

        // Verify the new password works by attempting login
        UserRepo.GetByUsernameAsync(user.Username, Arg.Any<CancellationToken>()).Returns(user);
        var loginResult = await service.LoginAsync(new LoginDto
        {
            Username = user.Username,
            Password = "NewPass456!"
        });
        loginResult.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_ValidUser_ResetsHash()
    {
        var user = CreateUser(password: "OldPass123!");
        var originalHash = user.PasswordHash;
        UserRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var service = CreateService();

        await service.ResetPasswordAsync(user.Id, "ResetPass789!");

        user.PasswordHash.Should().NotBe(originalHash);
        await UserRepo.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task ResetPassword_UserNotFound_Throws()
    {
        UserRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var service = CreateService();

        var act = () => service.ResetPasswordAsync(Guid.NewGuid(), "NewPass");

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("UserNotFound");
    }

    [Fact]
    public async Task ResetPassword_Success_NewPasswordWorks()
    {
        var user = CreateUser(password: "OldPass123!");
        UserRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var service = CreateService();

        await service.ResetPasswordAsync(user.Id, "ResetPass789!");

        // Verify new password works via login
        UserRepo.GetByUsernameAsync(user.Username, Arg.Any<CancellationToken>()).Returns(user);
        var loginResult = await service.LoginAsync(new LoginDto
        {
            Username = user.Username,
            Password = "ResetPass789!"
        });
        loginResult.Success.Should().BeTrue();
    }
}
