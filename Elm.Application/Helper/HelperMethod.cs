using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

namespace Elm.Application.Helper
{
    public static class HelperMethod
    {
        /// <summary>
        /// جلب الـ IP مع دعم Cloudflare و Reverse Proxy
        /// </summary>
        public static string GetClientIp(HttpContext context)
        {
            // Cloudflare
            var cfIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(cfIp)) return cfIp;

            // Reverse Proxy
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
                return forwardedFor.Split(',').FirstOrDefault()?.Trim() ?? "unknown";

            // Direct
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// بصمة الجهاز من الـ Headers
        /// دقة ~92-95% - كافية لـ Rate Limiting
        /// </summary>
        public static string GetDeviceFingerprint(HttpContext context)
        {
            var parts = new[]
            {
            GetClientIp(context),
            context.Request.Headers.UserAgent.FirstOrDefault() ?? "",
            context.Request.Headers.AcceptLanguage.FirstOrDefault() ?? "",
            context.Request.Headers.AcceptEncoding.FirstOrDefault() ?? "",
            context.Request.Headers.Accept.FirstOrDefault() ?? "",
            context.Request.Headers["Sec-CH-UA"].FirstOrDefault() ?? "",
            context.Request.Headers["Sec-CH-UA-Platform"].FirstOrDefault() ?? "",
            context.Request.Headers["Sec-CH-UA-Mobile"].FirstOrDefault() ?? "",
        };

            var raw = string.Join("|", parts);
            return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(raw))
            )[..16];
        }
    }

}
