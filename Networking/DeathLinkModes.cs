namespace Celeste.Mod.CelesteArchipelago
{
    public enum DeathLinkMode
    {
        // Normal Death
        Room,
        // Restarts sub chapter
        SubChapter,
        // Restarts chapter
        Chapter,
        // Restarts chapter however if on the goal level it will commence a room death
        ChapterNoGoal
    }
}