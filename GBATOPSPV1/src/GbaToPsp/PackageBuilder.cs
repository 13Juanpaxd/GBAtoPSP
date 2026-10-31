using System.Buffers.Binary;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;

namespace GbaToPsp;

internal sealed record PackageOptions(
    string Title,
    string RomPath,
    string BiosPath,
    string EmulatorSourcePath,
    string PatchedElfPath,
    string OutputRoot,
    string? IconPath,
    string? InformationPath,
    string? BackgroundPath,
    string? VideoPath,
    string? SoundPath,
    bool EnableHomeHook,
    bool EnableTvOut);

internal static class PackageBuilder
{
    private const string BootstrapMarker = "G2PS_BOOTSTRAP_V1";
    private sealed record BundleFile(string Name, string Source);

    public static string Build(PackageOptions options)
    {
        Validate(options);
        string folderName = MakeFolderName(options.Title);
        string destination = Path.Combine(Path.GetFullPath(options.OutputRoot), "PSP", "GAME", folderName);
        if (Directory.Exists(destination))
            throw new IOException($"Ya existe la carpeta de salida: {destination}");

        var files = GetBundleFiles(options);
        byte[] sfo = BuildSfo(options.Title);
        byte[] icon = ReadPng(options.IconPath, 144, 80);
        byte[] info = ReadPng(options.InformationPath, 480, 272);
        byte[] background = ReadPng(options.BackgroundPath, 480, 272);
        byte[] video = ReadOptional(options.VideoPath);
        byte[] sound = ReadOptional(options.SoundPath);
        long archiveSize = 24;
        foreach (BundleFile file in files)
            archiveSize += 2 + 4 + Encoding.UTF8.GetByteCount(file.Name) + new FileInfo(file.Source).Length;

        long elfSize = new FileInfo(options.PatchedElfPath).Length;
        // DATA.PSAR makes PPSSPP treat the PBP as a PS1 package. Keep the private
        // archive after the ELF, inside DATA.PSP, and leave DATA.PSAR empty.
        long[] lengths = [sfo.Length, icon.Length, video.Length, info.Length,
            background.Length, sound.Length, elfSize + archiveSize + 8, 0];
        var offsets = new uint[8];
        long offset = 40;
        for (int i = 0; i < offsets.Length; i++)
        {
            if (offset > uint.MaxValue) throw new InvalidOperationException("El PBP supera 4 GiB.");
            offsets[i] = (uint)offset;
            offset += lengths[i];
        }
        if (offset > uint.MaxValue) throw new InvalidOperationException("El PBP supera 4 GiB.");

        Directory.CreateDirectory(destination);
        string output = Path.Combine(destination, "EBOOT.PBP");
        try
        {
            using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(new byte[] { 0, (byte)'P', (byte)'B', (byte)'P' });
            writer.Write(0x00010000u);
            foreach (uint item in offsets) writer.Write(item);
            writer.Write(sfo);
            writer.Write(icon);
            writer.Write(video);
            writer.Write(info);
            writer.Write(background);
            writer.Write(sound);
            CopyFile(options.PatchedElfPath, stream);
            uint archiveOffset = checked(offsets[6] + (uint)elfSize);
            writer.Write(new byte[] { (byte)'G', (byte)'2', (byte)'P', (byte)'S' });
            writer.Write((uint)files.Count);
            writer.Write(Guid.NewGuid().ToByteArray());
            foreach (BundleFile file in files)
            {
                byte[] name = Encoding.UTF8.GetBytes(file.Name);
                writer.Write((ushort)name.Length);
                writer.Write((uint)new FileInfo(file.Source).Length);
                writer.Write(name);
                CopyFile(file.Source, stream);
            }
            writer.Write(new byte[] { (byte)'G', (byte)'2', (byte)'O', (byte)'F' });
            writer.Write(archiveOffset);
            writer.Flush();
            if (stream.Length != offset) throw new InvalidDataException("El tamaÃ±o final del PBP no coincide.");
            return output;
        }
        catch
        {
            if (File.Exists(output)) File.Delete(output);
            if (Directory.Exists(destination))
            {
                try { Directory.Delete(destination); }
                catch (IOException) { /* No borrar archivos ajenos si la carpeta cambiÃ³. */ }
            }
            throw;
        }
    }

