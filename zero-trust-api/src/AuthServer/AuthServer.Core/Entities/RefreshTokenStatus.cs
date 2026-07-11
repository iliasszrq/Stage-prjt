namespace AuthServer.Core.Entities;

public enum RefreshTokenStatus
{
    Active = 0,
    Retired = 1,
    Revoked = 2
}