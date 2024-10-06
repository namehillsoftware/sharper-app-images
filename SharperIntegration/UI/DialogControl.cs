using System.Diagnostics;

namespace SharperIntegration.UI;

public class DialogControl : IDialogControl
{
    public async Task<string[]> GetYesNoDialogCommand(string title, string question, CancellationToken cancellationToken = default)
    {
        return await GetAvailableDialogType(cancellationToken) switch
        {
            DialogType.Zenity =>
            [
                "zenity",
                "--question",
                $"--title={title}",
                $"--text={question}",
            ],
            DialogType.KDialog =>
            [
                "kdialog",
                $"--title={title}",
                $"--yesno={question}",
            ],
            _ => [],
        };
    }

    private static async Task<DialogType?> GetAvailableDialogType(CancellationToken cancellationToken = default)
    {
        if (await CheckIfProgramExists("zenity", cancellationToken)) return DialogType.Zenity;
        if (await CheckIfProgramExists("kdialog", cancellationToken)) return DialogType.KDialog;
        return null;
    }

    private static async Task<bool> CheckIfProgramExists(string programName, CancellationToken cancellationToken = default)
    {   
        var whichProcess = Process.Start(new ProcessStartInfo("which", programName)
        {
            RedirectStandardOutput = true,
        });
        
        if (whichProcess is null) return false;
        
        await whichProcess.WaitForExitAsync(cancellationToken);
        
        var whichProcessOutput = await whichProcess.StandardOutput.ReadToEndAsync(cancellationToken);
        return whichProcess.ExitCode == 0 && !string.IsNullOrWhiteSpace(whichProcessOutput);
    }
}