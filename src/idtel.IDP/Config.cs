// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using IdentityServer4;
using IdentityServer4.Models;

namespace idtel.IDP {
    public static class Config {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[] {
                new IdentityResources.OpenId (),
                new IdentityResources.Profile (),
                new IdentityResources.Address (), // add address resource
                new IdentityResource ("roles", "Your role(s)", new List<string> { "role" }), // custome scope/resource role
                new IdentityResource ("country", "The country you're living in", new List<string> { "country" }),
                new IdentityResource ("subscriptionLevel", "Your Subscription Level", new List<string> { "subscriptionLevel" }),
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[] {
                new ApiScope ("imagegalleryapi", "Image Gallery API", new List<string> { "role" })
            };

        public static IEnumerable<ApiResource> ApiResources =>
            new ApiResource[] {
                new ApiResource ("imagegalleryapi", "Image Gallery API", new List<string> { "role" }) {
                Scopes = { "imagegalleryapi" }
                }
            };

        public static IEnumerable<Client> Clients =>
            new Client[] {
                new Client {
                ClientName = "Image Gallery",
                ClientId = "imagegalleryclient",
                AllowedGrantTypes = GrantTypes.Code,
                RedirectUris = new List<string> {
                "https://localhost:44389/signin-oidc"
                },
                PostLogoutRedirectUris = new List<string> {
                "https://localhost:44389/signout-callback-oidc"
                },
                AllowedScopes = {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                IdentityServerConstants.StandardScopes.Address, // allows to get address scope in claims
                "roles",
                "imagegalleryapi",
                "country",
                "subscriptionLevel"
                },
                ClientSecrets = {
                new Secret ("secret".Sha256 ())
                },
                RequirePkce = !false,
                // RequireConsent = true // to prompt consent screen on IDP level
                }
            };
    }
}