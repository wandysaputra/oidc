using System;
using System.IdentityModel.Tokens.Jwt;
using IdentityModel;
using ImageGallery.Client.HttpHandlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

namespace ImageGallery.Client {
    public class Startup {
        public IConfiguration Configuration { get; }

        public Startup (IConfiguration configuration) {
            Configuration = configuration;
            // clear out default claims mapping
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear ();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices (IServiceCollection services) {
            services.AddControllersWithViews ()
                .AddJsonOptions (opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);

            services.AddAuthorization (options => {
                options.AddPolicy ("CanOrderFrame", configurePolicy => {
                    configurePolicy.RequireAuthenticatedUser ();
                    configurePolicy.RequireClaim ("country", "be");
                    configurePolicy.RequireClaim ("subscriptionLevel", "PayingUser");
                });
            });

            services.AddHttpContextAccessor ();
            services.AddTransient<BearerTokenHandler> ();

            // create an HttpClient used for accessing the API
            services.AddHttpClient ("APIClient", client => {
                    client.BaseAddress = new Uri ("https://localhost:44366/");
                    client.DefaultRequestHeaders.Clear ();
                    client.DefaultRequestHeaders.Add (HeaderNames.Accept, "application/json");
                })
                .AddHttpMessageHandler<BearerTokenHandler> ();

            // create an HttpClient used for accessing the IDP API
            services.AddHttpClient ("IDPClient", client => {
                client.BaseAddress = new Uri ("https://localhost:44318/");
                client.DefaultRequestHeaders.Clear ();
                client.DefaultRequestHeaders.Add (HeaderNames.Accept, "application/json");
            });

            services.AddAuthentication (options => {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                }).AddCookie (CookieAuthenticationDefaults.AuthenticationScheme, options => {
                    options.AccessDeniedPath = "/Authorization/AccessDenied"; // set access denied path when Role base authorization failed
                })
                .AddOpenIdConnect (OpenIdConnectDefaults.AuthenticationScheme, options => {
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.Authority = "https://localhost:44318/";
                    options.ClientId = "imagegalleryclient";
                    options.ResponseType = "code";
                    options.UsePkce = !false;
                    // options.CallbackPath = new Microsoft.AspNetCore.Http.PathString("...") // default: "/signin-oidc"
                    // options.SignedOutCallbackPath = new Microsoft.AspNetCore.Http.PathString("..."); // default: signout-callback-oidc

                    // commentted out because by default added https://github.com/dotnet/aspnetcore/blob/main/src/Security/Authentication/OpenIdConnect/src/OpenIdConnectOptions.cs#L43
                    // options.Scope.Add("openid");
                    // options.Scope.Add("profile");
                    options.Scope.Add ("address"); // request for address scope

                    options.Scope.Add ("roles");
                    options.ClaimActions.MapUniqueJsonKey ("role", "role"); // to added claim scope to ClaimIndetity
                    options.TokenValidationParameters = new TokenValidationParameters { // to mapped to ClaimPrincipal IsInRole `User.IsInRole("PayingUser")`
                        NameClaimType = JwtClaimTypes.GivenName,
                        RoleClaimType = JwtClaimTypes.Role
                    };

                    options.SaveTokens = true;
                    options.ClientSecret = "secret";
                    // options.Prompt = OpenIdConnectPrompt.Consent; // to prompt consent screen on Client level
                    options.GetClaimsFromUserInfoEndpoint = true;

                    // options.ClaimActions.Remove("nbf"); // nbf by default exlcuded, uses Remove to includes it
                    // https://github.com/dotnet/aspnetcore/blob/main/src/Security/Authentication/OpenIdConnect/src/OpenIdConnectOptions.cs#L52
                    options.ClaimActions.DeleteClaim ("sid");
                    options.ClaimActions.DeleteClaim ("idp");
                    options.ClaimActions.DeleteClaim ("s_hash");
                    options.ClaimActions.DeleteClaim ("auth_time");

                    // options.ClaimActions.MapUniqueJsonKey("address", "address");

                    options.Scope.Add ("imagegalleryapi");

                    options.Scope.Add ("country");
                    options.Scope.Add ("subscriptionLevel");
                    options.ClaimActions.MapUniqueJsonKey ("country", "country");
                    options.ClaimActions.MapUniqueJsonKey ("subscriptionLevel", "subscriptionLevel");
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
            app.UseStaticFiles ();

            if (env.IsDevelopment ()) {
                app.UseDeveloperExceptionPage ();
            } else {
                app.UseExceptionHandler ("/Shared/Error");
                // The default HSTS value is 30 days. You may want to change this for
                // production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts ();
            }
            app.UseHttpsRedirection ();
            app.UseStaticFiles ();

            app.UseRouting ();

            app.UseAuthentication ();
            app.UseAuthorization ();

            app.UseEndpoints (endpoints => {
                endpoints.MapControllerRoute (
                    name: "default",
                    pattern: "{controller=Gallery}/{action=Index}/{id?}");
            });
        }
    }
}