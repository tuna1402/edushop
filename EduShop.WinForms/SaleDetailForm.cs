using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class SaleDetailForm : Form
{
    private readonly SalesService   _salesService;
    private readonly AccountService _accountService;

    private readonly long _saleId;

    private Label _lblHeader = null!;
    private DataGridView _gridItems = null!;
    private DataGridView _gridAccounts = null!;
    private Button _btnClose = null!;

    public SaleDetailForm(SalesService salesService, AccountService accountService, long saleId)
    {
        _salesService   = salesService;
        _accountService = accountService;
        _saleId         = saleId;

        Text = "주문/견적 상세";
        Width = 900;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        LoadSale();
    }

    private void InitializeControls()
    {
        _lblHeader = new Label
        {
            Left = 10,
            Top = 10,
            Width = ClientSize.Width - 20,
            AutoSize = false
        };

        _gridItems = new DataGridView
        {
            Left = 10,
            Top = 40,
            Width = ClientSize.Width - 20,
            Height = 220,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false
        };

        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "코드",
            DataPropertyName = "ProductCode",
            Width = 100
        });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상품명",
            DataPropertyName = "ProductName",
            Width = 260
        });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "단가",
            DataPropertyName = "UnitPrice",
            Width = 90
        });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "수량",
            DataPropertyName = "Quantity",
            Width = 70
        });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "금액",
            DataPropertyName = "LineTotal",
            Width = 100
        });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "마진",
            DataPropertyName = "LineProfit",
            Width = 100
        });

        _gridAccounts = new DataGridView
        {
            Left = 10,
            Top = _gridItems.Bottom + 10,
            Width = ClientSize.Width - 20,
            Height = 250,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false
        };

        _gridAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "이메일",
            DataPropertyName = "Email",
            Width = 200
        });
        _gridAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상태",
            DataPropertyName = "Status",
            Width = 100
        });
        _gridAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "시작일",
            DataPropertyName = "StartDate",
            Width = 90,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _gridAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "만료일",
            DataPropertyName = "EndDate",
            Width = 90,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _gridAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "납품일",
            DataPropertyName = "DeliveryDate",
            Width = 90,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _gridAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "메모",
            DataPropertyName = "Memo",
            Width = 220
        });

        _btnClose = new Button
        {
            Text = "닫기",
            Width = 80,
            Left = ClientSize.Width - 90,
            Top = ClientSize.Height - 40,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnClose.Click += (_, _) => Close();

        Controls.Add(_lblHeader);
        Controls.Add(_gridItems);
        Controls.Add(_gridAccounts);
        Controls.Add(_btnClose);
    }

    private void LoadSale()
    {
        var sale = _salesService.GetSale(_saleId);
        if (sale == null)
        {
            MessageBox.Show("주문/견적 정보를 찾을 수 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
            return;
        }

        var headerText = $"번호: {sale.SaleId} / 일자: {sale.SaleDate:yyyy-MM-dd} / 고객: {sale.CustomerName ?? "(무기명)"}";
        _lblHeader.Text = headerText;

        var items = _salesService.GetSaleItems(_saleId);
        _gridItems.DataSource = null;
        _gridItems.DataSource = items;

        var accounts = _accountService.GetByOrderId(_saleId);
        var rows = accounts.Select(a => new
        {
            a.Email,
            Status = AccountStatusHelper.ToDisplay(a.Status),
            StartDate = a.SubscriptionStartDate,
            EndDate = a.SubscriptionEndDate,
            DeliveryDate = a.DeliveryDate,
            a.Memo
        }).ToList();

        _gridAccounts.DataSource = null;
        _gridAccounts.DataSource = rows;
    }
}
