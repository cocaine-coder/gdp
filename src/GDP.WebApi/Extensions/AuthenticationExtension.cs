using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using static FastEndpoints.Security.JWTBearer;

namespace GDP.WebApi.Extensions
{
    public static class AuthenticationExtension
    {
        public static IServiceCollection AddAuthenticationWithJWTBearer(this IServiceCollection services,
                                                                string tokenSigningKey,
                                                                string? issuer = null,
                                                                string? audience = null,
                                                                bool validateLifetime = true,
                                                                TokenSigningStyle tokenSigningStyle = TokenSigningStyle.Symmetric,
                                                                Action<TokenValidationParameters>? tokenValidationConfiguration = null)
        {
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                SecurityKey key;
                if (tokenSigningStyle == TokenSigningStyle.Symmetric)
                {
                    key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tokenSigningKey));
                }
                else
                {
                    using var rsa = RSA.Create();
                    rsa.ImportRSAPublicKey(Convert.FromBase64String(tokenSigningKey), out _);
                    key = new RsaSecurityKey(rsa);
                }
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateAudience = audience is not null,
                    ValidAudience = audience,
                    ValidateIssuer = issuer is not null,
                    ValidIssuer = issuer,
                    ValidateLifetime = validateLifetime,
                    IssuerSigningKey = key,
                };

                o.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        //如果token过期在返回头上添加过期标识
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            context.Response.Headers.Add("Token-Expired", "true");

                        return Task.CompletedTask;
                    },

                    OnMessageReceived = context =>
                    {
                        context.Request.Headers.TryGetValue("Authorization", out var token);

                        // 从请求参数中 access_token 获取token
                        if (string.IsNullOrEmpty(token) && context.Request.Query.TryGetValue("access_token", out token))
                            context.Token = token;

                        return Task.CompletedTask;
                    }
                };

                tokenValidationConfiguration?.Invoke(o.TokenValidationParameters);
            });

            return services;
        }
    }
}