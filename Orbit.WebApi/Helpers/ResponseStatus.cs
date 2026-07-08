namespace Orbit.WebApi.Helpers;

public static class ResponseStatus
{
    public static T Ok<T>(HttpContext context, T data)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        return data;
    }

    public static T Created<T>(HttpContext context, T data)
    {
        context.Response.StatusCode = StatusCodes.Status201Created;
        return data;
    }

    public static T NoContent<T>(HttpContext context, T data)
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return data;
    }

    public static T Updated<T>(HttpContext context, T data)
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return data;
    }

    public static T BadRequest<T>(HttpContext context, T data)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return data;
    }

    public static T Unauthorized<T>(HttpContext context, T data)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return data;
    }

    public static T NotFound<T>(HttpContext context, T data)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return data;
    }

    public static T Conflict<T>(HttpContext context, T data)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        return data;
    }

    public static T InternalServerError<T>(HttpContext context, T data)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return data;
    }
}
