namespace CollegeErp.Application.Common;

/// <summary>Base type for expected, user-facing failures. The API layer maps these to HTTP codes.</summary>
public abstract class AppException(string message) : Exception(message);

public sealed class NotFoundException(string message) : AppException(message)
{
    public static NotFoundException For<T>(object id) => new($"{typeof(T).Name} {id} was not found.");
}

public sealed class ConflictException(string message) : AppException(message);

public sealed class ForbiddenException(string message) : AppException(message);

public sealed class UnauthorizedException(string message = "Invalid credentials.") : AppException(message);

public sealed class InvalidInputException(IDictionary<string, string[]> errors)
    : AppException("One or more validation errors occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;

    public InvalidInputException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] }) { }
}
