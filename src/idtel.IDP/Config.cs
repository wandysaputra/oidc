// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace idtel.IDP
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Address() // add address resource 
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            { };

        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                new Client{
                    ClientName = "Image Gallery",
                    ClientId = "imagegalleryclient",
                    AllowedGrantTypes = GrantTypes.Code,
                    RedirectUris = new List<string> {
                        "https://localhost:44389/signin-oidc"
                    },
                    PostLogoutRedirectUris = new List<string>{
                        "https://localhost:44389/signout-callback-oidc"
                    },
                    AllowedScopes ={
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Address // allows to get address scope in claims
                    } ,
                    ClientSecrets={
                        new Secret("secret".Sha256())
                    },
                    RequirePkce = !false,
                    // RequireConsent = true 
                }
            };
    }
}