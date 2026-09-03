namespace BlackHorizon.Missions
{
    /// <summary>
    /// What a mission objective asks the player to accomplish.
    /// </summary>
    public enum ObjectiveType
    {
        ReachLocation,
        EliminateEnemies,
        Interact,
        SecureArea,
        Investigate,
        Collect,
        ReachExtraction,
    }

    /// <summary>
    /// Current state of a single mission objective in the flow.
    /// </summary>
    public enum ObjectiveState
    {
        Inactive,
        Active,
        Completed,
        Failed,
    }
}
