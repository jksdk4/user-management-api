using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace UserManagementAPI.Middleware
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;
        private static ConcurrentDictionary<string, int> _requestCounts = new ConcurrentDictionary<string, int>();

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.ToString();
            _requestCounts.AddOrUpdate(path, 1, (key, count) => count + 1);

            _logger.LogInformation("Incoming request: {Method} {Path}", context.Request.Method, path);

            // Capture the original response body stream
            var originalBodyStream = context.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                await _next(context);

                var count = _requestCounts[path];
                _logger.LogInformation("Outgoing response: {StatusCode}, Request Count: {Count}", context.Response.StatusCode, count);

                // Read the response body
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseBodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                // Deserialize and re-serialize the original response to ensure proper JSON formatting
                object originalResponseObject;
                try
                {
                    originalResponseObject = JsonSerializer.Deserialize<object>(responseBodyText)!;
                }
                catch (JsonException)
                {
                    originalResponseObject = responseBodyText;
                }

                // Append log information to the response body
                var logInfo = new
                {
                    Method = context.Request.Method,
                    Path = path,
                    StatusCode = context.Response.StatusCode,
                    RequestCount = count,
                };

                var modifiedResponseBody = new
                {
                    OriginalResponse = originalResponseObject,
                    LogInfo = logInfo
                };

                var modifiedResponseBodyJson = JsonSerializer.Serialize(modifiedResponseBody);

                // Set the response content type to application/json
                context.Response.ContentType = "application/json";

                // Write the modified response body
                var modifiedResponseBodyBytes = Encoding.UTF8.GetBytes(modifiedResponseBodyJson);
                await context.Response.Body.WriteAsync(modifiedResponseBodyBytes, 0, modifiedResponseBodyBytes.Length);

                // Copy the contents of the new memory stream (which contains the response) to the original stream
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        public static int GetRequestCount(string path)
        {
            _requestCounts.TryGetValue(path, out int count);
            return count;
        }
    }
}