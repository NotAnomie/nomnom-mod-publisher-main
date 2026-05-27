using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnomieNomnomPublisher.Models;

namespace AnomieNomnomPublisher;

public sealed class AnomiePublisherForm : Form
{
    private readonly string settingsPath;
    private AppSettings settings;
    private string accessToken = "";
    private string generatedManifest = "";
    private string lastPullRequestUrl = "";
    private CancellationTokenSource? loginCts;

    private AnomieTextBox clientIdBox = null!;
    private AnomieTextBox accountBox = null!;
    private AnomieTextBox releaseRepoBox = null!;
    private AnomieTextBox upstreamOwnerBox = null!;
    private AnomieTextBox upstreamRepoBox = null!;
    private AnomieTextBox upstreamBranchBox = null!;
    private AnomieTextBox manifestFolderBox = null!;
    private AnomieTextBox userCodeBox = null!;
    private AnomieTextBox verificationUrlBox = null!;
    private CheckBox createReleaseRepoBox = null!;
    private CheckBox privateReleaseRepoBox = null!;
    private CheckBox createPrBox = null!;
    private CheckBox autoUpdateBox = null!;
    private CheckBox includeBepInExLayoutBox = null!;

    private AnomieTextBox dllPathBox = null!;
    private AnomieTextBox extraFolderBox = null!;
    private AnomieTextBox outputZipBox = null!;
    private AnomieTextBox fileNameBox = null!;
    private AnomieTextBox releaseTagBox = null!;

    private AnomieTextBox modIdBox = null!;
    private AnomieTextBox displayNameBox = null!;
    private AnomieTextBox descriptionBox = null!;
    private AnomieTextBox tagsBox = null!;
    private AnomieTextBox authorsBox = null!;
    private AnomieTextBox infoUrlBox = null!;
    private AnomieTextBox gameVersionBox = null!;
    private AnomieTextBox versionBox = null!;
    private ComboBox artifactTypeBox = null!;
    private ComboBox categoryBox = null!;

    private AnomieTextBox dependenciesBox = null!;
    private AnomieTextBox incompatibilitiesBox = null!;
    private AnomieTextBox extendsIdBox = null!;
    private AnomieTextBox extendsVersionBox = null!;

    private AnomieTextBox manifestPreviewBox = null!;
    private AnomieTextBox downloadUrlBox = null!;
    private AnomieTextBox hashBox = null!;
    private AnomieTextBox logBox = null!;
    private Label statusLabel = null!;
    private Label accountPill = null!;
    private readonly List<Button> actionButtons = new();

    private DataGridView modGrid = null!;
    private AnomieTextBox browserSearchBox = null!;
    private ComboBox browserSourceBox = null!;
    private Label browserCountLabel = null!;
    private readonly List<BrowserModItem> browserItems = new();
    private const string DefaultReleaseRepository = "Nomnom-Mod-Releases";
    private bool editorLoadedFromBrowserManifest;

    public AnomiePublisherForm()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        settingsPath = Path.Combine(appData, "Anomie", "NomnomPublisher", "settings.json");
        var oldSettingsPath = Path.Combine(appData, "Anomie", "NomnomPublisher", "settings.json");
        if (!File.Exists(settingsPath) && File.Exists(oldSettingsPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            File.Copy(oldSettingsPath, settingsPath, true);
        }
        settings = AppSettings.Load(settingsPath);
        NormalizeSettings();
        accessToken = SecureTokenStore.Unprotect(settings.EncryptedAccessToken);
        Text = "Anomie UI - NOMNOM Publisher";
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(1280, 820);
        Size = new Size(1520, 920);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        BackColor = AnomieTheme.Background;
        ForeColor = AnomieTheme.Text;
        Font = AnomieTheme.TextFont();
        BuildUi();
        LoadSettingsToUi();
    }


