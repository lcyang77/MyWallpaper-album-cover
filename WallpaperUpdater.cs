// 引入必要的命名空间
using System; // 基本的系统功能
using System.IO; // 文件和数据流的输入输出操作
using System.Linq; // 提供数据查询的功能
using System.Collections.Concurrent; // 提供线程安全的集合类
using System.Collections.Generic; // 使用泛型集合
using System.Threading.Tasks; // 进行异步编程和并行编程
using SixLabors.ImageSharp; // 图像处理
using SixLabors.ImageSharp.PixelFormats; // 处理像素格式
using System.Threading; // 多线程编程

// 壁纸更新器类，实现IDisposable接口用于资源管理
public class WallpaperUpdater : IDisposable
{
    private const int MaxLoadRetries = 3; // 最大重试次数常量
    private const double GridUpdateIntervalSeconds = 10; // 网格更新检查间隔时间（秒）

    // 允许的文件扩展名集合
    private readonly HashSet<string> _allowedExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".bmp" };

    // 更新间隔时间变量
    private readonly int _minInterval;
    private readonly int _maxInterval;

    // 异步操作锁
    private readonly SemaphoreSlim _updateLock = new SemaphoreSlim(1, 1);

    // 文件夹路径和目标文件夹路径
    private readonly string _folderPath;
    private readonly string _destFolder;

    // 壁纸尺寸和网格数量
    private readonly int _width;
    private readonly int _height;
    private readonly int _rows;
    private readonly int _cols;

    // 图像管理器
    private ImageManager _imageManager;

    // 封面路径列表
    private List<string> _coverPaths;

    // 壁纸和网格列表
    private Image<Rgba32> _wallpaper;
    private List<Grid> _grids;

    // 定时器和更新时间字典
    private System.Threading.Timer _timer;
    private ConcurrentDictionary<Grid, DateTime> _lastUpdateTimes;

    // 更新状态标记
    private bool _isFirstUpdate = true;
    private bool _disposed = false;

    // 构造函数，初始化壁纸更新器
    public WallpaperUpdater(
        string folderPath, 
        string destFolder, 
        int width, 
        int height, 
        int rows, 
        int cols, 
        ImageManager imageManager,
        int minInterval, 
        int maxInterval  
    )
    {
        // 参数检查和赋值
        _folderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath), "文件夹路径不能为 null。");
        _destFolder = destFolder ?? throw new ArgumentNullException(nameof(destFolder), "目标文件夹路径不能为 null。");
        _width = width;
        _height = height;
        _rows = rows;
        _cols = cols;
        _imageManager = imageManager ?? throw new ArgumentNullException(nameof(imageManager), "图像管理器不能为 null。");
        _minInterval = minInterval; 
        _maxInterval = maxInterval; 
        _lastUpdateTimes = new ConcurrentDictionary<Grid, DateTime>();
        _coverPaths = new List<string>();
        _grids = new List<Grid>();
    }

    // 开始更新壁纸的方法
    public void Start()
    {
        // 检查文件夹路径
        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Console.WriteLine("文件夹路径无效。");
            return;
        }

        // 加载封面和初始化壁纸和网格
        LoadAlbumCovers();
        InitializeWallpaperAndGrids();
        ScheduleUpdate();
    }

    // 加载封面图片的方法
    private void LoadAlbumCovers()
    {
        int retries = 0;
        while (retries < MaxLoadRetries)
        {
            try
            {
                // 筛选允许的文件扩展名，并排除已经作为壁纸的图片
                _coverPaths = Directory.EnumerateFiles(_folderPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(file => _allowedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()) &&
                                   !file.EndsWith("wallpaper.jpg", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载封面路径时发生错误：{ex.Message}");
                retries++;
                if (retries >= MaxLoadRetries)
                {
                    Console.WriteLine("无法加载封面图片，将使用默认封面。");
                    // 可以添加默认封面处理逻辑
                }
            }
        }
    }

   private void InitializeWallpaperAndGrids()
{
    // 基于壁纸尺寸和网格数量计算每个网格的基本尺寸
    int baseGridSize = Math.Min(_width / _cols, _height / _rows);

    // 计算所有网格占用的总宽度和高度
    int totalGridWidth = baseGridSize * _cols;
    int totalGridHeight = baseGridSize * _rows;
    // 计算剩余的宽度和高度
    int remainingWidth = _width - totalGridWidth;
    int remainingHeight = _height - totalGridHeight;

    // 如果网格总尺寸超过壁纸尺寸，则适当减小网格尺寸
    if (remainingWidth < 0) {
        baseGridSize -= 1; // 减小网格宽度
        totalGridWidth = baseGridSize * _cols;
        remainingWidth = _width - totalGridWidth;
    }

    if (remainingHeight < 0) {
        baseGridSize -= 1; // 减小网格高度
        totalGridHeight = baseGridSize * _rows;
        remainingHeight = _height - totalGridHeight;
    }

    // 根据剩余空间计算网格间的间隙大小
    int gapSizeWidth = _cols > 1 ? remainingWidth / (_cols - 1) : 0;
    int gapSizeHeight = _rows > 1 ? remainingHeight / (_rows - 1) : 0;

    // 初始化壁纸图像和网格列表
    _wallpaper = new SixLabors.ImageSharp.Image<Rgba32>(_width, _height);
    _grids = new List<Grid>();

    // 创建网格，并添加到网格列表中
    for (int i = 0; i < _rows * _cols; i++)
    {
        int col = i % _cols;
        int row = i / _cols;

        int offsetX = col * (baseGridSize + gapSizeWidth);
        int offsetY = row * (baseGridSize + gapSizeHeight);

        SixLabors.ImageSharp.PointF topLeft = new SixLabors.ImageSharp.PointF(offsetX, offsetY);
        Grid grid = new Grid(topLeft, new SixLabors.ImageSharp.SizeF(baseGridSize, baseGridSize), _imageManager);
        _grids.Add(grid);
    }
}

private void ScheduleUpdate()
{
    // 创建一个定时器，用于定期更新壁纸
    _timer = new System.Threading.Timer(async _ =>
    {
        await UpdateWallpaper(); // 调用异步更新壁纸的方法
    }, null, TimeSpan.Zero, Timeout.InfiniteTimeSpan); // 设定立即触发一次更新，后续更新由代码控制
}

private async Task UpdateWallpaper()
{
    try
    {
        // 获取异步操作的锁
        await _updateLock.WaitAsync();

        var now = DateTime.Now;
        List<Grid> updateCandidates;

        // 判断是不是首次更新
        if (_isFirstUpdate)
        {
            updateCandidates = _grids.ToList();
            _isFirstUpdate = false;
        }
        else
        {
            // 根据上次更新时间选择需要更新的网格
            updateCandidates = _grids.Where(g => 
                !_lastUpdateTimes.ContainsKey(g) || 
                (now - _lastUpdateTimes[g]).TotalSeconds >= GridUpdateIntervalSeconds).ToList();

            int maxUpdateCount = Math.Min(updateCandidates.Count, _grids.Count / 4) + 1;
            int updateCount = (updateCandidates.Count <= 3) 
                ? updateCandidates.Count 
                : Random.Shared.Next(3, maxUpdateCount);
            updateCandidates = updateCandidates.OrderBy(x => Guid.NewGuid()).Take(updateCount).ToList();
        }

        var currentCovers = _grids.Select(g => g.CurrentCoverPath).ToList();
        var availableCovers = _coverPaths.Except(currentCovers).ToList();

        // 更新选定的网格
        foreach (var grid in updateCandidates)
        {
            if (!availableCovers.Any()) 
            {
                Console.WriteLine("没有可用的封面进行更新。");
                break;
            }

            var newCoverPath = availableCovers[Random.Shared.Next(availableCovers.Count)];
            availableCovers.Remove(newCoverPath);

            await grid.UpdateCoverAsync(newCoverPath, _wallpaper);
            _lastUpdateTimes[grid] = now;
        }

        // 保存并设置新的壁纸
        string wallpaperPath = Path.Combine(_destFolder, "wallpaper.jpg");
        _wallpaper.SaveAsJpeg(wallpaperPath);
        Wallpaper.Set(wallpaperPath);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"更新壁纸时发生错误: {ex.Message}");
        // 可以添加错误处理逻辑
    }
    finally
    {
        // 释放锁
        _updateLock.Release();

        // 如果对象未被销毁，计划下一次更新
        if (!_disposed)
        {
            var nextUpdateInSeconds = Random.Shared.Next(_minInterval, _maxInterval + 1);
            _timer?.Change(TimeSpan.FromSeconds(nextUpdateInSeconds), Timeout.InfiniteTimeSpan);
        }
    }
}

public void Dispose()
{
    if (!_disposed)
    {
        _disposed = true;

        // 释放定时器、壁纸和锁等资源
        _timer?.Dispose();
        _timer = null;

        _wallpaper?.Dispose();
        _updateLock?.Dispose();

        // 防止垃圾回收器重复处理
        GC.SuppressFinalize(this);
    }
}
}

