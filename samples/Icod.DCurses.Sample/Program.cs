using System.Reflection;

Assembly assembly = Assembly.Load("Icod.DCurses");
AssemblyName name = assembly.GetName();

Console.WriteLine($"{name.Name} {name.Version}");
Console.WriteLine("T01 repository scaffold is active. Interactive curses APIs begin in later 0.1.0 tranches.");

return (name.Name == "Icod.DCurses") ? 0 : 1;
