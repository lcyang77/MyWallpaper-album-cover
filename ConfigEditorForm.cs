using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Size = System.Drawing.Size;

// 定义配置编辑器窗体类，继承自Form
public class ConfigEditorForm : Form
{
    private Configuration config; // 配置对象
    private string configPath; // 配置文件路径

    // 控件声明
    // 初始化各种文本框和数字调节控件，用于输入和调节配置参数
    private TextBox folderPathTextBox = new TextBox();
    private TextBox destFolderTextBox = new TextBox();
    private NumericUpDown widthNumericUpDown = new NumericUpDown { Maximum = 10000, Minimum = 1 };
    private NumericUpDown heightNumericUpDown = new NumericUpDown { Maximum = 10000, Minimum = 1 };
    private NumericUpDown rowsNumericUpDown = new NumericUpDown { Maximum = 100, Minimum = 1 };
    private NumericUpDown colsNumericUpDown = new NumericUpDown { Maximum = 100, Minimum = 1 };
    private NumericUpDown minIntervalNumericUpDown = new NumericUpDown { Maximum = 3600, Minimum = 1 };
    private NumericUpDown maxIntervalNumericUpDown = new NumericUpDown { Maximum = 3600, Minimum = 1 };
    private Button confirmButton = new Button { Text = "确认" };
    private Button cancelButton = new Button { Text = "取消" };

    // 构造函数，初始化窗体
    public ConfigEditorForm(string configPath)
    {
        this.configPath = configPath;
        InitializeFormControls(); // 初始化窗体控件
        LoadConfiguration(); // 加载配置
    }

