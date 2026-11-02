using System.Diagnostics.CodeAnalysis;

namespace PostulaAI.Api.Shared.Errors;

/// <summary>An expected, user-facing error with a stable machine-readable code.</summary>
public sealed record AppError(string Code, int Status, string Title)
{
    public static AppError Unauthorized(string code, string title) => new(code, StatusCodes.Status401Unauthorized, title);

    public static AppError NotFound(string code, string title) => new(code, StatusCodes.Status404NotFound, title);

    public static AppError Conflict(string code, string title) => new(code, StatusCodes.Status409Conflict, title);
}

/// <summary>Outcome of a service operation: either a value or an <see cref="AppError"/>.</summary>
public sealed class Result<T>
    where T : class
{
    private Result(T? value, AppError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public AppError? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(AppError error) => new(null, error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(AppError error) => Failure(error);
}
