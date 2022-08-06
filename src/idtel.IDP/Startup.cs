// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Security.Cryptography.X509Certificates;
using IdentityServerHost.Quickstart.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            // uncomment, if you want to add an MVC-based UI
            services.AddControllersWithViews();

            var builder = services.AddIdentityServer(options =>
            {
                // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                options.EmitStaticAudienceClaim = !true;
            })
                .AddInMemoryIdentityResources(Config.IdentityResources)
                .AddInMemoryApiScopes(Config.ApiScopes)
                .AddInMemoryApiResources(Config.ApiResources)
                .AddInMemoryClients(Config.Clients)
                .AddTestUsers(TestUsers.Users);

            // not recommended for production - you need to store your key material somewhere secure
            // builder.AddDeveloperSigningCredential();
            builder.AddSigningCredential(LoadCertificateFromStore());
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

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
*/
        public X509Certificate2 LoadCertificateFromStore(){
            string thumbPrint = "b87746f2096eff567c7ee8bef1c00b05b6f77703";

            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine)){
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbPrint, true);

                if(certCollection.Count == 0){
                    throw new Exception("The specified certificate wasn't found!");
                }

                return certCollection[0];
            }
        }
    }
}
