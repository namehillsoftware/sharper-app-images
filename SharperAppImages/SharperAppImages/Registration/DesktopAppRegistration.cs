using System.Text;
using PathLib;
using SharperAppImages.Extraction;

namespace SharperAppImages.Registration;

public class DesktopAppRegistration(
    IAppImageExtractionConfiguration appImageExtractionConfiguration,
    IDesktopAppLocations appLocations
) : IDesktopAppRegistration
{
    public async Task RegisterResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
        var stagedUsrShare = appImageExtractionConfiguration.StagingDirectory / "usr" / "share" / "icons";
        var directoryString = stagedUsrShare.DirectoryInfo.FullName;
        foreach (var icon in desktopResources.Icons)
        {
            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

            var newIconPath = icon.FileInfo.FullName.StartsWith(directoryString)
                ? icon.RelativeTo(stagedUsrShare)
                : new CompatPath(icon.Filename);
            newIconPath = appLocations.IconDirectory / newIconPath;
            newIconPath.Parent().Mkdir(makeParents: true);
            icon.FileInfo.CopyTo(newIconPath.FileInfo.FullName, true);
        }

        var desktopEntry = desktopResources.DesktopEntry;
        if (desktopEntry == null) return;
        if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

        var table = await ParseDesktopEntry(desktopEntry, cancellationToken);
        var entry = table["Desktop Entry"];

        const string entryExecKey = "Exec";
        var exec = entry[entryExecKey];
        var appImagePath = appImage.Path.FileInfo.FullName;
        entry[entryExecKey] = [appImagePath, ..exec.Skip(1)];
        entry["TryExec"] = [appImagePath];

        var actions = table.Where(kv => kv.Key.StartsWith("Desktop Action"));
        foreach (var (_, section) in actions)
        {
            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

            exec = section[entryExecKey];
            section[entryExecKey] = [appImagePath, ..exec.Skip(1)];
        }

        var newDesktopEntry = appLocations.DesktopEntryDirectory /
                              $"{appImage.Path.BasenameWithoutExtensions}.desktop";
        await WriteDesktopEntry(newDesktopEntry, table, cancellationToken);
    }

    private static async Task<Dictionary<string, Dictionary<string, IEnumerable<string>>>>
        ParseDesktopEntry(IPath entry, CancellationToken cancellationToken = default)
    {
        using var entryReader = entry.FileInfo.OpenText();
        var returnValue = new Dictionary<string, Dictionary<string, IEnumerable<string>>>();
        var currentSection = new Dictionary<string, IEnumerable<string>>();
        string? line;
        while ((line = await entryReader.ReadLineAsync(cancellationToken)) != null)
        {
            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();
            
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#')) continue;

            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                currentSection = new Dictionary<string, IEnumerable<string>>();
                returnValue[trimmedLine.Trim('[', ']')] = currentSection;
                continue;
            }

            var (key, sections) = ParseKeyValue();
            currentSection[key] = sections;
        }

        return returnValue;

        (string, IEnumerable<string>) ParseKeyValue()
        {
            var inBetweenQuotes = false;
            var escapeNext = false;
            var keyValue = line.Split("=", 2);
            var key = keyValue[0];
            var value = keyValue[1];
            var section = new StringBuilder();
            var sections = new List<string>();
            foreach (var c in value)
            {
                if (c == '"')
                {
                    if (inBetweenQuotes)
                        FinishSection();

                    inBetweenQuotes = !inBetweenQuotes;
                    continue;
                }

                if (inBetweenQuotes)
                {
                    if (escapeNext)
                    {
                        section.Append(c);
                        escapeNext = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escapeNext = true;
                        continue;
                    }

                    section.Append(c);
                }
                else
                {
                    if (c == ' ')
                        FinishSection();
                    else
                        section.Append(c);
                }
            }

            if (section.Length > 0)
                FinishSection();

            return (key, sections);

            void FinishSection()
            {
                sections.Add(section.ToString());
                section.Clear();
            }
        }
    }

    private static async Task WriteDesktopEntry(
        IPath entry,
        Dictionary<string, Dictionary<string, IEnumerable<string>>> contents,
        CancellationToken cancellationToken = default)
    {
        await using var entryWriter = entry.FileInfo.CreateText();

        foreach (var (sectionHeader, sectionContents) in contents)
        {
            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

            await entryWriter.WriteLineAsync($"[{sectionHeader}]");

            foreach (var (key, value) in sectionContents)
            {
                if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

                await entryWriter.WriteLineAsync($"{key}={string.Join(" ", value)}");
            }

            await entryWriter.WriteLineAsync();
        }
    }
}