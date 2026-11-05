using System.Text;
using Atlassian.Mcp.Core.Errors;

namespace Atlassian.Mcp.Infrastructure.Http;

public class ErrorHandler : DelegatingHandler
{
    private const int MaxErrorBodyBytes = 2048;

    private readonly AtlassianSystem _system;
    private readonly Func<string?, string?> _errorMessageParser;

    public ErrorHandler(AtlassianSystem system, Func<string?, string?> errorMessageParser)
    {
        _system = system;
        _errorMessageParser = errorMessageParser;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await CreateErrorAsync(response, cancellationToken);
                response.Dispose();

                throw error;
            }

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            throw new AtlassianApiException(
                _system,
                null,
                $"{_system} request failed: network error.",
                null,
                exception);
        }
        catch (TaskCanceledException exception)
        {
            throw new AtlassianApiException(
                _system,
                null,
                $"{_system} request failed: timeout exceeded.",
                null,
                exception);
        }
    }

    private async Task<AtlassianApiException> CreateErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await ReadBodyAsync(response, cancellationToken);

        return new AtlassianApiException(
            _system,
            response.StatusCode,
            $"{_system} request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.",
            _errorMessageParser(body));
    }

    private static async Task<string?> ReadBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[MaxErrorBodyBytes];
        var read = await stream.ReadAsync(buffer, cancellationToken);

        return read == 0 ? null : Encoding.UTF8.GetString(buffer, 0, read);
    }
}