﻿/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/aspnet-contrib/AspNet.Security.OpenIdConnect.Server
 * for more information concerning the license and the contributors participating to this project.
 */

using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AspNet.Security.OpenIdConnect.Server.Tests {
    public class OpenIdConnectServerMiddlewareTests {
        [Fact]
        public void Constructor_MissingProviderThrowsAnException() {
            // Arrange, act, assert
            var exception = Assert.Throws<ArgumentException>(() => CreateAuthorizationServer(options => {
                options.Provider = null;
            }));

            Assert.Equal("options", exception.ParamName);
            Assert.StartsWith("The authorization provider registered in the options cannot be null.", exception.Message);
        }

        [Fact]
        public void Constructor_MissingClockThrowsAnException() {
            // Arrange, act, assert
            var exception = Assert.Throws<ArgumentException>(() => CreateAuthorizationServer(options => {
                options.SystemClock = null;
            }));

            Assert.Equal("options", exception.ParamName);
            Assert.StartsWith("The system clock registered in the options cannot be null.", exception.Message);
        }

        [Fact]
        public void Constructor_RelativeIssuerThrowsAnException() {
            // Arrange, act, assert
            var exception = Assert.Throws<ArgumentException>(() => CreateAuthorizationServer(options => {
                options.Issuer = new Uri("/path", UriKind.Relative);
            }));

            Assert.Equal("options", exception.ParamName);
            Assert.StartsWith("The issuer registered in the options must be a valid absolute URI.", exception.Message);
        }

        [Theory]
        [InlineData("http://www.fabrikam.com/path?param=value")]
        [InlineData("http://www.fabrikam.com/path#param=value")]
        public void Constructor_InvalidIssuerThrowsAnException(string issuer) {
            // Arrange, act, assert
            var exception = Assert.Throws<ArgumentException>(() => CreateAuthorizationServer(options => {
                options.Issuer = new Uri(issuer);
            }));

            Assert.Equal("options", exception.ParamName);
            Assert.StartsWith("The issuer registered in the options must contain " +
                              "no query and no fragment parts.", exception.Message);
        }

        [Fact]
        public void Constructor_NonHttpsIssuerThrowsAnExceptionWhenAllowInsecureHttpIsNotEnabled() {
            // Arrange, act, assert
            var exception = Assert.Throws<ArgumentException>(() => CreateAuthorizationServer(options => {
                options.AllowInsecureHttp = false;
                options.Issuer = new Uri("http://www.fabrikam.com/");
            }));

            Assert.Equal("options", exception.ParamName);
            Assert.StartsWith("The issuer registered in the options must be a HTTPS URI when " +
                              "AllowInsecureHttp is not set to true.", exception.Message);
        }

        [Fact]
        public void Constructor_AutomaticAuthenticateThrowsAnException() {
            // Arrange, act, assert
            var exception = Assert.Throws<ArgumentException>(() => CreateAuthorizationServer(options => {
                options.AutomaticAuthenticate = true;
            }));

            Assert.Equal("options", exception.ParamName);
            Assert.StartsWith("Automatic authentication cannot be used with " +
                              "the OpenID Connect server middleware.", exception.Message);
        }

        [Fact]
        public void Constructor_AutomaticChallengeThrowsAnException() {
            // Arrange, act, assert
            var exception = Assert.Throws<ArgumentException>(() => CreateAuthorizationServer(options => {
                options.AutomaticChallenge = true;
            }));

            Assert.Equal("options", exception.ParamName);
            Assert.StartsWith("Automatic authentication cannot be used with " +
                              "the OpenID Connect server middleware.", exception.Message);
        }

        [Fact]
        public void Constructor_MissingSigningCredentialsThrowAnException() {
            // Arrange, act, assert
            var exception = Assert.Throws<ArgumentException>(() => CreateAuthorizationServer(options => {
                options.AccessTokenHandler = new JwtSecurityTokenHandler();
                options.SigningCredentials.Clear();
            }));

            Assert.Equal("options", exception.ParamName);
            Assert.StartsWith("At least one signing key must be registered when using JWT as the access token format. " +
                              "Consider registering a X.509 certificate using 'services.AddOpenIddict().AddSigningCertificate()' " +
                              "or call 'services.AddOpenIddict().AddEphemeralSigningKey()' to use an ephemeral key.", exception.Message);
        }

        private static TestServer CreateAuthorizationServer(Action<OpenIdConnectServerOptions> configuration = null) {
            var builder = new WebHostBuilder();

            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services => services.AddAuthentication());

            builder.Configure(app => {
                app.UseOpenIdConnectServer(options => {
                    options.AllowInsecureHttp = true;

                    // Note: overriding the default data protection provider is not necessary for the tests to pass,
                    // but is useful to ensure unnecessary keys are not persisted in testing environments, which also
                    // helps make the unit tests run faster, as no registry or disk access is required in this case.
                    options.DataProtectionProvider = new EphemeralDataProtectionProvider(app.ApplicationServices);

                    // Run the configuration delegate
                    // registered by the unit tests.
                    configuration?.Invoke(options);
                });
            });

            return new TestServer(builder);
        }
    }
}