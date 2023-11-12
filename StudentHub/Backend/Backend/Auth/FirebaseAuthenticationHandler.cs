using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Backend.Auth;

public class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public FirebaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Get the Authorization header
        if (!Request.Headers.TryGetValue("Authorization", out StringValues authorization))
            return AuthenticateResult.Fail("Cannot read authorization header.");

        string idToken = authorization.ToString().Split(' ').Last();

        try
        {
            // Validate the ID token
            FirebaseToken? decodedToken = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(idToken);

            // Create claims
            List<Claim> claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, decodedToken.Uid)
                // Add other claims as needed
            };

            // Create claims identity
            ClaimsIdentity claimsIdentity = new(claims, Scheme.Name);

            // Create authentication ticket
            AuthenticationTicket authenticationTicket = new(
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties(),
                Scheme.Name);

            return AuthenticateResult.Success(authenticationTicket);
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail(ex.Message);
        }
    }
}