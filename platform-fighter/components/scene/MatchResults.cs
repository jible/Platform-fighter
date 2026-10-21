// What happened in a finished match, for the results screen. Built once by the PlayManager when the match ends
public class MatchResults
{
    // Indexed by player number, like PlayerManager.AllPlayers. Null for empty slots
    public readonly PlayerMatchResult[] Players = new PlayerMatchResult[PlayerManager.MaxPlayerCount];
}

public class PlayerMatchResult
{
    // 1 for the winner
    public int Placement;
    public CharacterId Character;
}