    private void NormalizeSettings()
    {
        if (string.Equals(settings.UpstreamOwner, "NuclearOptionModding", StringComparison.OrdinalIgnoreCase))
        {
            settings.UpstreamOwner = "KopterBuzz";
        }
        if (string.IsNullOrWhiteSpace(settings.UpstreamRepo)) settings.UpstreamRepo = "NOMNOM";
        if (string.IsNullOrWhiteSpace(settings.UpstreamBranch)) settings.UpstreamBranch = "main";
        if (string.IsNullOrWhiteSpace(settings.ManifestFolder)) settings.ManifestFolder = "modManifests";
        settings.SavedDescriptions ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(settings.ModId) && !string.IsNullOrWhiteSpace(settings.Description) && !settings.SavedDescriptions.ContainsKey(settings.ModId))
        {
            settings.SavedDescriptions[settings.ModId] = settings.Description.Trim();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        loginCts?.Cancel();
        SaveSettingsFromUi();
        settings.Save(settingsPath);
        base.OnFormClosing(e);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AnomieTheme.Background,
            Padding = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 376));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AnomieTheme.Deep,
            Padding = new Padding(22, 20, 22, 20)
        };
        root.Controls.Add(sidebar, 0, 0);

        var brand = new AnomiePanel
        {
            Dock = DockStyle.Top,
            Height = 156,
            Fill = Color.FromArgb(11, 17, 25),
            Stroke = Color.FromArgb(55, 87, 104),
            Glow = true,
            Padding = new Padding(18)
        };
        sidebar.Controls.Add(brand);

        var brandTitle = new Label
        {
            Text = "ANOMIE UI",
            Dock = DockStyle.Top,
            Height = 38,
            ForeColor = AnomieTheme.Text,
            Font = AnomieTheme.TitleFont(22f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        brand.Controls.Add(brandTitle);

        var brandLine = new Label
        {
            Text = "NOMNOM publisher console",
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = AnomieTheme.Muted,
            Font = AnomieTheme.TextFont(9.5f)
        };
        brand.Controls.Add(brandLine);
        brandLine.BringToFront();

        accountPill = new Label
        {
            Text = "GITHUB: OFFLINE",
            Dock = DockStyle.Bottom,
            Height = 34,
            BackColor = Color.FromArgb(20, 31, 42),
            ForeColor = AnomieTheme.Warning,
            Font = AnomieTheme.MonoFont(9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        brand.Controls.Add(accountPill);

        var actions = new AnomiePanel
        {
            Dock = DockStyle.Top,
            Height = 500,
            Top = 172,
            Fill = AnomieTheme.Surface,
            Stroke = AnomieTheme.BorderSoft,
            Padding = new Padding(16)
        };
        sidebar.Controls.Add(actions);
        actions.BringToFront();

        var actionTitle = new Label
        {
            Text = "PUBLISH FLOW",
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = AnomieTheme.Muted,
            Font = AnomieTheme.MonoFont(9f, FontStyle.Bold)
        };
        actions.Controls.Add(actionTitle);

        var actionList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 0)
        };
        actions.Controls.Add(actionList);
        actionList.BringToFront();

        AddAction(actionList, "Sign in with GitHub", async () => await SignInWithGitHubAsync(), true);
        AddAction(actionList, "Read DLL metadata", async () => await ReadDllMetadataAsync());
        AddAction(actionList, "Build release ZIP", async () => await PackageZipAsync());
        AddAction(actionList, "Upload GitHub release", async () => await UploadReleaseAsync());
        AddAction(actionList, "Generate manifest", async () => await GenerateManifestAsync(true));
        AddAction(actionList, "Fork + open PR", async () => await PublishManifestPrAsync());
        AddAction(actionList, "Refresh Mod Browser", async () => await RefreshModBrowserAsync());
        AddAction(actionList, "Run full publish", async () => await RunFullPublishAsync(), true);

        statusLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 110,
            ForeColor = AnomieTheme.Muted,
            Font = AnomieTheme.MonoFont(9f),
            Text = "Ready."
        };
        sidebar.Controls.Add(statusLabel);

        var main = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AnomieTheme.Background,
            Padding = new Padding(18)
        };
        root.Controls.Add(main, 1, 0);

        var header = new AnomieHeader
        {
            Dock = DockStyle.Top,
            HeaderText = "NOMNOM MOD PUBLISHER",
            SubText = "Package the DLL, release it, generate the manifest, fork NOMNOM and open the pull request."
        };
        main.Controls.Add(header);

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = AnomieTheme.TextFont(9.5f, FontStyle.Bold),
            Padding = new Point(16, 7),
            BackColor = AnomieTheme.Background,
            ForeColor = AnomieTheme.Text
        };
        main.Controls.Add(tabs);
        tabs.BringToFront();

        tabs.TabPages.Add(CreateGitHubTab());
        tabs.TabPages.Add(CreatePackageTab());
        tabs.TabPages.Add(CreateManifestTab());
        tabs.TabPages.Add(CreateRelationsTab());
        tabs.TabPages.Add(CreateModBrowserTab());
        tabs.TabPages.Add(CreateOutputTab());

        ApplyThemeRecursive(this);
    }

    private void AddAction(FlowLayoutPanel list, string text, Func<Task> action, bool primary = false)
    {
        var button = new AnomieButton
        {
            Text = text,
            Width = 288,
            Height = 38,
            Primary = primary,
            Margin = new Padding(0, 0, 0, 7)
        };
        button.Click += async (_, _) => await RunActionAsync(action);
        actionButtons.Add(button);
        list.Controls.Add(button);
    }

    private TabPage CreateGitHubTab()
    {
        var page = CreatePage("GitHub Access");
        var grid = CreateGrid();
        page.Controls.Add(grid);
        AddSection(grid, "Authentication", "Use GitHub Device Flow. No client secret is stored in the app.");
        clientIdBox = AddText(grid, "OAuth Client ID", "Paste your OAuth App Client ID once", false, 32);
        AddButtonRow(grid, "GitHub Login", "Open authorization", async () => await SignInWithGitHubAsync(), true);
        userCodeBox = AddText(grid, "Current Login Code", "Appears during sign-in", true, 30);
        verificationUrlBox = AddText(grid, "Verification URL", "https://github.com/login/device", true, 30);
        AddButtonRow(grid, "Browser", "Open verification page", () => Task.Run(() => GitHubAuthService.OpenBrowser(string.IsNullOrWhiteSpace(verificationUrlBox.Text) ? "https://github.com/login/device" : verificationUrlBox.Text)), false);
        accountBox = AddText(grid, "Detected GitHub Account", "Resolved automatically after login", true, 30);

        AddSection(grid, "Repositories", "The release repo stores your ZIP files. The PR target repository can be changed for NOMNOM forks or test registries.");
        releaseRepoBox = AddText(grid, "Release Repository", "Nomnom-Mod-Releases", false, 30);
        createReleaseRepoBox = AddCheck(grid, "Create missing release repo", true);
        privateReleaseRepoBox = AddCheck(grid, "Private release repo", false);
        upstreamOwnerBox = AddText(grid, "PR Target Owner", "KopterBuzz", false, 30);
        upstreamRepoBox = AddText(grid, "PR Target Repository", "NOMNOM", false, 30);
        upstreamBranchBox = AddText(grid, "PR Target Branch", "main", false, 30);
        manifestFolderBox = AddText(grid, "Manifest Folder", "modManifests", false, 30);
        createPrBox = AddCheck(grid, "Create pull request automatically", true);
        autoUpdateBox = AddCheck(grid, "Enable NOMNOM auto-update fields", true);
        return page;
    }

    private TabPage CreatePackageTab()
    {
        var page = CreatePage("Package");
        var grid = CreateGrid();
        page.Controls.Add(grid);
        AddSection(grid, "DLL Package", "Select your BepInEx plugin and let the tool build the release archive.");
        dllPathBox = AddText(grid, "Plugin DLL", "", false, 30);
        AddButtonRow(grid, "DLL", "Select DLL", SelectDllAsync, false);
        extraFolderBox = AddText(grid, "Extra Folder", "Optional files, configs or assets", false, 30);
        AddButtonRow(grid, "Extra Files", "Select folder", SelectExtraFolderAsync, false);
        outputZipBox = AddText(grid, "Output ZIP", "", false, 30);
        AddButtonRow(grid, "Archive", "Choose ZIP path", SelectOutputZipAsync, false);
        includeBepInExLayoutBox = AddCheck(grid, "Use BepInEx/plugins layout inside ZIP", true);
        fileNameBox = AddText(grid, "Release File Name", "Auto-filled from ZIP", false, 30);
        releaseTagBox = AddText(grid, "Release Tag", "v1.0.0", false, 30);
        AddButtonRow(grid, "Metadata", "Read DLL metadata", async () => await ReadDllMetadataAsync(), false);
        AddButtonRow(grid, "Package", "Build ZIP", async () => await PackageZipAsync(), true);
        return page;
    }

    private TabPage CreateManifestTab()
    {
        var page = CreatePage("Manifest");
        var grid = CreateGrid();
        page.Controls.Add(grid);
        AddSection(grid, "Mod Metadata", "These fields become the NOMNOM manifest entry.");
        modIdBox = AddText(grid, "Mod ID", "AssemblyName or ModAssembly.UniqueName", false, 30);
        displayNameBox = AddText(grid, "Display Name", "Human readable name", false, 30);
        descriptionBox = AddText(grid, "Description", "Briefly explain what the mod does", false, 66);
        AddButtonRow(grid, "Catalog", "Pull catalog description", async () => await PullCatalogDescriptionForCurrentModAsync(), false);
        tagsBox = AddText(grid, "Tags", "QoL,Utility,Server", false, 30);
        authorsBox = AddText(grid, "Authors", "Anomie", false, 30);
        infoUrlBox = AddText(grid, "Info URL", "Repository, documentation or mod page", false, 30);
        gameVersionBox = AddText(grid, "Game Version", "0.32", false, 30);
        versionBox = AddText(grid, "Mod Version", "Must match DLL metadata for plugins", false, 30);
        artifactTypeBox = AddCombo(grid, "Artifact Type", new[] { "plugin", "addOn" });
        categoryBox = AddCombo(grid, "Category", new[] { "Release", "Pre-Release" });
        downloadUrlBox = AddText(grid, "Download URL", "Filled after release upload", false, 30);
        hashBox = AddText(grid, "Release Hash", "sha256:... filled after release upload", false, 30);
        AddButtonRow(grid, "Manifest", "Generate JSON", async () => await GenerateManifestAsync(true), true);
        return page;
    }

    private TabPage CreateRelationsTab()
    {
        var page = CreatePage("Relations");
        var grid = CreateGrid();
        page.Controls.Add(grid);
        AddSection(grid, "Dependencies", "Use id@version entries separated by commas, semicolons or new lines.");
        dependenciesBox = AddText(grid, "Dependencies", "Example.Mod@1.0.0", false, 82);
        incompatibilitiesBox = AddText(grid, "Incompatibilities", "Other.Mod@2.0.0", false, 82);
        AddSection(grid, "Add-On Extension", "Only required when Artifact Type is addOn.");
        extendsIdBox = AddText(grid, "Extends Mod ID", "", false, 30);
        extendsVersionBox = AddText(grid, "Minimum Version", "", false, 30);
        return page;
    }



    private TabPage CreateModBrowserTab()
    {
        var page = CreatePage("Mod Browser");
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = AnomieTheme.Background,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(root);

        var intro = new AnomiePanel
        {
            Dock = DockStyle.Fill,
            Fill = Color.FromArgb(12, 20, 30),
            Stroke = Color.FromArgb(52, 83, 99),
            Padding = new Padding(18),
            Glow = true
        };
        root.Controls.Add(intro, 0, 0);

        var introTitle = new Label
        {
            Text = "MOD BROWSER",
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = AnomieTheme.Text,
            Font = AnomieTheme.TextFont(12.5f, FontStyle.Bold)
        };
        var introText = new Label
        {
            Text = "View your own releases and submitted manifests. The official NOMNOM catalog can be inspected, but foreign entries stay read-only and cannot be republished or deleted.",
            Dock = DockStyle.Fill,
            ForeColor = AnomieTheme.Muted,
            Font = AnomieTheme.TextFont(9.3f)
        };
        intro.Controls.Add(introText);
        intro.Controls.Add(introTitle);
        introText.BringToFront();

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Padding = new Padding(2, 8, 2, 4)
        };
        root.Controls.Add(bar, 0, 1);

        browserSourceBox = new ComboBox
        {
            Width = 210,
            Height = 36,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AnomieTheme.Surface3,
            ForeColor = AnomieTheme.Text,
            FlatStyle = FlatStyle.Flat,
            Font = AnomieTheme.TextFont(9.2f),
            Margin = new Padding(0, 4, 8, 4)
        };
        browserSourceBox.Items.AddRange(new object[] { "My mods only", "My Release Repo", "My submitted manifests", "Official NOMNOM catalog" });
        browserSourceBox.SelectedIndex = 0;
        bar.Controls.Add(browserSourceBox);

        browserSearchBox = new AnomieTextBox
        {
            Width = 360,
            Height = 36,
            PlaceholderText = "Search mod, version, file...",
            Margin = new Padding(0, 4, 8, 4)
        };
        browserSearchBox.TextChanged += (_, _) => ApplyModBrowserFilter();
        bar.Controls.Add(browserSearchBox);

        bar.Controls.Add(MakeBrowserButton("Refresh", async () => await RefreshModBrowserAsync(), true));
        bar.Controls.Add(MakeBrowserButton("Load Selected", async () => await LoadSelectedModAsync(), false));
        bar.Controls.Add(MakeBrowserButton("Update Selected", async () => await UpdateSelectedModAsync(), true));
        bar.Controls.Add(MakeBrowserButton("Delete Release", async () => await DeleteSelectedReleaseAsync(), false, true));
        bar.Controls.Add(MakeBrowserButton("Delete Manifest PR", async () => await DeleteSelectedManifestPrAsync(), false, true));
        bar.Controls.Add(MakeBrowserButton("Open", async () => await OpenSelectedModUrlAsync(), false));

        browserCountLabel = new Label
        {
            AutoSize = false,
            Width = 280,
            Height = 36,
            Margin = new Padding(8, 4, 0, 4),
            Text = "No mods loaded.",
            ForeColor = AnomieTheme.Muted,
            Font = AnomieTheme.MonoFont(8.8f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        bar.Controls.Add(browserCountLabel);

        var gridPanel = new AnomiePanel
        {
            Dock = DockStyle.Fill,
            Fill = AnomieTheme.Surface,
            Stroke = AnomieTheme.BorderSoft,
            Padding = new Padding(12)
        };
        root.Controls.Add(gridPanel, 0, 2);

        modGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.FromArgb(9, 14, 20),
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            GridColor = Color.FromArgb(34, 50, 62),
            Font = AnomieTheme.TextFont(9f),
            ColumnHeadersHeight = 36,
            RowTemplate = { Height = 34 }
        };
        modGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(18, 29, 42);
        modGrid.ColumnHeadersDefaultCellStyle.ForeColor = AnomieTheme.Accent;
        modGrid.ColumnHeadersDefaultCellStyle.Font = AnomieTheme.MonoFont(8.8f, FontStyle.Bold);
        modGrid.DefaultCellStyle.BackColor = Color.FromArgb(10, 16, 23);
        modGrid.DefaultCellStyle.ForeColor = AnomieTheme.Text;
        modGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(28, 63, 74);
        modGrid.DefaultCellStyle.SelectionForeColor = AnomieTheme.Text;
        modGrid.Columns.Add("Source", "Source");
        modGrid.Columns.Add("ModId", "Mod ID");
        modGrid.Columns.Add("Version", "Version");
        modGrid.Columns.Add("Type", "Type");
        modGrid.Columns.Add("Game", "Game");
        modGrid.Columns.Add("File", "File");
        modGrid.Columns.Add("Repo", "Repo");
        modGrid.Columns.Add("Path", "Path / Release");
        modGrid.Columns[0].FillWeight = 80;
        modGrid.Columns[1].FillWeight = 150;
        modGrid.Columns[2].FillWeight = 70;
        modGrid.Columns[3].FillWeight = 65;
        modGrid.Columns[4].FillWeight = 60;
        modGrid.Columns[5].FillWeight = 130;
        modGrid.Columns[6].FillWeight = 110;
        modGrid.Columns[7].FillWeight = 160;
        modGrid.CellDoubleClick += async (_, _) => await RunActionAsync(LoadSelectedModAsync);
        gridPanel.Controls.Add(modGrid);

        return page;
    }

    private AnomieButton MakeBrowserButton(string text, Func<Task> action, bool primary, bool danger = false)
    {
        var button = new AnomieButton
        {
            Text = text,
            Width = Math.Max(136, Math.Min(190, text.Length * 8 + 44)),
            Height = 36,
            Primary = primary,
            Danger = danger,
            Margin = new Padding(0, 4, 8, 4)
        };
        button.Click += async (_, _) => await RunActionAsync(action);
        actionButtons.Add(button);
        return button;
    }

    private TabPage CreateOutputTab()
    {
        var page = CreatePage("Output");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AnomieTheme.Background,
            Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        page.Controls.Add(layout);

        manifestPreviewBox = new AnomieTextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = AnomieTheme.MonoFont(9f),
            BackColor = Color.FromArgb(10, 15, 21),
            ReadOnly = false
        };
        layout.Controls.Add(WrapBox("Manifest Preview", manifestPreviewBox), 0, 0);

        logBox = new AnomieTextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            Font = AnomieTheme.MonoFont(9f),
            BackColor = Color.FromArgb(10, 15, 21),
            ReadOnly = true
        };
        layout.Controls.Add(WrapBox("Run Log", logBox), 1, 0);
        return page;
    }

    private TabPage CreatePage(string title)
    {
        return new TabPage(title)
        {
            BackColor = AnomieTheme.Background,
            ForeColor = AnomieTheme.Text,
            Padding = new Padding(0)
        };
    }

    private TableLayoutPanel CreateGrid()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoScroll = true,
            BackColor = AnomieTheme.Background,
            Padding = new Padding(18)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return grid;
    }

    private Control WrapBox(string title, Control child)
    {
        var panel = new AnomiePanel
        {
            Dock = DockStyle.Fill,
            Fill = AnomieTheme.Surface,
            Stroke = AnomieTheme.BorderSoft,
            Padding = new Padding(14)
        };
        var label = new Label
        {
            Text = title.ToUpperInvariant(),
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = AnomieTheme.Muted,
            Font = AnomieTheme.MonoFont(9f, FontStyle.Bold)
        };
        panel.Controls.Add(child);
        panel.Controls.Add(label);
        child.BringToFront();
        return panel;
    }

    private void AddSection(TableLayoutPanel grid, string title, string detail)
    {
        var panel = new AnomiePanel
        {
            Dock = DockStyle.Top,
            Height = 74,
            Fill = Color.FromArgb(12, 19, 28),
            Stroke = Color.FromArgb(45, 69, 84),
            Padding = new Padding(14),
            Margin = new Padding(0, 8, 0, 12)
        };
        var head = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = AnomieTheme.Text,
            Font = AnomieTheme.TextFont(11f, FontStyle.Bold)
        };
        var body = new Label
        {
            Text = detail,
            Dock = DockStyle.Fill,
            ForeColor = AnomieTheme.Muted,
            Font = AnomieTheme.TextFont(9.2f)
        };
        panel.Controls.Add(body);
        panel.Controls.Add(head);
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
        grid.Controls.Add(panel, 0, grid.RowCount);
        grid.SetColumnSpan(panel, 2);
        grid.RowCount++;
    }

    private AnomieTextBox AddText(TableLayoutPanel grid, string label, string placeholder, bool readOnly, int height)
    {
        var row = grid.RowCount;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Math.Max(height + 14, 46)));
        var caption = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            ForeColor = AnomieTheme.Muted,
            Font = AnomieTheme.TextFont(9.2f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 0, 12, 0)
        };
        var box = new AnomieTextBox
        {
            Dock = DockStyle.Fill,
            Height = height,
            Multiline = height > 36,
            ReadOnly = readOnly,
            PlaceholderText = placeholder,
            Margin = new Padding(0, 5, 0, 5)
        };
        if (height > 36) box.ScrollBars = ScrollBars.Vertical;
        grid.Controls.Add(caption, 0, row);
        grid.Controls.Add(box, 1, row);
        grid.RowCount++;
        return box;
    }

    private CheckBox AddCheck(TableLayoutPanel grid, string text, bool defaultValue)
    {
        var row = grid.RowCount;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        var spacer = new Label { Dock = DockStyle.Fill };
        var check = new CheckBox
        {
            Text = text,
            Checked = defaultValue,
            Dock = DockStyle.Fill,
            ForeColor = AnomieTheme.Text,
            Font = AnomieTheme.TextFont(9.4f),
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 4, 0, 4)
        };
        grid.Controls.Add(spacer, 0, row);
        grid.Controls.Add(check, 1, row);
        grid.RowCount++;
        return check;
    }

    private ComboBox AddCombo(TableLayoutPanel grid, string label, string[] items)
    {
        var row = grid.RowCount;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        var caption = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            ForeColor = AnomieTheme.Muted,
            Font = AnomieTheme.TextFont(9.2f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 0, 12, 0)
        };
        var combo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AnomieTheme.Surface3,
            ForeColor = AnomieTheme.Text,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 5, 0, 5)
        };
        combo.Items.AddRange(items.Cast<object>().ToArray());
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        grid.Controls.Add(caption, 0, row);
        grid.Controls.Add(combo, 1, row);
        grid.RowCount++;
        return combo;
    }

    private void AddButtonRow(TableLayoutPanel grid, string label, string buttonText, Func<Task> action, bool primary)
    {
        var row = grid.RowCount;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        var caption = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            ForeColor = AnomieTheme.Muted,
            Font = AnomieTheme.TextFont(9.2f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 0, 12, 0)
        };
        var button = new AnomieButton
        {
            Text = buttonText,
            Primary = primary,
            Width = 220,
            Dock = DockStyle.Left,
            Margin = new Padding(0, 4, 0, 4)
        };
        button.Click += async (_, _) => await RunActionAsync(action);
        actionButtons.Add(button);
        grid.Controls.Add(caption, 0, row);
        grid.Controls.Add(button, 1, row);
        grid.RowCount++;
    }

    private void LoadSettingsToUi()
    {
        clientIdBox.Text = settings.OAuthClientId;
        accountBox.Text = settings.GitHubLogin;
        releaseRepoBox.Text = settings.ReleaseRepo;
        createReleaseRepoBox.Checked = settings.CreateReleaseRepo;
        privateReleaseRepoBox.Checked = settings.PrivateReleaseRepo;
        upstreamOwnerBox.Text = settings.UpstreamOwner;
        upstreamRepoBox.Text = settings.UpstreamRepo;
        upstreamBranchBox.Text = settings.UpstreamBranch;
        manifestFolderBox.Text = settings.ManifestFolder;
        createPrBox.Checked = settings.CreatePullRequest;
        autoUpdateBox.Checked = settings.AutoUpdateArtifacts;

        dllPathBox.Text = settings.DllPath;
        extraFolderBox.Text = settings.ExtraFolder;
        outputZipBox.Text = settings.OutputZip;
        includeBepInExLayoutBox.Checked = settings.IncludeBepInExLayout;
        fileNameBox.Text = Path.GetFileName(settings.OutputZip);
        releaseTagBox.Text = string.IsNullOrWhiteSpace(settings.Version) ? "v1.0.0" : $"v{settings.Version}";

        modIdBox.Text = settings.ModId;
        displayNameBox.Text = settings.DisplayName;
        descriptionBox.Text = settings.Description;
        tagsBox.Text = settings.Tags;
        authorsBox.Text = settings.Authors;
        infoUrlBox.Text = settings.InfoUrl;
        gameVersionBox.Text = settings.GameVersion;
        versionBox.Text = settings.Version;
        artifactTypeBox.SelectedItem = settings.ArtifactType;
        categoryBox.SelectedItem = settings.Category;
        downloadUrlBox.Text = "";
        hashBox.Text = settings.ReleaseHash;
        dependenciesBox.Text = settings.Dependencies;
        incompatibilitiesBox.Text = settings.Incompatibilities;
        extendsIdBox.Text = settings.ExtendsId;
        extendsVersionBox.Text = settings.ExtendsVersion;

        if (!string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(settings.GitHubLogin))
        {
            accountPill.Text = $"GITHUB: @{settings.GitHubLogin}";
            accountPill.ForeColor = AnomieTheme.Accent;
        }
    }

    private void SaveSettingsFromUi()
    {
        settings.OAuthClientId = clientIdBox.Text.Trim();
        settings.GitHubLogin = accountBox.Text.Trim();
        settings.ReleaseRepo = releaseRepoBox.Text.Trim();
        settings.CreateReleaseRepo = createReleaseRepoBox.Checked;
        settings.PrivateReleaseRepo = privateReleaseRepoBox.Checked;
        settings.UpstreamOwner = upstreamOwnerBox.Text.Trim();
        settings.UpstreamRepo = upstreamRepoBox.Text.Trim();
        settings.UpstreamBranch = upstreamBranchBox.Text.Trim();
        settings.ManifestFolder = manifestFolderBox.Text.Trim();
        settings.CreatePullRequest = createPrBox.Checked;
        settings.AutoUpdateArtifacts = autoUpdateBox.Checked;
        settings.DllPath = dllPathBox.Text.Trim();
        settings.ExtraFolder = extraFolderBox.Text.Trim();
        settings.OutputZip = outputZipBox.Text.Trim();
        settings.ReleaseHash = hashBox.Text.Trim();
        settings.IncludeBepInExLayout = includeBepInExLayoutBox.Checked;
        settings.ModId = modIdBox.Text.Trim();
        settings.DisplayName = displayNameBox.Text.Trim();
        settings.Description = descriptionBox.Text.Trim();
        RememberDescriptionForCurrentMod();
        settings.Tags = tagsBox.Text.Trim();
        settings.Authors = authorsBox.Text.Trim();
        settings.InfoUrl = infoUrlBox.Text.Trim();
        settings.GameVersion = gameVersionBox.Text.Trim();
        settings.Version = versionBox.Text.Trim();
        settings.ArtifactType = artifactTypeBox.SelectedItem?.ToString() ?? "plugin";
        settings.Category = categoryBox.SelectedItem?.ToString() ?? "Release";
        settings.Dependencies = dependenciesBox.Text.Trim();
        settings.Incompatibilities = incompatibilitiesBox.Text.Trim();
        settings.ExtendsId = extendsIdBox.Text.Trim();
        settings.ExtendsVersion = extendsVersionBox.Text.Trim();
    }

    private void RememberDescriptionForCurrentMod()
    {
        settings.SavedDescriptions ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var modId = modIdBox.Text.Trim();
        var description = descriptionBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(modId)) return;

        if (string.IsNullOrWhiteSpace(description))
        {
            settings.SavedDescriptions.Remove(modId);
            return;
        }

        if (LooksLikeForeignBlueprinterText(description)) return;
        settings.SavedDescriptions[modId] = description;
    }

    private string GetSavedDescription(string modId)
    {
        if (settings.SavedDescriptions is null || string.IsNullOrWhiteSpace(modId)) return "";
        if (settings.SavedDescriptions.TryGetValue(modId, out var exact) && !string.IsNullOrWhiteSpace(exact)) return exact;
        foreach (var item in settings.SavedDescriptions)
        {
            if (string.Equals(item.Key, modId, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.Value)) return item.Value;
        }
        return "";
    }

    private void SaveDescriptionForModId(string modId, string description)
    {
        settings.SavedDescriptions ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        modId = modId.Trim();
        description = description.Trim();
        if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(description)) return;
        if (LooksLikeForeignBlueprinterText(description)) return;
        settings.SavedDescriptions[modId] = description;
        settings.Save(settingsPath);
    }

    private async Task PullCatalogDescriptionForCurrentModAsync()
    {
        SaveSettingsFromUi();
        var modId = modIdBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(modId)) throw new InvalidOperationException("Mod ID is required before pulling a catalog description. Read DLL metadata first.");
        var found = await TryPullCatalogDescriptionForModAsync(modId, true, true);
        if (!found)
        {
            Log($"No catalog description found for Mod ID '{modId}'.");
            SetStatus("No catalog description found.");
        }
    }

    private async Task<bool> TryPullCatalogDescriptionForModAsync(string modId, bool overwriteEditor, bool verbose)
    {
        modId = modId.Trim();
        if (string.IsNullOrWhiteSpace(modId)) return false;
        if (!overwriteEditor && !string.IsNullOrWhiteSpace(descriptionBox.Text)) return false;

        var fromLoadedBrowser = TryGetDescriptionFromLoadedBrowserItems(modId);
        if (!string.IsNullOrWhiteSpace(fromLoadedBrowser))
        {
            ApplyCatalogDescription(modId, fromLoadedBrowser, overwriteEditor || string.IsNullOrWhiteSpace(descriptionBox.Text), "loaded Mod Browser data");
            return true;
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            if (verbose) Log("Sign in with GitHub first, then pull the catalog description again.");
            return false;
        }

        SaveSettingsFromUi();
        var github = new GitHubClient(accessToken);
        var candidates = BuildCatalogDescriptionCandidates();
        foreach (var candidate in candidates)
        {
            var json = await TryReadCatalogManifestByModIdAsync(github, candidate.Owner, candidate.Repo, candidate.Branch, modId, CancellationToken.None);
            var description = ExtractDescriptionIfManifestMatches(json, modId);
            if (string.IsNullOrWhiteSpace(description)) continue;
            ApplyCatalogDescription(modId, description, overwriteEditor || string.IsNullOrWhiteSpace(descriptionBox.Text), candidate.Label);
            return true;
        }

        return false;
    }

    private List<(string Owner, string Repo, string Branch, string Label)> BuildCatalogDescriptionCandidates()
    {
        var list = new List<(string Owner, string Repo, string Branch, string Label)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string owner, string repo, string branch, string label)
        {
            owner = owner.Trim();
            repo = repo.Trim();
            branch = string.IsNullOrWhiteSpace(branch) ? "main" : branch.Trim();
            if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo)) return;
            var key = $"{owner}/{repo}/{branch}";
            if (!seen.Add(key)) return;
            list.Add((owner, repo, branch, label));
        }

        var login = accountBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(login)) Add(login, settings.UpstreamRepo, settings.UpstreamBranch, "your NOMNOM fork");
        Add(settings.UpstreamOwner, settings.UpstreamRepo, settings.UpstreamBranch, "official NOMNOM catalog");
        return list;
    }

    private async Task<string> TryReadCatalogManifestByModIdAsync(GitHubClient github, string owner, string repo, string branch, string modId, CancellationToken cancellationToken)
    {
        var path = $"{settings.ManifestFolder.Trim().Trim('/')}/{BuildManifestFileName(modId)}";
        try
        {
            var file = await github.ReadTextFileAsync(owner, repo, path, branch, cancellationToken);
            return file.Text;
        }
        catch (Exception ex) when (ex.Message.Contains("404", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }
        catch (Exception ex)
        {
            Log($"Could not read catalog manifest from {owner}/{repo}:{path}. {BuildGitHubHint(ex.Message)}");
            return "";
        }
    }

    private string TryGetDescriptionFromLoadedBrowserItems(string modId)
    {
        foreach (var item in browserItems)
        {
            if (!string.Equals(item.ModId, modId, StringComparison.OrdinalIgnoreCase)) continue;
            var description = ExtractDescriptionIfManifestMatches(item.ManifestJson, modId);
            if (!string.IsNullOrWhiteSpace(description)) return description;
        }
        return "";
    }

    private static string ExtractDescriptionIfManifestMatches(string json, string modId)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(modId)) return "";
        try
        {
            var node = JsonNode.Parse(json)?.AsObject();
            if (node is null) return "";
            var id = node["id"]?.GetValue<string>() ?? "";
            if (!string.Equals(id, modId, StringComparison.OrdinalIgnoreCase)) return "";
            return node["description"]?.GetValue<string>()?.Trim() ?? "";
        }
        catch
        {
            return "";
        }
    }

    private void ApplyCatalogDescription(string modId, string description, bool updateEditor, string source)
    {
        SaveDescriptionForModId(modId, description);
        if (updateEditor) descriptionBox.Text = description;
        Log($"Catalog description saved for {modId} from {source}.");
        SetStatus("Catalog description saved.");
    }

    private async Task SignInWithGitHubAsync()
    {
        SaveSettingsFromUi();
        if (string.IsNullOrWhiteSpace(settings.OAuthClientId)) throw new InvalidOperationException("OAuth Client ID is required. Create a GitHub OAuth App once, enable Device Flow, then paste its Client ID here.");
        loginCts?.Cancel();
        loginCts = new CancellationTokenSource();
        var auth = new GitHubAuthService();
        var scopes = settings.PrivateReleaseRepo ? "repo read:user" : "public_repo read:user";
        SetStatus("Requesting GitHub login code...");
        var device = await auth.RequestDeviceCodeAsync(settings.OAuthClientId, scopes, loginCts.Token);
        Log($"GitHub OAuth scopes requested: {scopes}");
        userCodeBox.Text = device.UserCode;
        verificationUrlBox.Text = device.VerificationUri;
        Clipboard.SetText(device.UserCode);
        Log($"GitHub code copied to clipboard: {device.UserCode}");
        GitHubAuthService.OpenBrowser(device.VerificationUri);
        SetStatus("Authorize the code in your browser.");
        var token = await auth.PollForTokenAsync(settings.OAuthClientId, device, SetStatus, loginCts.Token);
        accessToken = token.AccessToken;
        settings.EncryptedAccessToken = SecureTokenStore.Protect(accessToken);
        var github = new GitHubClient(accessToken);
        var identity = await github.GetUserAsync(loginCts.Token);
        settings.GitHubLogin = identity.Login;
        accountBox.Text = identity.Login;
        accountPill.Text = $"GITHUB: @{identity.Login}";
        accountPill.ForeColor = AnomieTheme.Accent;
        if (string.IsNullOrWhiteSpace(authorsBox.Text)) authorsBox.Text = identity.Login;
        if (string.IsNullOrWhiteSpace(infoUrlBox.Text) && !string.IsNullOrWhiteSpace(releaseRepoBox.Text)) infoUrlBox.Text = $"https://github.com/{identity.Login}/{releaseRepoBox.Text.Trim()}";
        settings.Save(settingsPath);
        Log($"Signed in as @{identity.Login}.");
        SetStatus("GitHub connected.");
    }

    private async Task RunFastAsync(Action action)
    {
        await Task.Run(action);
    }

    private async Task ReadDllMetadataAsync()
    {
        var dll = dllPathBox.Text.Trim();
        if (!File.Exists(dll)) throw new FileNotFoundException("DLL not found.", dll);
        var meta = await Task.Run(() => ZipBuilder.ReadAssemblyName(dll));
        ResetEditorForSelectedDll(dll, meta);
        if (string.IsNullOrWhiteSpace(descriptionBox.Text))
        {
            await TryPullCatalogDescriptionForModAsync(meta.ModId, false, false);
        }
    }

    private void ResetEditorForSelectedDll(string dll, AssemblyNameInfo meta)
    {
        var wasBrowserImport = editorLoadedFromBrowserManifest;
        var previousModId = modIdBox.Text.Trim();
        var previousDescription = descriptionBox.Text.Trim();
        var sameMod = !string.IsNullOrWhiteSpace(previousModId) && string.Equals(previousModId, meta.ModId, StringComparison.OrdinalIgnoreCase);
        modIdBox.Text = meta.ModId;
        displayNameBox.Text = meta.DisplayName;
        versionBox.Text = meta.Version;
        releaseTagBox.Text = $"v{meta.Version}";

        if (string.IsNullOrWhiteSpace(releaseRepoBox.Text) || IsForeignReleaseRepoForMod(releaseRepoBox.Text, meta.ModId) || wasBrowserImport)
        {
            releaseRepoBox.Text = DefaultReleaseRepository;
        }

        var outDir = Path.Combine(Path.GetDirectoryName(dll) ?? Environment.CurrentDirectory, "nomnom-release");
        var canonicalFile = BuildReleaseFileName(meta.ModId, meta.Version);
        outputZipBox.Text = Path.Combine(outDir, canonicalFile);
        fileNameBox.Text = canonicalFile;
        downloadUrlBox.Text = "";
        hashBox.Text = "";
        generatedManifest = "";
        manifestPreviewBox.Text = "";

        var savedDescription = GetSavedDescription(meta.ModId);
        if (!string.IsNullOrWhiteSpace(savedDescription))
        {
            descriptionBox.Text = savedDescription;
        }
        else if (sameMod && !wasBrowserImport && !string.IsNullOrWhiteSpace(previousDescription) && !LooksLikeForeignBlueprinterText(previousDescription))
        {
            descriptionBox.Text = previousDescription;
            RememberDescriptionForCurrentMod();
        }
        else
        {
            descriptionBox.Text = "";
        }

        if (wasBrowserImport || string.IsNullOrWhiteSpace(tagsBox.Text) || tagsBox.Text.Contains("blueprinter", StringComparison.OrdinalIgnoreCase)) tagsBox.Text = "QoL,Utility";
        if (wasBrowserImport || string.IsNullOrWhiteSpace(authorsBox.Text) || authorsBox.Text.Contains("nikkorap", StringComparison.OrdinalIgnoreCase)) authorsBox.Text = "Anomie";

        if (string.IsNullOrWhiteSpace(infoUrlBox.Text) || wasBrowserImport || IsForeignInfoUrl(infoUrlBox.Text))
        {
            var login = accountBox.Text.Trim();
            infoUrlBox.Text = string.IsNullOrWhiteSpace(login) ? "" : $"https://github.com/{login}/{releaseRepoBox.Text.Trim()}";
        }

        SetCombo(artifactTypeBox, "plugin");
        SetCombo(categoryBox, "Release");
        dependenciesBox.Text = "";
        incompatibilitiesBox.Text = "";
        extendsIdBox.Text = "";
        extendsVersionBox.Text = "";
        editorLoadedFromBrowserManifest = false;
        SaveSettingsFromUi();
        Log($"DLL metadata loaded from selected DLL: {meta.DisplayName} {meta.Version} (ID: {meta.ModId}, Source: {(meta.FromBepInPlugin ? "BepInPlugin" : "Assembly")})");
    }

    private async Task SelectDllAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "BepInEx Plugin DLL (*.dll)|*.dll|All files (*.*)|*.*",
            Title = "Select plugin DLL"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            dllPathBox.Text = dialog.FileName;
            await ReadDllMetadataAsync();
        }
    }

    private Task SelectExtraFolderAsync()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select optional extra folder" };
        if (dialog.ShowDialog(this) == DialogResult.OK) extraFolderBox.Text = dialog.SelectedPath;
        return Task.CompletedTask;
    }

    private Task SelectOutputZipAsync()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "ZIP archive (*.zip)|*.zip",
            Title = "Choose output ZIP",
            FileName = string.IsNullOrWhiteSpace(fileNameBox.Text) ? "mod-release.zip" : fileNameBox.Text
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            outputZipBox.Text = dialog.FileName;
            fileNameBox.Text = Path.GetFileName(dialog.FileName);
        }
        return Task.CompletedTask;
    }

    private async Task PackageZipAsync()
    {
        SaveSettingsFromUi();
        NormalizeReleaseArchivePath();
        SaveSettingsFromUi();
        await Task.Run(() =>
        {
            var output = ZipBuilder.BuildZip(settings.DllPath, settings.ExtraFolder, settings.OutputZip, settings.IncludeBepInExLayout);
            Invoke(() =>
            {
                outputZipBox.Text = output;
                fileNameBox.Text = Path.GetFileName(output);
                Log($"ZIP built: {output}");
            });
        });
        SetStatus("Release ZIP ready.");
    }

    private async Task UploadReleaseAsync()
    {
        SaveSettingsFromUi();
        var github = RequireGitHubClient();
        var login = await EnsureGitHubLoginAsync(github, CancellationToken.None);
        if (string.IsNullOrWhiteSpace(settings.ReleaseRepo)) throw new InvalidOperationException("Release repository is required.");
        if (!File.Exists(settings.OutputZip)) throw new FileNotFoundException("Output ZIP not found.", settings.OutputZip);
        ValidatePublishTargetForSelectedMod();
        if (settings.CreateReleaseRepo && !await github.RepoExistsAsync(login, settings.ReleaseRepo, CancellationToken.None))
        {
            Log($"Creating release repository: {login}/{settings.ReleaseRepo}");
            await github.CreateRepoAsync(settings.ReleaseRepo, settings.PrivateReleaseRepo, CancellationToken.None);
        }
        var tag = string.IsNullOrWhiteSpace(releaseTagBox.Text) ? $"v{settings.Version}" : releaseTagBox.Text.Trim();
        var title = string.IsNullOrWhiteSpace(settings.DisplayName) ? tag : $"{settings.DisplayName} {tag}";
        var release = await github.GetOrCreateReleaseAsync(login, settings.ReleaseRepo, tag, title, "Automated release created by Anomie UI NOMNOM Publisher.", CancellationToken.None);
        Log($"Release ready: {release.HtmlUrl}");
        var asset = await github.UploadReleaseAssetAsync(login, settings.ReleaseRepo, release.Id, settings.OutputZip, Log, CancellationToken.None);
        downloadUrlBox.Text = asset.BrowserDownloadUrl;
        fileNameBox.Text = asset.Name;
        var localHash = ComputeSha256Digest(settings.OutputZip);
        if (!string.IsNullOrWhiteSpace(asset.Digest))
        {
            hashBox.Text = asset.Digest;
            if (!string.Equals(asset.Digest, localHash, StringComparison.OrdinalIgnoreCase))
            {
                Log($"WARNING: GitHub asset digest differs from local file hash. GitHub={asset.Digest}, Local={localHash}");
            }
        }
        else
        {
            hashBox.Text = localHash;
            Log($"GitHub did not return an asset digest. Using local SHA-256 hash: {localHash}");
        }
        SaveSettingsFromUi();
        Log($"Asset uploaded: {asset.BrowserDownloadUrl}");
        Log($"Release asset hash: {hashBox.Text}");
        SetStatus("GitHub release uploaded.");
    }

    private async Task GenerateManifestAsync(bool showLog)
    {
        SaveSettingsFromUi();
        if (string.IsNullOrWhiteSpace(downloadUrlBox.Text))
        {
            if (!File.Exists(settings.OutputZip))
            {
                if (!File.Exists(settings.DllPath))
                {
                    throw new InvalidOperationException("Download URL is empty and no release ZIP can be built. Select a DLL, build/upload the release, or paste a Download URL.");
                }
                Log("Download URL is empty. Building the release ZIP first...");
                await PackageZipAsync();
                SaveSettingsFromUi();
            }

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                Log("Download URL is empty. Uploading the GitHub release first...");
                await UploadReleaseAsync();
                SaveSettingsFromUi();
            }
            else
            {
                throw new InvalidOperationException("Download URL is required. Sign in with GitHub and upload the release, or paste a direct ZIP download URL.");
            }
        }

        EnsureReleaseHashForManifest();
        var input = BuildManifestInput();
        ValidateManifestInputAgainstPublishTarget(input);
        generatedManifest = ManifestBuilder.Build(input);
        manifestPreviewBox.Text = generatedManifest;
        if (showLog) Log($"Manifest generated: {BuildManifestFileName(input.ModId)}");
        SetStatus("Manifest generated.");
        await Task.CompletedTask;
    }

    private async Task PublishManifestPrAsync()
    {
        SaveSettingsFromUi();
        var github = RequireGitHubClient();
        var login = await EnsureGitHubLoginAsync(github, CancellationToken.None);
        await GenerateManifestAsync(false);
        var input = BuildManifestInput();
        var upstreamOwner = settings.UpstreamOwner;
        var upstreamRepo = settings.UpstreamRepo;
        var upstreamBranch = settings.UpstreamBranch;
        var branch = BuildSafeBranchName("anomie-ui/add", input.ModId, input.Version);
        var manifestFileName = BuildManifestFileName(input.ModId);
        var path = $"{settings.ManifestFolder.Trim().Trim('/')}/{manifestFileName}";
        var legacyPath = BuildLegacyDisplayNameManifestPath(settings.ManifestFolder, input.DisplayName, path);

        if (!await github.RepoExistsAsync(upstreamOwner, upstreamRepo, CancellationToken.None))
        {
            throw new InvalidOperationException($"Upstream NOMNOM repository was not found: {upstreamOwner}/{upstreamRepo}. Use KopterBuzz/NOMNOM unless the project moved again.");
        }

        Log($"Forking {upstreamOwner}/{upstreamRepo}...");
        await github.CreateForkAsync(upstreamOwner, upstreamRepo, CancellationToken.None);
        await github.WaitForForkAsync(login, upstreamRepo, Log, CancellationToken.None);

        var baseSha = await github.GetBranchShaAsync(login, upstreamRepo, upstreamBranch, CancellationToken.None);
        await github.EnsureBranchAsync(login, upstreamRepo, branch, baseSha, CancellationToken.None);
        var oldSha = await github.GetFileShaAsync(login, upstreamRepo, path, branch, CancellationToken.None);
        var message = $"Add {input.DisplayName} manifest";
        await github.CommitFileAsync(login, upstreamRepo, branch, path, generatedManifest, message, oldSha, CancellationToken.None);
        Log($"Manifest committed to {login}/{upstreamRepo}:{branch}/{path}");

        if (!string.IsNullOrWhiteSpace(legacyPath))
        {
            var legacySha = await github.GetFileShaAsync(login, upstreamRepo, legacyPath, branch, CancellationToken.None);
            if (!string.IsNullOrWhiteSpace(legacySha))
            {
                await github.DeleteFileAsync(login, upstreamRepo, branch, legacyPath, $"Remove legacy manifest filename for {input.ModId}", legacySha, CancellationToken.None);
                Log($"Removed legacy manifest path because NOMNOM requires file name to match mod ID: {legacyPath}");
            }
        }

        if (settings.CreatePullRequest)
        {
            var prTitle = $"Add {input.DisplayName}";
            var prBody = $"Adds NOMNOM manifest for `{input.ModId}` version `{input.Version}`.\n\nCreated with Anomie UI NOMNOM Publisher.";
            lastPullRequestUrl = await github.CreatePullRequestAsync(upstreamOwner, upstreamRepo, prTitle, prBody, $"{login}:{branch}", upstreamBranch, CancellationToken.None);
            Log($"Pull request opened: {lastPullRequestUrl}");
            if (!string.IsNullOrWhiteSpace(lastPullRequestUrl)) Process.Start(new ProcessStartInfo(lastPullRequestUrl) { UseShellExecute = true });
        }
        SetStatus("Manifest PR submitted.");
    }

    private async Task RunFullPublishAsync()
    {
        await SignInIfNeededAsync();
        await ReadDllMetadataAsync();
        await PackageZipAsync();
        await UploadReleaseAsync();
        await GenerateManifestAsync(true);
        await PublishManifestPrAsync();
        SetStatus("Full publish complete.");
    }

    private async Task SignInIfNeededAsync()
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            var github = new GitHubClient(accessToken);
            await EnsureGitHubLoginAsync(github, CancellationToken.None);
            return;
        }
        await SignInWithGitHubAsync();
    }

    private GitHubClient RequireGitHubClient()
    {
        if (string.IsNullOrWhiteSpace(accessToken)) throw new InvalidOperationException("Sign in with GitHub first.");
        return new GitHubClient(accessToken);
    }

    private async Task<string> EnsureGitHubLoginAsync(GitHubClient github, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(accountBox.Text)) return accountBox.Text.Trim();
        var identity = await github.GetUserAsync(cancellationToken);
        accountBox.Text = identity.Login;
        settings.GitHubLogin = identity.Login;
        settings.Save(settingsPath);
        accountPill.Text = $"GITHUB: @{identity.Login}";
        accountPill.ForeColor = AnomieTheme.Accent;
        return identity.Login;
    }

    private ManifestInput BuildManifestInput()
    {
        SaveSettingsFromUi();
        return new ManifestInput
        {
            ModId = settings.ModId,
            DisplayName = settings.DisplayName,
            Description = settings.Description,
            Tags = ManifestBuilder.ParseList(settings.Tags),
            Authors = ManifestBuilder.ParseList(settings.Authors),
            InfoUrl = settings.InfoUrl,
            GitHubOwner = settings.GitHubLogin,
            GitHubRepoName = settings.ReleaseRepo,
            AutoUpdateArtifacts = settings.AutoUpdateArtifacts,
            ArtifactType = settings.ArtifactType,
            FileName = string.IsNullOrWhiteSpace(fileNameBox.Text) ? Path.GetFileName(settings.OutputZip) : fileNameBox.Text.Trim(),
            DownloadUrl = downloadUrlBox.Text.Trim(),
            Hash = hashBox.Text.Trim(),
            GameVersion = settings.GameVersion,
            Version = settings.Version,
            Category = settings.Category,
            ExtendsId = settings.ExtendsId,
            ExtendsVersion = settings.ExtendsVersion,
            Dependencies = ManifestBuilder.ParseRelations(settings.Dependencies),
            Incompatibilities = ManifestBuilder.ParseRelations(settings.Incompatibilities)
        };
    }

    private void NormalizeReleaseArchivePath()
    {
        var dll = settings.DllPath;
        if (string.IsNullOrWhiteSpace(dll) || !File.Exists(dll)) return;

        var version = string.IsNullOrWhiteSpace(settings.Version) ? "1.0.0" : settings.Version.Trim().TrimStart('v', 'V');
        var id = string.IsNullOrWhiteSpace(settings.ModId) ? Path.GetFileNameWithoutExtension(dll) : settings.ModId;
        var canonicalFile = BuildReleaseFileName(id, version);
        var currentFile = Path.GetFileName(settings.OutputZip);

        if (string.IsNullOrWhiteSpace(settings.OutputZip) || ContainsUnsafeAssetCharacters(currentFile))
        {
            var currentDir = !string.IsNullOrWhiteSpace(settings.OutputZip) ? Path.GetDirectoryName(settings.OutputZip) : null;
            var outDir = !string.IsNullOrWhiteSpace(currentDir)
                ? currentDir!
                : Path.Combine(Path.GetDirectoryName(dll) ?? Environment.CurrentDirectory, "nomnom-release");
            settings.OutputZip = Path.Combine(outDir, canonicalFile);
            outputZipBox.Text = settings.OutputZip;
            fileNameBox.Text = canonicalFile;
            Log($"Release file name normalized for GitHub: {canonicalFile}");
        }
    }

    private static bool ContainsUnsafeAssetCharacters(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return true;
        return fileName.Any(char.IsWhiteSpace) || fileName.Any(c => Path.GetInvalidFileNameChars().Contains(c));
    }

    private static string BuildManifestFileName(string modId)
    {
        var id = (modId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Mod ID is required before a manifest file name can be built.");
        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || id.Contains('/') || id.Contains('\\'))
        {
            throw new InvalidOperationException("Mod ID contains characters that cannot be used as a NOMNOM manifest file name. Fix the Mod ID first.");
        }
        return id.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? id : $"{id}.json";
    }

    private static string BuildLegacyDisplayNameManifestPath(string manifestFolder, string displayName, string correctPath)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return string.Empty;
        if (displayName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || displayName.Contains('/') || displayName.Contains('\\')) return string.Empty;
        var legacyPath = $"{manifestFolder.Trim().Trim('/')}/{displayName.Trim()}.json";
        return string.Equals(legacyPath, correctPath, StringComparison.OrdinalIgnoreCase) ? string.Empty : legacyPath;
    }

    private static string BuildReleaseFileName(string idOrName, string version)
    {
        var safeName = BuildSafeAssetNameBase(idOrName);
        var safeVersion = BuildSafeAssetNameBase(version.Trim().TrimStart('v', 'V'));
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "mod";
        if (string.IsNullOrWhiteSpace(safeVersion)) safeVersion = "1.0.0";
        return $"{safeName}_v{safeVersion}.zip";
    }

    private static string InferModIdFromRelease(string? releaseName, string? releaseTag, string? assetName)
    {
        var candidates = new[]
        {
            Path.GetFileNameWithoutExtension(assetName ?? ""),
            StripReleaseTagFromDisplayName(releaseName ?? "", releaseTag ?? ""),
            releaseName ?? ""
        };

        foreach (var raw in candidates)
        {
            var candidate = StripVersionSuffixFromReleaseName(raw);
            if (LooksLikeUsefulModId(candidate)) return candidate;
        }

        return StripVersionSuffixFromReleaseName(candidates.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "mod");
    }

    private static string BuildReleaseDisplayName(string? releaseName, string modId, string? releaseTag)
    {
        var cleaned = StripReleaseTagFromDisplayName(releaseName ?? "", releaseTag ?? "").Trim();
        cleaned = StripVersionSuffixFromReleaseName(cleaned).Trim();
        if (!string.IsNullOrWhiteSpace(cleaned) && !LooksLikeVersionOnly(cleaned)) return cleaned;
        return DisplayNameFromModId(modId);
    }

    private static string StripVersionSuffixFromReleaseName(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        value = Path.GetFileNameWithoutExtension(value);
        value = Regex.Replace(value, @"(?i)([._\-\s])v?\d+(?:\.\d+){1,4}.*$", "");
        return value.Trim(' ', '.', '_', '-');
    }

    private static bool LooksLikeUsefulModId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (LooksLikeVersionOnly(value)) return false;
        if (value.Equals("No asset", StringComparison.OrdinalIgnoreCase)) return false;
        return value.Any(char.IsLetter);
    }

    private static bool LooksLikeVersionOnly(string value)
    {
        value = (value ?? string.Empty).Trim();
        return Regex.IsMatch(value, @"(?i)^v?\d+(?:\.\d+){1,4}$");
    }

    private static string NormalizeReleaseVersion(string value)
    {
        value = (value ?? string.Empty).Trim();
        return value.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? value[1..] : value;
    }

    private static string DisplayNameFromModId(string modId)
    {
        modId = StripVersionSuffixFromReleaseName(modId);
        if (string.IsNullOrWhiteSpace(modId)) return "Mod";
        var parts = Regex.Split(modId, @"[._\-\s]+")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(TitleCaseToken);
        return string.Join(" ", parts);
    }

    private static string TitleCaseToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;
        if (token.Length <= 2 && token.All(char.IsUpper)) return token;
        return char.ToUpperInvariant(token[0]) + token[1..];
    }

    private static string BuildSafeAssetNameBase(string value)
    {
        var builder = new StringBuilder();
        foreach (var c in value.Trim())
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-') builder.Append(c);
            else if (c == '.' || char.IsWhiteSpace(c)) builder.Append('.');
            else builder.Append('.');
        }
        var result = builder.ToString();
        while (result.Contains("..", StringComparison.Ordinal)) result = result.Replace("..", ".", StringComparison.Ordinal);
        return result.Trim('.');
    }

    private static string BuildSafeBranchName(string prefix, params string[] parts)
    {
        var cleaned = parts.Select(BuildSafeBranchPart).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        var branch = $"{prefix.Trim().Trim('/')}/{string.Join('-', cleaned)}".ToLowerInvariant();
        while (branch.Contains("//", StringComparison.Ordinal)) branch = branch.Replace("//", "/", StringComparison.Ordinal);
        while (branch.Contains("--", StringComparison.Ordinal)) branch = branch.Replace("--", "-", StringComparison.Ordinal);
        branch = branch.Trim('/', '-', '.');
        return string.IsNullOrWhiteSpace(branch) ? $"{prefix.Trim().Trim('/')}/mod-{DateTime.UtcNow:yyyyMMddHHmmss}" : branch;
    }

    private static string BuildSafeBranchPart(string value)
    {
        var builder = new StringBuilder();
        foreach (var c in value.Trim())
        {
            if (char.IsLetterOrDigit(c)) builder.Append(char.ToLowerInvariant(c));
            else builder.Append('-');
        }
        var result = builder.ToString();
        while (result.Contains("--", StringComparison.Ordinal)) result = result.Replace("--", "-", StringComparison.Ordinal);
        return result.Trim('-');
    }



    private async Task RefreshModBrowserAsync()
    {
        SaveSettingsFromUi();
        await SignInIfNeededAsync();
        var github = RequireGitHubClient();
        var login = await EnsureGitHubLoginAsync(github, CancellationToken.None);
        browserItems.Clear();
        var source = browserSourceBox.SelectedItem?.ToString() ?? "My mods only";

        if (source is "My mods only" or "My Release Repo")
        {
            if (!string.IsNullOrWhiteSpace(settings.ReleaseRepo) && await github.RepoExistsAsync(login, settings.ReleaseRepo, CancellationToken.None))
            {
                var releases = await github.ListReleasesAsync(login, settings.ReleaseRepo, CancellationToken.None);
                foreach (var release in releases)
                {
                    if (release.Assets.Count == 0)
                    {
                        var inferredModId = InferModIdFromRelease(release.Name, release.TagName, "");
                        browserItems.Add(new BrowserModItem
                        {
                            Source = "Release",
                            Owner = login,
                            Repo = settings.ReleaseRepo,
                            ModId = inferredModId,
                            DisplayName = BuildReleaseDisplayName(release.Name, inferredModId, release.TagName),
                            Version = release.TagName,
                            FileName = "No asset",
                            HtmlUrl = release.HtmlUrl,
                            ReleaseId = release.Id
                        });
                        continue;
                    }
                    foreach (var asset in release.Assets)
                    {
                        var inferredModId = InferModIdFromRelease(release.Name, release.TagName, asset.Name);
                        browserItems.Add(new BrowserModItem
                        {
                            Source = "Release",
                            Owner = login,
                            Repo = settings.ReleaseRepo,
                            ModId = inferredModId,
                            DisplayName = BuildReleaseDisplayName(release.Name, inferredModId, release.TagName),
                            Version = release.TagName,
                            FileName = asset.Name,
                            DownloadUrl = asset.BrowserDownloadUrl,
                            Hash = asset.Digest,
                            HtmlUrl = release.HtmlUrl,
                            ReleaseId = release.Id
                        });
                    }
                }
                Log($"Loaded {releases.Count} GitHub releases from {login}/{settings.ReleaseRepo}.");
            }
            else
            {
                Log($"Release repo not found yet: {login}/{settings.ReleaseRepo}");
            }
        }

        if (source is "My mods only" or "My submitted manifests")
        {
            if (await github.RepoExistsAsync(login, settings.UpstreamRepo, CancellationToken.None))
            {
                await TryLoadManifestDirectoryAsync(github, login, settings.UpstreamRepo, settings.UpstreamBranch, "My submission", true, CancellationToken.None);
            }
            else
            {
                Log($"NOMNOM fork not found yet: {login}/{settings.UpstreamRepo}");
            }
        }

        if (source is "Official NOMNOM catalog")
        {
            await TryLoadManifestDirectoryAsync(github, settings.UpstreamOwner, settings.UpstreamRepo, settings.UpstreamBranch, "Official catalog", false, CancellationToken.None);
        }

        ApplyModBrowserFilter();
        SetStatus("Mod browser refreshed.");
    }

    private async Task TryLoadManifestDirectoryAsync(GitHubClient github, string owner, string repo, string branch, string source, bool ownedOnly, CancellationToken cancellationToken)
    {
        try
        {
            await LoadManifestDirectoryAsync(github, owner, repo, branch, source, ownedOnly, cancellationToken);
        }
        catch (Exception ex)
        {
            Log($"Could not load {source} manifests from {owner}/{repo}:{settings.ManifestFolder}. {BuildGitHubHint(ex.Message)}");
        }
    }

    private async Task LoadManifestDirectoryAsync(GitHubClient github, string owner, string repo, string branch, string source, bool ownedOnly, CancellationToken cancellationToken)
    {
        var files = await github.ListDirectoryContentsAsync(owner, repo, settings.ManifestFolder, branch, cancellationToken);
        var jsonFiles = files.Where(x => string.Equals(x.Type, "file", StringComparison.OrdinalIgnoreCase) && x.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).ToList();
        var loaded = 0;
        foreach (var file in jsonFiles)
        {
            try
            {
                var content = await github.ReadTextFileAsync(owner, repo, file.Path, branch, cancellationToken);
                var item = ParseBrowserManifest(source, owner, repo, content.Path, content.Sha, content.Text, content.HtmlUrl);
                if (ownedOnly && !BrowserItemBelongsToCurrentUser(item)) continue;
                browserItems.Add(item);
                loaded++;
            }
            catch (Exception ex)
            {
                Log($"Skipped {file.Path}: {ex.Message}");
            }
        }
        Log(ownedOnly
            ? $"Loaded {loaded} owned/submitted manifests from {owner}/{repo}:{settings.ManifestFolder}."
            : $"Loaded {loaded} manifests from {owner}/{repo}:{settings.ManifestFolder}.");
    }

    private BrowserModItem ParseBrowserManifest(string source, string owner, string repo, string path, string sha, string json, string htmlUrl)
    {
        var node = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
        var artifact = node["artifacts"] is JsonArray artifacts && artifacts.Count > 0 ? artifacts[0] : null;
        return new BrowserModItem
        {
            Source = source,
            Owner = owner,
            Repo = repo,
            ManifestPath = path,
            ManifestSha = sha,
            ManifestJson = json,
            ModId = node["id"]?.GetValue<string>() ?? Path.GetFileNameWithoutExtension(path),
            DisplayName = node["displayName"]?.GetValue<string>() ?? "",
            Version = artifact?["version"]?.GetValue<string>() ?? "",
            ArtifactType = artifact?["type"]?.GetValue<string>() ?? "",
            GameVersion = artifact?["gameVersion"]?.GetValue<string>() ?? "",
            Category = artifact?["category"]?.GetValue<string>() ?? "",
            FileName = artifact?["fileName"]?.GetValue<string>() ?? "",
            DownloadUrl = artifact?["downloadUrl"]?.GetValue<string>() ?? "",
            Hash = artifact?["hash"]?.GetValue<string>() ?? "",
            HtmlUrl = htmlUrl
        };
    }

    private bool BrowserItemBelongsToCurrentUser(BrowserModItem item)
    {
        var login = accountBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(login) && string.Equals(item.Owner, login, StringComparison.OrdinalIgnoreCase) && string.Equals(item.Source, "Release", StringComparison.OrdinalIgnoreCase)) return true;

        if (string.IsNullOrWhiteSpace(item.ManifestJson)) return false;

        try
        {
            var node = JsonNode.Parse(item.ManifestJson)?.AsObject();
            if (node is null) return false;

            var githubOwner = node["githubOwner"]?.GetValue<string>() ?? "";
            if (!string.IsNullOrWhiteSpace(login) && string.Equals(githubOwner, login, StringComparison.OrdinalIgnoreCase)) return true;

            var authors = JoinJsonArray(node["authors"]);
            if (!string.IsNullOrWhiteSpace(authors) && AuthorsContainCurrentIdentity(authors)) return true;

            var artifact = node["artifacts"] is JsonArray artifacts && artifacts.Count > 0 ? artifacts[0] : null;
            var downloadUrl = artifact?["downloadUrl"]?.GetValue<string>() ?? "";
            if (!string.IsNullOrWhiteSpace(login) && downloadUrl.Contains($"github.com/{login}/", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    private void ApplyModBrowserFilter()
    {
        if (modGrid is null) return;
        var query = browserSearchBox?.Text.Trim() ?? "";
        var visible = browserItems.Where(item => string.IsNullOrWhiteSpace(query) || BrowserText(item).Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        modGrid.Rows.Clear();
        foreach (var item in visible)
        {
            var rowIndex = modGrid.Rows.Add(item.Source, item.ModId, item.Version, item.ArtifactType, item.GameVersion, item.FileName, $"{item.Owner}/{item.Repo}", string.IsNullOrWhiteSpace(item.ManifestPath) ? item.HtmlUrl : item.ManifestPath);
            modGrid.Rows[rowIndex].Tag = item;
        }
        browserCountLabel.Text = $"{visible.Count} shown / {browserItems.Count} loaded";
    }

    private static string BrowserText(BrowserModItem item)
    {
        return $"{item.Source} {item.Owner} {item.Repo} {item.ModId} {item.DisplayName} {item.Version} {item.ArtifactType} {item.GameVersion} {item.Category} {item.FileName} {item.ManifestPath} {item.DownloadUrl}";
    }

    private BrowserModItem GetSelectedBrowserItem()
    {
        if (modGrid.SelectedRows.Count == 0) throw new InvalidOperationException("Select a mod in the Mod Browser first.");
        return modGrid.SelectedRows[0].Tag as BrowserModItem ?? throw new InvalidOperationException("The selected row has no mod data.");
    }

    private async Task LoadSelectedModAsync()
    {
        var item = GetSelectedBrowserItem();
        if (!string.IsNullOrWhiteSpace(item.ManifestJson))
        {
            if (ManifestLooksForeign(item.ManifestJson))
            {
                generatedManifest = "";
                manifestPreviewBox.Text = item.ManifestJson;
                Log($"Inspected foreign manifest only: {item.ModId}. Publishing fields were not changed.");
                SetStatus("Foreign manifest opened read-only.");
                await Task.CompletedTask;
                return;
            }

            LoadManifestIntoEditor(item.ManifestJson);
            editorLoadedFromBrowserManifest = true;
            generatedManifest = item.ManifestJson;
            manifestPreviewBox.Text = item.ManifestJson;
            Log($"Loaded own manifest into editor: {item.ModId}");
        }
        else
        {
            if (!string.Equals(item.Source, "Release", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Only your own GitHub release rows can be loaded as release editor state.");
            await LoadReleaseIntoEditorAsync(item);
        }
        SetStatus("Selected mod loaded.");
        await Task.CompletedTask;
    }

    private async Task LoadReleaseIntoEditorAsync(BrowserModItem item)
    {
        var modId = InferModIdFromRelease(item.DisplayName, item.Version, item.FileName);
        var version = NormalizeReleaseVersion(item.Version);
        modIdBox.Text = modId;
        displayNameBox.Text = BuildReleaseDisplayName(item.DisplayName, modId, item.Version);
        versionBox.Text = version;
        releaseTagBox.Text = string.IsNullOrWhiteSpace(version) ? item.Version : $"v{version}";
        fileNameBox.Text = item.FileName;
        downloadUrlBox.Text = item.DownloadUrl;
        hashBox.Text = item.Hash;
        infoUrlBox.Text = item.HtmlUrl;
        releaseRepoBox.Text = item.Repo;
        SetCombo(artifactTypeBox, "plugin");
        SetCombo(categoryBox, "Release");
        dependenciesBox.Text = "";
        incompatibilitiesBox.Text = "";
        extendsIdBox.Text = "";
        extendsVersionBox.Text = "";
        generatedManifest = "";
        manifestPreviewBox.Text = "";
        editorLoadedFromBrowserManifest = false;

        var savedDescription = GetSavedDescription(modId);
        if (!string.IsNullOrWhiteSpace(savedDescription))
        {
            descriptionBox.Text = savedDescription;
        }
        else
        {
            descriptionBox.Text = "";
            await TryPullCatalogDescriptionForModAsync(modId, false, false);
        }

        if (string.IsNullOrWhiteSpace(tagsBox.Text) || tagsBox.Text.Contains("blueprinter", StringComparison.OrdinalIgnoreCase)) tagsBox.Text = "QoL,Utility";
        if (string.IsNullOrWhiteSpace(authorsBox.Text) || authorsBox.Text.Contains("nikkorap", StringComparison.OrdinalIgnoreCase)) authorsBox.Text = "Anomie";
        Log($"Loaded release into editor: {modId} {item.Version}");
    }

    private void LoadManifestIntoEditor(string json)
    {
        var node = JsonNode.Parse(json)?.AsObject() ?? throw new InvalidOperationException("Invalid manifest JSON.");
        modIdBox.Text = node["id"]?.GetValue<string>() ?? "";
        displayNameBox.Text = node["displayName"]?.GetValue<string>() ?? "";
        descriptionBox.Text = node["description"]?.GetValue<string>() ?? "";
        RememberDescriptionForCurrentMod();
        tagsBox.Text = JoinJsonArray(node["tags"]);
        authorsBox.Text = JoinJsonArray(node["authors"]);
        infoUrlBox.Text = ExtractInfoUrl(node["urls"]);
        if (node["autoUpdateArtifacts"] is not null) autoUpdateBox.Checked = string.Equals(node["autoUpdateArtifacts"]?.GetValue<string>(), "True", StringComparison.OrdinalIgnoreCase);

        var artifact = node["artifacts"] is JsonArray artifacts && artifacts.Count > 0 ? artifacts[0] : null;
        if (artifact is null) return;
        SetCombo(artifactTypeBox, artifact["type"]?.GetValue<string>() ?? "plugin");
        fileNameBox.Text = artifact["fileName"]?.GetValue<string>() ?? "";
        downloadUrlBox.Text = artifact["downloadUrl"]?.GetValue<string>() ?? "";
        hashBox.Text = artifact["hash"]?.GetValue<string>() ?? "";
        gameVersionBox.Text = artifact["gameVersion"]?.GetValue<string>() ?? "";
        versionBox.Text = artifact["version"]?.GetValue<string>() ?? "";
        releaseTagBox.Text = string.IsNullOrWhiteSpace(versionBox.Text) ? "" : $"v{versionBox.Text}";
        SetCombo(categoryBox, artifact["category"]?.GetValue<string>() ?? "Release");
        dependenciesBox.Text = RelationsToText(artifact["dependencies"]);
        incompatibilitiesBox.Text = RelationsToText(artifact["incompatibilities"]);
        if (artifact["extends"] is JsonObject ext)
        {
            extendsIdBox.Text = ext["id"]?.GetValue<string>() ?? "";
            extendsVersionBox.Text = ext["version"]?.GetValue<string>() ?? "";
        }
        else
        {
            extendsIdBox.Text = "";
            extendsVersionBox.Text = "";
        }
    }

    private bool ManifestLooksForeign(string json)
    {
        try
        {
            var node = JsonNode.Parse(json)?.AsObject();
            if (node is null) return true;
            var login = accountBox.Text.Trim();
            var owner = node["githubOwner"]?.GetValue<string>() ?? "";
            var authors = JoinJsonArray(node["authors"]);
            if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(login) && !string.Equals(owner, login, StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.IsNullOrWhiteSpace(authors) && !AuthorsContainCurrentIdentity(authors)) return true;
            return false;
        }
        catch
        {
            return true;
        }
    }

    private bool AuthorsContainCurrentIdentity(string manifestAuthors)
    {
        var login = accountBox.Text.Trim();
        var currentAuthors = authorsBox?.Text ?? settings.Authors;
        var accepted = SplitIdentityList(currentAuthors).ToList();
        if (!string.IsNullOrWhiteSpace(login)) accepted.Add(login);
        if (accepted.Count == 0) return false;

        foreach (var manifestAuthor in SplitIdentityList(manifestAuthors))
        {
            if (accepted.Any(known => string.Equals(known, manifestAuthor, StringComparison.OrdinalIgnoreCase))) return true;
        }
        return false;
    }

    private static IEnumerable<string> SplitIdentityList(string value)
    {
        return (value ?? string.Empty)
            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }

    private void EnsureReleaseHashForManifest()
    {
        if (!string.IsNullOrWhiteSpace(hashBox.Text)) return;
        var zip = outputZipBox.Text.Trim();
        if (!File.Exists(zip)) throw new InvalidOperationException("Release hash is required, but no local ZIP exists to hash. Upload/build the release ZIP first.");
        var digest = ComputeSha256Digest(zip);
        hashBox.Text = digest;
        SaveSettingsFromUi();
        Log($"Release hash calculated locally: {digest}");
    }

    private static string ComputeSha256Digest(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private void ValidatePublishTargetForSelectedMod()
    {
        SaveSettingsFromUi();
        if (string.IsNullOrWhiteSpace(settings.ModId)) throw new InvalidOperationException("Mod ID is empty. Select your DLL and read metadata first.");
        if (string.IsNullOrWhiteSpace(settings.ReleaseRepo)) releaseRepoBox.Text = DefaultReleaseRepository;
        if (IsForeignReleaseRepoForMod(releaseRepoBox.Text, settings.ModId))
        {
            throw new InvalidOperationException($"Release repository '{releaseRepoBox.Text}' does not match the selected mod '{settings.ModId}'. This is a safety stop to prevent uploading your DLL into another mod's repo. Set Release Repository to '{DefaultReleaseRepository}' or to a repo name that clearly matches your mod, then try again.");
        }
    }

    private void ValidateManifestInputAgainstPublishTarget(ManifestInput input)
    {
        if (string.IsNullOrWhiteSpace(input.DownloadUrl)) throw new InvalidOperationException("Download URL is required.");
        if (string.IsNullOrWhiteSpace(input.Hash)) throw new InvalidOperationException("Release hash is required. Upload the release asset first or keep the generated ZIP so the tool can calculate sha256.");
        if (!input.Hash.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Release hash must use NOMNOM/GitHub digest format: sha256:<hex>.");
        if (string.IsNullOrWhiteSpace(input.GitHubRepoName)) throw new InvalidOperationException("Release repository is required.");
        if (IsForeignReleaseRepoForMod(input.GitHubRepoName, input.ModId))
        {
            throw new InvalidOperationException($"Manifest blocked: release repo '{input.GitHubRepoName}' does not match mod '{input.ModId}'. This prevents mixed manifests like Cargo Request metadata inside a Railgun repo.");
        }
        if (!string.IsNullOrWhiteSpace(input.GitHubOwner) && input.DownloadUrl.Contains("github.com/", StringComparison.OrdinalIgnoreCase))
        {
            var expected = $"github.com/{input.GitHubOwner}/{input.GitHubRepoName}/releases/download/";
            if (!input.DownloadUrl.Contains(expected, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Manifest blocked: Download URL does not point to {input.GitHubOwner}/{input.GitHubRepoName}. Current URL: {input.DownloadUrl}");
            }
        }
        if (!string.IsNullOrWhiteSpace(input.FileName) && !input.DownloadUrl.EndsWith(input.FileName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Manifest blocked: fileName '{input.FileName}' does not match the Download URL asset name.");
        }
    }

    private static bool IsForeignReleaseRepoForMod(string repo, string modId)
    {
        if (string.IsNullOrWhiteSpace(repo)) return true;
        if (string.Equals(repo.Trim(), DefaultReleaseRepository, StringComparison.OrdinalIgnoreCase)) return false;
        var normalizedRepo = BuildSafeBranchPart(repo);
        var normalizedMod = BuildSafeBranchPart(modId);
        if (string.IsNullOrWhiteSpace(normalizedRepo) || string.IsNullOrWhiteSpace(normalizedMod)) return true;
        var compactRepo = normalizedRepo.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        var compactMod = normalizedMod.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        return !compactRepo.Contains(compactMod, StringComparison.OrdinalIgnoreCase) && !compactMod.Contains(compactRepo, StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeForeignBlueprinterText(string text)
    {
        return text.Contains("railgun", StringComparison.OrdinalIgnoreCase)
            || text.Contains("155mm", StringComparison.OrdinalIgnoreCase)
            || text.Contains("blueprinter", StringComparison.OrdinalIgnoreCase)
            || text.Contains("school bus", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsForeignInfoUrl(string url)
    {
        return url.Contains("NOBlueprinter", StringComparison.OrdinalIgnoreCase)
            || url.Contains("nikkorap", StringComparison.OrdinalIgnoreCase)
            || url.Contains("155mmRailgun", StringComparison.OrdinalIgnoreCase);
    }

    private static void SetCombo(ComboBox combo, string value)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (string.Equals(combo.Items[i]?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }

    private static string JoinJsonArray(JsonNode? node)
    {
        if (node is not JsonArray array) return "";
        return string.Join(",", array.Select(x => x?.GetValue<string>()).Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string ExtractInfoUrl(JsonNode? node)
    {
        if (node is not JsonArray array) return "";
        foreach (var item in array)
        {
            if (string.Equals(item?["name"]?.GetValue<string>(), "info", StringComparison.OrdinalIgnoreCase)) return item?["url"]?.GetValue<string>() ?? "";
        }
        return array.FirstOrDefault()?["url"]?.GetValue<string>() ?? "";
    }

    private static string RelationsToText(JsonNode? node)
    {
        if (node is not JsonArray array) return "";
        return string.Join(Environment.NewLine, array.Select(x => $"{x?["id"]?.GetValue<string>()}@{x?["version"]?.GetValue<string>()}").Where(x => !string.IsNullOrWhiteSpace(x) && x != "@"));
    }

    private static string StripReleaseTagFromDisplayName(string name, string tag)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(tag)) return name;
        return name.EndsWith(tag, StringComparison.OrdinalIgnoreCase)
            ? name[..^tag.Length].Trim('-', ' ', '_')
            : name;
    }

    private async Task UpdateSelectedModAsync()
    {
        var item = GetSelectedBrowserItem();
        if (!BrowserItemBelongsToCurrentUser(item))
        {
            throw new InvalidOperationException("This Mod Browser entry does not belong to your GitHub account/author identity. It can be inspected, but it cannot be used for publishing or updates.");
        }
        if (!string.IsNullOrWhiteSpace(item.ManifestJson) && ManifestLooksForeign(item.ManifestJson))
        {
            throw new InvalidOperationException("This is a foreign/upstream manifest. It can be inspected, but it will not be used as a template for publishing. Select your DLL and run Full Publish for your own mod.");
        }

        await LoadSelectedModAsync();
        if (File.Exists(dllPathBox.Text.Trim()))
        {
            await ReadDllMetadataAsync();
            await PackageZipAsync();
        }
        if (File.Exists(outputZipBox.Text.Trim())) await UploadReleaseAsync();
        await GenerateManifestAsync(true);
        await PublishManifestPrAsync();
        SetStatus("Selected mod update submitted.");
    }

    private async Task DeleteSelectedReleaseAsync()
    {
        var item = GetSelectedBrowserItem();
        if (item.ReleaseId <= 0 || !string.Equals(item.Source, "Release", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The selected item is not one of your GitHub releases.");
        var github = RequireGitHubClient();
        var login = await EnsureGitHubLoginAsync(github, CancellationToken.None);
        if (!string.Equals(item.Owner, login, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("For safety, the tool only deletes releases from your own selected release repository.");
        var confirm = MessageBox.Show(this, $"Delete release '{item.DisplayName}' / '{item.Version}' from {item.Owner}/{item.Repo}?\n\nThis does not delete the NOMNOM manifest.", "Delete release", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;
        await github.DeleteReleaseAsync(item.Owner, item.Repo, item.ReleaseId, CancellationToken.None);
        Log($"Deleted release {item.Version} from {item.Owner}/{item.Repo}.");
        await RefreshModBrowserAsync();
    }

    private async Task DeleteSelectedManifestPrAsync()
    {
        var item = GetSelectedBrowserItem();
        if (string.IsNullOrWhiteSpace(item.ManifestPath)) throw new InvalidOperationException("The selected item has no manifest path.");
        if (!BrowserItemBelongsToCurrentUser(item)) throw new InvalidOperationException("This manifest does not belong to your GitHub account/author identity. Delete PRs are disabled for foreign catalog entries.");
        var confirm = MessageBox.Show(this, $"Create a pull request that removes this manifest?\n\n{item.ManifestPath}", "Delete manifest PR", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;
        SaveSettingsFromUi();
        var github = RequireGitHubClient();
        var login = await EnsureGitHubLoginAsync(github, CancellationToken.None);
        await github.CreateForkAsync(settings.UpstreamOwner, settings.UpstreamRepo, CancellationToken.None);
        await github.WaitForForkAsync(login, settings.UpstreamRepo, Log, CancellationToken.None);
        var baseSha = await github.GetBranchShaAsync(login, settings.UpstreamRepo, settings.UpstreamBranch, CancellationToken.None);
        var branch = BuildSafeBranchName("anomie-ui/delete", item.ModId, DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
        await github.EnsureBranchAsync(login, settings.UpstreamRepo, branch, baseSha, CancellationToken.None);
        var sha = await github.GetFileShaAsync(login, settings.UpstreamRepo, item.ManifestPath, branch, CancellationToken.None) ?? throw new InvalidOperationException("Could not find manifest in your fork branch.");
        await github.DeleteFileAsync(login, settings.UpstreamRepo, branch, item.ManifestPath, $"Remove {item.ModId} manifest", sha, CancellationToken.None);
        var prUrl = await github.CreatePullRequestAsync(settings.UpstreamOwner, settings.UpstreamRepo, $"Remove {item.ModId}", $"Removes NOMNOM manifest `{item.ManifestPath}`.\n\nCreated with Anomie UI NOMNOM Publisher.", $"{login}:{branch}", settings.UpstreamBranch, CancellationToken.None);
        Log($"Deletion pull request opened: {prUrl}");
        if (!string.IsNullOrWhiteSpace(prUrl)) Process.Start(new ProcessStartInfo(prUrl) { UseShellExecute = true });
        SetStatus("Deletion PR submitted.");
    }

    private async Task OpenSelectedModUrlAsync()
    {
        var item = GetSelectedBrowserItem();
        var url = !string.IsNullOrWhiteSpace(item.HtmlUrl) ? item.HtmlUrl : item.DownloadUrl;
        if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("The selected item has no URL to open.");
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        await Task.CompletedTask;
    }

    private static string BuildGitHubHint(string message)
    {
        if (message.Contains("404", StringComparison.OrdinalIgnoreCase) || message.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
        {
            return "GitHub returned 404. Check owner/repo/branch/folder. Current public NOMNOM target is KopterBuzz/NOMNOM.";
        }
        if (message.Contains("already_exists", StringComparison.OrdinalIgnoreCase))
        {
            return "GitHub says the asset name already exists. The uploader will now retry replacement automatically; if it still fails, use a new release tag or delete the asset manually.";
        }
        return message;
    }

    private async Task RunActionAsync(Func<Task> action)
    {
        try
        {
            SetBusy(true);
            SaveSettingsFromUi();
            settings.Save(settingsPath);
            await action();
            SaveSettingsFromUi();
            settings.Save(settingsPath);
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            SetStatus("Error. Check Run Log.");
            MessageBox.Show(this, ex.Message, "Anomie UI", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        foreach (var button in actionButtons) button.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void SetStatus(string text)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetStatus(text));
            return;
        }
        statusLabel.Text = text;
    }

    private void Log(string text)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(text));
            return;
        }
        var line = $"[{DateTime.Now:HH:mm:ss}] {text}";
        logBox.AppendText(line + Environment.NewLine);
        statusLabel.Text = text;
    }

    private static string FriendlyName(string name)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsWhiteSpace(name[i - 1])) builder.Append(' ');
            builder.Append(c == '.' || c == '_' || c == '-' ? ' ' : c);
        }
        return builder.ToString().Trim();
    }

    private static void ApplyThemeRecursive(Control control)
    {
        foreach (Control child in control.Controls)
        {
            switch (child)
            {
                case TabControl tabs:
                    tabs.BackColor = AnomieTheme.Background;
                    tabs.ForeColor = AnomieTheme.Text;
                    break;
                case TabPage page:
                    page.BackColor = AnomieTheme.Background;
                    page.ForeColor = AnomieTheme.Text;
                    break;
                case Label label when label.BackColor == SystemColors.Control:
                    label.BackColor = Color.Transparent;
                    break;
                case TableLayoutPanel table:
                    table.BackColor = AnomieTheme.Background;
                    break;
                case FlowLayoutPanel flow:
                    flow.BackColor = Color.Transparent;
                    break;
            }
            ApplyThemeRecursive(child);
        }
    }
}
