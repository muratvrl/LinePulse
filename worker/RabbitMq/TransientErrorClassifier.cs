using System.Net;

namespace LinePulse.RabbitMq;

public static class TransientErrorClassifier
{
    public static bool IsTransient(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        HttpRequestException { StatusCode: null } => true,
        HttpRequestException { StatusCode: HttpStatusCode.InternalServerError } => true,
        HttpRequestException { StatusCode: HttpStatusCode.BadGateway } => true,
        HttpRequestException { StatusCode: HttpStatusCode.ServiceUnavailable } => true,
        HttpRequestException { StatusCode: HttpStatusCode.GatewayTimeout } => true,
        _ => false
    };
}
