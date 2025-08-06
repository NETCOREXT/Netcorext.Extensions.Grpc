using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Netcorext.Extensions.Grpc.Options;

public class OriginHeaderPassingInterceptorOptions
{
    public bool EnableHeaderEncoding { get; set; }
    public Func<KeyValuePair<string, StringValues>, bool> Handler { get; set; } = entry => entry.Key == HeaderNames.Authorization;
}
