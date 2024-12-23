namespace UserManagementAPI.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Bypass authentication for the root page
            if (context.Request.Path.Equals("/"))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("401 Unauthorized: Authorization header missing");
                return;
            }
            else
            {
                var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                if (!ValidateToken(token))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid token");
                    return;
                }
                else
                {
                    await _next(context);
                }
            }
        }

        private bool ValidateToken(string token)
        {
            return token == "valid-token"; // Example validation
        }
    }
}