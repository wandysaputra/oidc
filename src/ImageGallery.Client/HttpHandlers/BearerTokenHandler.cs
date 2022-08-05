using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ImageGallery.Client.HttpHandlers {
    public class BearerTokenHandler : DelegatingHandler {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IHttpClientFactory httpClientFactory;

        public BearerTokenHandler (IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory) {
            this.httpContextAccessor = httpContextAccessor;
            this.httpClientFactory = httpClientFactory;
        }
        protected override async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken) {
            // var accessToken = await httpContextAccessor.HttpContext.GetTokenAsync (OpenIdConnectParameterNames.AccessToken);

            // var refreshToken = await httpContextAccessor.HttpContext.GetTokenAsync (OpenIdConnectParameterNames.RefreshToken);

            var accessToken = await GetAccessTokenAsync ();

            if (!string.IsNullOrWhiteSpace (accessToken)) {
                request.SetBearerToken (accessToken);
            }

            return await base.SendAsync (request, cancellationToken);
        }

        public async Task<string> GetAccessTokenAsync () {
            // get the expires_at value and parse it
            // expires_at is one of the value OIDC middleware automatically stores when we choose to save the tokens.
            // This is the date and time in universal time notation when the access token will expire.
            var expiresAt = await httpContextAccessor.HttpContext.GetTokenAsync ("expires_at");

            var expiresAtAsDateTimeOffset = DateTimeOffset.Parse (expiresAt, CultureInfo.InvariantCulture);

            if (expiresAtAsDateTimeOffset.AddSeconds (-60).ToUniversalTime () > DateTime.UtcNow) {
                // no need to refresh, return the access token
                return await httpContextAccessor.HttpContext.GetTokenAsync (OpenIdConnectParameterNames.AccessToken);
            }
            Console.WriteLine ("RefreshToken");

            var idpClient = httpClientFactory.CreateClient ("IDPClient");

            //get the discovery document
            var discoveryResponse = await idpClient.GetDiscoveryDocumentAsync ();

            //refresh the tokens
            var refreshToken = await httpContextAccessor.HttpContext.GetTokenAsync (OpenIdConnectParameterNames.RefreshToken);
            Console.WriteLine ($"Current Refresh Token : {refreshToken}");

            if (string.IsNullOrWhiteSpace (refreshToken)) {
                return default;
            }

            var refreshResponse = await idpClient.RequestRefreshTokenAsync (new RefreshTokenRequest {
                Address = discoveryResponse.TokenEndpoint,
                    ClientId = "imagegalleryclient",
                    ClientSecret = "secret",
                    RefreshToken = refreshToken
            });

            //store the tokens
            var updatedTokens = new List<AuthenticationToken> ();
            updatedTokens.Add (new AuthenticationToken { Name = OpenIdConnectParameterNames.IdToken, Value = refreshResponse.IdentityToken });
            updatedTokens.Add (new AuthenticationToken { Name = OpenIdConnectParameterNames.AccessToken, Value = refreshResponse.AccessToken });
            updatedTokens.Add (new AuthenticationToken { Name = OpenIdConnectParameterNames.RefreshToken, Value = refreshResponse.RefreshToken });
            updatedTokens.Add (new AuthenticationToken { Name = "expires_at", Value = (DateTime.UtcNow + TimeSpan.FromSeconds (refreshResponse.ExpiresIn)).ToString ("o", CultureInfo.InvariantCulture) });

            foreach (var updatedToken in updatedTokens) {
                Console.WriteLine ($"{updatedToken.Name} - {updatedToken.Value}");
            }

            // get authenticate result, containing the current principal and properties
            var currentAuthenticateResult = await httpContextAccessor.HttpContext.AuthenticateAsync (CookieAuthenticationDefaults.AuthenticationScheme);

            // store the updated tokens
            currentAuthenticateResult.Properties.StoreTokens (updatedTokens);

            // sign in
            await httpContextAccessor.HttpContext.SignInAsync (CookieAuthenticationDefaults.AuthenticationScheme, currentAuthenticateResult.Principal, currentAuthenticateResult.Properties);

            return refreshResponse.AccessToken;
        }
    }

}