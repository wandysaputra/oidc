// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServerHost.Quickstart.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace idtel.IDP
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }

        public Startup(IWebHostEnvironment environment)
        {
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var idTelIdpDataDbConnectionString = "Server=(localdb)\\mssqllocaldb;Database=IdTelIDPDataDB;Trusted_Connection=true;";

            // uncomment, if you want to add an MVC-based UI
            services.AddControllersWithViews();

            var builder = services.AddIdentityServer(options =>
            {
                // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                options.EmitStaticAudienceClaim = !true;
            })
                // .AddInMemoryIdentityResources(Config.IdentityResources)
                // .AddInMemoryApiScopes(Config.ApiScopes)
                // .AddInMemoryApiResources(Config.ApiResources)
                // .AddInMemoryClients(Config.Clients)
                .AddTestUsers(TestUsers.Users);

            // not recommended for production - you need to store your key material somewhere secure
            // builder.AddDeveloperSigningCredential();
            builder.AddSigningCredential(LoadCertificateFromStore());

            var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            builder.AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = builder =>
                    builder.UseSqlServer(idTelIdpDataDbConnectionString,
                    options => options.MigrationsAssembly(migrationAssembly));
            });

            builder.AddOperationalStore(options =>
            {
                options.ConfigureDbContext = builder =>
                    builder.UseSqlServer(idTelIdpDataDbConnectionString,
                    options => options.MigrationsAssembly(migrationAssembly));
            });
            // after added AddOperationalStore, execute `dotnet ef migrations add InitialIdentityServerPersistedGrantDBMigration --context PersistedGrantDbContext`
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            InitializeDatabase(app);

            // uncomment if you want to add MVC
            app.UseStaticFiles();
            app.UseRouting();

            app.UseIdentityServer();

            // uncomment, if you want to add MVC
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
        /*
        - To generate local certificate, open powershell as admin and execute below script
        `New-SelfSignedCertificate -Subject "CN=IdTelIdSrvSigningCert" -CertStoreLocation "cert:\LocalMachine\My"`
        - Open `Manage computer certificates`
        - Go to certifcation we just created  in folder Personal\Certificates
        - Double click it and go to details
        - Copy the thumbprint's value
        - Copy the certificate and paste it to folder Trusted Root Certification Authorities\Certificates
        - Once implemented in IDP level, go to /.well-known/openid-configuration/jwks and check kid's value, it should be same as thumbprint's value
        - need execute `dotnet run` as admin
        */
        public X509Certificate2 LoadCertificateFromStore()
        {
            string thumbPrint = "b87746f2096eff567c7ee8bef1c00b05b6f77703";

            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbPrint, true);

                if (certCollection.Count == 0)
                {
                    throw new Exception("The specified certificate wasn't found!");
                }

                return certCollection[0];
            }
        }

        /*
        steps to add migration for Configuration Data
        - open `Package Managaer Console`
        - execute `add-migration -name InitialIdentityServerConfigurationDBMigration -context ConfigurationDbContext`
        - note: `-context ConfigurationDbContext` coming from package `IdentityServer4.EntityFramework`
        OR
        - execute this syntax from cli `dotnet ef migrations add InitialIdentityServerConfigurationDBMigration --context ConfigurationDbContext` after add package `Microsoft.EntityFrameworkCore.Tools`
        - after added `builder.AddedOperationalStore` execute this syntax, `dotnet ef migrations add InitialIdentityServerPersistedGrantDBMigration --context PersistedGrantDbContext`
        - need execute `dotnet run` as admin
        */
        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices
                .GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider
                    .GetRequiredService<PersistedGrantDbContext>().Database.Migrate(); // linked with builder.AddedOperationalStore

                var context = serviceScope.ServiceProvider
                    .GetRequiredService<ConfigurationDbContext>();
                context.Database.Migrate();
                if (!context.Clients.Any())
                {
                    foreach (var client in Config.Clients)
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Config.IdentityResources)
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var resource in Config.ApiResources)
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiScopes.Any())
                {
                    foreach (var scope in Config.ApiScopes)
                    {
                        context.ApiScopes.Add(scope.ToEntity());
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}
