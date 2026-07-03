namespace Orbit.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string[]? Errors { get; private set; }

    public static Result<T> Success(T data, string message = "Success")
        => new() { IsSuccess = true, Data = data, Message = message };

    public static Result<T> Failure(string message, params string[]? errors)
        => new() { IsSuccess = false, Message = message, Errors = errors };
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string[]? Errors { get; private set; }

    public static Result Success(string message = "Success")
        => new() { IsSuccess = true, Message = message };

    public static Result Failure(string message, params string[]? errors)
        => new() { IsSuccess = false, Message = message, Errors = errors };
}
