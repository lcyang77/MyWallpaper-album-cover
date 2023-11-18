// 引入必要的命名空间
using System;
using System.Runtime.InteropServices; // 用于调用Windows API
using System.IO; // 文件输入输出操作
using System.Runtime.Versioning; // 用于识别操作系统平台

// 定义一个静态类，用于设置壁纸
public static class Wallpaper
{
    // 定义与Windows API相关的常量
    private const int SPI_SETDESKWALLPAPER = 20; // 设置桌面壁纸的参数
    private const int SPIF_UPDATEINIFILE = 0x01; // 更新INI文件
    private const int SPIF_SENDCHANGE = 0x02; // 发送设置更改通知

    // 导入user32.dll中的SystemParametersInfo函数，用于设置壁纸
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    // 公共方法，用于设置壁纸
    public static void Set(string path)
    {
        // 验证壁纸路径的有效性
        ValidateWallpaperPath(path);

        // 判断操作系统是否为Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // 如果是Windows，则设置壁纸
            SetWindowsWallpaper(path);
        }
        else
        {
            // 对于其他操作系统，抛出不支持的异常
            throw new PlatformNotSupportedException("当前操作系统不支持自动设置壁纸。");
        }
    }

    // Windows平台设置壁纸的方法
    private static void SetWindowsWallpaper(string path)
    {
        // 调用SystemParametersInfo函数设置壁纸
        if (SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE) == 0)
        {
            // 如果返回值为0，表示设置失败，抛出异常
            throw new InvalidOperationException("无法设置桌面壁纸。可能是由于操作系统错误或权限不足。");
        }
    }

    // 验证壁纸路径的方法
    private static void ValidateWallpaperPath(string path)
    {
        // 检查路径是否为空或空白
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("路径不能为空或空白。", nameof(path));
        }

        // 检查文件是否存在
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("找不到指定的文件。", path);
        }
    }
}
