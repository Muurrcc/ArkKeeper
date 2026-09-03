namespace ArkKeeper.Networking.Rcon;

/// <summary>Packet type ids from the Source RCON protocol (used by ARK's dedicated server).</summary>
public enum RconPacketType
{
    ResponseValue = 0,

    /// <summary>Used for both SERVERDATA_EXECCOMMAND (client to server) and
    /// SERVERDATA_AUTH_RESPONSE (server to client) — the protocol reuses the same wire value.</summary>
    ExecCommandOrAuthResponse = 2,

    Auth = 3,
}
