namespace WarehouseInvoiceSystem.Tests.Services.UserService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class MutationTests : UserServiceTestBase
{
    [Fact]
    public async Task CreateUser_ValidDto_CreatesWithHashedPassword()
    {
        UserRepo.GetByUsernameAsync("newuser", Arg.Any<CancellationToken>()).Returns((User?)null);
        var service = CreateService();

        await service.CreateUserAsync(BuildCreateDto());

        await UserRepo.Received(1).CreateAsync(Arg.Is<User>(u =>
            u.Username == "newuser" &&
            u.Email == "new@test.com" &&
            u.Role == UserRole.User &&
            u.IsActive &&
            !string.IsNullOrEmpty(u.PasswordHash) &&
            u.PasswordHash != "NewPass123!"));
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_Throws()
    {
        var existing = CreateUser(username: "taken");
        UserRepo.GetByUsernameAsync("taken", Arg.Any<CancellationToken>()).Returns(existing);
        var service = CreateService();

        var act = () => service.CreateUserAsync(BuildCreateDto(username: "taken"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("UsernameAlreadyExists");
    }

    [Fact]
    public async Task CreateUser_AdminRole_SetsCorrectRole()
    {
        UserRepo.GetByUsernameAsync("adminuser", Arg.Any<CancellationToken>()).Returns((User?)null);
        var service = CreateService();

        await service.CreateUserAsync(BuildCreateDto(username: "adminuser", role: UserRole.Admin));

        await UserRepo.Received(1).CreateAsync(Arg.Is<User>(u => u.Role == UserRole.Admin));
    }

    [Fact]
    public async Task UpdateUser_ValidDto_UpdatesFields()
    {
        var user = CreateUser(username: "oldname");
        UserRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        UserRepo.GetByUsernameAsync("newname", Arg.Any<CancellationToken>()).Returns((User?)null);
        var service = CreateService();

        await service.UpdateUserAsync(user.Id, BuildUpdateDto(username: "newname", email: "new@mail.com", role: UserRole.Admin));

        user.Username.Should().Be("newname");
        user.Email.Should().Be("new@mail.com");
        user.Role.Should().Be(UserRole.Admin);
        await UserRepo.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task UpdateUser_NotFound_Throws()
    {
        UserRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var service = CreateService();

        var act = () => service.UpdateUserAsync(Guid.NewGuid(), BuildUpdateDto());

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("UserNotFound");
    }

    [Fact]
    public async Task UpdateUser_DuplicateUsername_Throws()
    {
        var user = CreateUser(username: "user1");
        var other = CreateUser(username: "user2");
        UserRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        UserRepo.GetByUsernameAsync("user2", Arg.Any<CancellationToken>()).Returns(other);
        var service = CreateService();

        var act = () => service.UpdateUserAsync(user.Id, BuildUpdateDto(username: "user2"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("UsernameAlreadyExists");
    }

    [Fact]
    public async Task UpdateUser_SameUsername_NoConflict()
    {
        var user = CreateUser(username: "keepname");
        UserRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        UserRepo.GetByUsernameAsync("keepname", Arg.Any<CancellationToken>()).Returns(user);
        var service = CreateService();

        await service.UpdateUserAsync(user.Id, BuildUpdateDto(username: "keepname"));

        await UserRepo.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task DeleteUser_DelegatesToRepository()
    {
        UserRepo.DeleteAsync(Arg.Any<Guid>()).Returns(true);
        var service = CreateService();

        var result = await service.DeleteUserAsync(Guid.NewGuid());

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetActiveStatus_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        UserRepo.SetActiveStatusAsync(id, false, Arg.Any<CancellationToken>()).Returns(true);
        var service = CreateService();

        var result = await service.SetActiveStatusAsync(id, false);

        result.Should().BeTrue();
        await UserRepo.Received(1).SetActiveStatusAsync(id, false, Arg.Any<CancellationToken>());
    }
}
