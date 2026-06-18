using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WaterMarkTool.Models;
using WaterMarkTool.Services;
using WaterMarkTool.Views;
using WpfApplication = System.Windows.Application;

namespace WaterMarkTool.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly WatermarkSettings _settings = new();
    private readonly ExportSettings _exportSettings = new();
    private readonly PresetStorageService _storage = new();
    private bool _isUpdating;
    private bool _suspendSettingsRefresh;
    private DeletedImageSnapshot? _lastDeleted;
    private string _customVariableText = string.Empty;

    public MainViewModel()
    {
        Settings = _settings;
        ExportSettings = _exportSettings;
        Preview = new PreviewViewModel();
        Presets = new PresetViewModel();
        Preview.PropertyChanged += (_, _) => OnPreviewPropertiesChanged();

        _settings.PropertyChanged += OnSettingsChanged;
        _settings.ImageOverlay.PropertyChanged += OnSettingsChanged;
        _exportSettings.PropertyChanged += (_, _) => OnPropertyChanged(nameof(ExportSettings));

        LoadSessionSettings();
        UpdatePatternVisibility();
        UpdateOverlayVisibility();

        FontFamilies =
        [
            "黑体", "宋体", "仿宋", "楷体", "隶书", "幼圆",
            "Arial", "Helvetica", "Tahoma", "Verdana", "Georgia", "Times New Roman"
        ];
    }

    public WatermarkSettings Settings { get; }
    public ExportSettings ExportSettings { get; }
    public PreviewViewModel Preview { get; }
    public PresetViewModel Presets { get; }

    public ObservableCollection<WatermarkImageItem> Images { get; } = [];

    public IReadOnlyList<string> FontFamilies { get; }

    public IReadOnlyList<WatermarkTemplate> Templates => WatermarkTemplates.All;

    public Array OverlayModes => Enum.GetValues(typeof(ImageOverlayMode));
    public Array ExportFormats => Enum.GetValues(typeof(ExportFormat));
    public Array PreviewModes => Enum.GetValues(typeof(PreviewMode));

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private string _themeToggleLabel = "🌞 切换主题";

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusText = "点击或拖拽图片到右侧区域上传";

    [ObservableProperty]
    private WatermarkImageItem? _selectedImage;

    [ObservableProperty]
    private bool _showCountControl;

    [ObservableProperty]
    private bool _showPositionControl;

    [ObservableProperty]
    private bool _showOverlayControls;

    [ObservableProperty]
    private bool _showLogoControls;

    [ObservableProperty]
    private bool _showQrControls;

    [ObservableProperty]
    private bool _autoAdaptOnImport = true;

    [ObservableProperty]
    private BitmapImage? _previewImage;

    [ObservableProperty]
    private BitmapImage? _sourcePreviewImage;

    public bool IsPreviewOpen => Preview.IsPreviewOpen;
    public int PreviewIndex => Preview.PreviewIndex;
    public string PreviewFileName => Preview.PreviewFileName;
    public PreviewMode PreviewMode => Preview.PreviewMode;
    public bool IsPositionEditorOpen => Preview.IsPositionEditorOpen;
    public double SliderPosition
    {
        get => Preview.SliderPosition;
        set => Preview.SliderPosition = value;
    }

    public bool CanUndoDelete => _lastDeleted != null;

    public ImageOverlayMode OverlayMode
    {
        get => Settings.ImageOverlay.Mode;
        set
        {
            if (Settings.ImageOverlay.Mode == value)
            {
                return;
            }

            Settings.ImageOverlay.Mode = value;
            UpdateOverlayVisibility();
            RefreshAllWatermarks();
            OnPropertyChanged();
        }
    }

    public WatermarkPattern Pattern
    {
        get => Settings.Pattern;
        set
        {
            if (Settings.Pattern == value)
            {
                return;
            }

            Settings.Pattern = value;
            UpdatePatternVisibility();
            RefreshAllWatermarks();
            OnPropertyChanged();
        }
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        ThemeToggleLabel = value ? "🌙 切换主题" : "🌞 切换主题";
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!_suspendSettingsRefresh)
        {
            RefreshAllWatermarks();
        }
    }

    private void UpdatePatternVisibility()
    {
        ShowCountControl = Settings.Pattern == WatermarkPattern.Custom;
        ShowPositionControl = Settings.Pattern == WatermarkPattern.Single;
    }

    private void UpdateOverlayVisibility()
    {
        ShowOverlayControls = Settings.ImageOverlay.Mode != ImageOverlayMode.None;
        ShowLogoControls = Settings.ImageOverlay.Mode == ImageOverlayMode.Logo;
        ShowQrControls = Settings.ImageOverlay.Mode == ImageOverlayMode.QrCode;
        OnPropertyChanged(nameof(OverlayMode));
    }

    private void LoadSessionSettings()
    {
        var session = _storage.LoadLastSettings();
        if (session == null)
        {
            return;
        }

        _suspendSettingsRefresh = true;
        WatermarkSettingsMapper.ApplyDto(Settings, session.Watermark);
        ExportSettings.Format = session.Export.Format;
        ExportSettings.JpegQuality = session.Export.JpegQuality;
        ExportSettings.PreserveMetadata = session.Export.PreserveMetadata;
        _suspendSettingsRefresh = false;
        UpdatePatternVisibility();
        UpdateOverlayVisibility();
    }

    public void SaveSessionSettings()
    {
        _storage.SaveLastSettings(Settings, ExportSettings);
    }

    [RelayCommand]
    private void ApplyTemplate(WatermarkTemplate template)
    {
        Settings.Text = template.Text;
    }

    [RelayCommand]
    private void SaveAsPreset()
    {
        var owner = WpfApplication.Current.MainWindow;
        if (owner != null)
        {
            Presets.TrySavePreset(owner, Settings);
        }
    }

    [RelayCommand]
    private void LoadSelectedPreset()
    {
        Presets.ApplyPreset(Settings, Presets.SelectedPreset);
        UpdatePatternVisibility();
        UpdateOverlayVisibility();
        RefreshAllWatermarks();
    }

    [RelayCommand]
    private void DeleteSelectedPreset()
    {
        Presets.DeleteSelectedPreset();
    }

    [RelayCommand]
    private void ToggleBold() => Settings.IsBold = !Settings.IsBold;

    [RelayCommand]
    private void ToggleItalic() => Settings.IsItalic = !Settings.IsItalic;

    [RelayCommand]
    private void ToggleTheme() => IsDarkTheme = !IsDarkTheme;

    [RelayCommand]
    private void AutoAdaptSettings()
    {
        if (Images.Count == 0)
        {
            StatusText = "请先导入图片，再使用智能配色";
            return;
        }

        var suggestion = ImageBackgroundAnalyzer.AnalyzeAverage(Images.Select(image => image.Source.GetFirstFrame()));
        ApplyAutoSettings(suggestion);
        StatusText = suggestion.Summary;
    }

    private void ApplyAutoSettings(WatermarkAutoSettings suggestion)
    {
        _suspendSettingsRefresh = true;
        Settings.Color = suggestion.Color;
        Settings.Opacity = suggestion.Opacity;
        Settings.Size = suggestion.Size;
        Settings.IsBold = suggestion.IsBold;
        Settings.OutlineColor = suggestion.OutlineColor;
        _suspendSettingsRefresh = false;
        RefreshAllWatermarks();
    }

    [RelayCommand]
    private void SelectLogo()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.ico"
        };

        if (dialog.ShowDialog() == true)
        {
            Settings.ImageOverlay.LogoPath = dialog.FileName;
            WatermarkCompositor.ClearLogoCache();
            RefreshAllWatermarks();
        }
    }

    [RelayCommand]
    private async Task AddImagesAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp;*.ico;*.tiff;*.tif;*.avif",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadImagesAsync(dialog.FileNames);
        }
    }

    public async Task LoadImagesAsync(IEnumerable<string> paths)
    {
        var pathList = paths.ToList();
        var heic = pathList.Where(ImageHelper.IsHeic).ToList();
        if (heic.Count > 0)
        {
            MessageBox.Show("暂不支持 HEIC/HEIF 格式，请先在相册中转为 JPG。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        var validPaths = pathList.Where(ImageHelper.IsSupported).ToList();
        if (validPaths.Count == 0)
        {
            if (heic.Count == 0)
            {
                MessageBox.Show("不支持的图片格式。支持：jpg、jpeg、png、gif、webp、bmp、ico、tiff、avif", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return;
        }

        if (!EnsureVariableText())
        {
            return;
        }

        await LoadDocumentsAsync(validPaths.Select(path => (Path: path, Name: Path.GetFileName(path))));
    }

    public async Task PasteImageAsync()
    {
        if (!Clipboard.ContainsImage())
        {
            StatusText = "剪贴板中没有图片";
            return;
        }

        var image = Clipboard.GetImage();
        if (image == null)
        {
            StatusText = "无法读取剪贴板图片";
            return;
        }

        try
        {
            using var bitmap = ImageHelper.FromBitmapSource(image);
            var doc = ImageDocument.FromBitmap(bitmap, string.Empty, ".png");
            if (!EnsureVariableText())
            {
                return;
            }

            await LoadDocumentsAsync([(Path: string.Empty, Name: $"粘贴图片_{DateTime.Now:HHmmss}.png", Document: doc)]);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"粘贴图片失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool EnsureVariableText()
    {
        if (!NeedsVariableInput())
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(_customVariableText))
        {
            return true;
        }

        var owner = WpfApplication.Current.MainWindow;
        if (owner == null)
        {
            return false;
        }

        if (!VariableInputDialog.TryShow(owner, out var customText))
        {
            return false;
        }

        _customVariableText = customText;
        return true;
    }

    private bool NeedsVariableInput()
    {
        return WatermarkTextResolver.ContainsPlaceholders(Settings.Text)
               || WatermarkTextResolver.ContainsPlaceholders(Settings.ImageOverlay.QrContent);
    }

    private async Task LoadDocumentsAsync(IEnumerable<(string Path, string Name, ImageDocument? Document)> sources)
    {
        var items = sources.ToList();
        if (items.Count == 0)
        {
            return;
        }

        IsProcessing = true;
        StatusText = $"正在处理 {items.Count} 张图片...";

        var startIndex = Images.Count;
        await Task.Run(() =>
        {
            var loadedFrames = new List<Bitmap>();
            var pendingItems = new List<WatermarkImageItem>();
            var processed = 0;

            foreach (var entry in items)
            {
                try
                {
                    ImageDocument source;
                    if (entry.Document != null)
                    {
                        source = entry.Document;
                    }
                    else
                    {
                        source = ImageSharpLoader.LoadWithMetadata(entry.Path, ExportSettings.PreserveMetadata);
                    }

                    loadedFrames.Add(source.GetFirstFrame());
                    var index = startIndex + processed + 1;
                    var context = CreateRenderContext(entry.Name, index, startIndex + items.Count);

                    var item = new WatermarkImageItem
                    {
                        FilePath = entry.Path,
                        FileName = entry.Name,
                        Source = source
                    };

                    item.Watermarked = FolderBatchProcessor.ApplyWatermarkPreview(source, _settings, context);
                    pendingItems.Add(item);
                    processed++;
                }
                catch (Exception ex)
                {
                    entry.Document?.Dispose();
                    WpfApplication.Current.Dispatcher.Invoke(() =>
                    {
                        var name = string.IsNullOrEmpty(entry.Path) ? entry.Name : Path.GetFileName(entry.Path);
                        MessageBox.Show($"处理 {name} 失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
            }

            WpfApplication.Current.Dispatcher.Invoke(() =>
            {
                foreach (var item in pendingItems)
                {
                    item.UpdatePreview();
                    Images.Add(item);
                }
            });

            if (AutoAdaptOnImport && loadedFrames.Count > 0)
            {
                var suggestion = ImageBackgroundAnalyzer.AnalyzeAverage(loadedFrames);
                WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    ApplyAutoSettings(suggestion);
                    StatusText = $"已加载 {Images.Count} 张图片，{suggestion.Summary}";
                });
            }
        });

        IsProcessing = false;
        if (!AutoAdaptOnImport || items.Count == 0)
        {
            StatusText = Images.Count > 0 ? $"已加载 {Images.Count} 张图片" : "点击或拖拽图片到右侧区域上传";
        }
    }

    private Task LoadDocumentsAsync(IEnumerable<(string Path, string Name)> paths)
    {
        return LoadDocumentsAsync(paths.Select(p => (p.Path, p.Name, (ImageDocument?)null)));
    }

    private WatermarkRenderContext CreateRenderContext(string fileName, int index, int total)
    {
        return new WatermarkRenderContext
        {
            FileName = fileName,
            Index = index,
            Total = total,
            CustomText = _customVariableText
        };
    }

    [RelayCommand]
    private void RemoveImage(WatermarkImageItem? item)
    {
        if (item == null)
        {
            return;
        }

        var index = Images.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        ClearLastDeleted();
        _lastDeleted = new DeletedImageSnapshot { Item = ImageHelper.CloneItem(item), Index = index };
        OnPropertyChanged(nameof(CanUndoDelete));

        item.Dispose();
        Images.Remove(item);

        if (Preview.IsPreviewOpen && Preview.PreviewIndex == index)
        {
            ClosePreview();
        }
        else if (Preview.IsPreviewOpen && Preview.PreviewIndex > index)
        {
            Preview.PreviewIndex--;
            OnPreviewPropertiesChanged();
        }

        StatusText = "图片已删除，按 Ctrl+Z 可恢复";
    }

    [RelayCommand]
    private void UndoDelete()
    {
        if (_lastDeleted == null)
        {
            return;
        }

        var insertIndex = Math.Min(_lastDeleted.Index, Images.Count);
        Images.Insert(insertIndex, _lastDeleted.Item);
        _lastDeleted = null;
        OnPropertyChanged(nameof(CanUndoDelete));
        StatusText = $"已恢复图片，当前共 {Images.Count} 张";
    }

    private void ClearLastDeleted()
    {
        _lastDeleted?.Dispose();
        _lastDeleted = null;
        OnPropertyChanged(nameof(CanUndoDelete));
    }

    [RelayCommand]
    private void OpenPreview(WatermarkImageItem? item)
    {
        if (item == null)
        {
            return;
        }

        var index = Images.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        ShowPreviewAt(index);
    }

    [RelayCommand]
    private void ClosePreview()
    {
        Preview.Reset();
        OnPreviewPropertiesChanged();
    }

    [RelayCommand]
    private void PreviewNext()
    {
        if (!Preview.IsPreviewOpen || Images.Count <= 1)
        {
            return;
        }

        ShowPreviewAt((Preview.PreviewIndex + 1) % Images.Count);
    }

    [RelayCommand]
    private void PreviewPrevious()
    {
        if (!Preview.IsPreviewOpen || Images.Count <= 1)
        {
            return;
        }

        ShowPreviewAt((Preview.PreviewIndex - 1 + Images.Count) % Images.Count);
    }

    [RelayCommand]
    private void CyclePreviewMode() => Preview.CyclePreviewMode();

    [RelayCommand]
    private void TogglePositionEditor()
    {
        Preview.IsPositionEditorOpen = !Preview.IsPositionEditorOpen;
        OnPropertyChanged(nameof(IsPositionEditorOpen));
    }

    public void ApplyDraggedPosition(double relativeX, double relativeY, bool isOverlay)
    {
        if (isOverlay)
        {
            Settings.ImageOverlay.UseCustomPosition = true;
            Settings.ImageOverlay.CustomOffsetX = relativeX;
            Settings.ImageOverlay.CustomOffsetY = relativeY;
        }
        else
        {
            Settings.UseCustomPosition = true;
            Settings.CustomOffsetX = relativeX;
            Settings.CustomOffsetY = relativeY;
        }

        RefreshAllWatermarks();
        if (Preview.IsPreviewOpen)
        {
            UpdatePreviewImages();
        }
    }

    private void ShowPreviewAt(int index)
    {
        if (index < 0 || index >= Images.Count)
        {
            return;
        }

        var item = Images[index];
        Preview.PreviewIndex = index;
        Preview.PreviewFileName = item.FileName;
        Preview.IsPreviewOpen = true;
        UpdatePreviewImages();
        OnPreviewPropertiesChanged();
    }

    private void UpdatePreviewImages()
    {
        if (Preview.PreviewIndex < 0 || Preview.PreviewIndex >= Images.Count)
        {
            return;
        }

        var item = Images[Preview.PreviewIndex];
        PreviewImage = item.PreviewImage;
        SourcePreviewImage = item.SourcePreviewImage;
        OnPropertyChanged(nameof(SourcePreviewImage));
    }

    private void OnPreviewPropertiesChanged()
    {
        OnPropertyChanged(nameof(IsPreviewOpen));
        OnPropertyChanged(nameof(PreviewIndex));
        OnPropertyChanged(nameof(PreviewFileName));
        OnPropertyChanged(nameof(PreviewMode));
        OnPropertyChanged(nameof(SourcePreviewImage));
        OnPropertyChanged(nameof(PreviewImage));
        OnPropertyChanged(nameof(IsPositionEditorOpen));
    }

    [RelayCommand]
    private void DeleteAll()
    {
        if (Images.Count == 0)
        {
            MessageBox.Show("没有可删除的图片", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show("确定要删除所有图片吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        ClearLastDeleted();
        ClosePreview();

        foreach (var image in Images)
        {
            image.Dispose();
        }

        Images.Clear();
        StatusText = "点击或拖拽图片到右侧区域上传";
    }

    [RelayCommand]
    private async Task DownloadAllAsync()
    {
        if (Images.Count == 0)
        {
            MessageBox.Show("没有可下载的图片", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "ZIP 压缩包|*.zip",
            FileName = $"水印图片_{DateTime.Now:yyyyMMddHHmmss}.zip"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IsProcessing = true;
        StatusText = "正在生成压缩包...";

        try
        {
            await Task.Run(() =>
            {
                using var archive = ZipFile.Open(dialog.FileName, ZipArchiveMode.Create);
                var i = 0;
                foreach (var item in Images)
                {
                    i++;
                    var ext = ImageExportService.GetDefaultExtension(item, ExportSettings);
                    var entryName = $"{Path.GetFileNameWithoutExtension(item.FileName)}_{DateTime.Now:yyyy-MM-dd HHmmss}{ext}";
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    var context = CreateRenderContext(item.FileName, i, Images.Count);
                    ImageExportService.SaveItem(item, entryStream, ExportSettings, _settings, context);
                }
            });

            StatusText = "下载完成";
            MessageBox.Show("所有图片已保存到压缩包", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"下载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
            StatusText = Images.Count > 0 ? $"已加载 {Images.Count} 张图片" : "点击或拖拽图片到右侧区域上传";
        }
    }

    [RelayCommand]
    private void SaveImage(WatermarkImageItem? item)
    {
        if (item == null)
        {
            return;
        }

        var ext = ImageExportService.GetDefaultExtension(item, ExportSettings);
        var dialog = new SaveFileDialog
        {
            Filter = ImageExportService.GetSaveFilter(ExportSettings),
            FileName = $"{Path.GetFileNameWithoutExtension(item.FileName)}_{DateTime.Now:yyyy-MM-dd HHmmss}{ext}"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var index = Images.IndexOf(item);
            var context = CreateRenderContext(item.FileName, index >= 0 ? index + 1 : 1, Images.Count);
            ImageExportService.SaveItem(item, dialog.FileName, ExportSettings, _settings, context);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void CopyImage(WatermarkImageItem? item)
    {
        if (item?.PreviewImage == null)
        {
            return;
        }

        Clipboard.SetImage(item.PreviewImage);
        StatusText = "图片已复制到剪贴板";
    }

    [RelayCommand]
    private async Task FolderBatchAsync()
    {
        if (!EnsureVariableText())
        {
            return;
        }

        var owner = WpfApplication.Current.MainWindow;
        if (owner == null)
        {
            return;
        }

        var dialog = new FolderBatchDialog { Owner = owner };
        if (dialog.ShowDialog() != true || dialog.Result == null)
        {
            return;
        }

        IsProcessing = true;
        var processor = new FolderBatchProcessor();
        var progress = new Progress<FolderBatchProgress>(p =>
        {
            StatusText = $"批处理 {p.Current}/{p.Total}: {Path.GetFileName(p.CurrentFile)}";
        });

        try
        {
            var result = await processor.ProcessAsync(dialog.Result, Settings, ExportSettings, _customVariableText, progress);
            StatusText = $"批处理完成：成功 {result.Succeeded}，失败 {result.Failed}";
            var message = $"成功 {result.Succeeded} 张，失败 {result.Failed} 张。";
            if (!string.IsNullOrEmpty(result.ErrorLogPath))
            {
                message += $"\n错误日志：{result.ErrorLogPath}";
            }

            if (MessageBox.Show(message + "\n\n是否打开输出文件夹？", "批处理完成", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo(dialog.Result.OutputFolder) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"批处理失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void RefreshAllWatermarks()
    {
        if (_isUpdating || Images.Count == 0)
        {
            return;
        }

        _isUpdating = true;
        Task.Run(() =>
        {
            var list = Images.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                try
                {
                    var context = CreateRenderContext(item.FileName, i + 1, list.Count);
                    item.Watermarked?.Dispose();
                    item.Watermarked = FolderBatchProcessor.ApplyWatermarkPreview(item.Source, _settings, context);
                    WpfApplication.Current.Dispatcher.Invoke(() =>
                    {
                        item.UpdatePreview();
                        if (Preview.IsPreviewOpen && Preview.PreviewIndex >= 0 && Preview.PreviewIndex < Images.Count && Images[Preview.PreviewIndex] == item)
                        {
                            UpdatePreviewImages();
                        }
                    });
                }
                catch
                {
                    // ignore single image failures during live preview
                }
            }

            WpfApplication.Current.Dispatcher.Invoke(() => _isUpdating = false);
        });
    }
}
