using System;
using System.IO;
using System.Windows.Forms;

namespace EduShop.WinForms;

public class SettingsForm : Form
{
    private NumericUpDown _numExpiringDays = null!;
    private TextBox _txtEncoding = null!;
    private TextBox _txtQuoteFolder = null!;
    private Button _btnBrowse = null!;
    private Button _btnOk = null!;
    private Button _btnCancel = null!;

    public SettingsForm()
    {
        Text = "환경 설정";
        Width = 500;
        Height = 260;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        InitializeControls();
        LoadSettings();
    }

    private void InitializeControls()
    {
        var lblExpiring = new Label
        {
            Text = "만료 예정 계정 기준일 (일)",
            Left = 20,
            Top  = 20,
            Width = 200
        };

        _numExpiringDays = new NumericUpDown
        {
            Left = 240,
            Top  = 18,
            Width = 80,
            Minimum = 1,
            Maximum = 365,
            Value = 30
        };

        var lblEncoding = new Label
        {
            Text = "CSV 인코딩",
            Left = 20,
            Top  = 60,
            Width = 200
        };

        _txtEncoding = new TextBox
        {
            Left = 240,
            Top  = 56,
            Width = 200
        };

        var lblQuoteFolder = new Label
        {
            Text = "견적서 기본 저장 폴더",
            Left = 20,
            Top  = 100,
            Width = 200
        };

        _txtQuoteFolder = new TextBox
        {
            Left = 20,
            Top  = 125,
            Width = 350
        };

        _btnBrowse = new Button
        {
            Text = "찾아보기...",
            Left = _txtQuoteFolder.Right + 5,
            Top  = 123,
            Width = 80
        };
        _btnBrowse.Click += (_, _) => BrowseFolder();

        _btnOk = new Button
        {
            Text = "확인",
            Width = 80,
            Left = ClientSize.Width - 190,
            Top  = ClientSize.Height - 50,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnOk.Click += (_, _) => SaveAndClose();

        _btnCancel = new Button
        {
            Text = "취소",
            Width = 80,
            Left = ClientSize.Width - 100,
            Top  = ClientSize.Height - 50,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.Add(lblExpiring);
        Controls.Add(_numExpiringDays);
        Controls.Add(lblEncoding);
        Controls.Add(_txtEncoding);
        Controls.Add(lblQuoteFolder);
        Controls.Add(_txtQuoteFolder);
        Controls.Add(_btnBrowse);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
    }

    private void LoadSettings()
    {
        var s = AppSettingsManager.Current;

        _numExpiringDays.Value = s.ExpiringDays;
        _txtEncoding.Text      = s.CsvEncodingName;
        _txtQuoteFolder.Text   = s.QuoteOutputFolder;
    }

    private void BrowseFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "견적서 기본 저장 위치를 선택하세요."
        };

        if (Directory.Exists(_txtQuoteFolder.Text))
            dlg.SelectedPath = _txtQuoteFolder.Text;

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _txtQuoteFolder.Text = dlg.SelectedPath;
        }
    }

    private void SaveAndClose()
    {
        if (_numExpiringDays.Value <= 0)
        {
            MessageBox.Show("만료 예정 기준일은 1일 이상이어야 합니다.", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtEncoding.Text))
        {
            MessageBox.Show("CSV 인코딩을 입력하세요. (예: UTF-8)", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var s = AppSettingsManager.Current;

        s.ExpiringDays     = (int)_numExpiringDays.Value;
        s.CsvEncodingName  = _txtEncoding.Text.Trim();
        s.QuoteOutputFolder = _txtQuoteFolder.Text.Trim();

        try
        {
            AppSettingsManager.Save();
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"설정 저장 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
