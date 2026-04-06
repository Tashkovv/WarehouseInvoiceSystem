namespace WarehouseInvoiceSystem.BlazorUI.Services
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.Components.Authorization;

    public class WisAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor) : AuthenticationStateProvider
    {
        private ClaimsPrincipal? _cachedUser;

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_cachedUser is not null)
                return Task.FromResult(new AuthenticationState(_cachedUser));

            // On circuit start, read claims from the auth cookie via HttpContext
            var httpUser = httpContextAccessor.HttpContext?.User;

            _cachedUser = httpUser?.Identity?.IsAuthenticated == true
                ? httpUser
                : new ClaimsPrincipal(new ClaimsIdentity());

            return Task.FromResult(new AuthenticationState(_cachedUser));
        }

        public void MarkUserAsAuthenticated(ClaimsPrincipal user)
        {
            _cachedUser = user;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void MarkUserAsLoggedOut()
        {
            _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
