// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Test;

namespace IdentityServerHost.Quickstart.UI {
    public class TestUsers {
        public static List<TestUser> Users = new List<TestUser> {
            new TestUser {
            SubjectId = "d860efca-22d9-47fd-8249-791ba61b07c7",
            Username = "Frank",
            Password = "password",

            Claims = new List<Claim> {
            new Claim ("given_name", "Frank"),
            new Claim ("family_name", "Underwood"),
            new Claim ("address", "Main Road 1"), // to be show in address resource/claim
            new Claim ("role", "FreeUser"),

            }
            },
            new TestUser {
            SubjectId = "b7539694-97e7-4dfe-84da-b4256e1ff5c7",
            Username = "Claire",
            Password = "password",

            Claims = new List<Claim> {
            new Claim ("given_name", "Claire"),
            new Claim ("family_name", "Underwood"),
            new Claim ("address", "Big Street 2"),
            new Claim ("role", "PayingUser"),
            }
            }
        };
        /*
        public static List<TestUser> Users
        {
            get
            {
                var address = new
                {
                    street_address = "One Hacker Way",
                    locality = "Heidelberg",
                    postal_code = 69118,
                    country = "Germany"
                };

                return new List<TestUser>
                {
                    new TestUser
                    {
                        SubjectId = "818727",
                        Username = "alice",
                        Password = "alice",
                        Claims =
                        {
                            new Claim(JwtClaimTypes.Name, "Alice Smith"),
                            new Claim(JwtClaimTypes.GivenName, "Alice"),
                            new Claim(JwtClaimTypes.FamilyName, "Smith"),
                            new Claim(JwtClaimTypes.Email, "AliceSmith@email.com"),
                            new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                            new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                            new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json)
                        }
                    },
                    new TestUser
                    {
                        SubjectId = "88421113",
                        Username = "bob",
                        Password = "bob",
                        Claims =
                        {
                            new Claim(JwtClaimTypes.Name, "Bob Smith"),
                            new Claim(JwtClaimTypes.GivenName, "Bob"),
                            new Claim(JwtClaimTypes.FamilyName, "Smith"),
                            new Claim(JwtClaimTypes.Email, "BobSmith@email.com"),
                            new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                            new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                            new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json)
                        }
                    }
                };
            }
        }*/
    }
}