using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Easy3D.Core;
using Easy3D.Exporters;
using Easy3D.Geometry;
using Easy3D.Rendering;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Easy3D.UI.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Core
        private Scene _scene;
        private SoftwareRenderer _renderer;
        private DispatcherTimer _renderTimer;

        // State
        private Point _lastMousePos;
        private bool _isDragging;
        private bool _isOrbiting;
        private bool _isPanning;
        private EditMode _editMode = EditMode.Object;
        private ToolType _currentTool = ToolType.Select;
        private RenderMode _renderMode = RenderMode.Solid;

        // UI State
        public ObservableCollection<ObjectItem> Objects { get; } = new();
        private ObjectItem? _selectedObject;
        public ObjectItem? SelectedObject { get => _selectedObject; set { _selectedObject = value; OnPropertyChanged(); UpdateSelection(); } }

        // Transform properties
        private double _posX, _posY, _posZ;
        private double _rotX, _rotY, _rotZ;
        private double _scaleX = 1, _scaleY = 1, _scaleZ = 1;
        public double PosX { get => _posX; set { _posX = value; OnPropertyChanged(); ApplyTransform(); } }
        public double PosY { get => _posY; set { _posY = value; OnPropertyChanged(); ApplyTransform(); } }
        public double PosZ { get => _posZ; set { _posZ = value; OnPropertyChanged(); ApplyTransform(); } }
        public double RotX { get => _rotX; set { _rotX = value; OnPropertyChanged(); ApplyTransform(); } }
        public double RotY { get => _rotY; set { _rotY = value; OnPropertyChanged(); ApplyTransform(); } }
        public double RotZ { get => _rotZ; set { _rotZ = value; OnPropertyChanged(); ApplyTransform(); } }
        public double ScaleX { get => _scaleX; set { _scaleX = value; OnPropertyChanged(); ApplyTransform(); } }
        public double ScaleY { get => _scaleY; set { _scaleY = value; OnPropertyChanged(); ApplyTransform(); } }
        public double ScaleZ { get => _scaleZ; set { _scaleZ = value; OnPropertyChanged(); ApplyTransform(); } }

        // Material properties
        private double _metalness, _roughness = 0.5, _opacity = 1, _shininess = 32, _emissiveIntensity;
        private int _shadingModelIndex = 4;
        private string _albedoColorHex = "#4a90d9";
        public double Metalness { get => _metalness; set { _metalness = value; OnPropertyChanged(); ApplyMaterial(); } }
        public double Roughness { get => _roughness; set { _roughness = value; OnPropertyChanged(); ApplyMaterial(); } }
        public double Opacity { get => _opacity; set { _opacity = value; OnPropertyChanged(); ApplyMaterial(); } }
        public double Shininess { get => _shininess; set { _shininess = value; OnPropertyChanged(); ApplyMaterial(); } }
        public double EmissiveIntensity { get => _emissiveIntensity; set { _emissiveIntensity = value; OnPropertyChanged(); ApplyMaterial(); } }
        public int ShadingModelIndex { get => _shadingModelIndex; set { _shadingModelIndex = value; OnPropertyChanged(); ApplyMaterial(); } }
        public string AlbedoColorHex { get => _albedoColorHex; set { _albedoColorHex = value; OnPropertyChanged(); } }

        // Light properties
        private double _lightIntensity = 1;
        private bool _castShadows = true;
        public double LightIntensity { get => _lightIntensity; set { _lightIntensity = value; OnPropertyChanged(); } }
        public bool CastShadows { get => _castShadows; set { _castShadows = value; OnPropertyChanged(); } }

        // View state
        private bool _isObjectMode = true, _isEditMode, _isSculptMode;
        private bool _isSelectTool = true, _isMoveTool, _isRotateTool, _isScaleTool;
        private bool _isSolidMode = true, _isWireframeMode, _isPBRMode;
        private bool _showGrid = true, _showAxes = true, _showBoundingBoxes, _showWireframe, _showNormals;
        public bool IsObjectMode { get => _isObjectMode; set { _isObjectMode = value; OnPropertyChanged(); } }
        public bool IsEditMode { get => _isEditMode; set { _isEditMode = value; OnPropertyChanged(); } }
        public bool IsSculptMode { get => _isSculptMode; set { _isSculptMode = value; OnPropertyChanged(); } }
        public bool IsSelectTool { get => _isSelectTool; set { _isSelectTool = value; OnPropertyChanged(); } }
        public bool IsMoveTool { get => _isMoveTool; set { _isMoveTool = value; OnPropertyChanged(); } }
        public bool IsRotateTool { get => _isRotateTool; set { _isRotateTool = value; OnPropertyChanged(); } }
        public bool IsScaleTool { get => _isScaleTool; set { _isScaleTool = value; OnPropertyChanged(); } }
        public bool IsSolidMode { get => _isSolidMode; set { _isSolidMode = value; OnPropertyChanged(); } }
        public bool IsWireframeMode { get => _isWireframeMode; set { _isWireframeMode = value; OnPropertyChanged(); } }
        public bool IsPBRMode { get => _isPBRMode; set { _isPBRMode = value; OnPropertyChanged(); } }
        public bool ShowGrid { get => _showGrid; set { _showGrid = value; OnPropertyChanged(); } }
        public bool ShowAxes { get => _showAxes; set { _showAxes = value; OnPropertyChanged(); } }
        public bool ShowBoundingBoxes { get => _showBoundingBoxes; set { _showBoundingBoxes = value; OnPropertyChanged(); } }
        public bool ShowWireframe { get => _showWireframe; set { _showWireframe = value; OnPropertyChanged(); } }
        public bool ShowNormals { get => _showNormals; set { _showNormals = value; OnPropertyChanged(); } }

        // Status
        private string _statusMessage = "جاهز";
        private string _statusText = "";
        private string _fps = "FPS: 0";
        private string _stats = "";
        private string _objectCount = "الكائنات: 0";
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }
        public string FPS { get => _fps; set { _fps = value; OnPropertyChanged(); } }
        public string Stats { get => _stats; set { _stats = value; OnPropertyChanged(); } }
        public string ObjectCount { get => _objectCount; set { _objectCount = value; OnPropertyChanged(); } }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _scene = new Scene();
            _renderer = new SoftwareRenderer(800, 600);

            // Find controls
            var viewportImage = this.FindControl<Image>("ViewportImage");
            var overlayCanvas = this.FindControl<Canvas>("OverlayCanvas");

            // Mouse events on viewport
            if (viewportImage != null)
            {
                viewportImage.PointerPressed += OnPointerPressed;
                viewportImage.PointerMoved += OnPointerMoved;
                viewportImage.PointerReleased += OnPointerReleased;
                viewportImage.PointerWheelChanged += OnPointerWheelChanged;
            }

            // Add default object
            var cube = MeshFactory.CreateCube();
            _scene.AddObject(cube);
            _scene.SaveState();
            UpdateObjectsList();

            // Render timer
            _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _renderTimer.Tick += (s, e) => RenderFrame(viewportImage);
            _renderTimer.Start();

            // Resize handler
            GetObservable(BoundsProperty).Subscribe(bounds =>
            {
                if (bounds.Width > 0 && bounds.Height > 0)
                    _renderer.Resize((int)bounds.Width - 520, (int)bounds.Height - 120);
            });
        }

        private void RenderFrame(Image? viewportImage)
        {
            if (viewportImage == null) return;

            _renderer.ShowGrid = _showGrid;
            _renderer.ShowAxes = _showAxes;
            _renderer.ShowBoundingBoxes = _showBoundingBoxes;
            _renderer.ShowWireframe = _showWireframe;
            _renderer.ShowNormals = _showNormals;

            var bitmap = _renderer.Render(_scene);

            // Convert SKBitmap to Avalonia Bitmap
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = data.AsStream();
            viewportImage.Source = new Bitmap(stream);

            FPS = $"FPS: {_renderer.FPS:F0}";
            StatusText = $"Triangles: {_renderer.TrianglesRendered} | Vertices: {_renderer.VerticesProcessed}";
        }

        // ==================== Mouse Events ====================

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pos = e.GetPosition(sender as Control);
            _lastMousePos = new Point(pos.X, pos.Y);
            var props = e.GetCurrentPoint(sender as Control).Properties;

            if (props.IsLeftButtonPressed)
            {
                _isDragging = true;
                if (_currentTool == ToolType.Select)
                    TrySelectObject((float)pos.X, (float)pos.Y);
            }
            else if (props.IsMiddleButtonPressed)
                _isOrbiting = true;
            else if (props.IsRightButtonPressed)
                _isPanning = true;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var pos = e.GetPosition(sender as Control);
            float dx = (float)(pos.X - _lastMousePos.X);
            float dy = (float)(pos.Y - _lastMousePos.Y);

            if (_isOrbiting)
                _scene.Camera.Orbit(dx, dy);
            else if (_isPanning)
                _scene.Camera.Pan(-dx, dy);
            else if (_isDragging && _scene.SelectedObject != null)
            {
                float speed = 0.01f * _scene.Camera.Distance;
                switch (_currentTool)
                {
                    case ToolType.Move:
                        _scene.SelectedObject.Position += new Vector3(dx * speed, -dy * speed, 0);
                        break;
                    case ToolType.Rotate:
                        _scene.SelectedObject.Rotation += new Vector3(dy * 0.01f, dx * 0.01f, 0);
                        break;
                    case ToolType.Scale:
                        float factor = 1 + dx * 0.01f;
                        _scene.SelectedObject.Scale *= factor;
                        break;
                }
                UpdateTransformUI();
            }

            _lastMousePos = new Point(pos.X, pos.Y);
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _isDragging = false;
            _isOrbiting = false;
            _isPanning = false;
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            _scene.Camera.Zoom(e.Delta.Y > 0 ? 1 : -1);
        }

        // ==================== Selection ====================

        private void TrySelectObject(float x, float y)
        {
            var (rayOrigin, rayDir) = _scene.Camera.ScreenToWorldRay(x, y, _renderer._width, _renderer._height);

            Mesh3D? closest = null;
            float closestDist = float.MaxValue;

            foreach (var obj in _scene.Objects)
            {
                var center = obj.Position;
                float radius = obj.Bounds.Size.Length / 2;
                var oc = rayOrigin - center;
                float a = rayDir.Dot(rayDir);
                float b = 2 * oc.Dot(rayDir);
                float c = oc.Dot(oc) - radius * radius;
                float disc = b * b - 4 * a * c;
                if (disc >= 0)
                {
                    float t = (-b - MathF.Sqrt(disc)) / (2 * a);
                    if (t > 0 && t < closestDist) { closestDist = t; closest = obj; }
                }
            }

            if (closest != null) _scene.SelectObject(closest);
            else _scene.DeselectAll();

            UpdateObjectsList();
            UpdateTransformUI();
            UpdateMaterialUI();
        }

        private void UpdateSelection()
        {
            if (_selectedObject != null)
            {
                var obj = _scene.FindObject(_selectedObject.Name);
                if (obj != null) _scene.SelectObject(obj);
            }
            UpdateTransformUI();
            UpdateMaterialUI();
        }

        // ==================== UI Updates ====================

        private void UpdateObjectsList()
        {
            Objects.Clear();
            foreach (var obj in _scene.Objects)
                Objects.Add(new ObjectItem { Name = obj.Name, Icon = "⬜" });
            ObjectCount = $"الكائنات: {_scene.Objects.Count}";
        }

        private void UpdateTransformUI()
        {
            if (_scene.SelectedObject == null) return;
            var obj = _scene.SelectedObject;
            _posX = obj.Position.X; _posY = obj.Position.Y; _posZ = obj.Position.Z;
            _rotX = obj.Rotation.X * 180 / MathF.PI; _rotY = obj.Rotation.Y * 180 / MathF.PI; _rotZ = obj.Rotation.Z * 180 / MathF.PI;
            _scaleX = obj.Scale.X; _scaleY = obj.Scale.Y; _scaleZ = obj.Scale.Z;
            OnPropertyChanged(nameof(PosX)); OnPropertyChanged(nameof(PosY)); OnPropertyChanged(nameof(PosZ));
            OnPropertyChanged(nameof(RotX)); OnPropertyChanged(nameof(RotY)); OnPropertyChanged(nameof(RotZ));
            OnPropertyChanged(nameof(ScaleX)); OnPropertyChanged(nameof(ScaleY)); OnPropertyChanged(nameof(ScaleZ));
            Stats = $"الكائن: {obj.Name}\nالنقاط: {obj.Vertices.Count}\nالأوجه: {obj.Faces.Count}\nالحجم: {obj.Bounds.Size}";
        }

        private void UpdateMaterialUI()
        {
            if (_scene.SelectedObject == null) return;
            var mat = _scene.SelectedObject.Materials.FirstOrDefault();
            if (mat == null) return;
            _metalness = mat.Metalness; _roughness = mat.Roughness; _opacity = mat.Opacity;
            _shininess = mat.Shininess; _emissiveIntensity = mat.EmissiveIntensity;
            _albedoColorHex = mat.AlbedoColor.ToHex();
            OnPropertyChanged(nameof(Metalness)); OnPropertyChanged(nameof(Roughness));
            OnPropertyChanged(nameof(Opacity)); OnPropertyChanged(nameof(Shininess));
            OnPropertyChanged(nameof(EmissiveIntensity)); OnPropertyChanged(nameof(AlbedoColorHex));
        }

        private void ApplyTransform()
        {
            if (_scene.SelectedObject == null) return;
            _scene.SelectedObject.Position = new((float)_posX, (float)_posY, (float)_posZ);
            _scene.SelectedObject.Rotation = new((float)(_rotX * MathF.PI / 180), (float)(_rotY * MathF.PI / 180), (float)(_rotZ * MathF.PI / 180));
            _scene.SelectedObject.Scale = new((float)_scaleX, (float)_scaleY, (float)_scaleZ);
        }

        private void ApplyMaterial()
        {
            if (_scene.SelectedObject == null) return;
            var mat = _scene.SelectedObject.Materials.FirstOrDefault();
            if (mat == null) return;
            mat.Metalness = (float)_metalness; mat.Roughness = (float)_roughness;
            mat.Opacity = (float)_opacity; mat.Shininess = (float)_shininess;
            mat.EmissiveIntensity = (float)_emissiveIntensity;
            mat.AlbedoColor = Color.FromHex(_albedoColorHex);
            if (_shadingModelIndex >= 0 && _shadingModelIndex <= 6)
                mat.ShadingModel = (ShadingModel)_shadingModelIndex;
        }

        // ==================== Add Objects ====================

        private void AddMesh(Mesh3D mesh) { _scene.AddObject(mesh); _scene.SaveState(); UpdateObjectsList(); StatusMessage = $"تم إضافة: {mesh.Name}"; }

        // ==================== File Operations ====================

        private async Task NewProject()
        {
            _scene = new Scene();
            AddMesh(MeshFactory.CreateCube());
            _scene.SaveState();
            UpdateObjectsList();
            StatusMessage = "مشروع جديد";
        }

        private async Task OpenProject()
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "فتح مشروع",
                AllowMultiple = false,
                FileTypeFilter = new[] {
                    new FilePickerFileType("Easy3D Files") { Patterns = new[] { "*.e3d", "*.json" } },
                    new FilePickerFileType("OBJ Files") { Patterns = new[] { "*.obj" } },
                    new FilePickerFileType("STL Files") { Patterns = new[] { "*.stl" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count > 0)
            {
                try
                {
                    var path = files[0].Path.LocalPath;
                    string ext = System.IO.Path.GetExtension(path).ToLower();
                    if (ext == ".e3d" || ext == ".json")
                        _scene.Deserialize(System.IO.File.ReadAllText(path));
                    else
                    {
                        var imported = ExportManager.Import(path);
                        _scene = imported;
                    }
                    _scene.FilePath = path;
                    _scene.SaveState();
                    UpdateObjectsList();
                    StatusMessage = $"تم فتح: {path}";
                }
                catch (Exception ex) { StatusMessage = $"خطأ: {ex.Message}"; }
            }
        }

        private async Task SaveProject()
        {
            if (_scene.FilePath != null)
            {
                System.IO.File.WriteAllText(_scene.FilePath, _scene.Serialize());
                _scene.IsDirty = false;
                StatusMessage = $"تم الحفظ: {_scene.FilePath}";
            }
            else await SaveProjectAs();
        }

        private async Task SaveProjectAs()
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "حفظ مشروع",
                FileTypeChoices = new[] { new FilePickerFileType("Easy3D Files") { Patterns = new[] { "*.e3d" } } },
                DefaultExtension = ".e3d"
            });
            if (file != null)
            {
                _scene.FilePath = file.Path.LocalPath;
                await SaveProject();
            }
        }

        private async Task ExportAs(ExportFormat format)
        {
            var ext = ExportManager.Extensions[format];
            var desc = ExportManager.Descriptions[format];
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = $"تصدير {desc}",
                FileTypeChoices = new[] { new FilePickerFileType(desc) { Patterns = new[] { $"*{ext}" } } },
                DefaultExtension = ext
            });
            if (file != null)
            {
                try
                {
                    ExportManager.Export(_scene, file.Path.LocalPath, format);
                    StatusMessage = $"تم التصدير: {file.Path.LocalPath}";
                }
                catch (Exception ex) { StatusMessage = $"خطأ: {ex.Message}"; }
            }
        }

        // ==================== Keyboard ====================

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                switch (e.Key)
                {
                    case Key.W: _currentTool = ToolType.Select; break;
                    case Key.G: _currentTool = ToolType.Move; break;
                    case Key.R: _currentTool = ToolType.Rotate; break;
                    case Key.S: _currentTool = ToolType.Scale; break;
                    case Key.Delete: _scene.RemoveSelected(); UpdateObjectsList(); break;
                    case Key.F: /* Focus selected */ break;
                }
            }
        }

        // ==================== Commands ====================

        public Avalonia.Input.ICommand NewProjectCommand => new AsyncCommand(() => NewProject());
        public Avalonia.Input.ICommand OpenProjectCommand => new AsyncCommand(() => OpenProject());
        public Avalonia.Input.ICommand SaveProjectCommand => new AsyncCommand(() => SaveProject());
        public Avalonia.Input.ICommand SaveAsCommand => new AsyncCommand(() => SaveProjectAs());
        public Avalonia.Input.ICommand ExportOBJCommand => new AsyncCommand(() => ExportAs(ExportFormat.OBJ));
        public Avalonia.Input.ICommand ExportSTLCommand => new AsyncCommand(() => ExportAs(ExportFormat.STL));
        public Avalonia.Input.ICommand ExportGLTFCommand => new AsyncCommand(() => ExportAs(ExportFormat.GLTF));
        public Avalonia.Input.ICommand ExportThreeJSCommand => new AsyncCommand(() => ExportAs(ExportFormat.ThreeJS));
        public Avalonia.Input.ICommand ExportFBXCommand => new AsyncCommand(() => ExportAs(ExportFormat.FBX));
        public Avalonia.Input.ICommand ExportPLYCommand => new AsyncCommand(() => ExportAs(ExportFormat.PLY));
        public Avalonia.Input.ICommand ExportCOLLADACommand => new AsyncCommand(() => ExportAs(ExportFormat.COLLADA));
        public Avalonia.Input.ICommand ExportX3DCommand => new AsyncCommand(() => ExportAs(ExportFormat.X3D));
        public Avalonia.Input.ICommand ExportVRMLCommand => new AsyncCommand(() => ExportAs(ExportFormat.VRML));
        public Avalonia.Input.ICommand ExportUSDCommand => new AsyncCommand(() => ExportAs(ExportFormat.USD));
        public Avalonia.Input.ICommand ExportAMFCommand => new AsyncCommand(() => ExportAs(ExportFormat.AMF));
        public Avalonia.Input.ICommand ExportOFFCommand => new AsyncCommand(() => ExportAs(ExportFormat.OFF));
        public Avalonia.Input.ICommand ExportCSVCommand => new AsyncCommand(() => ExportAs(ExportFormat.CSV));
        public Avalonia.Input.ICommand ImportOBJCommand => new AsyncCommand(async () => { /* Import OBJ */ });
        public Avalonia.Input.ICommand ImportSTLCommand => new AsyncCommand(async () => { /* Import STL */ });
        public Avalonia.Input.ICommand ImportGLTFCommand => new AsyncCommand(async () => { /* Import GLTF */ });
        public Avalonia.Input.ICommand ImportPLYCommand => new AsyncCommand(async () => { /* Import PLY */ });
        public Avalonia.Input.ICommand UndoCommand => new Command(() => { _scene.Undo(); UpdateObjectsList(); });
        public Avalonia.Input.ICommand RedoCommand => new Command(() => { _scene.Redo(); UpdateObjectsList(); });
        public Avalonia.Input.ICommand DuplicateCommand => new Command(() => { _scene.DuplicateSelected(); UpdateObjectsList(); });
        public Avalonia.Input.ICommand DeleteCommand => new Command(() => { _scene.RemoveSelected(); UpdateObjectsList(); });
        public Avalonia.Input.ICommand SelectAllCommand => new Command(() => { /* Select all */ });
        public Avalonia.Input.ICommand DeselectAllCommand => new Command(() => { _scene.DeselectAll(); UpdateObjectsList(); });
        public Avalonia.Input.ICommand ExitCommand => new Command(() => Close());
        public Avalonia.Input.ICommand AddCubeCommand => new Command(() => AddMesh(MeshFactory.CreateCube()));
        public Avalonia.Input.ICommand AddSphereCommand => new Command(() => AddMesh(MeshFactory.CreateSphere()));
        public Avalonia.Input.ICommand AddCylinderCommand => new Command(() => AddMesh(MeshFactory.CreateCylinder()));
        public Avalonia.Input.ICommand AddConeCommand => new Command(() => AddMesh(MeshFactory.CreateCone()));
        public Avalonia.Input.ICommand AddPlaneCommand => new Command(() => AddMesh(MeshFactory.CreatePlane()));
        public Avalonia.Input.ICommand AddTorusCommand => new Command(() => AddMesh(MeshFactory.CreateTorus()));
        public Avalonia.Input.ICommand AddIcosphereCommand => new Command(() => AddMesh(MeshFactory.CreateIcosphere()));
        public Avalonia.Input.ICommand AddStarCommand => new Command(() => AddMesh(MeshFactory.CreateStar()));
        public Avalonia.Input.ICommand AddDirectionalLightCommand => new Command(() => _scene.AddLight(Light.CreateDirectional()));
        public Avalonia.Input.ICommand AddPointLightCommand => new Command(() => _scene.AddLight(Light.CreatePoint()));
        public Avalonia.Input.ICommand AddSpotLightCommand => new Command(() => _scene.AddLight(Light.CreateSpot()));
        public Avalonia.Input.ICommand SubdivideCommand => new Command(() => { _scene.SelectedObject?.Subdivide(); });
        public Avalonia.Input.ICommand DecimateCommand => new Command(() => { _scene.SelectedObject?.Decimate(0.5f); });
        public Avalonia.Input.ICommand FlipNormalsCommand => new Command(() => { _scene.SelectedObject?.FlipNormals(); });
        public Avalonia.Input.ICommand ApplyTransformCommand => new Command(() => { _scene.SelectedObject?.ApplyTransform(); });
        public Avalonia.Input.ICommand CalculateNormalsCommand => new Command(() => { _scene.SelectedObject?.CalculateNormals(); });
        public Avalonia.Input.ICommand CalculateTangentsCommand => new Command(() => { _scene.SelectedObject?.CalculateTangents(); });
        public Avalonia.Input.ICommand ViewFrontCommand => new Command(() => _scene.Camera.SetView(CameraView.Front));
        public Avalonia.Input.ICommand ViewBackCommand => new Command(() => _scene.Camera.SetView(CameraView.Back));
        public Avalonia.Input.ICommand ViewRightCommand => new Command(() => _scene.Camera.SetView(CameraView.Right));
        public Avalonia.Input.ICommand ViewLeftCommand => new Command(() => _scene.Camera.SetView(CameraView.Left));
        public Avalonia.Input.ICommand ViewTopCommand => new Command(() => _scene.Camera.SetView(CameraView.Top));
        public Avalonia.Input.ICommand ViewBottomCommand => new Command(() => _scene.Camera.SetView(CameraView.Bottom));
        public Avalonia.Input.ICommand ResetCameraCommand => new Command(() => _scene.Camera.Reset());
        public Avalonia.Input.ICommand ToggleGridCommand => new Command(() => { _showGrid = !_showGrid; OnPropertyChanged(nameof(ShowGrid)); });
        public Avalonia.Input.ICommand ToggleAxesCommand => new Command(() => { _showAxes = !_showAxes; OnPropertyChanged(nameof(ShowAxes)); });
        public Avalonia.Input.ICommand ToggleBoundingBoxesCommand => new Command(() => { _showBoundingBoxes = !_showBoundingBoxes; OnPropertyChanged(nameof(ShowBoundingBoxes)); });
        public Avalonia.Input.ICommand ToggleWireframeCommand => new Command(() => { _showWireframe = !_showWireframe; OnPropertyChanged(nameof(ShowWireframe)); });
        public Avalonia.Input.ICommand ToggleNormalsCommand => new Command(() => { _showNormals = !_showNormals; OnPropertyChanged(nameof(ShowNormals)); });
        public Avalonia.Input.ICommand SelectToolCommand => new Command(() => { _currentTool = ToolType.Select; });
        public Avalonia.Input.ICommand MoveToolCommand => new Command(() => { _currentTool = ToolType.Move; });
        public Avalonia.Input.ICommand RotateToolCommand => new Command(() => { _currentTool = ToolType.Rotate; });
        public Avalonia.Input.ICommand ScaleToolCommand => new Command(() => { _currentTool = ToolType.Scale; });
        public Avalonia.Input.ICommand FocusSelectedCommand => new Command(() => { /* Focus */ });
        public Avalonia.Input.ICommand AddObjectCommand => new Command(() => AddMesh(MeshFactory.CreateCube()));
        public Avalonia.Input.ICommand ShowShortcutsCommand => new Command(async () => { await MessageBox(this, "الاختصارات", "W: اختيار\nG: تحريك\nR: تدوير\nS: تحجيم\nCtrl+Z: تراجع\nCtrl+Y: إعادة\nCtrl+D: تكرار\nDelete: حذف\nCtrl+S: حفظ\nCtrl+N: جديد\nCtrl+O: فتح"); });
        public Avalonia.Input.ICommand ShowAboutCommand => new Command(async () => { await MessageBox(this, "حول", "Easy3D v2.0\nبرنامج تصميم ثلاثي الأبعاد\nيعمل بدون كارت شاشة"); });

        private async Task MessageBox(Window parent, string title, string message)
        {
            var dialog = new Window { Title = title, Width = 400, Height = 200, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var panel = new StackPanel { Margin = new Avalonia.Thickness(20) };
            panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
            var btn = new Button { Content = "حسناً", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Avalonia.Thickness(0, 20, 0, 0) };
            btn.Click += (s, e) => dialog.Close();
            panel.Children.Add(btn);
            dialog.Content = panel;
            await dialog.ShowDialog(parent);
        }

        // ==================== INotifyPropertyChanged ====================

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Helper classes
    public class ObjectItem
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "⬜";
    }

    public enum EditMode { Object, Edit, Sculpt }
    public enum ToolType { Select, Move, Rotate, Scale }

    public class Command : Avalonia.Input.ICommand
    {
        private readonly Action _execute;
        public Command(Action execute) { _execute = execute; }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
    }

    public class AsyncCommand : Avalonia.Input.ICommand
    {
        private readonly Func<Task> _execute;
        public AsyncCommand(Func<Task> execute) { _execute = execute; }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public async void Execute(object? parameter) => await _execute();
    }
}
