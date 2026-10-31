namespace GbaToPsp;

internal static class CommandLine
{
    public static void Build(string[] args)
    {
        if (args.Length < 1 || !args[0].Equals("--build", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Uso: --build --rom <archivo.gba> --title <tÃ­tulo> [--output <carpeta>]");

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < args.Length; i += 2)
        {
            if (!args[i].StartsWith("--") || i + 1 >= args.Length)
                throw new ArgumentException("Cada opciÃ³n requiere un valor.");
            values[args[i][2..]] = args[i + 1];
        }

        string root = FindProjectRoot();
        string Required(string key) => values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value : throw new ArgumentException($"Falta --{key}.");
        string Optional(string key, string defaultValue) => values.TryGetValue(key, out string? value) ? value : defaultValue;
        bool OptionalBool(string key) => values.TryGetValue(key, out string? value) && bool.TryParse(value, out bool result) && result;

        string output = PackageBuilder.Build(new PackageOptions(
            Required("title"),
            Required("rom"),
            Required("bios"),
            Optional("source", Path.Combine(root, "vendor", "gpsp-kai")),
            Optional("elf", Path.Combine(root, "native", "build", "gpsp_bundle.elf")),
            Optional("output", Path.Combine(root, "out")),
            values.GetValueOrDefault("icon"),
            values.GetValueOrDefault("info"),
            values.GetValueOrDefault("background"),
            values.GetValueOrDefault("video"),
            values.GetValueOrDefault("sound"),
            OptionalBool("home"),
            OptionalBool("tv")));
        Console.WriteLine(output);
    }

    private static string FindProjectRoot()
    {
        for (DirectoryInfo? directory = new(Environment.CurrentDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "README.md"))) return directory.FullName;
        return Environment.CurrentDirectory;
    }
}


