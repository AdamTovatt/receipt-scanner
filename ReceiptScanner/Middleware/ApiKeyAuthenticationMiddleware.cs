using System.Security.Claims;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using EasyReasy.EnvironmentVariables;

namespace ReceiptScanner.Middleware
{
    public class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _apiKey;

        // Thread-safe dictionary to track failed authentication attempts by IP address
        private static readonly ConcurrentDictionary<string, FailedAttemptInfo> _failedAttempts = new ConcurrentDictionary<string, FailedAttemptInfo>();

        /// <summary>
        /// Gets or sets a millisecond value that controls how much delay to add to a request with invalid api key each time one fails.
        /// Delay is added to prevent brute force attacks.
        /// </summary>
        public static int FailedAttemptMillisecondDelay { get; set; } = 500;

        public ApiKeyAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
            _apiKey = EnvironmentVariables.GetVariable(EnvironmentVariable.ApiKey);
        }

        /// <summary>
        /// Information about failed authentication attempts for a specific IP address.
        /// </summary>
        private class FailedAttemptInfo
        {
            /// <summary>
            /// Gets or sets the number of failed attempts.
            /// </summary>
            public int FailedAttempts { get; set; }

            /// <summary>
            /// Gets or sets the timestamp of the last failed attempt.
            /// </summary>
            public DateTime LastFailedAttempt { get; set; }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for certain endpoints
            if (ShouldSkipAuthentication(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Check if API key is required
            if (string.IsNullOrEmpty(_apiKey))
            {
                // No API key configured, allow all requests
                await _next(context);
                return;
            }

            // Get client IP address for rate limiting
            string clientIp = GetClientIpAddress(context);

            // Get API key from request headers
            string? requestApiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();

            if (string.IsNullOrEmpty(requestApiKey))
            {
                // Record failed attempt and apply rate limiting
                await HandleFailedAuthentication(clientIp);

                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";
                string errorResponse = "{\"error\":\"API key is required\",\"message\":\"Please provide a valid X-API-Key header\"}";
                byte[] errorBytes = Encoding.UTF8.GetBytes(errorResponse);
                await context.Response.Body.WriteAsync(errorBytes);
                return;
            }

            // Validate API key
            if (!string.Equals(requestApiKey, _apiKey, StringComparison.Ordinal))
            {
                // Record failed attempt and apply rate limiting
                await HandleFailedAuthentication(clientIp);

                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";
                string errorResponse = "{\"error\":\"Invalid API key\",\"message\":\"The provided API key is not valid\"}";
                byte[] errorBytes = Encoding.UTF8.GetBytes(errorResponse);
                await context.Response.Body.WriteAsync(errorBytes);
                return;
            }

            // Add claims for the authenticated user
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "API User"),
                new Claim("ApiKey", requestApiKey)
            };

            ClaimsIdentity identity = new ClaimsIdentity(claims, "ApiKey");
            context.User = new ClaimsPrincipal(identity);

            await _next(context);
        }

        /// <summary>
        /// Gets the client IP address from the request context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>The client IP address.</returns>
        /// <remarks>
        /// This method checks for forwarded headers (X-Forwarded-For, X-Real-IP) to handle
        /// requests that come through proxies or load balancers, falling back to the direct
        /// connection IP address.
        /// </remarks>
        private static string GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded headers first
            string? forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one
                return forwardedFor.Split(',')[0].Trim();
            }

            string? realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to the direct connection IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// Handles a failed authentication attempt by recording it and applying rate limiting.
        /// </summary>
        /// <param name="clientIp">The client IP address.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method records the failed attempt and applies a progressive delay based on
        /// the number of failed attempts. The delay is 500ms multiplied by the number of
        /// failed attempts, providing an exponential backoff effect.
        /// </remarks>
        private static async Task HandleFailedAuthentication(string clientIp)
        {
            // Get or create failed attempt info for this IP
            FailedAttemptInfo failedInfo = _failedAttempts.GetOrAdd(clientIp, _ => new FailedAttemptInfo());

            // Update failed attempt count and timestamp
            failedInfo.FailedAttempts++;
            failedInfo.LastFailedAttempt = DateTime.UtcNow;

            // Apply progressive delay: 500ms * number of failed attempts
            int delayMs = Math.Max(1, (failedInfo.FailedAttempts - 1) * FailedAttemptMillisecondDelay);
            await Task.Delay(delayMs);
        }

        private static bool ShouldSkipAuthentication(PathString path)
        {
            string pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

            return pathValue.StartsWith("/health") ||
                   pathValue.StartsWith("/swagger") ||
                   pathValue.StartsWith("/swagger-ui") ||
                   pathValue == "/" ||
                   pathValue == "/styles.css" ||
                   pathValue == "/script.js" ||
                   pathValue == "/ping" ||
                   pathValue == "/favicon.ico";
        }
    }

    public static class ApiKeyAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        }
    }
}