namespace WarehouseInvoiceSystem.Tests.Services.UserService;

using Microsoft.AspNetCore.Identity;
using NSubstitute;
using WarehouseInvoiceSystem.Application.DTOs.User;
using WarehouseInvoiceSystem.Domain.Entities;
using WarehouseInvoiceSystem.Domain.Enums;
using WarehouseInvoiceSystem.Domain.Interfaces;

public abstract class UserServiceTestBase
{
    protected IUserRepository UserRepo { get; } = Substitute.For<IUserRepository>();

    private static readonly PasswordHasher<User> PasswordHasher = new();

    protected Application.Services.UserService CreateService() => new(UserRepo);

    protected static User CreateUser(
        string username = "testuser",
        string email = "test@test.com",
        string password = "Password123!",
        UserRole role = UserRole.User,
        bool isActive = true)
    {
        var user = new User
        {
            Username = username,
            Email = email,
            Role = role,
            IsActive = isActive,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        SetEntityId(user, Guid.NewGuid());
        user.PasswordHash = PasswordHasher.HashPassword(user, password);
        return user;
    }

    protected static CreateUserDto BuildCreateDto(
        string username = "newuser",
        string email = "new@test.com",
        string password = "NewPass123!",
        UserRole role = UserRole.User) => new()
    {
        Username = username,
        Email = email,
        Password = password,
        Role = role
    };

    protected static UpdateUserDto BuildUpdateDto(
        string username = "updateduser",
        string email = "updated@test.com",
        UserRole role = UserRole.User,
        bool isActive = true) => new()
    {
        Username = username,
        Email = email,
        Role = role,
        IsActive = isActive
    };

    protected static void SetEntityId(object entity, Guid id)
    {
        var prop = entity.GetType().GetProperty("Id")!;
        prop.SetValue(entity, id);
    }
}
