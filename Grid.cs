using System;
using System.Drawing; // 引入 System.Drawing 命名空间
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

public class Grid
{
    private string _currentCoverPath;  // 存储当前封面的路径

    public string CurrentCoverPath  
    {
        get => _currentCoverPath;
        private set => _currentCoverPath = value;
    }

    // 使用 SixLabors.ImageSharp 结构定义位置和尺寸的属性
    public SixLabors.ImageSharp.PointF Position { get; set; }
    public SixLabors.ImageSharp.SizeF Size { get; set; }

    // 当前封面图像，初始时为 null
    public Image<Rgba32>? CurrentCover { get; private set; }
    private ImageManager _imageManager; // 用于管理图像的 ImageManager 实例

    // 构造函数，用于初始化网格的位置、大小和 ImageManager
    public Grid(SixLabors.ImageSharp.PointF position, SixLabors.ImageSharp.SizeF size, ImageManager imageManager)
    {
        Position = position;
        Size = size;
        CurrentCover = null; // 不再创建空的图像实例
        _imageManager = imageManager;
        _currentCoverPath = string.Empty; // 将封面路径初始化为空
    }

    // 异步更新封面图像
    public async Task UpdateCoverAsync(string coverPath, Image<Rgba32> wallpaper)
    {
        // 验证 coverPath 和 wallpaper 输入
        if (string.IsNullOrWhiteSpace(coverPath))
        {
            throw new ArgumentException("封面图像路径不能为 null 或空白。", nameof(coverPath));
        }

        if (wallpaper == null)
        {
            throw new ArgumentNullException(nameof(wallpaper), "壁纸图像不能为 null。");
        }

        // 检查当前封面是否已设置为请求的路径
        if (_currentCoverPath == coverPath && CurrentCover != null)
        {
            return; 
        }

        try
        {
            // 检索或创建指定尺寸的封面图像
            var cover = await _imageManager.GetOrAddImageAsync(coverPath, new SixLabors.ImageSharp.Size((int)Size.Width, (int)Size.Height));

            CurrentCover?.Dispose(); // 释放旧封面图像资源

            // 在指定位置将新封面图像绘制到壁纸上
            wallpaper.Mutate(x => x.DrawImage(cover, new SixLabors.ImageSharp.Point((int)Position.X, (int)Position.Y), 1));
            CurrentCover = cover; // 设置新封面
            _currentCoverPath = coverPath; // 更新封面路径
        }
        catch (Exception ex)
        {
            // 记录更新过程中发生的任何错误
            Console.WriteLine($"更新封面时发生错误：{ex.Message}");
        }
    }
}
