using Orbit.Application.Helpers;
using Orbit.Application.Models.Responses;
using Orbit.Domain.Exceptions;
using Orbit.Shared.Constants;

namespace Orbit.WebApi.Middlewares
{
    public class ErrorHandlerMiddleware(ILogger<ErrorHandlerMiddleware> logger) : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);


            }
            catch (NotFoundException exception)
            {
                /*var response = ResponseHelper.Create(exception.Message);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                */
                await context.Response.WriteAsJsonAsync(ManageException(context, exception, StatusCodes.Status404NotFound));
            }
            catch (BadRequestException exception)
            {
                await context.Response.WriteAsJsonAsync(ManageException(context, exception, StatusCodes.Status400BadRequest));
            }
            catch (UnauthorizedException exception)
            {
                await context.Response.WriteAsJsonAsync(ManageException(context, exception, StatusCodes.Status401Unauthorized));
            }
            catch (Exception exception)
            {
                //var response = ResponseHelper.Create(exception.Message);

                //context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                var traceId = Guid.NewGuid();
                var message = ResponseConstants.ErrorUnexpected(traceId.ToString());

                logger.LogCritical("Se generó una excepcion no controlada: con el traceId: {traceId}. Excepción: {exception}", traceId, exception);
                await context.Response.WriteAsJsonAsync(ManageException(context, exception, StatusCodes.Status500InternalServerError, message));
            }
        }

        public GenericResponse<string> ManageException(HttpContext context, Exception exception, int statusCode, string? message = null)
        {
            var response = ResponseHelper.Create(
                data: message ?? exception.Message,
                message: message ?? exception.Message,
                errors: [message ?? exception.Message]
                );

            context.Response.StatusCode = statusCode;
            return response;
        }
    }
}
