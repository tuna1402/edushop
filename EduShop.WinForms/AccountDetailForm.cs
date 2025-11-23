using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class AccountDetailForm : Form
{
    private readonly AccountService _accountService;
    private readonly ProductService _productService;
    private readonly CustomerService _customerService;
    private readonly UserContext _currentUser;
    private readonly long _accountId;

    private TextBox _txtEmail = null!;
    private ComboBox _cboStatus = null!;
    private TextBox _txtProduct = null!;
    private TextBox _txtCustomer = null!;
    private DateTimePicker _dtStart = null!;
    private DateTimePicker _dtEnd = null!;
    private DateTimePicker _dtDelivery = null!;
    private TextBox _txtOrder = null!;
    private TextBox _txtMemo = null!;
    private Button _btnSave = null!;
    private Button _btnClose = null!;

    private DataGridView _gridLogs = null!;
    private DateTimePicker _dtLogFrom = null!;
    private DateTimePicker _dtLogTo = null!;
    private ComboBox _cboLogCustomer = null!;
    private ComboBox _cboLogProduct = null!;
    private Button _btnLogSearch = null!;

    private List<Product> _products = new();
    private List<Customer> _customers = new();

    private class UsageLogRow
    {
        public string ActionType { get; set; } = "";
        public string? RequestDate { get; set; }
        public string? ExpireDate { get; set; }
        public string? Customer { get; set; }
        public string? Product { get; set; }
        public string? Description { get; set; }
        public string CreatedAt { get; set; } = "";
        public string? CreatedBy { get; set; }
    }

    public AccountDetailForm(AccountService accountService, ProductService productService, CustomerService customerService, long accountId, UserContext currentUser)
    {
        _accountService  = accountService;
        _productService  = productService;
        _customerService = customerService;
        _accountId       = accountId;
        _currentUser     = currentUser;

        Text = "계정 상세";
        Width = 900;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        LoadReferenceData();
        LoadAccountInfo();
        LoadUsageLogs();
    }

    private void InitializeControls()
    {
        var tab = new TabControl
        {
            Dock = DockStyle.Fill
        };

        var tabInfo = new TabPage("기본 정보")
        {
            Padding = new Padding(10)
        };
        tabInfo.Controls.Add(BuildInfoLayout());

        var tabLogs = new TabPage("사용 로그")
        {
            Padding = new Padding(10)
        };
        tabLogs.Controls.Add(BuildLogLayout());

        tab.TabPages.Add(tabInfo);
        tab.TabPages.Add(tabLogs);

        Controls.Add(tab);
    }

    private Control BuildInfoLayout()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 0,
            AutoScroll = true,
            Padding = new Padding(5)
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _txtEmail = CreateReadOnlyTextBox();
        _cboStatus = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboStatus.DisplayMember = "Display";
        _cboStatus.ValueMember   = "Code";
        _cboStatus.DataSource    = AccountStatusHelper.GetAll().Select(x => new { x.Code, x.Display }).ToList();

        _txtProduct = CreateReadOnlyTextBox();
        _txtCustomer = CreateReadOnlyTextBox();

        _dtStart = new DateTimePicker
        {
            Dock = DockStyle.Left,
            Format = DateTimePickerFormat.Short,
            Width = 140
        };

        _dtEnd = new DateTimePicker
        {
            Dock = DockStyle.Left,
            Format = DateTimePickerFormat.Short,
            Width = 140
        };

        _dtDelivery = new DateTimePicker
        {
            Dock = DockStyle.Left,
            Format = DateTimePickerFormat.Short,
            Width = 140,
            ShowCheckBox = true
        };

        _txtOrder = CreateReadOnlyTextBox();
        _txtMemo = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            Height = 80,
            ScrollBars = ScrollBars.Vertical
        };

        AddRow(table, "이메일", _txtEmail);
        AddRow(table, "상태", _cboStatus);
        AddRow(table, "상품", _txtProduct);
        AddRow(table, "고객", _txtCustomer);
        AddRow(table, "구독 시작일", _dtStart);
        AddRow(table, "구독 만료일", _dtEnd);
        AddRow(table, "납품일", _dtDelivery);
        AddRow(table, "주문번호", _txtOrder);
        AddRow(table, "메모", _txtMemo);

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        _btnSave = new Button
        {
            Text = "저장",
            Width = 90
        };
        _btnSave.Click += (_, _) => ValidateAndSave();

        _btnClose = new Button
        {
            Text = "닫기",
            Width = 90
        };
        _btnClose.Click += (_, _) => Close();

        buttonRow.Controls.Add(_btnSave);
        buttonRow.Controls.Add(_btnClose);
        AddRow(table, string.Empty, buttonRow);

        return table;
    }

    private Control BuildLogLayout()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill
        };

        var filterPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70
        };

        var lblFrom = new Label
        {
            Text = "기간", Left = 10, Top = 12, Width = 40
        };
        _dtLogFrom = new DateTimePicker
        {
            Left = lblFrom.Right + 5,
            Top = 8,
            Width = 120,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddMonths(-6),
            ShowCheckBox = true,
            Checked = true
        };

        var lblWave = new Label
        {
            Text = "~",
            Left = _dtLogFrom.Right + 5,
            Top = 12,
            Width = 15
        };

        _dtLogTo = new DateTimePicker
        {
            Left = lblWave.Right + 5,
            Top = 8,
            Width = 120,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today,
            ShowCheckBox = true,
            Checked = true
        };

        var lblCustomer = new Label
        {
            Text = "고객",
            Left = _dtLogTo.Right + 15,
            Top = 12,
            Width = 40
        };
        _cboLogCustomer = new ComboBox
        {
            Left = lblCustomer.Right + 5,
            Top = 8,
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var lblProduct = new Label
        {
            Text = "상품",
            Left = _cboLogCustomer.Right + 15,
            Top = 12,
            Width = 40
        };
        _cboLogProduct = new ComboBox
        {
            Left = lblProduct.Right + 5,
            Top = 8,
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _btnLogSearch = new Button
        {
            Text = "조회",
            Left = _cboLogProduct.Right + 15,
            Top = 6,
            Width = 80,
            Height = 28
        };
        _btnLogSearch.Click += (_, _) => LoadUsageLogs();

        filterPanel.Controls.Add(lblFrom);
        filterPanel.Controls.Add(_dtLogFrom);
        filterPanel.Controls.Add(lblWave);
        filterPanel.Controls.Add(_dtLogTo);
        filterPanel.Controls.Add(lblCustomer);
        filterPanel.Controls.Add(_cboLogCustomer);
        filterPanel.Controls.Add(lblProduct);
        filterPanel.Controls.Add(_cboLogProduct);
        filterPanel.Controls.Add(_btnLogSearch);

        _gridLogs = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false
        };

        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Action", DataPropertyName = "ActionType", Width = 80 });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "요청일", DataPropertyName = "RequestDate", Width = 90 });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "만료일", DataPropertyName = "ExpireDate", Width = 90 });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "고객", DataPropertyName = "Customer", Width = 160 });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "상품", DataPropertyName = "Product", Width = 180 });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "설명", DataPropertyName = "Description", Width = 220 });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "기록일", DataPropertyName = "CreatedAt", Width = 140 });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "작성자", DataPropertyName = "CreatedBy", Width = 90 });

        panel.Controls.Add(_gridLogs);
        panel.Controls.Add(filterPanel);

        return panel;
    }

    private void LoadReferenceData()
    {
        _products  = _productService.GetAll();
        _customers = _customerService.GetAll();

        _cboLogCustomer.Items.Clear();
        _cboLogCustomer.Items.Add("(전체)");
        foreach (var c in _customers)
        {
            _cboLogCustomer.Items.Add(c.CustomerName);
        }
        _cboLogCustomer.SelectedIndex = 0;

        _cboLogProduct.Items.Clear();
        _cboLogProduct.Items.Add("(전체)");
        foreach (var p in _products)
        {
            _cboLogProduct.Items.Add($"{p.ProductName} / {p.PlanName}");
        }
        _cboLogProduct.SelectedIndex = 0;
    }

    private void LoadAccountInfo()
    {
        var acc = _accountService.GetById(_accountId);
        if (acc == null)
        {
            MessageBox.Show("계정을 찾을 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
            return;
        }

        Text = $"계정 상세 - {acc.Email}";

        _txtEmail.Text = acc.Email;
        _cboStatus.SelectedValue = acc.Status;
        _txtProduct.Text = GetProductName(acc.ProductId);
        _txtCustomer.Text = acc.CustomerId.HasValue ? GetCustomerName(acc.CustomerId.Value) : "";
        _dtStart.Value = acc.SubscriptionStartDate;
        _dtEnd.Value = acc.SubscriptionEndDate;
        if (acc.DeliveryDate.HasValue)
        {
            _dtDelivery.Value = acc.DeliveryDate.Value;
            _dtDelivery.Checked = true;
        }
        else
        {
            _dtDelivery.Checked = false;
        }

        _txtOrder.Text = acc.OrderId?.ToString() ?? "";
        _txtMemo.Text = acc.Memo ?? "";
    }

    private void LoadUsageLogs()
    {
        DateTime? from = _dtLogFrom.Checked ? _dtLogFrom.Value.Date : null;
        DateTime? to   = _dtLogTo.Checked ? _dtLogTo.Value.Date : null;

        long? customerId = null;
        if (_cboLogCustomer.SelectedIndex > 0)
        {
            customerId = _customers[_cboLogCustomer.SelectedIndex - 1].CustomerId;
        }

        long? productId = null;
        if (_cboLogProduct.SelectedIndex > 0)
        {
            productId = _products[_cboLogProduct.SelectedIndex - 1].ProductId;
        }

        var logs = _accountService.GetUsageLogs(_accountId, from, to, customerId, productId);

        var rows = logs
            .Select(log => new UsageLogRow
            {
                ActionType = log.ActionType,
                RequestDate = log.RequestDate?.ToString("yyyy-MM-dd"),
                ExpireDate  = log.ExpireDate?.ToString("yyyy-MM-dd"),
                Customer    = log.CustomerId.HasValue ? GetCustomerName(log.CustomerId.Value) : null,
                Product     = log.ProductId.HasValue ? GetProductName(log.ProductId.Value) : null,
                Description = log.Description,
                CreatedAt   = log.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                CreatedBy   = log.CreatedBy
            })
            .ToList();

        _gridLogs.DataSource = rows;
    }

    private void ValidateAndSave()
    {
        var status = _cboStatus.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(status))
        {
            MessageBox.Show("상태를 선택해 주세요.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var startDate = _dtStart.Value.Date;
        var endDate   = _dtEnd.Value.Date;

        if (endDate < startDate)
        {
            MessageBox.Show("만료일은 시작일 이후여야 합니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DateTime? deliveryDate = _dtDelivery.Checked ? _dtDelivery.Value.Date : null;
        var memo = _txtMemo.Text;

        try
        {
            _accountService.UpdateAccountBasicInfo(
                _accountId,
                status,
                startDate,
                endDate,
                deliveryDate,
                memo,
                _currentUser);

            MessageBox.Show("저장 완료", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"저장 중 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static TextBox CreateReadOnlyTextBox(bool multiline = false, int height = 25)
    {
        return new TextBox
        {
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Multiline = multiline,
            Height = height,
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
        };
    }

    private static void AddRow(TableLayoutPanel table, string labelText, Control control)
    {
        var rowIndex = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = new Label
        {
            Text = labelText,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(0, 6, 0, 6)
        };

        control.Margin = new Padding(3, 3, 3, 3);

        table.Controls.Add(label, 0, rowIndex);
        table.Controls.Add(control, 1, rowIndex);
    }

    private string GetProductName(long productId)
    {
        var product = _products.FirstOrDefault(p => p.ProductId == productId);
        if (product == null)
            return $"#{productId}";

        return string.IsNullOrWhiteSpace(product.PlanName)
            ? product.ProductName
            : $"{product.ProductName} / {product.PlanName}";
    }

    private string GetCustomerName(long customerId)
    {
        var customer = _customers.FirstOrDefault(c => c.CustomerId == customerId);
        return customer?.CustomerName ?? $"#{customerId}";
    }
}
