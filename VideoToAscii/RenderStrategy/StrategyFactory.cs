namespace VideoToAscii.RenderStrategy;

public static class StrategyFactory
{
    private static readonly Dictionary<string, IRenderStrategy> Strategies = new Dictionary<string, IRenderStrategy>
    {
        { "default", new AsciiColorFilledStrategy() },
        { "ascii-color", new AsciiColorStrategy() },
        { "just-ascii", new AsciiBWStrategy() },
        { "filled-ascii", new AsciiColorFilledStrategy() }
    };

    public static IRenderStrategy GetStrategy(string strategyName)
    {
        if (Strategies.TryGetValue(strategyName, out var strategy))
            return strategy;

        throw new ArgumentException($"Unknown strategy: {strategyName}", nameof(strategyName));
    }
}
