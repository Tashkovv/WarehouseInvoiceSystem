namespace WarehouseInvoiceSystem.Tests.Services.UserService;

using FluentAssertions;
using NSubstitute;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;

public class QueryTests : UserServiceTestBase
{
    [Fact]
    public async Task GetAllUsers_MapsToDto()
    {
        var user1 = CreateUser(username: "alice", role: UserRole.Admin);
        var user2 = CreateUser(username: "bob", role: UserRole.User);
        UserRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User> { user1, user2 });
        var service = CreateService();

        var result = (await service.GetAllUsersAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].Username.Should().Be("alice");
        result[0].Role.Should().Be(UserRole.Admin);
        result[1].Username.Should().Be("bob");
        result[1].Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task GetUserById_Found_ReturnsDto()
    {
        var user = CreateUser(username: "admin", email: "admin@test.com", role: UserRole.Admin);
        user.LastLogin = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        UserRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var service = CreateService();

        var result = await service.GetUserByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Username.Should().Be("admin");
        result.Email.Should().Be("admin@test.com");
        result.Role.Should().Be(UserRole.Admin);
        result.IsActive.Should().BeTrue();
        result.LastLogin.Should().Be(new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc));
        result.CreatedAt.Should().Be(user.CreatedAt);
    }

    [Fact]
    public async Task GetUserById_NotFound_ReturnsNull()
    {
        UserRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var service = CreateService();

        var result = await service.GetUserByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task AnyUsersExist_DelegatesToRepository()
    {
        UserRepo.AnyUsersExistAsync(Arg.Any<CancellationToken>()).Returns(true);
        var service = CreateService();

        var result = await service.AnyUsersExistAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyUsersExist_NoUsers_ReturnsFalse()
    {
        UserRepo.AnyUsersExistAsync(Arg.Any<CancellationToken>()).Returns(false);
        var service = CreateService();

        var result = await service.AnyUsersExistAsync();

        result.Should().BeFalse();
    }
}
