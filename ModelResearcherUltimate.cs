// ======================
// ModelResearcherUltimate - By Nazar Okruzhko
// ======================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace ModelResearcher
{
    public class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class Vector3
    {
        public float X, Y, Z;
        public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator *(Vector3 a, float s) => new Vector3(a.X * s, a.Y * s, a.Z * s);
        public float Length() => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public Vector3 Normalize() { float l = Length(); return l > 0 ? new Vector3(X / l, Y / l, Z / l) : this; }
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        public static float Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    public class Vector2
    {
        public float X, Y;
        public Vector2(float x, float y) { X = x; Y = y; }
    }

    public class MeshConfig
    {
        public string Name { get; set; }
        public int VertOffset { get; set; }
        public int VertCount { get; set; }
        public int VertInter { get; set; }
        public string VertType { get; set; }
        public string VertFormat { get; set; }
        public int FaceOffset { get; set; }
        public int FaceCount { get; set; }
        public int FaceInter { get; set; }
        public string FaceType { get; set; }
        public string FaceFormat { get; set; }
        public int UVOffset { get; set; }
        public int UVCount { get; set; }
        public int UVInter { get; set; }
        public string UVType { get; set; }
        public string UVFormat { get; set; }
    }

    public class MainForm : Form
    {
        // Windows API for optimizing RichTextBox updates
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0x000B;
        
        // ---- UI controls / fields ----
        private Panel configPanel, hexPanel, viewerPanel, objPanel;
        private TabControl configTabs;
        private TabPage meshTab, normalsTab, scriptTab;
        private NumericUpDown vertOffsetNum, vertCountNum, vertInterNum;
        private ComboBox vertTypeCombo, vertFormatCombo;
        private NumericUpDown faceOffsetNum, faceCountNum, faceInterNum;
        private ComboBox faceTypeCombo, faceFormatCombo;
        private NumericUpDown uvOffsetNum, uvCountNum, uvInterNum;
        private ComboBox uvTypeCombo, uvFormatCombo;
        private NumericUpDown normOffsetNum, normCountNum, normInterNum;
        private ComboBox normTypeCombo, normFormatCombo;
        private CheckBox autoCalcCheck, invertXCheck, invertYCheck, invertZCheck;
        private RadioButton littleEndianRadio, bigEndianRadio;
        private Button printBtn, renderBtn, viewUVsBtn;
        private MenuStrip menuStrip;
        private RichTextBox hexTextBox, objTextBox;
        
        // Meshes
        private ComboBox meshesCombo;
        private List<MeshConfig> meshConfigs = new List<MeshConfig>();
        
        // HEX Viewer UI
        private Panel hexFloatIndicator, hexShortIndicator, hexHalfFloatIndicator, hexShortSignedIndicator;
        private Label hexFloatLabel, hexShortLabel, hexHalfFloatLabel, hexShortSignedLabel, hexAddressLabel;
        private CheckBox highlightVerticesCheck, highlightFacesCheck, highlightUVsCheck, highlightNormalsCheck;
        private int selectedByteOffset = -1;
        private int lastCursorPosition = -1;
        
        // UV Map display
        private PictureBox uvMapBox;

        // ---- OpenGL specific fields ----
        private GLControl glControl;
        private bool glLoaded = false;
        private Color polygonsColor = Color.FromArgb(3, 103, 124);
        private bool wireframe = false;
        private float rotationX = -30, rotationY = 45, zoom = 150.0f;
        private Point lastMousePos;
        private Vector3 cameraTarget = new Vector3(0, 0, 0);

        // ---- Model data ----
        private byte[] fileData;
        private string currentFilePath;
        private List<Vector3> vertices = new List<Vector3>();
        private List<int> faces = new List<int>();
        private List<Vector2> uvs = new List<Vector2>();
        private List<Vector3> normals = new List<Vector3>();

        public MainForm()
        {
            Text = "Model Researcher Ultimate - By Nazar Okruzhko";
            Size = new Size(1600, 900);
            BackColor = Color.White;
            AllowDrop = true;
            DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            DragDrop += MainForm_DragDrop;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            menuStrip = new MenuStrip { BackColor = Color.FromArgb(240, 240, 240) };
            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Open", null, (s, e) => OpenFile());
            fileMenu.DropDownItems.Add("Save OBJ", null, (s, e) => SaveOBJ());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => Close());

            var viewMenu = new ToolStripMenuItem("View");
            viewMenu.DropDownItems.Add("Polygons Color", null, (s, e) => ChangePolygonsColor());
            var wireItem = new ToolStripMenuItem("Wireframe", null, (s, e) => { wireframe = !wireframe; RenderScene(); }) { Checked = false };
            viewMenu.DropDownItems.Add(wireItem);

            var helpMenu = new ToolStripMenuItem("Help");
            helpMenu.DropDownItems.Add("About", null, (s, e) => MessageBox.Show("Model Researcher Ultimate by Nazar Okruzhko 2025", "About"));

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, viewMenu, helpMenu });
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);

            // Panels
            configPanel = new Panel { Location = new Point(0, menuStrip.Height), Size = new Size(280, ClientSize.Height - menuStrip.Height), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left };
            hexPanel = new Panel { Location = new Point(280, menuStrip.Height), Size = new Size(575, ClientSize.Height - menuStrip.Height), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left };
            viewerPanel = new Panel { Location = new Point(855, menuStrip.Height), Size = new Size(425, ClientSize.Height - menuStrip.Height), BackColor = Color.FromArgb(102, 102, 102), BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            objPanel = new Panel { Location = new Point(1280, menuStrip.Height), Size = new Size(320, ClientSize.Height - menuStrip.Height), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right };
            Controls.AddRange(new Control[] { configPanel, hexPanel, viewerPanel, objPanel });

            // Labels
            configPanel.Controls.Add(new Label { Text = "Config", Font = new Font("Arial", 14, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true });
            hexPanel.Controls.Add(new Label { Text = "HEX Viewer", Font = new Font("Arial", 14, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true });
            viewerPanel.Controls.Add(new Label { Text = "3D Viewer", Font = new Font("Arial", 20, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true, ForeColor = Color.White });
            objPanel.Controls.Add(new Label { Text = "OBJ File", Font = new Font("Arial", 14, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true });

            // Config tabs
            configTabs = new TabControl { Location = new Point(5, 40), Size = new Size(265, ClientSize.Height - menuStrip.Height - 50), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            configPanel.Controls.Add(configTabs);

            CreateMeshTab();
            CreateNormalsTab();
            CreateScriptTab();
            CreateHexViewer();
            CreateOBJViewer();
            CreateViewport();
        }

        // Mesh tab
        private void CreateMeshTab()
        {
            meshTab = new TabPage("Mesh");
            int y = 5;

            // VERTICES section
            var vg = new GroupBox { Text = "Vertices", Location = new Point(5, y), Size = new Size(250, 90), ForeColor = Color.FromArgb(0, 70, 213) };
            vg.Controls.Add(new Label { Text = "Offset:", Location = new Point(8, 18), Size = new Size(45, 15), ForeColor = Color.Black });
            vertOffsetNum = new NumericUpDown { Location = new Point(55, 16), Size = new Size(70, 20), Maximum = int.MaxValue, Hexadecimal = true };
            vg.Controls.Add(vertOffsetNum);
            vg.Controls.Add(new Label { Text = "Type:", Location = new Point(130, 18), Size = new Size(35, 15), ForeColor = Color.Black });
            vertTypeCombo = new ComboBox { Location = new Point(165, 16), Size = new Size(75, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            vertTypeCombo.Items.AddRange(new[] { "Float", "Half_Float", "Short_Signed" });
            vertTypeCombo.SelectedIndex = 0;
            vg.Controls.Add(vertTypeCombo);

            vg.Controls.Add(new Label { Text = "Count:", Location = new Point(8, 40), Size = new Size(45, 15), ForeColor = Color.Black });
            vertCountNum = new NumericUpDown { Location = new Point(55, 38), Size = new Size(70, 20), Maximum = 999999 };
            vg.Controls.Add(vertCountNum);
            vg.Controls.Add(new Label { Text = "Form:", Location = new Point(130, 40), Size = new Size(35, 15), ForeColor = Color.Black });
            vertFormatCombo = new ComboBox { Location = new Point(165, 38), Size = new Size(75, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            vertFormatCombo.Items.AddRange(new[] { "XYZ", "XZY", "YXZ", "YZX", "ZXY", "ZYX" });
            vertFormatCombo.SelectedIndex = 0;
            vg.Controls.Add(vertFormatCombo);

            vg.Controls.Add(new Label { Text = "Padding:", Location = new Point(8, 62), Size = new Size(45, 15), ForeColor = Color.Black });
            vertInterNum = new NumericUpDown { Location = new Point(55, 60), Size = new Size(70, 20), Maximum = 999 };
            vg.Controls.Add(vertInterNum);
            meshTab.Controls.Add(vg);
            y += 97;

            // UVs section
            var ug = new GroupBox { Text = "UVs", Location = new Point(5, y), Size = new Size(250, 90), ForeColor = Color.FromArgb(0, 70, 213) };
            ug.Controls.Add(new Label { Text = "Offset:", Location = new Point(8, 18), Size = new Size(45, 15), ForeColor = Color.Black });
            uvOffsetNum = new NumericUpDown { Location = new Point(55, 16), Size = new Size(70, 20), Maximum = int.MaxValue, Hexadecimal = true };
            ug.Controls.Add(uvOffsetNum);
            ug.Controls.Add(new Label { Text = "Type:", Location = new Point(130, 18), Size = new Size(35, 15), ForeColor = Color.Black });
            uvTypeCombo = new ComboBox { Location = new Point(165, 16), Size = new Size(75, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            uvTypeCombo.Items.AddRange(new[] { "Float", "Half_Float", "Short_Signed" });
            uvTypeCombo.SelectedIndex = 0;
            ug.Controls.Add(uvTypeCombo);

            ug.Controls.Add(new Label { Text = "Count:", Location = new Point(8, 40), Size = new Size(45, 15), ForeColor = Color.Black });
            uvCountNum = new NumericUpDown { Location = new Point(55, 38), Size = new Size(70, 20), Maximum = 999999 };
            ug.Controls.Add(uvCountNum);
            ug.Controls.Add(new Label { Text = "Form:", Location = new Point(130, 40), Size = new Size(35, 15), ForeColor = Color.Black });
            uvFormatCombo = new ComboBox { Location = new Point(165, 38), Size = new Size(75, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            uvFormatCombo.Items.AddRange(new[] { "UV", "VU" });
            uvFormatCombo.SelectedIndex = 0;
            ug.Controls.Add(uvFormatCombo);

            ug.Controls.Add(new Label { Text = "Padding:", Location = new Point(8, 62), Size = new Size(45, 15), ForeColor = Color.Black });
            uvInterNum = new NumericUpDown { Location = new Point(55, 60), Size = new Size(70, 20), Maximum = 999 };
            ug.Controls.Add(uvInterNum);
            meshTab.Controls.Add(ug);
            y += 97;

            // FACES section
            var fg = new GroupBox { Text = "Faces", Location = new Point(5, y), Size = new Size(250, 90), ForeColor = Color.FromArgb(0, 70, 213) };
            fg.Controls.Add(new Label { Text = "Offset:", Location = new Point(8, 18), Size = new Size(45, 15), ForeColor = Color.Black });
            faceOffsetNum = new NumericUpDown { Location = new Point(55, 16), Size = new Size(70, 20), Maximum = int.MaxValue, Hexadecimal = true };
            fg.Controls.Add(faceOffsetNum);
            fg.Controls.Add(new Label { Text = "Type:", Location = new Point(130, 18), Size = new Size(35, 15), ForeColor = Color.Black });
            faceTypeCombo = new ComboBox { Location = new Point(165, 16), Size = new Size(75, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            faceTypeCombo.Items.AddRange(new[] { "Short", "Integer", "Byte" });
            faceTypeCombo.SelectedIndex = 0;
            fg.Controls.Add(faceTypeCombo);

            fg.Controls.Add(new Label { Text = "Count:", Location = new Point(8, 40), Size = new Size(45, 15), ForeColor = Color.Black });
            faceCountNum = new NumericUpDown { Location = new Point(55, 38), Size = new Size(70, 20), Maximum = 999999 };
            fg.Controls.Add(faceCountNum);
            fg.Controls.Add(new Label { Text = "Form:", Location = new Point(130, 40), Size = new Size(35, 15), ForeColor = Color.Black });
            faceFormatCombo = new ComboBox { Location = new Point(165, 38), Size = new Size(75, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            faceFormatCombo.Items.AddRange(new[] { "Quads", "Triangles", "TStrip", "TStripFF" });
            faceFormatCombo.SelectedIndex = 1;
            fg.Controls.Add(faceFormatCombo);

            fg.Controls.Add(new Label { Text = "Padding:", Location = new Point(8, 62), Size = new Size(45, 15), ForeColor = Color.Black });
            faceInterNum = new NumericUpDown { Location = new Point(55, 60), Size = new Size(70, 20), Maximum = 999 };
            fg.Controls.Add(faceInterNum);
            meshTab.Controls.Add(fg);
            y += 97;

            // MESHES section
            var mg = new GroupBox { Text = "Meshes", Location = new Point(5, y), Size = new Size(250, 50), ForeColor = Color.FromArgb(0, 70, 213) };
            meshesCombo = new ComboBox { Location = new Point(8, 18), Size = new Size(158, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            meshesCombo.Items.Add("Root");
            meshesCombo.SelectedIndex = 0;
            meshesCombo.SelectedIndexChanged += MeshesCombo_SelectedIndexChanged;
            mg.Controls.Add(meshesCombo);
            
            var addMeshBtn = new Button { Text = "+", Location = new Point(170, 17), Size = new Size(35, 22), Font = new Font("Arial", 10, FontStyle.Bold) };
            addMeshBtn.Click += AddMesh_Click;
            mg.Controls.Add(addMeshBtn);
            
            var removeMeshBtn = new Button { Text = "-", Location = new Point(208, 17), Size = new Size(35, 22), Font = new Font("Arial", 10, FontStyle.Bold) };
            removeMeshBtn.Click += RemoveMesh_Click;
            mg.Controls.Add(removeMeshBtn);
            meshTab.Controls.Add(mg);
            y += 57;

            var bg = new GroupBox { Text = "Endian", Location = new Point(5, y), Size = new Size(120, 50), ForeColor = Color.FromArgb(0, 70, 213) };
            littleEndianRadio = new RadioButton { Text = "Little Endian", Location = new Point(8, 16), Checked = true, AutoSize = true, ForeColor = Color.Black };
            bigEndianRadio = new RadioButton { Text = "Big Endian", Location = new Point(8, 32), AutoSize = true, ForeColor = Color.Black };
            bg.Controls.AddRange(new Control[] { littleEndianRadio, bigEndianRadio });
            meshTab.Controls.Add(bg);

            var ig = new GroupBox { Text = "Invert", Location = new Point(130, y), Size = new Size(120, 50), ForeColor = Color.FromArgb(0, 70, 213) };
            invertXCheck = new CheckBox { Text = "X", Location = new Point(8, 16), Size = new Size(30, 20), ForeColor = Color.Black };
            invertYCheck = new CheckBox { Text = "Y", Location = new Point(45, 16), Size = new Size(30, 20), ForeColor = Color.Black };
            invertZCheck = new CheckBox { Text = "Z", Location = new Point(82, 16), Size = new Size(30, 20), ForeColor = Color.Black };
            ig.Controls.AddRange(new Control[] { invertXCheck, invertYCheck, invertZCheck });
            meshTab.Controls.Add(ig);

            y += 54;

            // Buttons
            printBtn = new Button { Text = "Print", Location = new Point(5, y), Size = new Size(60, 28) };
            printBtn.Click += (s, e) => PrintMeshData();
            renderBtn = new Button { Text = "Render", Location = new Point(70, y), Size = new Size(60, 28) };
            renderBtn.Click += (s, e) => RenderMesh();
            viewUVsBtn = new Button { Text = "View UVs", Location = new Point(135, y), Size = new Size(70, 28) };
            viewUVsBtn.Click += (s, e) => RenderUVMap();
            meshTab.Controls.AddRange(new Control[] { printBtn, renderBtn, viewUVsBtn });

            y += 35;

            // UV Map display
            var uvMapLabel = new Label { Text = "UV Map", Location = new Point(5, y), Size = new Size(60, 15), Font = new Font("Arial", 9, FontStyle.Bold) };
            meshTab.Controls.Add(uvMapLabel);
            y += 20;
            
            uvMapBox = new PictureBox 
            { 
                Location = new Point(5, y), 
                Size = new Size(240, 240), 
                BackColor = Color.FromArgb(225, 225, 225),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            uvMapBox.Paint += UvMapBox_Paint;
            meshTab.Controls.Add(uvMapBox);

            configTabs.TabPages.Add(meshTab);
        }

        // Normals
        private void CreateNormalsTab()
        {
            normalsTab = new TabPage("Normals");
            int y = 5;
            
            // Header text
            var headerLabel = new Label 
            { 
                Text = "The Normals are optional and always can be Auto-Generated", 
                Location = new Point(5, y), 
                Size = new Size(250, 35),
                Font = new Font("Arial", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            normalsTab.Controls.Add(headerLabel);
            y += 40;

            var ng = new GroupBox { Text = "Normals", Location = new Point(5, y), Size = new Size(250, 90), ForeColor = Color.FromArgb(0, 70, 213) };
            ng.Controls.Add(new Label { Text = "Offset:", Location = new Point(8, 18), Size = new Size(45, 15), ForeColor = Color.Black });
            normOffsetNum = new NumericUpDown { Location = new Point(55, 16), Size = new Size(70, 20), Maximum = int.MaxValue, Hexadecimal = true };
            ng.Controls.Add(normOffsetNum);
            ng.Controls.Add(new Label { Text = "Type:", Location = new Point(130, 18), Size = new Size(35, 15), ForeColor = Color.Black });
            normTypeCombo = new ComboBox { Location = new Point(165, 16), Size = new Size(75, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            normTypeCombo.Items.AddRange(new[] { "Float", "Half_Float", "Short_Signed" });
            normTypeCombo.SelectedIndex = 0;
            ng.Controls.Add(normTypeCombo);

            ng.Controls.Add(new Label { Text = "Count:", Location = new Point(8, 40), Size = new Size(45, 15), ForeColor = Color.Black });
            normCountNum = new NumericUpDown { Location = new Point(55, 38), Size = new Size(70, 20), Maximum = 999999 };
            ng.Controls.Add(normCountNum);
            ng.Controls.Add(new Label { Text = "Form:", Location = new Point(130, 40), Size = new Size(35, 15), ForeColor = Color.Black });
            normFormatCombo = new ComboBox { Location = new Point(165, 38), Size = new Size(75, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            normFormatCombo.Items.AddRange(new[] { "XYZ" });
            normFormatCombo.SelectedIndex = 0;
            ng.Controls.Add(normFormatCombo);

            ng.Controls.Add(new Label { Text = "Padding:", Location = new Point(8, 62), Size = new Size(45, 15), ForeColor = Color.Black });
            normInterNum = new NumericUpDown { Location = new Point(55, 60), Size = new Size(70, 20), Maximum = 999 };
            ng.Controls.Add(normInterNum);

            autoCalcCheck = new CheckBox { Text = "Auto calculate", Location = new Point(130, 60), Checked = true, AutoSize = true, ForeColor = Color.Black };
            ng.Controls.Add(autoCalcCheck);

            normalsTab.Controls.Add(ng);
            configTabs.TabPages.Add(normalsTab);
        }

        // Script/Extensions Tab
        private void CreateScriptTab()
        {
            scriptTab = new TabPage("Script");
            scriptTab.Controls.Add(new RichTextBox
            {
                Location = new Point(5, 5),
                Size = new Size(250, 600),
                ReadOnly = true,
                Text = "Script section - For future scripting capabilities.",
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            });
            configTabs.TabPages.Add(scriptTab);
        }

        // Hex Viewer with indicators
        private void CreateHexViewer()
        {
            int y = 40;
            
            hexAddressLabel = new Label 
            { 
                Text = "0x00000000", 
                Location = new Point(8, y), 
                Size = new Size(90, 18),
                Font = new Font("Consolas", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft
            };
            hexPanel.Controls.Add(hexAddressLabel);
            
            var copyBtn = new Button 
            { 
                Text = "Copy", 
                Location = new Point(100, y), 
                Size = new Size(38, 18), 
                Font = new Font("Arial", 6f), 
                FlatStyle = FlatStyle.Flat, 
                Padding = new Padding(0), 
                TextAlign = ContentAlignment.TopCenter
            };
            copyBtn.Click += (s, e) => { if (selectedByteOffset >= 0) Clipboard.SetText($"{selectedByteOffset:X}"); };
            hexPanel.Controls.Add(copyBtn);
            y += 22;
            
            highlightVerticesCheck = new CheckBox { Text = "Vertices", Location = new Point(8, y), Checked = true, AutoSize = true };
            highlightVerticesCheck.CheckedChanged += (s, e) => { RenderScene(); uvMapBox.Invalidate(); };
            highlightUVsCheck = new CheckBox { Text = "UVs", Location = new Point(90, y), Checked = true, AutoSize = true };
            highlightUVsCheck.CheckedChanged += (s, e) => { RenderScene(); uvMapBox.Invalidate(); };
            hexPanel.Controls.AddRange(new Control[] { highlightVerticesCheck, highlightUVsCheck });
            
            hexFloatIndicator = new Panel { Location = new Point(315, y), Size = new Size(12, 12), BackColor = Color.Gray, BorderStyle = BorderStyle.FixedSingle };
            hexFloatLabel = new Label { Text = "Float:", Location = new Point(330, y + 1), Size = new Size(90, 14), Font = new Font("Arial", 8) };
            hexShortIndicator = new Panel { Location = new Point(435, y), Size = new Size(12, 12), BackColor = Color.Gray, BorderStyle = BorderStyle.FixedSingle };
            hexShortLabel = new Label { Text = "Short:", Location = new Point(450, y + 1), Size = new Size(70, 14), Font = new Font("Arial", 8) };
            hexPanel.Controls.AddRange(new Control[] { hexFloatIndicator, hexFloatLabel, hexShortIndicator, hexShortLabel });
            y += 20;
            
            highlightFacesCheck = new CheckBox { Text = "Faces", Location = new Point(8, y), Checked = true, AutoSize = true };
            highlightFacesCheck.CheckedChanged += (s, e) => RenderScene();
            highlightNormalsCheck = new CheckBox { Text = "Normals", Location = new Point(90, y), Checked = true, AutoSize = true };
            hexPanel.Controls.AddRange(new Control[] { highlightFacesCheck, highlightNormalsCheck });
            
            hexHalfFloatIndicator = new Panel { Location = new Point(315, y), Size = new Size(12, 12), BackColor = Color.Gray, BorderStyle = BorderStyle.FixedSingle };
            hexHalfFloatLabel = new Label { Text = "Half-Float:", Location = new Point(330, y + 1), Size = new Size(100, 14), Font = new Font("Arial", 8) };
            hexShortSignedIndicator = new Panel { Location = new Point(435, y), Size = new Size(12, 12), BackColor = Color.Gray, BorderStyle = BorderStyle.FixedSingle };
            hexShortSignedLabel = new Label { Text = "Short Signed:", Location = new Point(450, y + 1), Size = new Size(100, 14), Font = new Font("Arial", 8) };
            hexPanel.Controls.AddRange(new Control[] { hexHalfFloatIndicator, hexHalfFloatLabel, hexShortSignedIndicator, hexShortSignedLabel });
            y += 28;
            
            hexTextBox = new RichTextBox
            {
                Location = new Point(8, y),
                Size = new Size(550, ClientSize.Height - menuStrip.Height - y - 10),
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                DetectUrls = false,
                Text = "# No file loaded",
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            hexTextBox.SelectionChanged += HexTextBox_SelectionChanged;
            hexTextBox.KeyDown += HexTextBox_KeyDown;
            hexPanel.Controls.Add(hexTextBox);
        }
        
        // Arrow key navigation for hex cursor
        private void HexTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (fileData == null) return;
            
            int bytesPerLine = 16;
            int currentByte = selectedByteOffset;
            
            if (currentByte < 0) currentByte = 0; // Initialize if not set
            
            if (e.KeyCode == Keys.Left)
            {
                if (currentByte > 0) currentByte--;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right)
            {
                if (currentByte < fileData.Length - 1) currentByte++;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (currentByte >= bytesPerLine) currentByte -= bytesPerLine;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (currentByte + bytesPerLine < fileData.Length) currentByte += bytesPerLine;
                e.Handled = true;
            }
            
            if (e.Handled && currentByte != selectedByteOffset)
            {
                selectedByteOffset = currentByte;
                hexAddressLabel.Text = $"0x{selectedByteOffset:X8}";
                UpdateHexCursor();
                UpdateHexIndicators();
            }
        }
        
        private void HexTextBox_SelectionChanged(object sender, EventArgs e)
        {
            if (fileData == null || hexTextBox.SelectionStart < 0) return;
            
            string text = hexTextBox.Text;
            int cursorPos = hexTextBox.SelectionStart;
            
            // Find current line
            int lineStart = text.LastIndexOf('\n', Math.Max(0, cursorPos - 1)) + 1;
            int lineEnd = text.IndexOf('\n', cursorPos);
            if (lineEnd == -1) lineEnd = text.Length;
            
            string currentLine = text.Substring(lineStart, lineEnd - lineStart);
            
            // Line format: "00000000  XX XX XX XX XX XX XX XX  XX XX XX XX XX XX XX XX  ................"
            if (currentLine.Length > 10)
            {
                try
                {
                    int lineOffset = Convert.ToInt32(currentLine.Substring(0, 8), 16);
                    int posInLine = cursorPos - lineStart - 10; // Skip "00000000  "
                    
                    if (posInLine >= 0)
                    {
                        int byteIndex = 0;
                        int charsSeen = 0;
                        
                        // Calculate byte index from character position
                        // Each byte is "XX " (3 chars), with extra space after 8th byte
                        while (charsSeen <= posInLine && byteIndex < 16)
                        {
                            if (byteIndex == 8) charsSeen++; // Extra space after 8th byte
                            
                            // If we're within this byte's characters
                            if (charsSeen <= posInLine && posInLine < charsSeen + 2)
                            {
                                // We're on this byte
                                break;
                            }
                            
                            if (charsSeen <= posInLine)
                            {
                                charsSeen += 3; // "XX "
                                if (charsSeen <= posInLine)
                                    byteIndex++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        
                        int newOffset = lineOffset + byteIndex;
                        if (newOffset < fileData.Length && newOffset != selectedByteOffset)
                        {
                            selectedByteOffset = newOffset;
                            hexAddressLabel.Text = $"0x{selectedByteOffset:X8}";
                            UpdateHexCursor();
                            UpdateHexIndicators();
                        }
                    }
                }
                catch { }
            }
        }
        
        // Update light blue hex cursor
        private void UpdateHexCursor()
        {
            if (fileData == null || selectedByteOffset < 0 || selectedByteOffset >= fileData.Length) return;
            
            // Temporarily disable selection changed event
            hexTextBox.SelectionChanged -= HexTextBox_SelectionChanged;
            
            try
            {
                // Clear previous cursor
                if (lastCursorPosition >= 0 && lastCursorPosition < hexTextBox.TextLength - 2)
                {
                    hexTextBox.Select(lastCursorPosition, 2);
                    hexTextBox.SelectionBackColor = Color.FromArgb(240, 240, 240);
                }
                
                // Calculate position in hex display
                int line = selectedByteOffset / 16;
                int byteInLine = selectedByteOffset % 16;
                
                int lineStart = hexTextBox.GetFirstCharIndexFromLine(line);
                if (lineStart < 0) return;
                
                // Position: "00000000  XX XX XX XX XX XX XX XX  XX XX XX XX XX XX XX XX"
                //           ^0        ^10                     ^49
                int charPos = lineStart + 10 + (byteInLine * 3) + (byteInLine >= 8 ? 1 : 0);
                
                if (charPos >= 0 && charPos + 2 <= hexTextBox.TextLength)
                {
                    hexTextBox.Select(charPos, 2);
                    hexTextBox.SelectionBackColor = Color.FromArgb(150, 215, 230); // Light Blue
                    lastCursorPosition = charPos;
                    
                    // Keep the text cursor at the selected position (don't move it to start)
                    hexTextBox.SelectionStart = charPos;
                    hexTextBox.SelectionLength = 0;
                }
            }
            finally
            {
                // Re-enable selection changed event
                hexTextBox.SelectionChanged += HexTextBox_SelectionChanged;
            }
        }
        
        private void UpdateHexIndicators()
        {
            if (fileData == null || selectedByteOffset < 0) return;
            
            try
            {
                bool littleEndian = littleEndianRadio.Checked;
                int offset = selectedByteOffset;
                
                // Float value (4 bytes)
                if (offset + 4 <= fileData.Length)
                {
                    float floatVal = ReadFloat(offset, littleEndian);
                    bool isValid = !float.IsNaN(floatVal) && !float.IsInfinity(floatVal) && floatVal != 0;
                    
                    if (isValid)
                    {
                        float abs = Math.Abs(floatVal);
                        if (abs < 0.00000001f || abs > 99999999f)
                            isValid = false;
                    }
                    
                    hexFloatLabel.Text = $"Float: {floatVal:0.0000}";
                    hexFloatIndicator.BackColor = isValid ? Color.Green : Color.Red;
                }
                else
                {
                    hexFloatLabel.Text = "Float: ---";
                    hexFloatIndicator.BackColor = Color.Gray;
                }
                
                // Short unsigned value (2 bytes)
                if (offset + 2 <= fileData.Length)
                {
                    ushort ushortVal = ReadUShort(offset, littleEndian);
                    bool isValid = ushortVal != 0;
                    
                    hexShortLabel.Text = $"Short: {ushortVal}";
                    hexShortIndicator.BackColor = isValid ? Color.Green : Color.Red;
                }
                else
                {
                    hexShortLabel.Text = "Short: ---";
                    hexShortIndicator.BackColor = Color.Gray;
                }
                
                // Half-float value (2 bytes) - FIXED
                if (offset + 2 <= fileData.Length)
                {
                    ushort ushortVal = ReadUShort(offset, littleEndian);
                    float halfVal = ReadHalfFloat(offset, littleEndian);
                    bool isValid = !float.IsNaN(halfVal) && !float.IsInfinity(halfVal) && halfVal != 0;
                    
                    if (isValid)
                    {
                        float abs = Math.Abs(halfVal);
                        if (abs < 0.00001f || abs > 65000f)
                            isValid = false;
                    }
                    
                    hexHalfFloatLabel.Text = $"Half-Float: ${ushortVal:X4} = {halfVal:0.00}";
                    hexHalfFloatIndicator.BackColor = isValid ? Color.Green : Color.Red;
                }
                else
                {
                    hexHalfFloatLabel.Text = "Half-Float: ---";
                    hexHalfFloatIndicator.BackColor = Color.Gray;
                }
                
                // Short signed value (2 bytes) - FIXED
                if (offset + 2 <= fileData.Length)
                {
                    short shortVal = ReadShort(offset, littleEndian);
                    bool isValid = shortVal != 0;
                    
                    hexShortSignedLabel.Text = $"Short Signed: {shortVal}";
                    hexShortSignedIndicator.BackColor = isValid ? Color.Green : Color.Red;
                }
                else
                {
                    hexShortSignedLabel.Text = "Short Signed: ---";
                    hexShortSignedIndicator.BackColor = Color.Gray;
                }
            }
            catch 
            { 
                hexFloatLabel.Text = "Float: Error";
                hexShortLabel.Text = "Short: Error";
                hexHalfFloatLabel.Text = "Half-Float: Error";
                hexShortSignedLabel.Text = "Short Signed: Error";
                hexFloatIndicator.BackColor = Color.Gray;
                hexShortIndicator.BackColor = Color.Gray;
                hexHalfFloatIndicator.BackColor = Color.Gray;
                hexShortSignedIndicator.BackColor = Color.Gray;
            }
        }

        // OBJ Viewer
        private void CreateOBJViewer()
        {
            objTextBox = new RichTextBox
            {
                Location = new Point(8, 40),
                Size = new Size(295, ClientSize.Height - 60),
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                WordWrap = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = "# No mesh data loaded"
            };
            objPanel.Controls.Add(objTextBox);
        }

        // ---- OpenTK GLControl 3D Renderer ----
        private void CreateViewport()
        {
            glControl = new GLControl(new GraphicsMode(32, 24, 0, 0))
            {
                Location = new Point(8, 40),
                Size = new Size(viewerPanel.Width - 16, viewerPanel.Height - 48),
                BackColor = Color.FromArgb(102, 102, 102),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            glControl.Load += GlControl_Load;
            glControl.Paint += GlControl_Paint;
            glControl.Resize += GlControl_Resize;
            glControl.MouseDown += Viewport_MouseDown;
            glControl.MouseMove += Viewport_MouseMove;
            glControl.MouseWheel += Viewport_MouseWheel;
            viewerPanel.Controls.Add(glControl);
        }

        private void GlControl_Load(object sender, EventArgs e)
        {
            GL.ClearColor(Color.FromArgb(102, 102, 102));
            GL.Enable(EnableCap.DepthTest);
            glLoaded = true;
        }

        private void GlControl_Resize(object sender, EventArgs e)
        {
            if (!glLoaded) return;
            GL.Viewport(0, 0, glControl.Width, glControl.Height);
            glControl.Invalidate();
        }

        private void GlControl_Paint(object sender, PaintEventArgs e)
        {
            if (!glLoaded) return;
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Projection
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            float aspect = glControl.Width / (float)Math.Max(glControl.Height, 1);
            Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), aspect, 0.1f, 5000f);
            GL.LoadMatrix(ref proj);

            // Camera
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            float radX = rotationX * (float)Math.PI / 180f;
            float radY = rotationY * (float)Math.PI / 180f;
            float camX = zoom * (float)Math.Sin(radY) * (float)Math.Cos(radX);
            float camY = zoom * (float)Math.Sin(radX);
            float camZ = zoom * (float)Math.Cos(radY) * (float)Math.Cos(radX);
            Vector3 eye = new Vector3(camX + cameraTarget.X, camY + cameraTarget.Y, camZ + cameraTarget.Z);
            Matrix4 look = Matrix4.LookAt(
                new OpenTK.Vector3(eye.X, eye.Y, eye.Z),
                new OpenTK.Vector3(cameraTarget.X, cameraTarget.Y, cameraTarget.Z),
                OpenTK.Vector3.UnitY
            );
            GL.LoadMatrix(ref look);

            // ---- Black Grid ----
            GL.Disable(EnableCap.Lighting);
            GL.Color3(0, 0, 0);
            GL.Begin(PrimitiveType.Lines);
            for (int i = -10; i <= 10; i++)
            {
                float scale = 10f;
                GL.Vertex3(i * scale, 0, -100);
                GL.Vertex3(i * scale, 0, 100);
                GL.Vertex3(-100, 0, i * scale);
                GL.Vertex3(100, 0, i * scale);
            }
            GL.End();

            // ---- Mesh Drawing with SHADING ----
            if (vertices.Count > 0 && faces.Count > 0 && highlightFacesCheck.Checked)
            {
                GL.Disable(EnableCap.Lighting);
                GL.PolygonMode(MaterialFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
                
                GL.Begin(PrimitiveType.Triangles);
                for (int i = 0; i + 2 < faces.Count; i += 3)
                {
                    int i0 = faces[i], i1 = faces[i + 1], i2 = faces[i + 2];
                    if (i0 < vertices.Count && i1 < vertices.Count && i2 < vertices.Count)
                    {
                        var v0 = vertices[i0];
                        var v1 = vertices[i1];
                        var v2 = vertices[i2];
                        
                        // Calculate face normal
                        Vector3 edge1 = v1 - v0;
                        Vector3 edge2 = v2 - v0;
                        Vector3 normal = Vector3.Cross(edge1, edge2);
                        if (normal.Length() > 0) normal = normal.Normalize();
                        
                        // Simple shading based on normal direction
                        float shade = Math.Abs(normal.Y) * 0.4f + 0.6f;
                        
                        Color shadedColor = Color.FromArgb(
                            (int)(polygonsColor.R * shade),
                            (int)(polygonsColor.G * shade),
                            (int)(polygonsColor.B * shade)
                        );
                        GL.Color3(shadedColor);
                        
                        GL.Vertex3(v0.X, v0.Y, v0.Z);
                        GL.Vertex3(v1.X, v1.Y, v1.Z);
                        GL.Vertex3(v2.X, v2.Y, v2.Z);
                    }
                }
                GL.End();
            }

            // ---- Draw RED vertices as points ----
            if (vertices.Count > 0 && highlightVerticesCheck.Checked)
            {
                GL.PointSize(4.0f);
                GL.Color3(1.0f, 0.0f, 0.0f);
                GL.Begin(PrimitiveType.Points);
                foreach (var v in vertices)
                {
                    GL.Vertex3(v.X, v.Y, v.Z);
                }
                GL.End();
            }

            glControl.SwapBuffers();
        }
        
        // UV Map rendering with error handling
        private void UvMapBox_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.FromArgb(225, 225, 225));
            
            if (uvs.Count == 0 || !highlightUVsCheck.Checked) return;
            
            try
            {
                int w = uvMapBox.Width;
                int h = uvMapBox.Height;
                
                // Draw UV coordinates in blue
                using (Pen bluePen = new Pen(Color.Blue, 2))
                {
                    foreach (var uv in uvs)
                    {
                        float x = uv.X * w;
                        float y = (1.0f - uv.Y) * h;
                        
                        // Check for overflow
                        if (float.IsInfinity(x) || float.IsNaN(x) || float.IsInfinity(y) || float.IsNaN(y))
                            throw new OverflowException("UV coordinates out of range");
                        
                        g.FillEllipse(Brushes.Blue, x - 2, y - 2, 4, 4);
                    }
                }
                
                // Draw lines connecting UVs if we have faces
                if (faces.Count > 0)
                {
                    using (Pen bluePen = new Pen(Color.FromArgb(100, 0, 0, 255), 1))
                    {
                        for (int i = 0; i + 2 < faces.Count; i += 3)
                        {
                            int i0 = faces[i], i1 = faces[i + 1], i2 = faces[i + 2];
                            if (i0 < uvs.Count && i1 < uvs.Count && i2 < uvs.Count)
                            {
                                var uv0 = uvs[i0];
                                var uv1 = uvs[i1];
                                var uv2 = uvs[i2];
                                
                                float x0 = uv0.X * w, y0 = (1.0f - uv0.Y) * h;
                                float x1 = uv1.X * w, y1 = (1.0f - uv1.Y) * h;
                                float x2 = uv2.X * w, y2 = (1.0f - uv2.Y) * h;
                                
                                g.DrawLine(bluePen, x0, y0, x1, y1);
                                g.DrawLine(bluePen, x1, y1, x2, y2);
                                g.DrawLine(bluePen, x2, y2, x0, y0);
                            }
                        }
                    }
                }
            }
            catch (OverflowException)
            {
                // Draw red border to indicate error
                using (Pen redPen = new Pen(Color.Red, 4))
                {
                    g.DrawRectangle(redPen, 2, 2, uvMapBox.Width - 4, uvMapBox.Height - 4);
                }
            }
        }
        
        private void RenderUVMap()
        {
            try
            {
                if (vertices.Count == 0)
                {
                    ParseVertices();
                    ParseFaces();
                }
                ParseUVs();
                
                // Validate UV data before rendering
                bool hasInvalidUVs = false;
                foreach (var uv in uvs)
                {
                    if (float.IsInfinity(uv.X) || float.IsNaN(uv.X) || float.IsInfinity(uv.Y) || float.IsNaN(uv.Y))
                    {
                        hasInvalidUVs = true;
                        break;
                    }
                }
                
                if (hasInvalidUVs)
                {
                    MessageBox.Show("UV data contains invalid values (NaN or Infinity).\n\nPlease check your UV offset and format settings.", 
                        "Invalid UV Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                uvMapBox.Invalidate();
            }
            catch (Exception ex) 
            { 
                MessageBox.Show($"Could not read UV Data\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }

        // ----- Mouse and camera controls -----
        private void Viewport_MouseDown(object sender, MouseEventArgs e)
        {
            lastMousePos = e.Location;
        }
        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.None) return;
            int dx = e.X - lastMousePos.X;
            int dy = e.Y - lastMousePos.Y;
            if (e.Button == MouseButtons.Left)
            {
                rotationY -= dx * 0.5f;
                rotationX += dy * 0.5f;
                rotationX = Math.Max(-89f, Math.Min(89f, rotationX));
                RenderScene();
            }
            else if (e.Button == MouseButtons.Right)
            {
                float panSpeed = zoom * 0.001f;
                float radY = rotationY * (float)Math.PI / 180f;
                float rightX = (float)Math.Cos(radY);
                float rightZ = -(float)Math.Sin(radY);
                cameraTarget.X -= rightX * dx * panSpeed;
                cameraTarget.Z -= rightZ * dx * panSpeed;
                cameraTarget.Y += dy * panSpeed;
                RenderScene();
            }
            lastMousePos = e.Location;
        }
        private void Viewport_MouseWheel(object sender, MouseEventArgs e)
        {
            zoom -= e.Delta * 0.1f;
            zoom = Math.Max(10f, Math.Min(2000f, zoom));
            RenderScene();
        }
        private void RenderScene()
        {
            if (glLoaded && glControl != null) glControl.Invalidate();
        }

        // --- Mesh Management ---
        private void AddMesh_Click(object sender, EventArgs e)
        {
            string meshName = $"Mesh {meshConfigs.Count + 1}";
            var config = new MeshConfig
            {
                Name = meshName,
                VertOffset = (int)vertOffsetNum.Value,
                VertCount = (int)vertCountNum.Value,
                VertInter = (int)vertInterNum.Value,
                VertType = vertTypeCombo.Text,
                VertFormat = vertFormatCombo.Text,
                FaceOffset = (int)faceOffsetNum.Value,
                FaceCount = (int)faceCountNum.Value,
                FaceInter = (int)faceInterNum.Value,
                FaceType = faceTypeCombo.Text,
                FaceFormat = faceFormatCombo.Text,
                UVOffset = (int)uvOffsetNum.Value,
                UVCount = (int)uvCountNum.Value,
                UVInter = (int)uvInterNum.Value,
                UVType = uvTypeCombo.Text,
                UVFormat = uvFormatCombo.Text
            };
            meshConfigs.Add(config);
            meshesCombo.Items.Add(meshName);
            meshesCombo.SelectedIndex = meshesCombo.Items.Count - 1;
        }

        private void RemoveMesh_Click(object sender, EventArgs e)
        {
            if (meshesCombo.SelectedIndex > 0)
            {
                int idx = meshesCombo.SelectedIndex - 1;
                meshConfigs.RemoveAt(idx);
                meshesCombo.Items.RemoveAt(meshesCombo.SelectedIndex);
                meshesCombo.SelectedIndex = 0;
            }
        }

        private void MeshesCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (meshesCombo.SelectedIndex == 0) return;
            
            var config = meshConfigs[meshesCombo.SelectedIndex - 1];
            vertOffsetNum.Value = config.VertOffset;
            vertCountNum.Value = config.VertCount;
            vertInterNum.Value = config.VertInter;
            vertTypeCombo.Text = config.VertType;
            vertFormatCombo.Text = config.VertFormat;
            faceOffsetNum.Value = config.FaceOffset;
            faceCountNum.Value = config.FaceCount;
            faceInterNum.Value = config.FaceInter;
            faceTypeCombo.Text = config.FaceType;
            faceFormatCombo.Text = config.FaceFormat;
            uvOffsetNum.Value = config.UVOffset;
            uvCountNum.Value = config.UVCount;
            uvInterNum.Value = config.UVInter;
            uvTypeCombo.Text = config.UVType;
            uvFormatCombo.Text = config.UVFormat;
        }

        // --- Drag & Drop/load mesh file ---
        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    fileData = File.ReadAllBytes(files[0]);
                    currentFilePath = files[0];
                    Text = $"Model Researcher Ultimate - {Path.GetFileName(files[0])}";
                    ShowHexData();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // --- File Open/Save OBJ ---
        private void OpenFile()
        {
            using (var ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        fileData = File.ReadAllBytes(ofd.FileName);
                        currentFilePath = ofd.FileName;
                        Text = $"Model Researcher Ultimate - {Path.GetFileName(ofd.FileName)}";
                        ShowHexData();
                    }
                    catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
            }
        }
        private void SaveOBJ()
        {
            if (vertices.Count == 0) 
            { 
                MessageBox.Show("No mesh data to save!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
                return; 
            }
            using (var sfd = new SaveFileDialog { Filter = "OBJ Files (*.obj)|*.obj" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, objTextBox.Text);
                }
            }
        }
        private void ChangePolygonsColor()
        {
            using (var cd = new ColorDialog { Color = polygonsColor })
            {
                if (cd.ShowDialog() == DialogResult.OK) { polygonsColor = cd.Color; RenderScene(); }
            }
        }

        // --- Hex viewer logic ---
        private void ShowHexData()
        {
            if (fileData == null) return;
            var sb = new StringBuilder();
            int bytesPerLine = 16, totalLines = Math.Min(fileData.Length / bytesPerLine + 1, 10000);
            for (int line = 0; line < totalLines; line++)
            {
                int offset = line * bytesPerLine;
                if (offset >= fileData.Length) break;
                sb.Append($"{offset:X8}  ");
                for (int i = 0; i < bytesPerLine && offset + i < fileData.Length; i++)
                {
                    sb.Append($"{fileData[offset + i]:X2} ");
                    if (i == 7) sb.Append(" ");
                }
                if (offset + bytesPerLine > fileData.Length)
                {
                    int missing = bytesPerLine - (fileData.Length - offset);
                    for (int i = 0; i < missing; i++) sb.Append("   ");
                    if (fileData.Length - offset <= 8) sb.Append(" ");
                }
                sb.Append(" ");
                for (int i = 0; i < bytesPerLine && offset + i < fileData.Length; i++)
                {
                    byte b = fileData[offset + i];
                    sb.Append((b >= 32 && b < 127) ? (char)b : '.');
                }
                sb.AppendLine();
            }
            hexTextBox.Text = sb.ToString();
            
            // Initialize cursor at byte 0
            selectedByteOffset = 0;
            lastCursorPosition = -1;
            hexAddressLabel.Text = "0x00000000";
            UpdateHexCursor();
            UpdateHexIndicators();
        }

        // --- OBJ window update (FULL EXPORT) ---
        private void UpdateOBJView()
        {
            if (vertices.Count == 0) { objTextBox.Text = "# No mesh data loaded"; return; }
            var sb = new StringBuilder();
            sb.AppendLine("# Exported from Model Researcher Ultimate");
            sb.AppendLine($"# Vertices: {vertices.Count}");
            sb.AppendLine($"# Faces: {faces.Count / 3}");
            sb.AppendLine();
            
            foreach (var v in vertices)
                sb.AppendLine($"v {v.X.ToString(CultureInfo.InvariantCulture)} {v.Y.ToString(CultureInfo.InvariantCulture)} {v.Z.ToString(CultureInfo.InvariantCulture)}");
            
            if (uvs.Count > 0)
            {
                sb.AppendLine();
                foreach (var uv in uvs)
                    sb.AppendLine($"vt {uv.X.ToString(CultureInfo.InvariantCulture)} {uv.Y.ToString(CultureInfo.InvariantCulture)}");
            }
            if (normals.Count > 0)
            {
                sb.AppendLine();
                foreach (var n in normals)
                    sb.AppendLine($"vn {n.X.ToString(CultureInfo.InvariantCulture)} {n.Y.ToString(CultureInfo.InvariantCulture)} {n.Z.ToString(CultureInfo.InvariantCulture)}");
            }
            sb.AppendLine();
            
            for (int i = 0; i < faces.Count; i += 3)
            {
                if (i + 2 < faces.Count)
                    sb.AppendLine($"f {faces[i] + 1} {faces[i + 1] + 1} {faces[i + 2] + 1}");
            }
            objTextBox.Text = sb.ToString();
        }

        private void RenderMesh()
        {
            try
            {
                ParseVertices();
                ParseFaces();
                
                if (faces.Count > 0 && vertices.Count > 0)
                {
                    int maxFaceIndex = faces.Max();
                    if (maxFaceIndex >= vertices.Count)
                    {
                        MessageBox.Show(
                            $"Mismatch of faces and vertices!\n\nMaximum Index of faces: {maxFaceIndex}\nNumber of vertices: {vertices.Count}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                
                if (autoCalcCheck.Checked) CalculateNormals();
                UpdateOBJView();
                RenderScene();
            }
            catch (Exception ex) 
            { 
                MessageBox.Show($"Could not read Mesh Data\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }

        private void PrintMeshData()
        {
            try
            {
                ParseVertices();
                ParseFaces();
                
                if (faces.Count > 0 && vertices.Count > 0)
                {
                    int maxFaceIndex = faces.Max();
                    if (maxFaceIndex >= vertices.Count)
                    {
                        MessageBox.Show(
                            $"Mismatch of faces and vertices!\n\nMaximum Index of faces: {maxFaceIndex}\nNumber of vertices: {vertices.Count}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                
                var sb = new StringBuilder();
                sb.AppendLine("# Exported from Model Researcher Ultimate");
                sb.AppendLine($"# Vertices: {vertices.Count}");
                if (vertices.Count > 0)
                {
                    sb.AppendLine($"# MAX: {vertices.Max(v => Math.Max(Math.Max(v.X, v.Y), v.Z)):F4}");
                    sb.AppendLine($"# MIN: {vertices.Min(v => Math.Min(Math.Min(v.X, v.Y), v.Z)):F4}");
                }
                sb.AppendLine($"# Faces: {faces.Count / 3}");
                if (faces.Count > 0) sb.AppendLine($"# Face indices MIN: 0 MAX: {faces.Max()}");
                sb.AppendLine();
                
                for (int i = 0; i < Math.Min(100, vertices.Count); i++)
                {
                    var v = vertices[i];
                    sb.AppendLine($"v {v.X.ToString(CultureInfo.InvariantCulture)} {v.Y.ToString(CultureInfo.InvariantCulture)} {v.Z.ToString(CultureInfo.InvariantCulture)}");
                }
                sb.AppendLine();
                
                for (int i = 0; i < Math.Min(300, faces.Count); i += 3)
                {
                    if (i + 2 < faces.Count)
                        sb.AppendLine($"f {faces[i] + 1} {faces[i + 1] + 1} {faces[i + 2] + 1}");
                }
                objTextBox.Text = sb.ToString();
                
                ColorHexBytes();
            }
            catch (Exception ex) 
            { 
                MessageBox.Show($"Could not read Mesh Data\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }
        
        // ========================================
        // COMPLETE BYTE COLORING - ALL BYTES
        // ========================================
        private void ColorHexBytes()
        {
            if (fileData == null) return;
            
            SendMessage(hexTextBox.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            
            // Disable selection changed event during coloring
            hexTextBox.SelectionChanged -= HexTextBox_SelectionChanged;
            
            try
            {
                // Save current cursor position
                int savedByteOffset = selectedByteOffset;
                int savedCursorPos = lastCursorPosition;
                
                // Build complete byte-to-color mapping
                Dictionary<int, Color> byteColors = new Dictionary<int, Color>();
                
                // Vertices (RED)
                if (highlightVerticesCheck.Checked)
                {
                    int vertStart = (int)vertOffsetNum.Value;
                    int vertCount = (int)vertCountNum.Value;
                    int vertInter = (int)vertInterNum.Value;
                    string vertType = vertTypeCombo.Text;
                    int bytesPerComp = vertType == "Float" ? 4 : 2;
                    int vertBytesPerElement = bytesPerComp * 3;
                    int vertStride = vertBytesPerElement + vertInter;
                    
                    for (int i = 0; i < vertCount; i++)
                    {
                        int elementStart = vertStart + i * vertStride;
                        for (int b = 0; b < vertBytesPerElement && elementStart + b < fileData.Length; b++)
                        {
                            byteColors[elementStart + b] = Color.FromArgb(255, 0, 0);
                        }
                    }
                }
                
                // UVs (YELLOW)
                if (highlightUVsCheck.Checked)
                {
                    int uvStart = (int)uvOffsetNum.Value;
                    int uvCount = (int)uvCountNum.Value;
                    int uvInter = (int)uvInterNum.Value;
                    string uvType = uvTypeCombo.Text;
                    int bytesPerComp = uvType == "Float" ? 4 : 2;
                    int uvBytesPerElement = bytesPerComp * 2;
                    int uvStride = uvBytesPerElement + uvInter;
                    
                    for (int i = 0; i < uvCount; i++)
                    {
                        int elementStart = uvStart + i * uvStride;
                        for (int b = 0; b < uvBytesPerElement && elementStart + b < fileData.Length; b++)
                        {
                            byteColors[elementStart + b] = Color.FromArgb(255, 255, 0);
                        }
                    }
                }
                
                // Faces (GREEN)
                if (highlightFacesCheck.Checked)
                {
                    int faceStart = (int)faceOffsetNum.Value;
                    int faceCount = (int)faceCountNum.Value;
                    int faceInter = (int)faceInterNum.Value;
                    string faceType = faceTypeCombo.Text;
                    int faceBytesPerIndex = faceType == "Integer" ? 4 : (faceType == "Short" ? 2 : 1);
                    int faceStride = faceBytesPerIndex + faceInter;
                    
                    for (int i = 0; i < faceCount; i++)
                    {
                        int elementStart = faceStart + i * faceStride;
                        for (int b = 0; b < faceBytesPerIndex && elementStart + b < fileData.Length; b++)
                        {
                            byteColors[elementStart + b] = Color.FromArgb(0, 255, 0);
                        }
                    }
                }
                
                // Normals (BLUE - highest priority)
                if (highlightNormalsCheck.Checked)
                {
                    int normStart = (int)normOffsetNum.Value;
                    int normCount = (int)normCountNum.Value;
                    int normInter = (int)normInterNum.Value;
                    string normType = normTypeCombo.Text;
                    int bytesPerComp = normType == "Float" ? 4 : 2;
                    int normBytesPerElement = bytesPerComp * 3;
                    int normStride = normBytesPerElement + normInter;
                    
                    for (int i = 0; i < normCount; i++)
                    {
                        int elementStart = normStart + i * normStride;
                        for (int b = 0; b < normBytesPerElement && elementStart + b < fileData.Length; b++)
                        {
                            byteColors[elementStart + b] = Color.FromArgb(0, 0, 255);
                        }
                    }
                }
                
                // Reset all to white
                hexTextBox.SelectionStart = 0;
                hexTextBox.SelectionLength = hexTextBox.TextLength;
                hexTextBox.SelectionBackColor = Color.White;
                
                // Apply colors line by line for ALL bytes
                int maxLines = Math.Min(10000, fileData.Length / 16 + 1);
                
                for (int line = 0; line < maxLines; line++)
                {
                    int lineOffset = line * 16;
                    int lineStart = hexTextBox.GetFirstCharIndexFromLine(line);
                    if (lineStart < 0) break;
                    
                    for (int i = 0; i < 16; i++)
                    {
                        int byteOffset = lineOffset + i;
                        if (byteOffset >= fileData.Length) break;
                        
                        // Calculate character position: "00000000  XX XX XX XX XX XX XX XX  XX XX XX XX XX XX XX XX"
                        int charPos = lineStart + 10 + (i * 3) + (i >= 8 ? 1 : 0);
                        
                        // Don't color over the cursor position - cursor takes priority
                        bool isCursorPosition = (charPos == savedCursorPos);
                        
                        if (charPos + 2 <= hexTextBox.TextLength && byteColors.ContainsKey(byteOffset) && !isCursorPosition)
                        {
                            hexTextBox.Select(charPos, 2);
                            hexTextBox.SelectionBackColor = byteColors[byteOffset];
                        }
                    }
                }
                
                // Restore hex cursor if it exists
                if (savedByteOffset >= 0)
                {
                    selectedByteOffset = savedByteOffset;
                    lastCursorPosition = -1; // Force redraw
                    UpdateHexCursor();
                }
                else
                {
                    hexTextBox.Select(0, 0);
                }
            }
            finally
            {
                // Re-enable selection changed event
                hexTextBox.SelectionChanged += HexTextBox_SelectionChanged;
                
                SendMessage(hexTextBox.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                hexTextBox.Invalidate();
            }
        }

        // ======= Parsing Logic ==========
        private void ParseVertices()
        {
            vertices.Clear();
            int offset = (int)vertOffsetNum.Value, count = (int)vertCountNum.Value, inter = (int)vertInterNum.Value;
            bool littleEndian = littleEndianRadio.Checked;
            string format = vertFormatCombo.Text, type = vertTypeCombo.Text;
            int bytesPerVertex = type == "Float" ? 4 : 2, stride = bytesPerVertex * 3 + inter;
            
            if (offset + count * stride > fileData.Length)
            {
                throw new Exception($"Vertex data out of bounds!\nRequired: {offset + count * stride} bytes\nAvailable: {fileData.Length} bytes");
            }
            
            for (int i = 0; i < count; i++)
            {
                int pos = offset + i * stride;
                if (pos + bytesPerVertex * 3 > fileData.Length) break;
                float x = 0, y = 0, z = 0;
                
                if (type == "Float") 
                { 
                    x = ReadFloat(pos, littleEndian); 
                    y = ReadFloat(pos + 4, littleEndian); 
                    z = ReadFloat(pos + 8, littleEndian); 
                }
                else if (type == "Half_Float") 
                { 
                    x = ReadHalfFloat(pos, littleEndian); 
                    y = ReadHalfFloat(pos + 2, littleEndian); 
                    z = ReadHalfFloat(pos + 4, littleEndian); 
                }
                else if (type == "Short_Signed") 
                { 
                    x = ReadShort(pos, littleEndian) / 32767f; 
                    y = ReadShort(pos + 2, littleEndian) / 32767f; 
                    z = ReadShort(pos + 4, littleEndian) / 32767f; 
                }
                
                if (invertXCheck.Checked) x = -x; 
                if (invertYCheck.Checked) y = -y; 
                if (invertZCheck.Checked) z = -z;
                
                Vector3 v = format == "XYZ" ? new Vector3(x, y, z) : 
                           format == "XZY" ? new Vector3(x, z, y) : 
                           format == "YXZ" ? new Vector3(y, x, z) : 
                           format == "YZX" ? new Vector3(y, z, x) : 
                           format == "ZXY" ? new Vector3(z, x, y) : 
                           format == "ZYX" ? new Vector3(z, y, x) : 
                           new Vector3(x, y, z);
                vertices.Add(v);
            }
        }

        private void ParseFaces()
        {
            faces.Clear();
            int offset = (int)faceOffsetNum.Value, count = (int)faceCountNum.Value;
            string type = faceTypeCombo.Text;
            string format = faceFormatCombo.Text;
            bool littleEndian = littleEndianRadio.Checked;
            int bytesPerIndex = type == "Integer" ? 4 : (type == "Short" ? 2 : 1);
            
            if (offset + count * bytesPerIndex > fileData.Length)
            {
                throw new Exception($"Face data out of bounds!\nRequired: {offset + count * bytesPerIndex} bytes\nAvailable: {fileData.Length} bytes");
            }
            
            List<int> rawIndices = new List<int>();
            for (int i = 0; i < count; i++)
            {
                int pos = offset + i * bytesPerIndex;
                if (pos + bytesPerIndex > fileData.Length) break;
                int index = type == "Integer" ? ReadInt(pos, littleEndian) : 
                           type == "Short" ? ReadUShort(pos, littleEndian) : 
                           fileData[pos];
                rawIndices.Add(index);
            }
            
            if (format == "Triangles")
            {
                faces = rawIndices;
            }
            else if (format == "TStrip")
            {
                for (int i = 0; i + 2 < rawIndices.Count; i++)
                {
                    if (i % 2 == 0)
                    {
                        faces.Add(rawIndices[i]);
                        faces.Add(rawIndices[i + 1]);
                        faces.Add(rawIndices[i + 2]);
                    }
                    else
                    {
                        faces.Add(rawIndices[i]);
                        faces.Add(rawIndices[i + 2]);
                        faces.Add(rawIndices[i + 1]);
                    }
                }
            }
            else if (format == "TStripFF")
            {
                int startIdx = 1;
                for (int i = startIdx; i + 2 < rawIndices.Count; i++)
                {
                    if (i % 2 == startIdx % 2)
                    {
                        faces.Add(rawIndices[i]);
                        faces.Add(rawIndices[i + 1]);
                        faces.Add(rawIndices[i + 2]);
                    }
                    else
                    {
                        faces.Add(rawIndices[i]);
                        faces.Add(rawIndices[i + 2]);
                        faces.Add(rawIndices[i + 1]);
                    }
                }
            }
            else if (format == "Quads")
            {
                for (int i = 0; i + 3 < rawIndices.Count; i += 4)
                {
                    faces.Add(rawIndices[i]);
                    faces.Add(rawIndices[i + 1]);
                    faces.Add(rawIndices[i + 2]);
                    faces.Add(rawIndices[i]);
                    faces.Add(rawIndices[i + 2]);
                    faces.Add(rawIndices[i + 3]);
                }
            }
        }
        
        private void ParseUVs()
        {
            uvs.Clear();
            int offset = (int)uvOffsetNum.Value, count = (int)uvCountNum.Value, inter = (int)uvInterNum.Value;
            bool littleEndian = littleEndianRadio.Checked;
            string type = uvTypeCombo.Text;
            string format = uvFormatCombo.Text;
            int bytesPerCoord = type == "Float" ? 4 : 2, stride = bytesPerCoord * 2 + inter;
            
            if (offset + count * stride > fileData.Length)
            {
                throw new Exception($"UV data out of bounds!\nRequired: {offset + count * stride} bytes\nAvailable: {fileData.Length} bytes");
            }
            
            for (int i = 0; i < count; i++)
            {
                int pos = offset + i * stride;
                if (pos + bytesPerCoord * 2 > fileData.Length) break;
                float u = 0, v = 0;
                
                if (type == "Float") 
                { 
                    float first = ReadFloat(pos, littleEndian);
                    float second = ReadFloat(pos + 4, littleEndian);
                    u = format == "UV" ? first : second;
                    v = format == "UV" ? second : first;
                }
                else if (type == "Half_Float") 
                { 
                    float first = ReadHalfFloat(pos, littleEndian);
                    float second = ReadHalfFloat(pos + 2, littleEndian);
                    u = format == "UV" ? first : second;
                    v = format == "UV" ? second : first;
                }
                else if (type == "Short_Signed") 
                { 
                    float first = ReadShort(pos, littleEndian) / 32767f;
                    float second = ReadShort(pos + 2, littleEndian) / 32767f;
                    u = format == "UV" ? first : second;
                    v = format == "UV" ? second : first;
                }
                
                uvs.Add(new Vector2(u, v));
            }
        }

        private void CalculateNormals()
        {
            normals.Clear();
            for (int i = 0; i < vertices.Count; i++) normals.Add(new Vector3(0, 0, 0));
            for (int i = 0; i < faces.Count; i += 3)
            {
                if (i + 2 >= faces.Count) break;
                int i0 = faces[i], i1 = faces[i + 1], i2 = faces[i + 2];
                if (i0 >= vertices.Count || i1 >= vertices.Count || i2 >= vertices.Count) continue;
                Vector3 v0 = vertices[i0], v1 = vertices[i1], v2 = vertices[i2];
                Vector3 edge1 = v1 - v0, edge2 = v2 - v0, normal = Vector3.Cross(edge1, edge2);
                if (normal.Length() > 0) normal = normal.Normalize();
                normals[i0] = normals[i0] + normal; normals[i1] = normals[i1] + normal; normals[i2] = normals[i2] + normal;
            }
            for (int i = 0; i < normals.Count; i++) { if (normals[i].Length() > 0) normals[i] = normals[i].Normalize(); }
        }

        // ======= Binary Reading Utilities =========
        private float ReadFloat(int offset, bool littleEndian)
        {
            if (offset + 4 > fileData.Length) return 0;
            byte[] bytes = new byte[4];
            Array.Copy(fileData, offset, bytes, 0, 4);
            if (!littleEndian) Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }
        
        private float ReadHalfFloat(int offset, bool littleEndian)
        {
            if (offset + 2 > fileData.Length) return 0;
            byte[] bytes = new byte[2];
            Array.Copy(fileData, offset, bytes, 0, 2);
            if (!littleEndian) Array.Reverse(bytes);
            ushort half = BitConverter.ToUInt16(bytes, 0);
            
            int sign = (half >> 15) & 0x1;
            int exponent = (half >> 10) & 0x1F;
            int mantissa = half & 0x3FF;
            
            if (exponent == 0)
            {
                if (mantissa == 0)
                    return sign == 1 ? -0f : 0f;
                return (sign == 1 ? -1f : 1f) * (float)Math.Pow(2, -14) * (mantissa / 1024f);
            }
            else if (exponent == 31)
            {
                if (mantissa == 0)
                    return sign == 1 ? float.NegativeInfinity : float.PositiveInfinity;
                return float.NaN;
            }
            
            return (sign == 1 ? -1f : 1f) * (float)Math.Pow(2, exponent - 15) * (1f + mantissa / 1024f);
        }
        
        private short ReadShort(int offset, bool littleEndian)
        {
            if (offset + 2 > fileData.Length) return 0;
            byte[] bytes = new byte[2];
            Array.Copy(fileData, offset, bytes, 0, 2);
            if (!littleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt16(bytes, 0);
        }
        private ushort ReadUShort(int offset, bool littleEndian)
        {
            if (offset + 2 > fileData.Length) return 0;
            byte[] bytes = new byte[2];
            Array.Copy(fileData, offset, bytes, 0, 2);
            if (!littleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }
        private int ReadInt(int offset, bool littleEndian)
        {
            if (offset + 4 > fileData.Length) return 0;
            byte[] bytes = new byte[4];
            Array.Copy(fileData, offset, bytes, 0, 4);
            if (!littleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
