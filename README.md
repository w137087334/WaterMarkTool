# 离线图片水印工具 (WPF)

参考 [watermark.cdtools.click](https://watermark.cdtools.click/) 实现的 Windows 桌面版图片水印工具，所有处理均在本地完成，不上传任何图片。

## 功能

- 完全离线本地处理，保护隐私
- 批量导入图片（点击上传 / 拖拽 / 文件夹批处理）
- 支持 jpg、jpeg、png、gif、webp、bmp、ico、tiff、avif（不含 HEIC）
- GIF 动图逐帧加水印并保持动画
- 水印文字自定义，内置 6 种常用模板，支持 `{date}` `{filename}` `{xx}` 等变量
- 水印预设保存与加载，自动记住上次设置
- Logo / 二维码图片叠加层（共用位置与大小控件）
- 布局：平铺模式、单个水印、自定义数量
- 位置：居中、四角、拖拽自定义
- 文字描边、阴影、半透明底色条
- 字体：黑体、宋体、仿宋、楷体、隶书、幼圆及常用英文字体
- 粗体 / 斜体、颜色、不透明度、间隔、字号、旋转角度
- 导出：保持原格式 / PNG / JPEG，可调 JPEG 质量，尽力保留 EXIF
- 单张复制、下载、删除；批量 ZIP 打包
- 预览：原图 / 水印 / 左右对比 / 卷帘对比，可拖拽调整位置
- Ctrl+V 粘贴剪贴板图片，Ctrl+Z 撤销删除
- 明暗主题切换

## 运行要求

### 开发调试

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

### 便携版（给最终用户）

- 仅需 **Windows 10/11（64 位）**
- **不需要** 安装 .NET 运行时
- **不需要** 联网

## 启动

### 开发模式

```bash
cd WaterMarkTool
dotnet run --project WaterMarkTool
```

或在 Visual Studio 中打开 `WaterMarkTool.sln` 后按 F5 运行。

### 生成便携版（自包含，可拷 U 盘离线用）

在项目根目录执行：

```powershell
.\publish.ps1
```

输出目录：`publish\WaterMarkTool-win-x64\`  
运行：双击其中的 `WaterMarkTool.exe`（**请分发整个文件夹**，不要只复制 exe）。

单文件版（只有一个 exe，体积更大、首次启动稍慢）：

```powershell
.\publish.ps1 -Mode single
```

输出目录：`publish\WaterMarkTool-win-x64-single\WaterMarkTool.exe`

也可在 Visual Studio 中：**右键项目 → 发布 →** 选择 `PortableFolder` 或 `PortableSingleFile`。

## 项目结构

```
WaterMarkTool/
├── Models/              # 数据模型与设置
├── Services/            # 水印渲染、导入导出、批处理
├── ViewModels/          # MVVM 视图模型
├── Views/               # 对话框
├── Controls/            # 自定义控件（对比滑块等）
├── Converters/          # XAML 值转换器
├── MainWindow.xaml      # 主界面
└── ColorPickerWindow.cs # 颜色选择
```

水印渲染算法移植自开源项目 [houxiaozhao/photo-watermark](https://github.com/houxiaozhao/photo-watermark)。
