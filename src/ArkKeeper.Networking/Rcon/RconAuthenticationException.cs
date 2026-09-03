namespace ArkKeeper.Networking.Rcon;

public sealed class RconAuthenticationException : Exception
{
    public RconAuthenticationException()
        : base("RCON authentication failed — check the server's RCON password.")
    {
    }
}
