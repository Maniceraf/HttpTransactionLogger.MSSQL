using Azure;
using Extension;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Extenstion
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly ILoggerService _loggerService;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger, ILoggerService loggerService)
        {
            _next = next;
            _logger = logger;
            _loggerService = loggerService;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = context.Request;
            var requestBody = request.Body;

            //This line allows us to set the reader for the request back at the beginning of its stream.
            request.EnableBuffering();

            //We now need to read the request stream.  First, we create a new byte[] with the same length as the request stream...
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];

            //...Then we copy the entire request stream into the new buffer.
            await request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);

            //We convert the byte[] into a string using UTF8 encoding...
            var bodyAsText = Encoding.UTF8.GetString(buffer);

            // reset the stream position to 0, which is allowed because of EnableBuffering()
            request.Body.Seek(0, SeekOrigin.Begin);

            var remoteIpAddress = context.Connection.RemoteIpAddress.MapToIPv6().ToString();
            var referrerUrl = $"{request.Scheme}://{request.Host}{request.Path} {request.QueryString}";
            var method = request.Method;
            var timestamp = DateTime.Now;
            string requestContent = bodyAsText;

            //Copy a pointer to the original response body stream
            var originalBodyStream = context.Response.Body;

            //Create a new memory stream...
            using var responseBody = new MemoryStream();
            //...and use that for the temporary response body
            context.Response.Body = responseBody;

            await _next(context);

            var response = context.Response;

            //We need to read the response stream from the beginning...
            response.Body.Seek(0, SeekOrigin.Begin);

            //...and copy it into a string
            string responseContent = await new StreamReader(response.Body).ReadToEndAsync();

            //We need to reset the reader for the response so that the client can read it.
            response.Body.Seek(0, SeekOrigin.Begin);

            await responseBody.CopyToAsync(originalBodyStream);

            try
            {
                _loggerService.Log(method, referrerUrl, response.StatusCode, requestContent, responseContent, remoteIpAddress);

                // Log thông tin truy cập
                _logger.LogInformation($"Access Info - IP: {remoteIpAddress}; Method: {method}; Path: {referrerUrl}; Time: {timestamp}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
            }
        }
    }
}
