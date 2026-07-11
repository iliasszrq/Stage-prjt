using AuthServer.Core.Entities;
namespace AuthServer.Core.RefreshRotation;

public class RotationResult
{
    public bool Succeeded {get; private init; }
    public RotationFailure? Failure {get; private init; }
    public RefreshToken? NewToken {get; private init; }
    public static RotationResult Success(RefreshToken newToken) => new() { Succeeded = true, NewToken = newToken};
    public static RotationResult Fail(RotationFailure reason) => new(){Succeeded = false, Failure = reason};
}
public enum RotationFailure
{
    NotFound,
    Expired,
    Revoked,
    ReuseDetected
}