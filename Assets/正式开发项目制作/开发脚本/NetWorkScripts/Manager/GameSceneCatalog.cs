public static class GameSceneCatalog
{
    public const string Login = "LoadScene";
    public const string Register = "RegiserScene";
    public const string Lobby = "LobbyPanel";
    public const string DantiaoBattle = "dantiao_map";
    public const string PaiweiBattle = "paiwei_map";

    public static string GetBattleScene(GameModes mode)
    {
        switch (mode)
        {
            case GameModes.paiwei:
            case GameModes.paiwei_solo:
                return PaiweiBattle;
            case GameModes.dantiao:
                return DantiaoBattle;
            default:
                return mode + "_map";
        }
    }

    public static bool IsBattleScene(string sceneName)
    {
        return string.Equals(sceneName, DantiaoBattle, System.StringComparison.Ordinal)
            || string.Equals(sceneName, PaiweiBattle, System.StringComparison.Ordinal);
    }
}
