namespace Gravedigger2026.Core.Level
{
    public interface IStageModule
    {
        GameplayState HandledState { get; }

        void Enter(LevelStageContext context);

        void Exit(LevelStageContext context);
    }
}
