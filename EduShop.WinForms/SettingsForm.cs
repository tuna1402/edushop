using System;
using System.IO;
using System.Windows.Forms;
using EduShop.Core.Common;

namespace EduShop.WinForms;

public class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly AppSettings _editing;

    private NumericUpDown _numExpiringDays = null!;
    private TextBox _txtDefaultExportFolder = null!;
    private TextBox _txtCompanyName = null!;
    private TextBox _txtCompanyContact = null!;
    private TextBox _txtCompanyPhone = null!;
    private TextBox _txtCompanyEmail = null!;
    private TextBox _txtCompanyAddress = null!;
    private Button _btnBrowseFolder = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        _editing = new AppSettings
        {
            ExpiringDays = settings.ExpiringDays,
            DefaultExportFolder = settings.DefaultExportFolder,
            CompanyName = settings.CompanyName,
            CompanyContact = settings.CompanyContact,
            CompanyPhone = settings.CompanyPhone,
            CompanyEmail = settings.CompanyEmail,
            CompanyAddress = settings.CompanyAddress
        };

        Text = "환경 설정";
        Width = 600;
        Height = 400;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        InitializeControls();
        BindSettingsToControls();
    }

    private void InitializeControls()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(15),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _numExpiringDays = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 365,
            Width = 100
        };

        _txtDefaultExportFolder = new TextBox { Width = 320 };
        _btnBrowseFolder = new Button
        {
            Text = "찾아보기...",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        _btnBrowseFolder.Click += (_, _) => BrowseFolder();

        _txtCompanyName = new TextBox { Width = 320 };
        _txtCompanyContact = new TextBox { Width = 320 };
        _txtCompanyPhone = new TextBox { Width = 320 };
        _txtCompanyEmail = new TextBox { Width = 320 };
        _txtCompanyAddress = new TextBox { Width = 320 };

        layout.Controls.Add(new Label { Text = "만료 예정 계정 기준일 (일)", AutoSize = true }, 0, 0);
        layout.Controls.Add(_numExpiringDays, 1, 0);

        layout.Controls.Add(new Label { Text = "기본 저장 경로 (CSV/견적)", AutoSize = true }, 0, 1);
        var folderPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        folderPanel.Controls.Add(_txtDefaultExportFolder);
        folderPanel.Controls.Add(_btnBrowseFolder);
        layout.Controls.Add(folderPanel, 1, 1);

        layout.Controls.Add(new Label { Text = "회사명/상호", AutoSize = true }, 0, 2);
        layout.Controls.Add(_txtCompanyName, 1, 2);

        layout.Controls.Add(new Label { Text = "담당자", AutoSize = true }, 0, 3);
        layout.Controls.Add(_txtCompanyContact, 1, 3);

        layout.Controls.Add(new Label { Text = "전화", AutoSize = true }, 0, 4);
        layout.Controls.Add(_txtCompanyPhone, 1, 4);

        layout.Controls.Add(new Label { Text = "이메일", AutoSize = true }, 0, 5);
        layout.Controls.Add(_txtCompanyEmail, 1, 5);

        layout.Controls.Add(new Label { Text = "주소", AutoSize = true }, 0, 6);
        layout.Controls.Add(_txtCompanyAddress, 1, 6);

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        _btnSave = new Button { Text = "저장", Width = 90 };
        _btnSave.Click += (_, _) => SaveSettings();

        _btnCancel = new Button { Text = "취소", Width = 90 };
        _btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttonPanel.Controls.Add(_btnSave);
        buttonPanel.Controls.Add(_btnCancel);

        layout.Controls.Add(buttonPanel, 1, 7);

        var container = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        container.Controls.Add(layout);
        Controls.Add(container);
    }

    private void BindSettingsToControls()
    {
        _numExpiringDays.Value = Math.Min(Math.Max(_editing.ExpiringDays, 1), 365);
        _txtDefaultExportFolder.Text = _editing.DefaultExportFolder;
        _txtCompanyName.Text = _editing.CompanyName;
        _txtCompanyContact.Text = _editing.CompanyContact;
        _txtCompanyPhone.Text = _editing.CompanyPhone;
        _txtCompanyEmail.Text = _editing.CompanyEmail;
        _txtCompanyAddress.Text = _editing.CompanyAddress;
    }

    private void BrowseFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "기본 저장 위치를 선택하세요."
        };

        if (Directory.Exists(_txtDefaultExportFolder.Text))
            dlg.SelectedPath = _txtDefaultExportFolder.Text;

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _txtDefaultExportFolder.Text = dlg.SelectedPath;
        }
    }

    private void SaveSettings()
    {
        _editing.ExpiringDays = (int)_numExpiringDays.Value;
        _editing.DefaultExportFolder = _txtDefaultExportFolder.Text.Trim();
        _editing.CompanyName = _txtCompanyName.Text.Trim();
        _editing.CompanyContact = _txtCompanyContact.Text.Trim();
        _editing.CompanyPhone = _txtCompanyPhone.Text.Trim();
        _editing.CompanyEmail = _txtCompanyEmail.Text.Trim();
        _editing.CompanyAddress = _txtCompanyAddress.Text.Trim();

        _settings.ExpiringDays = _editing.ExpiringDays;
        _settings.DefaultExportFolder = _editing.DefaultExportFolder;
        _settings.CompanyName = _editing.CompanyName;
        _settings.CompanyContact = _editing.CompanyContact;
        _settings.CompanyPhone = _editing.CompanyPhone;
        _settings.CompanyEmail = _editing.CompanyEmail;
        _settings.CompanyAddress = _editing.CompanyAddress;

        SettingsStorage.Save(_settings);
        DialogResult = DialogResult.OK;
        Close();
    }
}
