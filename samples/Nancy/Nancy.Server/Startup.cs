using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Nancy.Server.Extensions;
using Nancy.Server.Providers;
using NWebsec.Owin;
using Owin;

namespace Nancy.Server {
    public class Startup {
        public void Configuration(IAppBuilder app) {
            app.SetDefaultSignInAsAuthenticationType("ServerCookie");

            app.UseWhen(context => context.Request.Path.StartsWithSegments(new PathString("/api")), map => {
                map.UseOAuthValidation();

                // Alternatively, you can also use the introspection middleware.
                // Using it is recommended if your resource server is in a
                // different application/separated from the authorization server.
                //
                // map.UseOAuthIntrospection(options => {
                //     options.AuthenticationMode = AuthenticationMode.Active;
                //     options.Authority = "http://localhost:54540/";
                //     options.Audiences.Add("resource_server");
                //     options.ClientId = "resource_server";
                //     options.ClientSecret = "875sqd4s5d748z78z7ds1ff8zz8814ff88ed8ea4z4zzd";
                // });
            });

            // Insert a new cookies middleware in the pipeline to store
            // the user identity returned by the external identity provider.
            app.UseWhen(context => !context.Request.Path.StartsWithSegments(new PathString("/api")), map => {
                map.UseCookieAuthentication(new CookieAuthenticationOptions {
                    AuthenticationMode = AuthenticationMode.Active,
                    AuthenticationType = "ServerCookie",
                    CookieName = CookieAuthenticationDefaults.CookiePrefix + "ServerCookie",
                    ExpireTimeSpan = TimeSpan.FromMinutes(5),
                    LoginPath = new PathString("/signin")
                });
            });

            // Insert a new middleware responsible of setting the Content-Security-Policy header.
            // See https://nwebsec.codeplex.com/wikipage?title=Configuring%20Content%20Security%20Policy&referringTitle=NWebsec
            app.UseCsp(options => options.DefaultSources(configuration => configuration.Self())
                                         .ScriptSources(configuration => configuration.UnsafeInline()));

            // Insert a new middleware responsible of setting the X-Content-Type-Options header.
            // See https://nwebsec.codeplex.com/wikipage?title=Configuring%20security%20headers&referringTitle=NWebsec
            app.UseXContentTypeOptions();

            // Insert a new middleware responsible of setting the X-Frame-Options header.
            // See https://nwebsec.codeplex.com/wikipage?title=Configuring%20security%20headers&referringTitle=NWebsec
            app.UseXfo(options => options.Deny());

            // Insert a new middleware responsible of setting the X-Xss-Protection header.
            // See https://nwebsec.codeplex.com/wikipage?title=Configuring%20security%20headers&referringTitle=NWebsec
            app.UseXXssProtection(options => options.EnabledWithBlockMode());

            app.Use(async (context, next) => {
                // Keep the original stream in a separate
                // variable to restore it later if necessary.
                var stream = context.Request.Body;

                // Optimization: don't buffer the request if
                // there was no stream or if it is rewindable.
                if (stream == Stream.Null || stream.CanSeek) {
                    await next();

                    return;
                }

                try {
                    using (var buffer = new MemoryStream()) {
                        // Copy the request stream to the memory stream.
                        await stream.CopyToAsync(buffer);

                        // Rewind the memory stream.
                        buffer.Position = 0L;

                        // Replace the request stream by the memory stream.
                        context.Request.Body = buffer;

                        // Invoke the rest of the pipeline.
                        await next();
                    }
                }

                finally {
                    // Restore the original stream.
                    context.Request.Body = stream;
                }
            });

            app.UseOpenIdConnectServer(options => {
                options.Provider = new AuthorizationProvider();

                // Enable the authorization, logout, token and userinfo endpoints.
                options.AuthorizationEndpointPath = new PathString("/connect/authorize");
                options.LogoutEndpointPath = new PathString("/connect/logout");
                options.TokenEndpointPath = new PathString("/connect/token");
                options.UserinfoEndpointPath = new PathString("/connect/userinfo");

                // Note: see AuthorizationModule.cs for more
                // information concerning ApplicationCanDisplayErrors.
                options.ApplicationCanDisplayErrors = true;
                options.AllowInsecureHttp = true;

                // Register a new ephemeral key, that is discarded when the application
                // shuts down. Tokens signed using this key are automatically invalidated.
                // This method should only be used during development.
                options.SigningCredentials.AddEphemeralKey();

                // On production, using a X.509 certificate stored in the machine store is recommended.
                // You can generate a self-signed certificate using Pluralsight's self-cert utility:
                // https://s3.amazonaws.com/pluralsight-free/keith-brown/samples/SelfCert.zip
                //
                // options.SigningCredentials.AddCertificate("7D2A741FE34CC2C7369237A5F2078988E17A6A75");
                //
                // Alternatively, you can also store the certificate as an embedded .pfx resource
                // directly in this assembly or in a file published alongside this project:
                //
                // options.SigningCredentials.AddCertificate(
                //     assembly: typeof(Startup).GetTypeInfo().Assembly,
                //     resource: "Nancy.Server.Certificate.pfx",
                //     password: "Owin.Security.OpenIdConnect.Server");

                // Note: to override the default access token format and use JWT, assign AccessTokenHandler:
                // options.AccessTokenHandler = new JwtSecurityTokenHandler();

                // Register the logging listeners used by the OpenID Connect server middleware.
                options.UseLogging(logger => logger.AddConsole().AddDebug());
            });

            app.Use((context, next) => {
                if (context.Request.Body.CanSeek) {
                    context.Request.Body.Position = 0L;
                }

                return next();
            });

            app.UseNancy(options => options.Bootstrapper = new NancyBootstrapper());
        }
    }
}
