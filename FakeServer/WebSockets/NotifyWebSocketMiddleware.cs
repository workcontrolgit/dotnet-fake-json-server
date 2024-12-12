﻿using FakeServer.Common;

namespace FakeServer.WebSockets;

public class NotifyWebSocketMiddleware
{
    private readonly List<string> _updateMethods = new() { "POST", "PUT", "PATCH", "DELETE" };

    private readonly RequestDelegate _next;
    private readonly IMessageBus _bus;

    public NotifyWebSocketMiddleware(RequestDelegate next, IMessageBus bus)
    {
        _next = next;
        _bus = bus;
    }

    public async Task Invoke(HttpContext context)
    {
        await _next(context);

        if (context.Request.Path.Value.StartsWith($"/{Config.ApiRoute}") &&
            _updateMethods.Contains(context.Request.Method) &&
            (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300))
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value;

            if (method == "POST")
            {
                var location = context.Response.Headers["Location"].ToString();
                var itemId = location.Substring(location.LastIndexOf('/') + 1);
                path = $"{path}/{itemId}";
            }

            var data = ObjectHelper.GetWebSocketMessage(method, path);
            _bus.Publish("updated", data);
        }
    }
}