    // 初始化窗体控件
    private void InitializeFormControls()
    {
        // 设置窗体的属性，如标题、大小、边框风格等
        this.Text = "配置编辑器";
        this.ClientSize = new Size(300, 300);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        // 创建并配置TableLayoutPanel，用于组织控件的布局
        TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 9,
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        // 设置列宽和行高
        tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        for (int i = 0; i < 9; i++)
        {
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        }

    // 初始化控件
    folderPathTextBox = new TextBox();
    destFolderTextBox = new TextBox();
    widthNumericUpDown = new NumericUpDown { Maximum = 10000, Minimum = 1 };
    heightNumericUpDown = new NumericUpDown { Maximum = 10000, Minimum = 1 };
    rowsNumericUpDown = new NumericUpDown { Maximum = 100, Minimum = 1 };
    colsNumericUpDown = new NumericUpDown { Maximum = 100, Minimum = 1 };
    minIntervalNumericUpDown = new NumericUpDown { Maximum = 3600, Minimum = 1 };
    maxIntervalNumericUpDown = new NumericUpDown { Maximum = 3600, Minimum = 1 };
    confirmButton = new Button { Text = "确认" };
    cancelButton = new Button { Text = "取消" };

    // 添加控件到 TableLayoutPanel
    tableLayoutPanel.Controls.Add(new Label { Text = "封面图片路径", TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
    tableLayoutPanel.Controls.Add(folderPathTextBox, 1, 0);
    tableLayoutPanel.Controls.Add(new Label { Text = "目标文件夹路径", TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
    tableLayoutPanel.Controls.Add(destFolderTextBox, 1, 1);
    tableLayoutPanel.Controls.Add(new Label { Text = "宽度（像素）", TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
    tableLayoutPanel.Controls.Add(widthNumericUpDown, 1, 2);
    tableLayoutPanel.Controls.Add(new Label { Text = "高度（像素）", TextAlign = ContentAlignment.MiddleLeft }, 0, 3);
    tableLayoutPanel.Controls.Add(heightNumericUpDown, 1, 3);
    tableLayoutPanel.Controls.Add(new Label { Text = "行数", TextAlign = ContentAlignment.MiddleLeft }, 0, 4);
    tableLayoutPanel.Controls.Add(rowsNumericUpDown, 1, 4);
    tableLayoutPanel.Controls.Add(new Label { Text = "列数", TextAlign = ContentAlignment.MiddleLeft }, 0, 5);
    tableLayoutPanel.Controls.Add(colsNumericUpDown, 1, 5);
    tableLayoutPanel.Controls.Add(new Label { Text = "最小间隔（秒）", TextAlign = ContentAlignment.MiddleLeft }, 0, 6);
    tableLayoutPanel.Controls.Add(minIntervalNumericUpDown, 1, 6);
    tableLayoutPanel.Controls.Add(new Label { Text = "最大间隔（秒）", TextAlign = ContentAlignment.MiddleLeft }, 0, 7);
    tableLayoutPanel.Controls.Add(maxIntervalNumericUpDown, 1, 7);

    // 创建并设置FlowLayoutPanel属性，用于放置确认和取消按钮
    FlowLayoutPanel buttonPanel = new FlowLayoutPanel
    {
        FlowDirection = FlowDirection.RightToLeft,
        AutoSize = false, // 设置为 false 来手动指定大小
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(0, 10, 10, 0),
        Height = 40, // 明确设置高度
        Dock = DockStyle.Bottom // 确保它始终停靠在底部
    };

        // 将按钮添加到FlowLayoutPanel
        buttonPanel.Controls.Add(confirmButton);
        buttonPanel.Controls.Add(cancelButton);

        // 为按钮添加事件处理器
        confirmButton.Click += ConfirmButton_Click;
        cancelButton.Click += CancelButton_Click;

        // 将LayoutPanel和FlowLayoutPanel添加到窗体
        this.Controls.Add(tableLayoutPanel);
        this.Controls.Add(buttonPanel);
    }


    private void LoadConfiguration()
    {
        var configText = File.ReadAllText(configPath);
        var configuration = JsonSerializer.Deserialize<Configuration>(configText);

        // 检查配置是否为null
        if (configuration == null)
        {
            MessageBox.Show("配置文件格式不正确或者为空。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.Close(); // 如果配置文件为空，关闭表单
            return;
        }
        
        config = configuration; // 如果配置不为null，赋值给字段
        // 加载配置到控件
        folderPathTextBox.Text = config.FolderPath;
        destFolderTextBox.Text = config.DestFolder;
        widthNumericUpDown.Value = config.Width;
        heightNumericUpDown.Value = config.Height;
        rowsNumericUpDown.Value = config.Rows;
        colsNumericUpDown.Value = config.Cols;
        minIntervalNumericUpDown.Value = config.MinInterval;
        maxIntervalNumericUpDown.Value = config.MaxInterval;
    }

    private void SaveConfiguration()
{
    try
    {
        // 验证 MinInterval 和 MaxInterval
        if (minIntervalNumericUpDown.Value > maxIntervalNumericUpDown.Value)
        {
            MessageBox.Show("最小间隔不能大于最大间隔！", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return; // 早期返回，不保存配置
        }

        // 从控件获取值并更新配置对象
        config.FolderPath = folderPathTextBox.Text;
        config.DestFolder = destFolderTextBox.Text;
        config.Width = (int)widthNumericUpDown.Value;
        config.Height = (int)heightNumericUpDown.Value;
        config.Rows = (int)rowsNumericUpDown.Value;
        config.Cols = (int)colsNumericUpDown.Value;
        config.MinInterval = (int)minIntervalNumericUpDown.Value;
        config.MaxInterval = (int)maxIntervalNumericUpDown.Value;

        // 序列化配置对象并写入文件
        var configText = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, configText);

        // 不再需要在这里显示消息框
    }
    catch (Exception ex)
    {
        // 如果发生异常，提示用户
        MessageBox.Show($"保存配置时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        throw; // 抛出异常以便于在调用方处理
    }
}


  private void ConfirmButton_Click(object sender, EventArgs e)
    {
        if (ValidateConfiguration())
        {
            try
            {
                SaveConfiguration();
                MessageBox.Show("配置已保存。请重启程序以应用更改。", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close(); // 保存后关闭窗口
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // 可以在这里添加日志记录或其他异常处理代码
            }
        }
    }

    private bool ValidateConfiguration()
    {
        if (minIntervalNumericUpDown.Value > maxIntervalNumericUpDown.Value)
        {
            MessageBox.Show("最小间隔不能大于最大间隔！", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false; // 验证失败
        }
        if (string.IsNullOrWhiteSpace(folderPathTextBox.Text))
    {
        MessageBox.Show("封面图片路径不能为空！", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }
    
    if (string.IsNullOrWhiteSpace(destFolderTextBox.Text))
    {
        MessageBox.Show("目标文件夹路径不能为空！", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }

    // 路径有效性检查
    if (!Directory.Exists(folderPathTextBox.Text))
    {
        MessageBox.Show("封面图片路径不存在！", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }

    if (!Directory.Exists(destFolderTextBox.Text))
    {
        MessageBox.Show("目标文件夹路径不存在！", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }

    // 数值范围检查
    if (widthNumericUpDown.Value <= 0 || heightNumericUpDown.Value <= 0)
    {
        MessageBox.Show("宽度和高度必须大于0！", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }

    // 依赖性检查
    if (minIntervalNumericUpDown.Value > maxIntervalNumericUpDown.Value)
    {
        MessageBox.Show("最小间隔不能大于最大间隔！", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }

        return true; // 验证成功
    }
    private void CancelButton_Click(object sender, EventArgs e)
    {
        this.Close(); // 关闭窗口
    }
}
