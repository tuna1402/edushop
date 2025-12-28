using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class AccountStatusSummaryForm : Form
{
    private readonly AccountService   _accountService;
    private readonly ProductService   _productService;
    private readonly SalesService     _salesService;
    private readonly CustomerService  _customerService;
    private readonly CardService      _cardService;
    private readonly UserContext      _currentUser;
    private readonly AppSettings      _appSettings;

    private DataGridView _dgvSummary = null!;
    private Button       _btnOpenList = null!;
    private Button       _btnClose = null!;

    private class StatusSummaryRow
    {
        public string StatusCode    { get; set; } = string.Empty;
        public string StatusDisplay { get; set; } = string.Empty;
        public int    Count         { get; set; }
    }

    public AccountStatusSummaryForm(
        AccountService  accountService,
        ProductService  productService,
        SalesService    salesService,
        CustomerService customerService,
        CardService     cardService,
        UserContext     currentUser,
        AppSettings     appSettings)
    {
        _accountService  = accountService;
        _productService  = productService;
        _salesService    = salesService;
        _customerService = customerService;
        _cardService     = cardService;
        _currentUser     = currentUser;
        _appSettings     = appSettings;

        Text = "계정 상태별 통계";
        Width = 600;
        Height = 400;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        LoadSummary();
    }

    private void InitializeControls()
    {
        var lblInfo = new Label
        {
            Text = "계정 상태별 개수 요약입니다. 더블클릭하면 해당 상태 계정 목록으로 이동합니다.",
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };

        _dgvSummary = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        _dgvSummary.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상태",
            DataPropertyName = "StatusDisplay",
            Width = 220
        });
        _dgvSummary.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "코드",
            DataPropertyName = "StatusCode",
            Width = 120,
            Visible = false
        });
        _dgvSummary.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "계정 수",
            DataPropertyName = "Count",
            Width = 100,
            DefaultCellStyle = { Format = "N0" }
        });

        _dgvSummary.DoubleClick += (_, _) => OpenAccountListForSelectedStatus();

        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };

        _btnOpenList = new Button
        {
            Text = "선택 상태 계정 목록 보기",
            Width = 200,
            Height = 28,
            Left = 10,
            Top = 10
        };
        _btnOpenList.Click += (_, _) => OpenAccountListForSelectedStatus();

        _btnClose = new Button
        {
            Text = "닫기",
            Width = 80,
            Height = 28,
            Dock = DockStyle.Right,
            Top = 10,
        };
        _btnClose.Click += (_, _) => Close();

        bottomPanel.Controls.Add(_btnOpenList);
        bottomPanel.Controls.Add(_btnClose);

        Controls.Add(_dgvSummary);
        Controls.Add(bottomPanel);
        Controls.Add(lblInfo);
    }

    private void LoadSummary()
    {
        var accounts = _accountService.GetAll();

        var groups = accounts
            .GroupBy(a => a.Status ?? string.Empty)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key);

        var list = new List<StatusSummaryRow>();

        foreach (var g in groups)
        {
            var code = g.Key;
            var display = string.IsNullOrWhiteSpace(code)
                ? "미지정"
                : AccountStatusHelper.ToDisplay(code);

            list.Add(new StatusSummaryRow
            {
                StatusCode    = code,
                StatusDisplay = display,
                Count         = g.Count()
            });
        }

        _dgvSummary.DataSource = list;

        if (_dgvSummary.Rows.Count > 0)
        {
            _dgvSummary.Rows[0].Selected = true;
        }
    }

    private void OpenAccountListForSelectedStatus()
    {
        if (_dgvSummary.CurrentRow?.DataBoundItem is not StatusSummaryRow row)
            return;

        using var dlg = new AccountListForm(
            _accountService,
            _productService,
            _salesService,
            _customerService,
            _cardService,
            _currentUser,
            _appSettings,
            expiringOnly: false,
            initialStatus: row.StatusCode);

        dlg.ShowDialog(this);
    }
}
