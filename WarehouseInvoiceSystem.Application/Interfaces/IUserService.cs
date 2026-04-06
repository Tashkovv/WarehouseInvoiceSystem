namespace WarehouseInvoiceSystem.Application.Interfaces
{
    using WarehouseInvoiceSystem.Application.DTOs.User;

    public interface IUserService
    {
        Task<LoginResultDto> LoginAsync(LoginDto loginDto, CancellationToken ct = default);
        Task CreateUserAsync(CreateUserDto createDto, CancellationToken ct = default);
        Task UpdateUserAsync(Guid id, UpdateUserDto updateDto, CancellationToken ct = default);
        Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDto changeDto, CancellationToken ct = default);
        Task ResetPasswordAsync(Guid id, string newPassword, CancellationToken ct = default);
        Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken ct = default);
        Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);
        Task<bool> DeleteUserAsync(Guid id, CancellationToken ct = default);
        Task<bool> AnyUsersExistAsync(CancellationToken ct = default);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive, CancellationToken ct = default);
    }
}
