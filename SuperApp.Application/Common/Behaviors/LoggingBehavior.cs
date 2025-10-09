using MediatR;
using Microsoft.Extensions.Logging;

namespace SuperApp.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class, IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request, 
            RequestHandlerDelegate<TResponse> next, 
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            
            _logger.LogInformation("Executing request: {RequestName}", requestName);
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                var response = await next();
                
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("Request {RequestName} completed successfully in {Duration}ms", 
                    requestName, duration.TotalMilliseconds);
                
                return response;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Request {RequestName} failed after {Duration}ms", 
                    requestName, duration.TotalMilliseconds);
                throw;
            }
        }
    }
}