using PathLib;
using SharperIntegration.Registration;
using SharperIntegration.Verification;

namespace SharperIntegration;

internal class ProgramPathsLookup(ICheckAppImages appImageChecker) : IProgramPaths
{
	private readonly SemaphoreSlim _semaphore = new(1, 1);
	private IPath? _programPath;

	public Task<IPath> GetProgramPathAsync(CancellationToken cancellationToken = default) =>
		GetProgramPathAsyncSynchronized(cancellationToken);

	private async Task<IPath> GetProgramPathAsyncSynchronized(CancellationToken cancellationToken = default)
	{
		if (_programPath != null) return _programPath;

		await _semaphore.WaitAsync(cancellationToken);
		try
		{
			if (_programPath != null) return _programPath;

			_programPath = await SearchForAppImageProgramPath(cancellationToken);
			return _programPath;
		}
		finally
		{
			_semaphore.Release();
		}
	}

	private async Task<IPath> SearchForAppImageProgramPath(CancellationToken cancellationToken = default)
	{
		var processPathString = Environment.GetCommandLineArgs().FirstOrDefault();
		if (processPathString != null)
		{
			var processPath = new CompatPath(processPathString);
			if (await appImageChecker.IsAppImage(processPath, cancellationToken)) return processPath;
		}

		var processId = Environment.ProcessId;

		var commandPath = await GetCommandLine(processId, cancellationToken);
		if (commandPath != null) return commandPath;

		while (processId > 0)
		{
			if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

			await using var processFileStream =
				new FileStream($"/proc/{processId}/status", FileMode.Open, FileAccess.Read);
			using var streamReader = new StreamReader(processFileStream);

			processId = 0;

			const string parentIdSearchString = "PPid:";
			while (await streamReader.ReadLineAsync(cancellationToken) is { } line)
			{
				if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

				if (!line.StartsWith(parentIdSearchString))
				{
					continue;
				}

				var processIdString = line[parentIdSearchString.Length..].Trim();
				var parentProcessId = int.Parse(processIdString);

				if (parentProcessId == 0)
				{
					throw new InvalidOperationException("Application is not executing as an AppImage.");
				}

				commandPath = await GetCommandLine(parentProcessId, cancellationToken);
				if (commandPath != null)
				{
					return commandPath;
				}

				processId = parentProcessId;
				break;
			}
		}

		throw new InvalidOperationException("Application is not executing as an AppImage.");
	}


	private async Task<IPath?> GetCommandLine(int processId, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

		await using var commandFileStream =
			new FileStream($"/proc/{processId}/cmdline", FileMode.Open, FileAccess.Read);
		using var commandStreamReader = new StreamReader(commandFileStream);
		var command = await commandStreamReader.ReadToEndAsync(cancellationToken);
		var commands = command.Split('\0', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		command = commands.FirstOrDefault();
		if (command == null)
		{
			return null;
		}

		var processPath = new CompatPath(command);
		if (await appImageChecker.IsAppImage(processPath, cancellationToken)) return processPath;

		return null;
	}
}
