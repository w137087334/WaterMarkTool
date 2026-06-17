using System.Collections.ObjectModel;
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
using WpfApplication = System.Windows.Application;

namespace WaterMarkTool.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly WatermarkSettings _settings = new();
    private bool _isUpdating;
    private bool _suspendSettingsRefresh;
    private DeletedImageSnapshot? _lastDeleted;

    public MainViewModel()
    {
        Settings = _settings;
        _settings.PropertyChanged += (_, _) =>
        {
            if (!_suspendSettingsRefresh)
            {
                RefreshAllWatermarks();
            }
        };
        UpdatePatternVisibility();

        FontFamilies =
        [
            "黑体", "宋体", "仿宋", "楷体", "隶书", "幼圆",
            "Arial", "Helvetica", "Tahoma", "Verdana", "Georgia", "Times New Roman"
        ];
    }

    public WatermarkSettings Settings { get; }

    public ObservableCollection<WatermarkImageItem> Images { get; } = [];

    public IReadOnlyList<string> FontFamilies { get; }

    public IReadOnlyList<WatermarkTemplate> Templates => WatermarkTemplates.All;

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
    private bool _isPreviewOpen;

    [ObservableProperty]
    private BitmapImage? _previewImage;

    [ObservableProperty]
    private int _previewIndex = -1;

    [ObservableProperty]
    private string _previewFileName = string.Empty;

    [ObservableProperty]
    private bool _autoAdaptOnImport = true;

    public bool CanUndoDelete => _lastDeleted != null;

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

    private void UpdatePatternVisibility()
    {
        ShowCountControl = Settings.Pattern == WatermarkPattern.Custom;
        ShowPositionControl = Settings.Pattern == WatermarkPattern.Single;
    }

    [RelayCommand]
    private void ApplyTemplate(WatermarkTemplate template)
    {
        Settings.Text = template.Text;
    }

    [RelayCommand]
    private void ToggleBold()
    {
        Settings.IsBold = !Settings.IsBold;
    }

    [RelayCommand]
    private void ToggleItalic()
    {
        Settings.IsItalic = !Settings.IsItalic;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    [RelayCommand]
    private void AutoAdaptSettings()
    {
        if (Images.Count == 0)
        {
            StatusText = "请先导入图片，再使用智能配色";
            return;
        }

        var suggestion = ImageBackgroundAnalyzer.AnalyzeAverage(Images.Select(image => image.SourceBitmap));
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
        _suspendSettingsRefresh = false;
        RefreshAllWatermarks();
    }

    [RelayCommand]
    private async Task AddImagesAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp;*.ico",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadImagesAsync(dialog.FileNames);
        }
    }

    public async Task LoadImagesAsync(IEnumerable<string> paths)
    {
        var validPaths = paths.Where(ImageHelper.IsSupported).ToList();
        if (validPaths.Count == 0)
        {
            MessageBox.Show("不支持的图片格式。支持：jpg、jpeg、png、gif、webp、bmp、ico", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await LoadBitmapsAsync(validPaths.Select(path => (Path: path, Name: Path.GetFileName(path))));
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
            var clone = new Bitmap(bitmap);
            await LoadBitmapsAsync([(Path: string.Empty, Name: $"粘贴图片_{DateTime.Now:HHmmss}.png", Bitmap: clone)]);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"粘贴图片失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadBitmapsAsync(IEnumerable<(string Path, string Name, Bitmap? Bitmap)> sources)
    {
        var items = sources.ToList();
        if (items.Count == 0)
        {
            return;
        }

        IsProcessing = true;
        StatusText = $"正在处理 {items.Count} 张图片...";

        await Task.Run(() =>
        {
            var loadedBitmaps = new List<Bitmap>();
            foreach (var entry in items)
            {
                try
                {
                    Bitmap source;
                    if (entry.Bitmap != null)
                    {
                        source = entry.Bitmap;
                    }
                    else
                    {
                        using var loaded = new Bitmap(entry.Path);
                        source = new Bitmap(loaded);
                    }

                    loadedBitmaps.Add(source);

                    var item = new WatermarkImageItem
                    {
                        FilePath = entry.Path,
                        FileName = entry.Name,
                        SourceBitmap = source
                    };

                    item.WatermarkedBitmap = WatermarkRenderer.ApplyWatermark(source, _settings);
                    item.UpdatePreview();

                    WpfApplication.Current.Dispatcher.Invoke(() => Images.Add(item));
                }
                catch (Exception ex)
                {
                    entry.Bitmap?.Dispose();
                    WpfApplication.Current.Dispatcher.Invoke(() =>
                    {
                        var name = string.IsNullOrEmpty(entry.Path) ? entry.Name : Path.GetFileName(entry.Path);
                        MessageBox.Show($"处理 {name} 失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
            }

            if (AutoAdaptOnImport && loadedBitmaps.Count > 0)
            {
                var suggestion = ImageBackgroundAnalyzer.AnalyzeAverage(loadedBitmaps);
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

    private Task LoadBitmapsAsync(IEnumerable<(string Path, string Name)> paths)
    {
        return LoadBitmapsAsync(paths.Select(p => (p.Path, p.Name, (Bitmap?)null)));
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
        _lastDeleted = new DeletedImageSnapshot
        {
            Item = ImageHelper.CloneItem(item),
            Index = index
        };
        OnPropertyChanged(nameof(CanUndoDelete));

        item.Dispose();
        Images.Remove(item);

        if (IsPreviewOpen && PreviewIndex == index)
        {
            ClosePreview();
        }
        else if (IsPreviewOpen && PreviewIndex > index)
        {
            PreviewIndex--;
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
        IsPreviewOpen = false;
        PreviewIndex = -1;
        PreviewImage = null;
        PreviewFileName = string.Empty;
    }

    [RelayCommand]
    private void PreviewNext()
    {
        if (!IsPreviewOpen || Images.Count <= 1)
        {
            return;
        }

        ShowPreviewAt((PreviewIndex + 1) % Images.Count);
    }

    [RelayCommand]
    private void PreviewPrevious()
    {
        if (!IsPreviewOpen || Images.Count <= 1)
        {
            return;
        }

        ShowPreviewAt((PreviewIndex - 1 + Images.Count) % Images.Count);
    }

    private void ShowPreviewAt(int index)
    {
        if (index < 0 || index >= Images.Count)
        {
            return;
        }

        var item = Images[index];
        PreviewIndex = index;
        PreviewFileName = item.FileName;
        PreviewImage = item.PreviewImage;
        IsPreviewOpen = true;
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
                foreach (var item in Images)
                {
                    var entryName = $"{Path.GetFileNameWithoutExtension(item.FileName)}_{DateTime.Now:yyyy-MM-dd HHmmss}.png";
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var bitmap = item.WatermarkedBitmap ?? item.SourceBitmap;
                    bitmap.Save(entryStream, System.Drawing.Imaging.ImageFormat.Png);
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

        var dialog = new SaveFileDialog
        {
            Filter = "PNG 图片|*.png",
            FileName = $"{Path.GetFileNameWithoutExtension(item.FileName)}_{DateTime.Now:yyyy-MM-dd HHmmss}.png"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        using var bitmap = item.WatermarkedBitmap ?? item.SourceBitmap;
        bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
    }

    [RelayCommand]
    private void CopyImage(WatermarkImageItem? item)
    {
        if (item?.PreviewImage == null)
        {
            return;
        }

        System.Windows.Clipboard.SetImage(item.PreviewImage);
        StatusText = "图片已复制到剪贴板";
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
            foreach (var item in Images.ToList())
            {
                try
                {
                    item.WatermarkedBitmap?.Dispose();
                    item.WatermarkedBitmap = WatermarkRenderer.ApplyWatermark(item.SourceBitmap, _settings);
                    WpfApplication.Current.Dispatcher.Invoke(() =>
                    {
                        item.UpdatePreview();
                        if (IsPreviewOpen && PreviewIndex >= 0 && PreviewIndex < Images.Count && Images[PreviewIndex] == item)
                        {
                            PreviewImage = item.PreviewImage;
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
