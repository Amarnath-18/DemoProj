using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace demoProject.Middleware
{
    public class CookieToHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        public CookieToHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Request.Cookies["auth_token"];
            
            if (!string.IsNullOrEmpty(token))
            {
                context.Request.Headers["Authorization"] = "Bearer " + token;
            }

            await _next(context);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class CookieToHeaderMiddlewareExtensions
    {
        public static IApplicationBuilder UseCookieToHeader(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CookieToHeaderMiddleware>();
        }
    }
}