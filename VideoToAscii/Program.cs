using System.CommandLine;

using VideoToAscii.Utils;

namespace VideoToAscii;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
            args = ["-h"];

        var fileOption = new Option<FileInfo>(
             aliases: ["-f", "--file"],
             description: "Input video file")
        {
            IsRequired = true
        }.LegalFilePathsOnly();

        var strategyOption = new Option<string>(
            aliases: ["-s", "--strategy", "--strat"],
            getDefaultValue: () => "filled-ascii",
            description: "Choose a strategy to render the output");

        strategyOption.AddValidator(result =>
        {
            var value = result.GetValueOrDefault<string>()?.ToLower();
            if (value is not "ascii-color" and not "just-ascii" and not "filled-ascii")
                result.ErrorMessage = "Strategy must be one of: ascii-color, just-ascii, filled-ascii";

            if (value is "filled-ascii" or "ascii-color")
                AnsiiUtils.EnableAnsiSupport();
        });

        var outputOption = new Option<FileInfo>(
            aliases: ["-o", "--output", "--out"],
            description: "Output file to export");

        var withAudioOption = new Option<bool>(
            aliases: ["-a", "--with-audio", "--audio"],
            description: "Play audio track");

        var rootCommand = new RootCommand
        {
            fileOption,
            strategyOption,
            outputOption,
            withAudioOption
        };

        rootCommand.Description = "A simple C# application to play videos in the terminal using colored characters as pixels";

        rootCommand.SetHandler(Play, fileOption, strategyOption, outputOption, withAudioOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static void Play(FileInfo file, string strategy, FileInfo output, bool withAudio)
    {
        try
        {
            var player = new Player(file.FullName, strategy)
            {
                OutputFilePath = output?.FullName,
                PlayAudio = withAudio
            };
            player.Play().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