    private static void Validate(PackageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Title)) throw new ArgumentException("Escribe un tÃ­tulo.");
        if (!File.Exists(options.RomPath) || !options.RomPath.EndsWith(".gba", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Selecciona una ROM .gba vÃ¡lida.");
        long romSize = new FileInfo(options.RomPath).Length;
        if (romSize == 0 || romSize > 32 * 1024 * 1024)
            throw new ArgumentException("La ROM debe medir entre 1 byte y 32 MiB.");
        if (!File.Exists(options.BiosPath) || new FileInfo(options.BiosPath).Length != 16 * 1024)
            throw new ArgumentException("Selecciona un BIOS GBA de 16 KiB.");
        if (!File.Exists(options.PatchedElfPath) || !HasBootstrapMarker(options.PatchedElfPath))
            throw new ArgumentException("Falta un gpSP compilado con G2PS_BOOTSTRAP_V1. La versiÃ³n original no abre el paquete automÃ¡ticamente.");
        if (!Directory.Exists(options.EmulatorSourcePath))
            throw new ArgumentException("Selecciona la carpeta del cÃ³digo de gpSP Kai.");
        if (string.IsNullOrWhiteSpace(options.OutputRoot))
            throw new ArgumentException("Selecciona una carpeta de salida.");
        ValidateOptional(options.VideoPath, ".pmf", "video");
        ValidateOptional(options.SoundPath, ".at3", "sonido");
        foreach (string? image in new[] { options.IconPath, options.InformationPath, options.BackgroundPath })
            ValidateOptional(image, ".png", "imagen");
    }

    private static bool HasBootstrapMarker(string path)
    {
        byte[] marker = Encoding.ASCII.GetBytes(BootstrapMarker);
        using var stream = File.OpenRead(path);
        if (stream.Length < 20) return false;
        Span<byte> header = stackalloc byte[20];
        stream.ReadExactly(header);
        if (header[0] != 0x7F || header[1] != 'E' || header[2] != 'L' || header[3] != 'F' ||
            BinaryPrimitives.ReadUInt16LittleEndian(header[18..20]) != 8) return false;
        stream.Position = 0;
        int matched = 0;
        int value;
        while ((value = stream.ReadByte()) >= 0)
        {
            matched = value == marker[matched] ? matched + 1 : value == marker[0] ? 1 : 0;
            if (matched == marker.Length) return true;
        }
        return false;
    }

    private static void ValidateOptional(string? path, string extension, string label)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!File.Exists(path) || !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Selecciona un archivo {extension} vÃ¡lido para {label}.");
    }

    private static string MakeFolderName(string title)
    {
        string result = new(title.Normalize(NormalizationForm.FormD)
            .Where(ch => ch <= 127 && char.IsLetterOrDigit(ch)).Take(24).ToArray());
        return result.Length == 0 ? "GBA_GAME" : result.ToUpperInvariant();
    }

    private static List<BundleFile> GetBundleFiles(PackageOptions options)
    {
        string root = options.EmulatorSourcePath;
        var files = new List<BundleFile>
        {
            new("gba_bios.bin", options.BiosPath),
            new("GBA/ROM/game.gba", options.RomPath),
            new("game_config.txt", Path.Combine(root, "game_config.txt")),
            new("cfg/dir.cfg", Path.Combine(root, "cfg", "dir.cfg")),
            new("cfg/english.fnt", Path.Combine(root, "cfg", "english.fnt")),
            new("cfg/english.msg", Path.Combine(root, "cfg", "english.msg")),
            new("font/knj10.fbm", Path.Combine(root, "font", "knj10.fbm")),
            new("font/5x10rk.fbm", Path.Combine(root, "font", "5x10rk.fbm"))
        };
        if (options.EnableHomeHook)
            files.Add(new BundleFile("homehook.prx", Path.Combine(root, "obj", "homehook.prx")));
        if (options.EnableTvOut)
        {
            files.Add(new BundleFile("dvemgr.prx", Path.Combine(root, "obj", "dvemgr.prx")));
            files.Add(new BundleFile("cfg/video.cfg", Path.Combine(root, "cfg", "video.cfg")));
        }
        foreach (BundleFile file in files)
        {
            if (!File.Exists(file.Source)) throw new FileNotFoundException($"Falta un recurso de gpSP: {file.Source}");
            if (Encoding.UTF8.GetByteCount(file.Name) > ushort.MaxValue)
                throw new InvalidDataException("Nombre demasiado largo en el paquete.");
            if (new FileInfo(file.Source).Length > uint.MaxValue)
                throw new InvalidDataException("Archivo demasiado grande en el paquete.");
        }
        return files;
    }

    private static byte[] BuildSfo(string title)
    {
        byte[] titleValue = Encoding.UTF8.GetBytes(title + "\0");
        var entries = new (string Key, ushort Format, byte[] Value, uint Capacity)[]
        {
            ("MEMSIZE", 0x0404, BitConverter.GetBytes(0), 4),
            ("BOOTABLE", 0x0404, BitConverter.GetBytes(1), 4),
            ("CATEGORY", 0x0204, Encoding.UTF8.GetBytes("MG\0"), 4),
            ("DISC_ID", 0x0204, Encoding.UTF8.GetBytes("UCJS10041\0"), 12),
            ("DISC_VERSION", 0x0204, Encoding.UTF8.GetBytes("1.00\0"), 8),
            ("PARENTAL_LEVEL", 0x0404, BitConverter.GetBytes(1), 4),
            ("PSP_SYSTEM_VER", 0x0204, Encoding.UTF8.GetBytes("1.00\0"), 8),
            ("REGION", 0x0404, BitConverter.GetBytes(32768), 4),
            ("TITLE", 0x0204, titleValue, (uint)titleValue.Length)
        };
        if (titleValue.Length > 256) throw new ArgumentException("El tÃ­tulo es demasiado largo.");
        using var keys = new MemoryStream();
        using var values = new MemoryStream();
        var keyOffsets = new List<ushort>();
        var dataOffsets = new List<uint>();
        foreach (var entry in entries)
        {
            keyOffsets.Add((ushort)keys.Position);
            byte[] key = Encoding.ASCII.GetBytes(entry.Key + "\0");
            keys.Write(key);
            dataOffsets.Add((uint)values.Position);
            values.Write(entry.Value);
            values.Write(new byte[entry.Capacity - entry.Value.Length]);
        }
        uint keyStart = (uint)(20 + entries.Length * 16);
        uint dataStart = (keyStart + (uint)keys.Length + 3) & ~3u;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(0x46535000u);
        writer.Write(0x00000101u);
        writer.Write(keyStart);
        writer.Write(dataStart);
        writer.Write((uint)entries.Length);
        for (int i = 0; i < entries.Length; i++)
        {
            writer.Write(keyOffsets[i]);
            writer.Write(entries[i].Format);
            writer.Write((uint)entries[i].Value.Length);
            writer.Write(entries[i].Capacity);
            writer.Write(dataOffsets[i]);
        }
        writer.Write(keys.ToArray());
        while (stream.Position < dataStart) writer.Write((byte)0);
        writer.Write(values.ToArray());
        return stream.ToArray();
    }

    private static byte[] ReadPng(string? path, int width, int height)
    {
        if (string.IsNullOrWhiteSpace(path)) return [];
        try
        {
            using Image source = Image.FromFile(path);
            using var output = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(output))
            {
                graphics.Clear(Color.Black);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                double scale = Math.Min((double)width / source.Width, (double)height / source.Height);
                int w = (int)Math.Round(source.Width * scale);
                int h = (int)Math.Round(source.Height * scale);
                graphics.DrawImage(source, (width - w) / 2, (height - h) / 2, w, h);
            }
            using var buffer = new MemoryStream();
            output.Save(buffer, ImageFormat.Png);
            return buffer.ToArray();
        }
        catch (OutOfMemoryException exception)
        {
            throw new ArgumentException($"La imagen no es un PNG compatible: {path}. " +
                "Renombrar un archivo WebP como .png no lo convierte; exportalo como PNG real.", exception);
        }
    }

    private static byte[] ReadOptional(string? path) => string.IsNullOrWhiteSpace(path) ? [] : File.ReadAllBytes(path);

    private static void CopyFile(string path, Stream destination)
    {
        using var source = File.OpenRead(path);
        source.CopyTo(destination);
    }
}


