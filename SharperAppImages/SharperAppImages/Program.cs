// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using SharperAppImages;

await using var serilogger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

using var loggerFactory = new SerilogLoggerFactory(serilogger);

var appImageChecker = new LoggingAppImageChecker(
    loggerFactory.CreateLogger<LoggingAppImageChecker>(),
    new AppImageChecker());
var isAppImage = await appImageChecker.IsAppImage(args[0]);

return isAppImage ? 0 : -1;