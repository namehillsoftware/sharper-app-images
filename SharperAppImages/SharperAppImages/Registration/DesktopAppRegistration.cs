using System.Text;
using PathLib;
using SharperAppImages.Extraction;

namespace SharperAppImages.Registration;

public class DesktopAppRegistration(
    IAppImageExtractionConfiguration appImageExtractionConfiguration, 
    IDesktopAppLocations appLocations
) : IDesktopAppRegistration
{
    public async Task RegisterResources(AppImage appImage, DesktopResources desktopResources)
    {
        var stagedUsrShare = appImageExtractionConfiguration.StagingDirectory / "usr" / "share" / "icons";
        var directoryString = stagedUsrShare.DirectoryInfo.FullName;
        foreach (var icon in desktopResources.Icons)
        {
            var newIconPath = icon.FileInfo.FullName.Contains(directoryString) ? icon.RelativeTo(stagedUsrShare) : new CompatPath(icon.Filename);
            newIconPath = appLocations.IconDirectory / newIconPath;
            newIconPath.Parent().Mkdir(makeParents: true);
            icon.FileInfo.CopyTo(newIconPath.FileInfo.FullName, true);
        }

        var desktopEntry = desktopResources.DesktopEntry;
        if (desktopEntry != null)
        {
            var table = await ParseDesktopEntry(desktopEntry);
            var entry = table["Desktop Entry"];
            var exec = entry["Exec"];
            var newEntry = appImage.Path.FileInfo.FullName;
            entry["Exec"] = [newEntry, ..exec.Skip(1)];

            var newDesktopEntry = appLocations.DesktopEntryDirectory / desktopEntry.Filename;
            await WriteDesktopEntry(newDesktopEntry, table);
        }
    }

    private static async Task<Dictionary<string, Dictionary<string, IEnumerable<string>>>> ParseDesktopEntry(IPath entry)
    {
        using var entryReader = entry.FileInfo.OpenText();
        var returnValue = new Dictionary<string, Dictionary<string, IEnumerable<string>>>();
        var currentSection = new Dictionary<string, IEnumerable<string>>();
        string? line;
        while ((line = await entryReader.ReadLineAsync()) != null)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith('#')) continue;
            
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
            foreach (var c in value) {
                if (c == '\"') {
                    if (inBetweenQuotes)
                        FinishSection();

                    inBetweenQuotes = !inBetweenQuotes;
                    continue;
                }

                if (inBetweenQuotes) {
                    if (escapeNext) {
                        section.Append(c);
                        escapeNext = false;
                        continue;
                    }

                    if (c == '\\') {
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
        Dictionary<string, Dictionary<string, IEnumerable<string>>> contents)
    {
        await using var entryWriter = entry.FileInfo.CreateText();

        foreach (var (sectionHeader, sectionContents) in contents)
        {
            await entryWriter.WriteLineAsync($"[{sectionHeader}]");

            foreach (var (key, value) in sectionContents)
            {
                await entryWriter.WriteLineAsync($"{key}={string.Join(" ", value)}");
            }
        }
    }
}