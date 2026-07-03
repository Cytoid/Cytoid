/// <summary>
/// User-selectable audio backend. Stored in LocalPlayerSettings and applied at AudioManager.Initialize().
/// </summary>
public enum AudioServerType
{
    Unity = 0,
    Exceed7 = 1,
    Bass = 2
}