using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

// 程序主类
class Program
{
    // 变量定义区域
    private static NotifyIcon? trayIcon; // 托盘图标变量
    private static ToolStripMenuItem? autoStartMenuItem; // 自启动菜单项
    private static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); // 应用数据路径
    private static string appFolder = Path.Combine(appDataPath, "MyWallpaperApp"); // 应用文件夹路径
    private static string configPath = Path.Combine(appFolder, "config.json"); // 配置文件路径
    private static WallpaperUpdater? updater; // 壁纸更新器实例
    private static Icon? appIcon; // 应用图标变量

    // 主函数入口
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        InitializeTrayIcon();

        // 检查应用文件夹是否存在，不存在则创建
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

        // 检查配置文件是否存在，不存在则尝试恢复或创建
        if (!File.Exists(configPath))
        {
            if (!RestoreBackupConfigFile()) // 尝试恢复备份配置文件
            {
                var defaultConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                if (File.Exists(defaultConfigPath))
                {
                    File.Copy(defaultConfigPath, configPath);
                }
                else
                {
                    MessageBox.Show("默认配置文件未找到。请确保应用程序目录中存在 config.json 文件。", "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        try
        {
            // 读取配置文件，创建和启动 WallpaperUpdater 实例
            var configText = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<Configuration>(configText);

            if (config == null)
            {
                MessageBox.Show("配置文件无法解析。请确保 config.json 的格式正确。", "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ValidateConfiguration(config);

            // 创建 ImageManager 实例
            ImageManager imageManager = new ImageManager(10); // 假设最大缓存大小为10

            // 创建并启动 WallpaperUpdater 实例
            updater = new WallpaperUpdater(
                config.FolderPath, 
                config.DestFolder, 
                config.Width, 
                config.Height, 
                config.Rows, 
                config.Cols, 
                imageManager,
                config.MinInterval, // 从配置中读取最小间隔
                config.MaxInterval // 从配置中读取最大间隔
            );
            updater.Start();

            // 成功读取和处理配置文件后进行备份
            BackupConfigFile();
        }
        catch (JsonException)
        {
            MessageBox.Show("配置文件格式错误。请确保 config.json 的格式正确。", "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        Application.Run();
    }

    // 备份配置文件函数
    private static void BackupConfigFile()
    {
        string backupConfigPath = Path.Combine(appFolder, "config_backup.json");
        File.Copy(configPath, backupConfigPath, true);
    }

    // 恢复备份配置文件函数
    private static bool RestoreBackupConfigFile()
    {
        string backupConfigPath = Path.Combine(appFolder, "config_backup.json");
        if (File.Exists(backupConfigPath))
        {
            File.Copy(backupConfigPath, configPath, true);
            return true;
        }
        return false;
    }

    // 初始化托盘图标函数
    private static void InitializeTrayIcon()
    {
        try
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string iconPath = Path.Combine(appDirectory, "appicon.ico");

            appIcon = new Icon(iconPath); // 直接创建图标

            trayIcon = new NotifyIcon()
            {
                Icon = appIcon,
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip()
            };

            // 添加菜单项
            autoStartMenuItem = new ToolStripMenuItem("开机自启动", null, ToggleAutoStart)
            {
                Checked = IsAutoStartEnabled()
            };
            trayIcon.ContextMenuStrip.Items.Add(autoStartMenuItem);

            // 其他菜单项
            trayIcon.ContextMenuStrip.Items.Add("配置编辑器", null, OpenConfigEditor);
            trayIcon.ContextMenuStrip.Items.Add("打开图片文件夹", null, OpenImageFolder);
            trayIcon.ContextMenuStrip.Items.Add("退出", null, OnExit);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"初始化系统托盘图标时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // 打开配置编辑器函数
    private static void OpenConfigEditor(object? sender, EventArgs e)
    {
        ConfigEditorForm editor = new ConfigEditorForm(configPath);
        editor.ShowDialog();
    }

    // 打开图片文件夹函数
    private static void OpenImageFolder(object? sender, EventArgs e)
    {
        if (File.Exists(configPath))
        {
            var configText = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<Configuration>(configText);
            if (config != null && !string.IsNullOrWhiteSpace(config.FolderPath) && Directory.Exists(config.FolderPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", config.FolderPath);
            }
            else
            {
                MessageBox.Show("图片文件夹路径无效或未设置。请先在配置编辑器中设置正确的路径。", "路径无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else
        {
            MessageBox.Show("未找到配置文件。请先创建配置文件。", "配置文件不存在", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // 开启或关闭自启动函数
    private static void ToggleAutoStart(object? sender, EventArgs e)
    {
        bool enabled = !IsAutoStartEnabled();
        SetAutoStart(enabled);
        autoStartMenuItem.Checked = enabled;
    }

    // 检查是否已开启自启动函数
    private static bool IsAutoStartEnabled()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
        {
            return key.GetValue("MyWallpaperApp") != null;
        }
    }

    // 设置自启动函数
    private static void SetAutoStart(bool enable)
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
        {
            if (enable)
            {
                string exePath = Application.ExecutablePath;
                key.SetValue("MyWallpaperApp", exePath);
            }
            else
            {
                key.DeleteValue("MyWallpaperApp", false);
            }
        }
    }

    // 退出程序函数
    private static void OnExit(object? sender, EventArgs e)
    {
        // 退出前释放 WallpaperUpdater 资源
        updater?.Dispose();

        // 释放图标资源
        appIcon?.Dispose();
        trayIcon?.Dispose();
        Application.Exit();
    }

    // 程序退出时的处理函数
    private static void OnProcessExit(object? sender, EventArgs e)
    {
        // 程序退出时确保 WallpaperUpdater 被正确释放
        updater?.Dispose();

        // 释放图标资源
        appIcon?.Dispose();
        trayIcon?.Dispose();
    }

    // 验证配置文件函数
    static void ValidateConfiguration(Configuration config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        
        if (string.IsNullOrWhiteSpace(config.FolderPath))
            throw new InvalidOperationException("源文件夹路径不能为空或 null。");
        
        if (!Directory.Exists(config.FolderPath))
            throw new InvalidOperationException("源文件夹路径无效或不存在。");

        if (string.IsNullOrWhiteSpace(config.DestFolder) || !Directory.Exists(config.DestFolder))
            throw new InvalidOperationException("目标文件夹路径无效或不存在。");

        if (config.Width <= 0 || config.Height <= 0)
            throw new InvalidOperationException("图片宽度和高度必须大于 0。");

        if (config.Rows <= 0 || config.Cols <= 0)
            throw new InvalidOperationException("行数和列数必须大于 0。");
    }
}

// 配置类定义
public class Configuration
{
    public string? FolderPath { get; set; } //图片文件夹路径

    public string? DestFolder { get; set; } //壁纸文件夹路径
    public int Width { get; set; }//壁纸宽度
    public int Height { get; set; }//壁纸高度
    public int Rows { get; set; }//壁纸网格行数
    public int Cols { get; set; }//壁纸网格列数
    public int MinInterval { get; set; } = 3;  // 更新最小时间默认值为 3
    public int MaxInterval { get; set; } = 10; // 更新最大时间默认值为 10
    //更新时间从MinInterval到MaxInterval之间的随机值
}
