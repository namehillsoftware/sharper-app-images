using System.Text;
using PathLib;
using SharperIntegration.Extraction;

namespace SharperIntegration.Registration;

public class DesktopResourceManagement(
    IAppImageExtractionConfiguration appImageExtractionConfiguration,
    IDesktopAppLocations appLocations,
    IStartProcesses processes
) : IDesktopResourceManagement
{
    public async Task RegisterResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
        var stagedIconPaths = GetStagedIconPaths(desktopResources).ToArray();
        foreach (var (icon, newIconPath) in stagedIconPaths)
        {
            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

            newIconPath.Parent().Mkdir(makeParents: true);
            icon.FileInfo.CopyTo(newIconPath.FileInfo.FullName, true);
        }
        
        await ModifyDesktopEntry(appImage, desktopResources, cancellationToken);
        
        await TriggerDesktopUpdates(cancellationToken);
    }

    public async Task RemoveResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
        foreach (var (_, icon) in GetStagedIconPaths(desktopResources))
        {
            if (icon.Exists())
                icon.Delete();
        }

        var stagedDesktopPath = GetStagedDesktopEntryPath(appImage);
       
        if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

        var appImageFileName = stagedDesktopPath.Filename;
        
        if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();
        var mimeTypesTable = await ParseDesktopEntry(appLocations.MimeConfigPath, cancellationToken);

        foreach (var associatedMimeTypes in mimeTypesTable.Values)
        {
            foreach (var (mimeType, value) in associatedMimeTypes)
            {
                if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();
                
                var appsList = ParseMultiValue(value.SingleOrDefault());
                associatedMimeTypes[mimeType] =
                    [WriteMultiValue(appsList.Where(a => a != appImageFileName).Distinct())];
            }
        }

        if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();
        await WriteDesktopEntry(appLocations.MimeConfigPath, mimeTypesTable, cancellationToken);
        
        if (stagedDesktopPath.Exists())
            stagedDesktopPath.Delete();

        await TriggerDesktopUpdates(cancellationToken);
    }

    private IEnumerable<(IPath source, IPath target)> GetStagedIconPaths(DesktopResources desktopResources)
    {
        var stagedUsrShare = appImageExtractionConfiguration.StagingDirectory / "usr" / "share" / "icons";
        var directoryString = stagedUsrShare.DirectoryInfo.FullName;
        foreach (var icon in desktopResources.Icons)
        {
            var newIconPath = icon.FileInfo.FullName.StartsWith(directoryString)
                ? icon.RelativeTo(stagedUsrShare)
                : new CompatPath(icon.Filename);
            newIconPath = appLocations.IconDirectory / newIconPath;

            yield return (icon, newIconPath);
        }
    }

    private async Task ModifyDesktopEntry(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
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

        var newDesktopEntry = GetStagedDesktopEntryPath(appImage);

        if (entry.TryGetValue("MimeType", out var mimeTypeValue) && appLocations.MimeConfigPath.Exists())
        {
            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();
    
            var appImageFileName = newDesktopEntry.Filename;
            
            var mimeTypes = ParseMultiValue(mimeTypeValue.SingleOrDefault()).ToArray();

            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();
            if (mimeTypes.Length > 0)
            {
                var mimeTypesTable = await ParseDesktopEntry(appLocations.MimeConfigPath, cancellationToken);

                var associatedMimeTypes = mimeTypesTable["Added Associations"];
                foreach (var mimeType in mimeTypes)
                {
                    if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

                    var apps = new HashSet<string>();
                    if (associatedMimeTypes.TryGetValue(mimeType, out var currentAppsValue))
                    {
                        var appsList = ParseMultiValue(currentAppsValue.SingleOrDefault());
                        apps = [..appsList];
                    }

                    apps.Add(appImageFileName);
                    associatedMimeTypes[mimeType] = [WriteMultiValue(apps)];
                }

                if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();
                await WriteDesktopEntry(appLocations.MimeConfigPath, mimeTypesTable, cancellationToken);
            }
        }

        var actions = table.Where(kv => kv.Key.StartsWith("Desktop Action"));
        foreach (var (_, section) in actions)
        {
            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

            exec = section[entryExecKey];
            section[entryExecKey] = [appImagePath, ..exec.Skip(1)];
        }
        
        var newAction = new Dictionary<string, IEnumerable<string>>
        {
            ["Name"] = ["Remove AppImage from Desktop"],
            ["Exec"] = [Environment.CommandLine, appImagePath, "--remove"],
        };

        const string removeAppImageAction = "remove-app-image";
        table[$"Desktop Action {removeAppImageAction}"] = newAction;
        
        var declaredActions = new List<string> { removeAppImageAction };
        if (entry.TryGetValue("Actions", out var existingActions))
        {
            declaredActions.AddRange(ParseMultiValue(existingActions.SingleOrDefault()));
        }
        
        entry["Actions"] = [WriteMultiValue(declaredActions)];

        await WriteDesktopEntry(newDesktopEntry, table, cancellationToken);   
    }

    private IPath GetStagedDesktopEntryPath(AppImage appImage)
    {
        return appLocations.DesktopEntryDirectory / $"{appImage.Path.Filename}.desktop";
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

    private Task<int> TriggerDesktopUpdates(CancellationToken cancellationToken = default)
    {
        return processes.RunProcess("xdg-desktop-menu", ["forceupdate"], cancellationToken);
    }

    private static IEnumerable<string> ParseMultiValue(string? value)
    {
        if (value is null) yield break;

        var escapeNext = false;
        var token = new StringBuilder();
        foreach (var c in value)
        {
            if (escapeNext)
            {
                token.Append(c);
                escapeNext = false;
                continue;
            }
            
            switch (c)
            {
                case '\\':
                    escapeNext = true;
                    continue;
                case ';':
                    yield return token.ToString();
                    token.Clear();
                    continue;
                default:
                    token.Append(c);
                    continue;
            }
        }

        if (token.Length > 0)
            yield return token.ToString();
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

    private static string WriteMultiValue(IEnumerable<string> values) => 
        string.Join(';', values.Select(part => part.Replace(";", "\\;")));
}