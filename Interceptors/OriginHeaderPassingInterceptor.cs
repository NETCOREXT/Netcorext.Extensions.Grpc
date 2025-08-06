using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Netcorext.Contracts;
using Netcorext.Extensions.Grpc.Helpers;
using Netcorext.Extensions.Grpc.Options;

namespace Netcorext.Extensions.Grpc.Interceptors;

public class OriginHeaderPassingInterceptor : Interceptor
{
    private readonly IContextState _contextState;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly OriginHeaderPassingInterceptorOptions _options;

    public OriginHeaderPassingInterceptor(IContextState contextState, IHttpContextAccessor httpContextAccessor, OriginHeaderPassingInterceptorOptions options)
    {
        _contextState = contextState;
        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
                                                                                  ClientInterceptorContext<TRequest, TResponse> context,
                                                                                  AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var entries = _httpContextAccessor.HttpContext?.Request.Headers
                                          .Where(_options.Handler)
                                          .Select(t =>
                                                  {
                                                      if (!_options.EnableHeaderEncoding)
                                                          return new Metadata.Entry(t.Key.ToLower(), t.Value);

                                                      // HTTP2 not supporting non-ASCII characters in header names and values
                                                      var isKeyAscii = t.Key.All(c => c <= 127);

                                                      if (!isKeyAscii)
                                                          return null;

                                                      var areValuesAscii = t.Value.All(value => value?.All(c => c <= 127) ?? false);

                                                      if (areValuesAscii)
                                                          return new Metadata.Entry(t.Key.ToLower(), t.Value);

                                                      var escapedValue = Uri.EscapeDataString(t.Value);

                                                      return new Metadata.Entry(t.Key.ToLower(), escapedValue);
                                                  })
                                          .ToList() ?? new List<Metadata.Entry>();


        var authorization = _contextState.GetAuthorizationToken(_httpContextAccessor.HttpContext?.Request.Headers);
        var authorizationHeader = new KeyValuePair<string, StringValues>(HeaderNames.Authorization, authorization);

        if (_options.Handler(authorizationHeader))
            entries.Add(new Metadata.Entry(authorizationHeader.Key, authorizationHeader.Value));

        if (entries.Count == 0)
            return continuation(request, context);

        var metadata = new Metadata();

        foreach (var entry in entries)
        {
            if (entry == null)
                continue;

            metadata.Add(entry);
        }

        var options = context.Options.WithHeaders(metadata);

        var newContext = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);

        return base.AsyncUnaryCall(request, newContext, continuation);
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var entries = _httpContextAccessor.HttpContext?.Request.Headers
                                          .Where(_options.Handler)
                                          .Select(t =>
                                                  {
                                                      if (!_options.EnableHeaderEncoding)
                                                          return new Metadata.Entry(t.Key.ToLower(), t.Value);

                                                      var isKeyAscii = t.Key.All(c => c <= 127);

                                                      if (!isKeyAscii)
                                                          return null;

                                                      var areValuesAscii = t.Value.All(value => value?.All(c => c <= 127) ?? false);

                                                      if (areValuesAscii)
                                                          return new Metadata.Entry(t.Key.ToLower(), t.Value);

                                                      var escapedValue = Uri.EscapeDataString(t.Value);

                                                      return new Metadata.Entry(t.Key.ToLower(), escapedValue);
                                                  })
                                          .ToList() ?? new List<Metadata.Entry>();

        var authorization = _contextState.GetAuthorizationToken(_httpContextAccessor.HttpContext?.Request.Headers);
        var authorizationHeader = new KeyValuePair<string, StringValues>(HeaderNames.Authorization, authorization);

        if (_options.Handler(authorizationHeader))
            entries.Add(new Metadata.Entry(authorizationHeader.Key, authorizationHeader.Value));

        if (entries.Count == 0)
            return continuation(request, context);

        var metadata = new Metadata();

        foreach (var entry in entries)
        {
            if (entry == null)
                continue;

            metadata.Add(entry);
        }

        var options = context.Options.WithHeaders(metadata);

        var newContext = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);

        return base.BlockingUnaryCall(request, newContext, continuation);
    }
}
