using NovaLight.Console;
using NovaLight.Core;

ConsoleHandler.Init();

string modulesPath = Path.Combine(AppContext.BaseDirectory, "modules");
if (!Directory.Exists(modulesPath))
    Directory.CreateDirectory(modulesPath);

DirectoryInfo directory = new(modulesPath);
FileInfo[] files = directory.GetFiles();

Logger logger = new();
logger.OnLog += (message) => ConsoleHandler.WriteMessage(message);

AssemblyContext assemblyContext = new(logger);
foreach (FileInfo file in files)
    assemblyContext.LoadAssemblyFromFile(file);
assemblyContext.Run();