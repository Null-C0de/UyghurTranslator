using System.Drawing;
using System.Windows.Forms;

namespace UyghurTranslator;

public class MainForm : Form
{
    private readonly TranslatorEngine _translator;
    private readonly TTSEngine _tts;
    
    private ComboBox _languageCombo = null!;
    private TextBox _inputTextBox = null!;
    private TextBox _outputTextBox = null!;
    private TrackBar _volumeTrackBar = null!;
    private Label _statusLabel = null!;
    private Label _volumeLabel = null!;

    public MainForm()
    {
        _translator = new TranslatorEngine();
        _tts = new TTSEngine();
        SetupForm();
    }

    private void SetupForm()
    {
        Text = "汉维互译翻译器";
        ClientSize = new Size(800, 600);
        MinimumSize = new Size(600, 450);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 247, 250);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        
        int y = 20;
        int margin = 20;
        int width = ClientSize.Width - margin * 2;
        
        // 标题
        var titleLabel = new Label
        {
            Text = "汉维互译翻译器",
            Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(41, 128, 185),
            Location = new Point(margin, y),
            Size = new Size(width, 40),
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(titleLabel);
        y += 50;

        // 控制栏
        var langLabel = new Label
        {
            Text = "翻译方向:",
            Font = new Font("Microsoft YaHei UI", 10F),
            Location = new Point(margin, y + 5),
            AutoSize = true
        };
        Controls.Add(langLabel);

        _languageCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(margin + 80, y),
            Size = new Size(150, 28),
            Font = new Font("Microsoft YaHei UI", 10F)
        };
        _languageCombo.Items.AddRange(new object[] { "汉语 → 维语", "维语 → 汉语" });
        _languageCombo.SelectedIndex = 0;
        Controls.Add(_languageCombo);

        // 音量控制
        var volumeIcon = new Label
        {
            Text = "🔊",
            Font = new Font("Segoe UI Emoji", 12),
            Location = new Point(width - 200, y + 3),
            AutoSize = true
        };
        Controls.Add(volumeIcon);

        _volumeTrackBar = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 100,
            Location = new Point(width - 170, y),
            Size = new Size(100, 30),
            TickStyle = TickStyle.None
        };
        _volumeTrackBar.Scroll += (_, _) =>
        {
            var vol = _volumeTrackBar.Value / 100f;
            _tts.SetVolume(vol);
            _volumeLabel.Text = $"{_volumeTrackBar.Value}%";
        };
        Controls.Add(_volumeTrackBar);

        _volumeLabel = new Label
        {
            Text = "100%",
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(width - 65, y + 5),
            AutoSize = true
        };
        Controls.Add(_volumeLabel);
        y += 40;

        // 输入标签
        var inputLabel = new Label
        {
            Text = "输入文本:",
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(52, 73, 94),
            Location = new Point(margin, y),
            AutoSize = true
        };
        Controls.Add(inputLabel);
        y += 25;

        // 输入文本框
        _inputTextBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Microsoft YaHei UI", 12F),
            Location = new Point(margin, y),
            Size = new Size(width, 120),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(_inputTextBox);
        y += 125;

        // 输入按钮 - 只保留清空
        var clearBtn = CreateButton("🗑 清空", Color.FromArgb(231, 76, 60), (_, _) => ClearAll(), 80);
        clearBtn.Location = new Point(margin, y);
        Controls.Add(clearBtn);
        y += 45;

        // 翻译按钮
        var translateBtn = new Button
        {
            Text = "🌐 翻 译",
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
            Size = new Size(200, 45),
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        translateBtn.FlatAppearance.BorderSize = 0;
        translateBtn.Location = new Point((ClientSize.Width - 200) / 2, y);
        translateBtn.Click += async (_, _) => await TranslateAsync();
        Controls.Add(translateBtn);
        y += 55;

        // 输出标签
        var outputLabel = new Label
        {
            Text = "翻译结果:",
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(52, 73, 94),
            Location = new Point(margin, y),
            AutoSize = true
        };
        Controls.Add(outputLabel);
        y += 25;

        // 输出文本框
        _outputTextBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Microsoft YaHei UI", 12F),
            Location = new Point(margin, y),
            Size = new Size(width, 100),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true
        };
        Controls.Add(_outputTextBox);
        y += 105;

        // 输出按钮 - 维语朗读和复制
        var speakOutputBtn = CreateButton("🔊 朗读维语", Color.FromArgb(39, 174, 96), async (_, _) => await SpeakOutputAsync());
        speakOutputBtn.Location = new Point(margin, y);
        Controls.Add(speakOutputBtn);

        var copyBtn = CreateButton("📋 复制", Color.FromArgb(155, 89, 182), (_, _) => CopyOutput(), 80);
        copyBtn.Location = new Point(margin + 130, y);
        Controls.Add(copyBtn);
        y += 45;

        // 状态栏
        _statusLabel = new Label
        {
            Text = "就绪 (仅支持维语朗读)",
            ForeColor = Color.FromArgb(127, 140, 141),
            Font = new Font("Microsoft YaHei UI", 9F),
            Location = new Point(margin, y),
            AutoSize = true
        };
        Controls.Add(_statusLabel);

        Resize += (_, _) => AdjustLayout();
    }

    private void AdjustLayout()
    {
        int margin = 20;
        int width = ClientSize.Width - margin * 2;
        
        foreach (Control ctrl in Controls)
        {
            if (ctrl is Label { Text: "汉维互译翻译器" } title)
                title.Size = new Size(width, 40);
            else if (ctrl is TextBox { Multiline: true } tb)
                tb.Width = width;
        }
    }

    private Button CreateButton(string text, Color backColor, EventHandler clickHandler, int width = 120)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Microsoft YaHei UI", 10F),
            Size = new Size(width, 35),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += clickHandler;
        return btn;
    }

    private async Task TranslateAsync()
    {
        var text = _inputTextBox.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            MessageBox.Show("请输入要翻译的文本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _statusLabel.Text = "翻译中...";

        try
        {
            var (result, newDirection) = await _translator.TranslateAsync(text, _languageCombo.Text);
            _outputTextBox.Text = result;
            if (!string.IsNullOrEmpty(newDirection))
                _languageCombo.Text = newDirection;
            _statusLabel.Text = "翻译完成";
        }
        catch (Exception ex)
        {
            _outputTextBox.Text = $"翻译出错: {ex.Message}";
            _statusLabel.Text = "翻译失败";
        }
    }

    private async Task SpeakOutputAsync()
    {
        var text = _outputTextBox.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            MessageBox.Show("请先翻译文本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_tts.IsSpeaking) return;

        _statusLabel.Text = "正在朗读维语...";

        var (success, error) = await _tts.SpeakAsync(text, "ug");

        _statusLabel.Text = success ? "就绪" : "朗读失败";

        if (!success)
        {
            MessageBox.Show(error, "朗读错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearAll()
    {
        _inputTextBox.Clear();
        _outputTextBox.Clear();
        _statusLabel.Text = "就绪 (仅支持维语朗读)";
    }

    private void CopyOutput()
    {
        if (!string.IsNullOrEmpty(_outputTextBox.Text))
        {
            Clipboard.SetText(_outputTextBox.Text);
            MessageBox.Show("已复制到剪贴板", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _translator.Dispose();
        _tts.Dispose();
        base.OnFormClosing(e);
    }
}
