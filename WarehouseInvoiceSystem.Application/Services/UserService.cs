namespace WarehouseInvoiceSystem.Application.Services
{
    using Microsoft.AspNetCore.Identity;
    using WarehouseInvoiceSystem.Application.DTOs.User;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;

    public class UserService(IUserRepository userRepository) : IUserService
    {
        private static readonly PasswordHasher<User> PasswordHasher = new();

        public async Task<LoginResultDto> LoginAsync(LoginDto loginDto, CancellationToken ct = default)
        {
            User? user = await userRepository.GetByUsernameAsync(loginDto.Username, ct);

            if (user is null || !user.IsActive)
                return new LoginResultDto { Success = false, ErrorMessage = "InvalidCredentials" };

            PasswordVerificationResult verification = PasswordHasher.VerifyHashedPassword(
                user, user.PasswordHash, loginDto.Password);

            if (verification == PasswordVerificationResult.Failed)
                return new LoginResultDto { Success = false, ErrorMessage = "InvalidCredentials" };

            user.LastLogin = DateTime.UtcNow;
            await userRepository.UpdateAsync(user);

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = PasswordHasher.HashPassword(user, loginDto.Password);
                await userRepository.UpdateAsync(user);
            }

            return new LoginResultDto { Success = true, User = MapToDto(user) };
        }

        public async Task CreateUserAsync(CreateUserDto createDto, CancellationToken ct = default)
        {
            User? existing = await userRepository.GetByUsernameAsync(createDto.Username, ct);
            if (existing is not null)
                throw new InvalidOperationException("UsernameAlreadyExists");

            var user = new User
            {
                Username = createDto.Username,
                Email = createDto.Email,
                Role = createDto.Role,
                IsActive = true
            };

            user.PasswordHash = PasswordHasher.HashPassword(user, createDto.Password);
            await userRepository.CreateAsync(user);
        }

        public async Task UpdateUserAsync(Guid id, UpdateUserDto updateDto, CancellationToken ct = default)
        {
            User? user = await userRepository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException("UserNotFound");

            User? existingWithUsername = await userRepository.GetByUsernameAsync(updateDto.Username, ct);
            if (existingWithUsername is not null && existingWithUsername.Id != id)
                throw new InvalidOperationException("UsernameAlreadyExists");

            user.Username = updateDto.Username;
            user.Email = updateDto.Email;
            user.Role = updateDto.Role;
            user.IsActive = updateDto.IsActive;

            await userRepository.UpdateAsync(user);
        }

        public async Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDto changeDto, CancellationToken ct = default)
        {
            User? user = await userRepository.GetByIdAsync(id, ct);
            if (user is null)
                return false;

            PasswordVerificationResult verification = PasswordHasher.VerifyHashedPassword(
                user, user.PasswordHash, changeDto.CurrentPassword);

            if (verification == PasswordVerificationResult.Failed)
                return false;

            user.PasswordHash = PasswordHasher.HashPassword(user, changeDto.NewPassword);
            await userRepository.UpdateAsync(user);
            return true;
        }

        public async Task ResetPasswordAsync(Guid id, string newPassword, CancellationToken ct = default)
        {
            User? user = await userRepository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException("UserNotFound");

            user.PasswordHash = PasswordHasher.HashPassword(user, newPassword);
            await userRepository.UpdateAsync(user);
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken ct = default)
        {
            IEnumerable<User> users = await userRepository.GetAllAsync(ct);
            return users.Select(MapToDto);
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
        {
            User? user = await userRepository.GetByIdAsync(id, ct);
            return user is null ? null : MapToDto(user);
        }

        public Task<bool> DeleteUserAsync(Guid id, CancellationToken ct = default) =>
            userRepository.DeleteAsync(id);

        public Task<bool> AnyUsersExistAsync(CancellationToken ct = default) =>
            userRepository.AnyUsersExistAsync(ct);

        public Task<bool> SetActiveStatusAsync(Guid id, bool isActive, CancellationToken ct = default) =>
            userRepository.SetActiveStatusAsync(id, isActive, ct);

        private static UserDto MapToDto(User user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLogin = user.LastLogin,
            CreatedAt = user.CreatedAt
        };
    }
}
