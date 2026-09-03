namespace BlackHorizon.AI
{
    /// <summary>
    /// States an enemy AI can be in. Mirrors the design doc's behaviour set.
    /// </summary>
    public enum AIState
    {
        Idle,
        Patrol,
        Investigate,
        Alert,
        Combat,
        Search,
        Retreat,
        Dead,
    }
}
