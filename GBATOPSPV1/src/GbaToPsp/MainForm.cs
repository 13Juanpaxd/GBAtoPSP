namespace GbaToPsp;

internal sealed class MainForm : Form
{
    private readonly TextBox title = new();
    private readonly TextBox rom = new();
    private readonly TextBox bios = new();
    private readonly TextBox icon = new();
    private readonly TextBox information = new();
    private readonly TextBox background = new();
    private readonly TextBox video = new();
    private readonly TextBox sound = new();
    private readonly TextBox output = new();
    private readonly CheckBox homeHook = new();
    private readonly CheckBox tvOut = new();
    private readonly Label status = new();
    private readonly Button build = new();

    public MainForm()
    {
        Text = "GBA a PSP";
        Width = 820;
        Height = 630;
        MinimumSize = new Size(680, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(16), AutoScroll = true };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
        Controls.Add(table);

        AddRow(table, 0, "Titulo", title);
        AddRow(table, 1, "Juego .gba", rom, "ROM GBA|*.gba");
        AddRow(table, 2, "BIOS GBA (16 KiB)", bios, "BIOS GBA|*.bin");
        AddRow(table, 3, "Icono (opcional)", icon, "Imagen PNG|*.png");
        AddRow(table, 4, "Informacion (opcional)", information, "Imagen PNG|*.png");
        AddRow(table, 5, "Fondo (opcional)", background, "Imagen PNG|*.png");
        AddRow(table, 6, "Video (opcional)", video, "Video PMF|*.pmf");
        AddRow(table, 7, "Sonido (opcional)", sound, "Audio AT3|*.at3");
        AddRow(table, 8, "Carpeta de salida", output, directory: true);

        var options = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        homeHook.Text = "Activar boton HOME";
        tvOut.Text = "Salida TV (PSP-2000)";
        options.Controls.Add(homeHook);
        options.Controls.Add(tvOut);
        table.Controls.Add(options, 0, 9);
        table.SetColumnSpan(options, 3);

        var note = new Label
        {
            Text = "Selecciona una copia local del juego y un BIOS propio fuera del proyecto. " +
                   "El EBOOT incluye el emulador y abre el juego directamente. Las imagenes son opcionales.",
            AutoSize = true, MaximumSize = new Size(760, 0), ForeColor = Color.DarkSlateGray,
            Padding = new Padding(0, 12, 0, 12)
        };
        table.Controls.Add(note, 0, 10);
        table.SetColumnSpan(note, 3);

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        build.Text = "Crear EBOOT.PBP";
        build.AutoSize = true;
        build.Click += BuildClicked;
        status.AutoSize = true;
        status.Padding = new Padding(12, 6, 0, 0);
        bottom.Controls.Add(build);
        bottom.Controls.Add(status);
        table.Controls.Add(bottom, 0, 11);
        table.SetColumnSpan(bottom, 3);

        output.Text = Path.Combine(FindProjectRoot(), "out");
        rom.TextChanged += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(title.Text) && File.Exists(rom.Text))
                title.Text = Path.GetFileNameWithoutExtension(rom.Text);
        };
    }

    private static string FindProjectRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md"))) return directory.FullName;
            directory = directory.Parent;
        }
        return Environment.CurrentDirectory;
    }

    private static void AddRow(TableLayoutPanel table, int index, string label, TextBox box,
        string? filter = null, bool directory = false)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        var caption = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left };
        box.Dock = DockStyle.Fill;
        box.Margin = new Padding(0, 4, 8, 4);
        var browse = new Button { Text = "Buscar...", Dock = DockStyle.Fill };
        browse.Click += (_, _) =>
        {
            if (directory)
            {
                using var dialog = new FolderBrowserDialog();
                if (Directory.Exists(box.Text)) dialog.InitialDirectory = box.Text;
                if (dialog.ShowDialog() == DialogResult.OK) box.Text = dialog.SelectedPath;
            }
            else
            {
                using var dialog = new OpenFileDialog { Filter = filter ?? "Todos los archivos|*.*" };
                if (File.Exists(box.Text)) dialog.FileName = box.Text;
                if (dialog.ShowDialog() == DialogResult.OK) box.Text = dialog.FileName;
            }
        };
        table.Controls.Add(caption, 0, index);
        table.Controls.Add(box, 1, index);
        table.Controls.Add(browse, 2, index);
    }

    private async void BuildClicked(object? sender, EventArgs args)
    {
        string root = FindProjectRoot();
        var options = new PackageOptions(title.Text, rom.Text, bios.Text,
            Path.Combine(root, "vendor", "gpsp-kai"),
            Path.Combine(root, "native", "build", "gpsp_bundle.elf"), output.Text,
            icon.Text, information.Text, background.Text, video.Text, sound.Text,
            homeHook.Checked, tvOut.Checked);
        build.Enabled = false;
        status.Text = "Creando paquete...";
        try
        {
            string result = await Task.Run(() => PackageBuilder.Build(options));
            status.Text = "Paquete creado.";
            MessageBox.Show(this, $"EBOOT creado en:\n{result}", "GBA a PSP", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            status.Text = "No se creo el paquete.";
            MessageBox.Show(this, exception.Message, "GBA a PSP", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { build.Enabled = true; }
    }
}


