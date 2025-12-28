using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Models;
using EduShop.Core.Repositories;
using EduShop.Core.Services;
using EduShop.Core.Common;

namespace EduShop.WinForms;

public class LogViewerForm : Form
{
    private readonly AuditLogRepository        _auditRepo;
    private readonly AccountUsageLogRepository _usageRepo;
    private readonly ProductService            _productService;
    private readonly CustomerService           _customerService;
    private readonly AccountService            _accountService;
    private readonly UserContext               _currentUser;

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };

    // Audit controls
    private DateTimePicker _dtpAuditFrom = null!;
    private DateTimePicker _dtpAuditTo = null!;
    private TextBox _txtAuditUser = null!;
    private ComboBox _cboAuditTable = null!;
    private ComboBox _cboAuditAction = null!;
    private TextBox _txtAuditKeyword = null!;
    private DataGridView _dgvAudit = null!;

    // Usage controls
    private DateTimePicker _dtpUsageFrom = null!;
    private DateTimePicker _dtpUsageTo = null!;
    private TextBox _txtUsageAccount = null!;
    private TextBox _txtUsageCustomer = null!;
    private TextBox _txtUsageProduct = null!;
    private ComboBox _cboUsageAction = null!;
    private DataGridView _dgvUsage = null!;

    public LogViewerForm(
        AuditLogRepository        auditRepo,
        AccountUsageLogRepository usageRepo,
        ProductService            productService,
        CustomerService           customerService,
        AccountService            accountService,
        UserContext               currentUser)
    {
        _auditRepo       = auditRepo;
        _usageRepo       = usageRepo;
        _productService  = productService;
        _customerService = customerService;
        _accountService  = accountService;
        _currentUser     = currentUser;

        Text = "로그/이력 조회";
        Width = 1100;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        LoadDefaultFilters();
        LoadAuditLogs();
        LoadUsageLogs();
    }

    private void InitializeControls()
    {
        var auditTab = new TabPage("시스템 로그(AuditLog)") { Padding = new Padding(6) };
        var usageTab = new TabPage("계정 사용 로그(AccountUsageLog)") { Padding = new Padding(6) };

        auditTab.Controls.Add(CreateAuditLayout());
        usageTab.Controls.Add(CreateUsageLayout());

        _tabs.TabPages.Add(auditTab);
        _tabs.TabPages.Add(usageTab);

        Controls.Add(_tabs);
    }

    private Control CreateAuditLayout()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var filter = new Panel { Dock = DockStyle.Fill };

        var lblFrom = new Label { Text = "기간", Left = 10, Top = 12, Width = 40 };
        _dtpAuditFrom = new DateTimePicker { Left = lblFrom.Right + 5, Top = 8, Width = 150, Format = DateTimePickerFormat.Short };
        var lblTo = new Label { Text = "~", Left = _dtpAuditFrom.Right + 5, Top = 12, Width = 20 };
        _dtpAuditTo = new DateTimePicker { Left = lblTo.Right + 5, Top = 8, Width = 150, Format = DateTimePickerFormat.Short };

        var lblUser = new Label { Text = "사용자", Left = _dtpAuditTo.Right + 20, Top = 12, Width = 50 };
        _txtAuditUser = new TextBox { Left = lblUser.Right + 5, Top = 8, Width = 120 };

        var lblTable = new Label { Text = "테이블", Left = _txtAuditUser.Right + 20, Top = 12, Width = 50 };
        _cboAuditTable = new ComboBox { Left = lblTable.Right + 5, Top = 8, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
        _cboAuditTable.Items.AddRange(new object[] { "", "Product", "Account", "Customer" });
        _cboAuditTable.SelectedIndex = 0;

        var lblAction = new Label { Text = "액션", Left = _cboAuditTable.Right + 20, Top = 12, Width = 40 };
        _cboAuditAction = new ComboBox { Left = lblAction.Right + 5, Top = 8, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        _cboAuditAction.Items.AddRange(new object[]
        {
            "",
            "PRODUCT_CREATE",
            "PRODUCT_UPDATE",
            "PRODUCT_STATUS_CHANGE",
            "ACCOUNT_STATUS_CHANGE",
            "ACCOUNT_CREATE"
        });
        _cboAuditAction.SelectedIndex = 0;

        var lblKeyword = new Label { Text = "키워드", Left = _cboAuditAction.Right + 20, Top = 12, Width = 50 };
        _txtAuditKeyword = new TextBox { Left = lblKeyword.Right + 5, Top = 8, Width = 140 };

        var btnSearch = new Button { Text = "조회", Left = _txtAuditKeyword.Right + 10, Top = 6, Width = 80 };
        btnSearch.Click += (_, _) => LoadAuditLogs();
        var btnReset = new Button { Text = "초기화", Left = btnSearch.Right + 10, Top = 6, Width = 80 };
        btnReset.Click += (_, _) => ResetAuditFilters();

        filter.Controls.AddRange(new Control[]
        {
            lblFrom, _dtpAuditFrom, lblTo, _dtpAuditTo,
            lblUser, _txtAuditUser,
            lblTable, _cboAuditTable,
            lblAction, _cboAuditAction,
            lblKeyword, _txtAuditKeyword,
            btnSearch, btnReset
        });

        _dgvAudit = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false
        };

        _dgvAudit.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "일시",
            DataPropertyName = nameof(AuditLogEntry.EventTime),
            Width = 150,
            DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm:ss" }
        });
        _dgvAudit.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "사용자",
            DataPropertyName = nameof(AuditLogEntry.UserName),
            Width = 120
        });
        _dgvAudit.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "액션",
            DataPropertyName = nameof(AuditLogEntry.ActionType),
            Width = 160
        });
        _dgvAudit.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "테이블",
            DataPropertyName = nameof(AuditLogEntry.TableName),
            Width = 120
        });
        _dgvAudit.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "대상 코드",
            DataPropertyName = nameof(AuditLogEntry.TargetCode),
            Width = 160
        });
        _dgvAudit.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "설명",
            DataPropertyName = nameof(AuditLogEntry.Description),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });

        panel.Controls.Add(filter);
        panel.Controls.Add(_dgvAudit);
        _dgvAudit.Dock = DockStyle.Fill;
        filter.Dock = DockStyle.Top;

        _dgvAudit.CellDoubleClick += (_, _) => ShowAuditDetails();

        return panel;
    }

    private Control CreateUsageLayout()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var filter = new Panel { Dock = DockStyle.Fill };

        var lblFrom = new Label { Text = "기간", Left = 10, Top = 12, Width = 40 };
        _dtpUsageFrom = new DateTimePicker { Left = lblFrom.Right + 5, Top = 8, Width = 150, Format = DateTimePickerFormat.Short };
        var lblTo = new Label { Text = "~", Left = _dtpUsageFrom.Right + 5, Top = 12, Width = 20 };
        _dtpUsageTo = new DateTimePicker { Left = lblTo.Right + 5, Top = 8, Width = 150, Format = DateTimePickerFormat.Short };

        var lblAccount = new Label { Text = "계정", Left = _dtpUsageTo.Right + 20, Top = 12, Width = 40 };
        _txtUsageAccount = new TextBox { Left = lblAccount.Right + 5, Top = 8, Width = 140 };

        var lblCustomer = new Label { Text = "고객", Left = _txtUsageAccount.Right + 20, Top = 12, Width = 40 };
        _txtUsageCustomer = new TextBox { Left = lblCustomer.Right + 5, Top = 8, Width = 140 };

        var lblProduct = new Label { Text = "상품", Left = _txtUsageCustomer.Right + 20, Top = 12, Width = 40 };
        _txtUsageProduct = new TextBox { Left = lblProduct.Right + 5, Top = 8, Width = 140 };

        var lblAction = new Label { Text = "액션", Left = _txtUsageProduct.Right + 20, Top = 12, Width = 40 };
        _cboUsageAction = new ComboBox { Left = lblAction.Right + 5, Top = 8, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
        _cboUsageAction.Items.AddRange(new object[]
        {
            "",
            AccountActionType.Create,
            AccountActionType.Deliver,
            AccountActionType.Cancel,
            AccountActionType.Renew,
            AccountActionType.Reuse,
            AccountActionType.Update,
            AccountActionType.StatusChange,
            AccountActionType.PasswordReset,
            AccountActionType.CardChange
        });
        _cboUsageAction.SelectedIndex = 0;

        var btnSearch = new Button { Text = "조회", Left = _cboUsageAction.Right + 10, Top = 6, Width = 80 };
        btnSearch.Click += (_, _) => LoadUsageLogs();
        var btnReset = new Button { Text = "초기화", Left = btnSearch.Right + 10, Top = 6, Width = 80 };
        btnReset.Click += (_, _) => ResetUsageFilters();

        filter.Controls.AddRange(new Control[]
        {
            lblFrom, _dtpUsageFrom, lblTo, _dtpUsageTo,
            lblAccount, _txtUsageAccount,
            lblCustomer, _txtUsageCustomer,
            lblProduct, _txtUsageProduct,
            lblAction, _cboUsageAction,
            btnSearch, btnReset
        });

        _dgvUsage = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false
        };

        _dgvUsage.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "기록 시각",
            DataPropertyName = nameof(UsageRow.CreatedAt),
            Width = 150,
            DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm:ss" }
        });
        _dgvUsage.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "액션",
            DataPropertyName = nameof(UsageRow.ActionType),
            Width = 120
        });
        _dgvUsage.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "계정",
            DataPropertyName = nameof(UsageRow.AccountEmail),
            Width = 160
        });
        _dgvUsage.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "고객",
            DataPropertyName = nameof(UsageRow.CustomerName),
            Width = 160
        });
        _dgvUsage.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상품",
            DataPropertyName = nameof(UsageRow.ProductName),
            Width = 160
        });
        _dgvUsage.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "시작일",
            DataPropertyName = nameof(UsageRow.RequestDate),
            Width = 110,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _dgvUsage.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "만료일",
            DataPropertyName = nameof(UsageRow.ExpireDate),
            Width = 110,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _dgvUsage.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "메모",
            DataPropertyName = nameof(UsageRow.Memo),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });

        panel.Controls.Add(filter);
        panel.Controls.Add(_dgvUsage);
        _dgvUsage.Dock = DockStyle.Fill;
        filter.Dock = DockStyle.Top;

        return panel;
    }

    private void LoadDefaultFilters()
    {
        _dtpAuditFrom.Value = DateTime.Today.AddDays(-30);
        _dtpAuditTo.Value   = DateTime.Today;

        _dtpUsageFrom.Value = DateTime.Today.AddDays(-90);
        _dtpUsageTo.Value   = DateTime.Today;
    }

    private void ResetAuditFilters()
    {
        LoadDefaultFilters();
        _txtAuditUser.Clear();
        _txtAuditKeyword.Clear();
        _cboAuditTable.SelectedIndex  = 0;
        _cboAuditAction.SelectedIndex = 0;
        LoadAuditLogs();
    }

    private void ResetUsageFilters()
    {
        _dtpUsageFrom.Value = DateTime.Today.AddDays(-90);
        _dtpUsageTo.Value   = DateTime.Today;
        _txtUsageAccount.Clear();
        _txtUsageCustomer.Clear();
        _txtUsageProduct.Clear();
        _cboUsageAction.SelectedIndex = 0;
        LoadUsageLogs();
    }

    private void LoadAuditLogs()
    {
        var from = _dtpAuditFrom.Value.Date;
        var to   = _dtpAuditTo.Value.Date.AddDays(1).AddSeconds(-1);

        var logs = _auditRepo.GetByDateRange(from, to);

        if (!string.IsNullOrWhiteSpace(_txtAuditUser.Text))
        {
            logs = logs
                .Where(l => (l.UserName ?? string.Empty)
                    .Contains(_txtAuditUser.Text, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(_cboAuditTable.Text))
        {
            logs = logs
                .Where(l => l.TableName == _cboAuditTable.Text)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(_cboAuditAction.Text))
        {
            logs = logs
                .Where(l => l.ActionType == _cboAuditAction.Text)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(_txtAuditKeyword.Text))
        {
            logs = logs
                .Where(l => (l.Description ?? string.Empty).Contains(_txtAuditKeyword.Text, StringComparison.OrdinalIgnoreCase)
                         || (l.DetailJson ?? string.Empty).Contains(_txtAuditKeyword.Text, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        _dgvAudit.DataSource = new BindingList<AuditLogEntry>(logs);
    }

    private void LoadUsageLogs()
    {
        var from = _dtpUsageFrom.Value.Date;
        var to   = _dtpUsageTo.Value.Date.AddDays(1).AddSeconds(-1);

        var logs = _usageRepo.GetByCreatedDateRange(from, to);

        var accounts  = _accountService.GetAll();
        var customers = _customerService.GetAll();
        var products  = _productService.GetAll();

        var accountMap  = accounts.ToDictionary(a => a.AccountId, a => a.Email);
        var customerMap = customers.ToDictionary(c => c.CustomerId, c => c.CustomerName);
        var productMap  = products.ToDictionary(p => p.ProductId, p => p.ProductName ?? p.ProductCode);

        if (!string.IsNullOrWhiteSpace(_txtUsageAccount.Text))
        {
            logs = logs
                .Where(l => l.AccountId > 0
                    && accountMap.TryGetValue(l.AccountId, out var email)
                    && email.Contains(_txtUsageAccount.Text, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(_txtUsageCustomer.Text))
        {
            logs = logs
                .Where(l => l.CustomerId.HasValue
                    && customerMap.TryGetValue(l.CustomerId.Value, out var name)
                    && name.Contains(_txtUsageCustomer.Text, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(_txtUsageProduct.Text))
        {
            logs = logs
                .Where(l => l.ProductId.HasValue
                    && productMap.TryGetValue(l.ProductId.Value, out var name)
                    && name.Contains(_txtUsageProduct.Text, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(_cboUsageAction.Text))
        {
            logs = logs
                .Where(l => l.ActionType == _cboUsageAction.Text)
                .ToList();
        }

        var rows = logs
            .Select(l => new UsageRow
            {
                CreatedAt    = l.CreatedAt,
                ActionType   = l.ActionType,
                AccountEmail = l.AccountId > 0 && accountMap.TryGetValue(l.AccountId, out var email) ? email : "",
                CustomerName = l.CustomerId.HasValue && customerMap.TryGetValue(l.CustomerId.Value, out var cname) ? cname : "",
                ProductName  = l.ProductId.HasValue && productMap.TryGetValue(l.ProductId.Value, out var pname) ? pname : "",
                RequestDate  = l.RequestDate,
                ExpireDate   = l.ExpireDate,
                Memo         = l.Description
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        _dgvUsage.DataSource = new BindingList<UsageRow>(rows);
    }

    private void ShowAuditDetails()
    {
        if (_dgvAudit.CurrentRow?.DataBoundItem is not AuditLogEntry log)
            return;

        var text = string.Join(Environment.NewLine + Environment.NewLine, new[]
        {
            $"[{log.EventTime:yyyy-MM-dd HH:mm:ss}] {log.UserName} ({log.ActionType})",
            log.Description,
            log.DetailJson
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        MessageBox.Show(this, text, "상세 정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private class UsageRow
    {
        public DateTime CreatedAt    { get; set; }
        public string   ActionType   { get; set; } = "";
        public string   AccountEmail { get; set; } = "";
        public string   CustomerName { get; set; } = "";
        public string   ProductName  { get; set; } = "";
        public DateTime? RequestDate { get; set; }
        public DateTime? ExpireDate  { get; set; }
        public string?  Memo         { get; set; }
    }
}
