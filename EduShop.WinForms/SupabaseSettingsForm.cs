using System;
using System.Windows.Forms;
using EduShop.Core.Infrastructure.Supabase;
using EduShop.Core.Repositories.Remote;
using EduShop.WinForms.Infrastructure;
using Supabase.Gotrue;

namespace EduShop.WinForms;

public class SupabaseSettingsForm : Form
{
    private readonly SupabaseSessionState _sessionState;

    private TextBox _txtUrl = null!;
    private TextBox _txtAnonKey = null!;
    private CheckBox _chkEnabled = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtPassword = null!;
    private Button _btnSignIn = null!;
    private Button _btnTest = null!;
    private Button _btnSave = null!;
    private Button _btnClose = null!;
    private Label _lblLoginStatus = null!;

    public SupabaseSettingsForm(SupabaseSessionState sessionState)
    {
        _sessionState = sessionState;

        Text = "Supabase 설정";
        Width = 620;
        Height = 360;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        BindState();
    }

    private void InitializeControls()
    {
        var lblUrl = new Label
        {
            Text = "Project URL",
            Left = 20,
            Top = 20,
            Width = 100
        };

        _txtUrl = new TextBox
        {
            Left = 130,
            Top = 16,
            Width = 440
        };

        var lblKey = new Label
        {
            Text = "Anon Key",
            Left = 20,
            Top = 55,
            Width = 100
        };

        _txtAnonKey = new TextBox
        {
            Left = 130,
            Top = 51,
            Width = 440
        };

        _chkEnabled = new CheckBox
        {
            Text = "Supabase 사용",
            Left = 130,
            Top = 85,
            Width = 160
        };

        var grpLogin = new GroupBox
        {
            Text = "로그인",
            Left = 20,
            Top = 120,
            Width = 550,
            Height = 130
        };

        var lblEmail = new Label
        {
            Text = "Email",
            Left = 15,
            Top = 30,
            Width = 70
        };

        _txtEmail = new TextBox
        {
            Left = 90,
            Top = 26,
            Width = 200
        };

        var lblPassword = new Label
        {
            Text = "Password",
            Left = 15,
            Top = 60,
            Width = 70
        };

        _txtPassword = new TextBox
        {
            Left = 90,
            Top = 56,
            Width = 200,
            UseSystemPasswordChar = true
        };

        _btnSignIn = new Button
        {
            Text = "Sign in",
            Left = 320,
            Top = 26,
            Width = 90
        };
        _btnSignIn.Click += async (_, _) => await SignInAsync();

        _btnTest = new Button
        {
            Text = "Test Connection",
            Left = 320,
            Top = 56,
            Width = 120
        };
        _btnTest.Click += async (_, _) => await TestConnectionAsync();

        _lblLoginStatus = new Label
        {
            Left = 90,
            Top = 90,
            Width = 440,
            ForeColor = System.Drawing.Color.DimGray
        };

        grpLogin.Controls.Add(lblEmail);
        grpLogin.Controls.Add(_txtEmail);
        grpLogin.Controls.Add(lblPassword);
        grpLogin.Controls.Add(_txtPassword);
        grpLogin.Controls.Add(_btnSignIn);
        grpLogin.Controls.Add(_btnTest);
        grpLogin.Controls.Add(_lblLoginStatus);

        _btnSave = new Button
        {
            Text = "저장",
            Left = 370,
            Top = 265,
            Width = 90
        };
        _btnSave.Click += (_, _) => SaveSettings();

        _btnClose = new Button
        {
            Text = "닫기",
            Left = 480,
            Top = 265,
            Width = 90
        };
        _btnClose.Click += (_, _) => Close();

        Controls.Add(lblUrl);
        Controls.Add(_txtUrl);
        Controls.Add(lblKey);
        Controls.Add(_txtAnonKey);
        Controls.Add(_chkEnabled);
        Controls.Add(grpLogin);
        Controls.Add(_btnSave);
        Controls.Add(_btnClose);
    }

    private void BindState()
    {
        _txtUrl.Text = _sessionState.Config.Url;
        _txtAnonKey.Text = _sessionState.Config.AnonKey;
        _chkEnabled.Checked = _sessionState.Config.Enabled;
        UpdateLoginStatus();
    }

    private SupabaseConfig GetEditingConfig()
    {
        return new SupabaseConfig
        {
            Url = _txtUrl.Text.Trim(),
            AnonKey = _txtAnonKey.Text.Trim(),
            Enabled = _chkEnabled.Checked
        };
    }

    private void SaveSettings()
    {
        var config = GetEditingConfig();
        LocalSettingsStore.SaveSupabaseConfig(config);
        _sessionState.UpdateConfig(config);
        UpdateLoginStatus();

        MessageBox.Show("Supabase 설정이 저장되었습니다.", "완료",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task SignInAsync()
    {
        try
        {
            var config = GetEditingConfig();
            var client = await SupabaseClientFactory.CreateAsync(config);

            var session = await client.Auth.SignIn(_txtEmail.Text.Trim(), _txtPassword.Text);
            if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
            {
                MessageBox.Show("로그인에 실패했습니다.", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _sessionState.SetAccessToken(session.AccessToken);
            UpdateLoginStatus(session);

            MessageBox.Show("로그인에 성공했습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"로그인 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            var config = GetEditingConfig();
            var client = await SupabaseClientFactory.CreateAsync(config, _sessionState.AccessToken);
            var repo = new RemoteCustomerRepository(client);
            await repo.GetAllAsync();

            MessageBox.Show("Supabase 연결에 성공했습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Supabase 연결 실패: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateLoginStatus(Session? session = null)
    {
        if (!_sessionState.HasAccessToken)
        {
            _lblLoginStatus.Text = "로그인 상태: 미인증";
            return;
        }

        var email = session?.User?.Email ?? _txtEmail.Text.Trim();
        _lblLoginStatus.Text = string.IsNullOrWhiteSpace(email)
            ? "로그인 상태: 인증됨"
            : $"로그인 상태: {email}";
    }
}
