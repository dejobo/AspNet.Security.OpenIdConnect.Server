﻿/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/aspnet-contrib/AspNet.Security.OpenIdConnect.Server
 * for more information concerning the license and the contributors participating to this project.
 */

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace AspNet.Security.OpenIdConnect.Server {
    /// <summary>
    /// Provides various settings needed to control
    /// the behavior of the OpenID Connect server.
    /// </summary>
    public class OpenIdConnectServerOptions : AuthenticationOptions {
        /// <summary>
        /// Creates an instance of authorization server options with default values.
        /// </summary>
        public OpenIdConnectServerOptions() {
            AuthenticationScheme = OpenIdConnectServerDefaults.AuthenticationScheme;
        }

        /// <summary>
        /// The base address used to uniquely identify the authorization server.
        /// The URI must be absolute and may contain a path, but no query string or fragment part.
        /// Unless AllowInsecureHttp has been set to true, an HTTPS address must be provided.
        /// </summary>
        public Uri Issuer { get; set; }

        /// <summary>
        /// Gets the list of the credentials used to sign JWT tokens. You can provide any symmetric (e.g <see cref="SymmetricSecurityKey"/>)
        /// or asymmetric (e.g <see cref="RsaSecurityKey"/>, or <see cref="X509SecurityKey"/>) security key, but you're strongly
        /// encouraged to use a 2048 or 4096-bits RSA asymmetric key in production. Note that only keys supporting the
        /// <see cref="SecurityAlgorithms.RsaSha256Signature"/> algorithm can be exposed on the configuration metadata endpoint.
        /// </summary>
        public IList<SigningCredentials> SigningCredentials { get; } = new List<SigningCredentials>();

        /// <summary>
        /// The request path where client applications will redirect the user-agent in order to
        /// obtain user consent to issue a token. Must begin with a leading slash, like "/connect/authorize".
        /// </summary>
        public PathString AuthorizationEndpointPath { get; set; }

        /// <summary>
        /// The request path where client applications will be able to retrieve the configuration metadata associated
        /// with this instance. Must begin with a leading slash, like "/.well-known/openid-configuration".
        /// This setting can be set to <see cref="PathString.Empty"/> to disable the configuration endpoint.
        /// </summary>
        public PathString ConfigurationEndpointPath { get; set; } = "/.well-known/openid-configuration";

        /// <summary>
        /// The request path where client applications will be able to retrieve the JSON Web Key Set
        /// associated with this instance. Must begin with a leading slash, like "/.well-known/jwks".
        /// This setting can be set to <see cref="PathString.Empty"/> to disable the cryptography endpoint.
        /// </summary>
        public PathString CryptographyEndpointPath { get; set; } = "/.well-known/jwks";

        /// <summary>
        /// The request path client applications communicate with to validate an access, identity or refresh token.
        /// Must begin with a leading slash, like "/connect/introspect".
        /// </summary>
        public PathString IntrospectionEndpointPath { get; set; }

        /// <summary>
        /// The request path client applications communicate with to log out.
        /// Must begin with a leading slash, like "/connect/logout".
        /// </summary>
        public PathString LogoutEndpointPath { get; set; }

        /// <summary>
        /// The request path client applications communicate with to revoke an access or refresh token.
        /// Must begin with a leading slash, like "/connect/revoke".
        /// </summary>
        public PathString RevocationEndpointPath { get; set; }

        /// <summary>
        /// The request path client applications communicate with directly as part of the OpenID Connect protocol.
        /// Must begin with a leading slash, like "/connect/token". If the client is issued a client_secret, it must
        /// be provided to this endpoint.
        /// </summary>
        public PathString TokenEndpointPath { get; set; }

        /// <summary>
        /// The request path client applications communicate with to retrieve user information.
        /// Must begin with a leading slash, like "/connect/userinfo".
        /// </summary>
        public PathString UserinfoEndpointPath { get; set; }

        /// <summary>
        /// Specifies a <see cref="OpenIdConnectServerProvider"/> that the <see cref="OpenIdConnectServerMiddleware" />
        /// invokes to enable developer control over the while authentication/authorization process.
        /// </summary>
        public OpenIdConnectServerProvider Provider { get; set; } = new OpenIdConnectServerProvider();

        /// <summary>
        /// The data format used to protect and unprotect the information contained in the authorization code.
        /// If not provided by the application the default data protection provider depends on the host server.
        /// The SystemWeb host on IIS will use ASP.NET machine key data protection, and HttpListener and other self-hosted
        /// servers will use DPAPI data protection.
        /// </summary>
        public ISecureDataFormat<AuthenticationTicket> AuthorizationCodeFormat { get; set; }

        /// <summary>
        /// The data format used to protect the information contained in the access token.
        /// If not provided by the application the default data protection provider depends on the host server.
        /// The SystemWeb host on IIS will use ASP.NET machine key data protection, and HttpListener and other self-hosted
        /// servers will use DPAPI data protection.
        /// This property is only used when <see cref="AccessTokenHandler"/> is explicitly set to <value>null</value>
        /// and when <see cref="OpenIdConnectServerProvider.SerializeAccessToken"/>
        /// doesn't call <see cref="BaseControlContext.HandleResponse"/>.
        /// </summary>
        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; set; }

        /// <summary>
        /// The data format used to protect and unprotect the information contained in the refresh token.
        /// If not provided by the application the default data protection provider depends on the host server.
        /// The SystemWeb host on IIS will use ASP.NET machine key data protection, and HttpListener and other self-hosted
        /// servers will use DPAPI data protection.
        /// This property is only used when <see cref="OpenIdConnectServerProvider.SerializeRefreshToken"/>
        /// doesn't call <see cref="BaseControlContext.HandleResponse"/>.
        /// </summary>
        public ISecureDataFormat<AuthenticationTicket> RefreshTokenFormat { get; set; }

        /// <summary>
        /// The <see cref="SecurityTokenHandler"/> instance used to forge access tokens.
        /// Note: this property is only used when <see cref="OpenIdConnectServerProvider.SerializeAccessToken"/>
        /// doesn't call <see cref="BaseControlContext.HandleResponse"/>.
        /// </summary>
        public SecurityTokenHandler AccessTokenHandler { get; set; }

        /// <summary>
        /// The <see cref="JwtSecurityTokenHandler"/> instance used to forge identity tokens.
        /// You can replace the default instance to change the way id_tokens are serialized.
        /// This property is only used when <see cref="OpenIdConnectServerProvider.SerializeIdentityToken"/>
        /// doesn't call <see cref="BaseControlContext.HandleResponse"/>.
        /// </summary>
        public JwtSecurityTokenHandler IdentityTokenHandler { get; set; } = new JwtSecurityTokenHandler();

        /// <summary>
        /// The period of time the authorization code remains valid after being issued. The default is 5 minutes.
        /// This time span must also take into account clock synchronization between servers in a web farm, so a very
        /// brief value could result in unexpectedly expired tokens.
        /// </summary>
        public TimeSpan AuthorizationCodeLifetime { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The period of time the access token remains valid after being issued. The default is 1 hour.
        /// The client application is expected to refresh or acquire a new access token after the token has expired.
        /// </summary>
        public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// The period of time the identity token remains valid after being issued. The default is 20 minutes.
        /// The client application is expected to refresh or acquire a new identity token after the token has expired.
        /// </summary>
        public TimeSpan IdentityTokenLifetime { get; set; } = TimeSpan.FromMinutes(20);

        /// <summary>
        /// The period of time the refresh token remains valid after being issued. The default is 14 days.
        /// The client application is expected to start a whole new authentication flow after the refresh token has expired.
        /// </summary>
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(14);

        /// <summary>
        /// Determines whether new refresh tokens should be issued during a grant_type=refresh_token request.
        /// Set this property to <c>true</c> to issue a new refresh token, <c>false</c> to prevent the OpenID Connect
        /// server middleware from issuing new refresh tokens when receiving a grant_type=refresh_token request.
        /// </summary>
        public bool UseSlidingExpiration { get; set; } = true;

        /// <summary>
        /// Set to true if the web application is able to render error messages on the authorization endpoint. This is only needed for cases where
        /// the browser is not redirected back to the client application, for example, when the client_id or redirect_uri are incorrect. The
        /// authorization endpoint should expect to see the OpenID Connect response added to the ASP.NET 5 environment.
        /// </summary>
        public bool ApplicationCanDisplayErrors { get; set; }

        /// <summary>
        /// Used to know what the current clock time is when calculating or validating token expiration. When not assigned default is based on
        /// DateTimeOffset.UtcNow. This is typically needed only for unit testing.
        /// </summary>
        public ISystemClock SystemClock { get; set; } = new SystemClock();

        /// <summary>
        /// Gets or sets a boolean indicating whether incoming requests arriving on non-HTTPS endpoints should be rejected.
        /// By default, this property is set to <c>false</c> to help mitigate man-in-the-middle attacks.
        /// </summary>
        public bool AllowInsecureHttp { get; set; }

        /// <summary>
        /// Used to sanitize HTML responses. If you don't provide an explicit instance,
        /// one will be automatically retrieved through the dependency injection system.
        /// </summary>
        public HtmlEncoder HtmlEncoder { get; set; }

        /// <summary>
        /// Gets or sets the data protection provider used to create the default
        /// data protectors used by <see cref="OpenIdConnectServerMiddleware"/>.
        /// When this property is set to <c>null</c>, the data protection provider
        /// is directly retrieved from the dependency injection container.
        /// </summary>
        public IDataProtectionProvider DataProtectionProvider { get; set; }
    }
}
