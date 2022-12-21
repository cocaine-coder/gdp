using FastEndpoints.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GDP.WebApi.Extensions
{
    public static class AuthenticationExtension
    {
        public static IServiceCollection AddAuthenticationWithJWTBearer(this IServiceCollection services, string tokenSigningKey)
        {
            return services.AddJWTBearerAuth(tokenSigningKey, tokenValidation: parameters => { parameters.ValidateLifetime = false; }, bearerEvents: events =>
             {
                 events = new JwtBearerEvents
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
             });
        }
    }
}