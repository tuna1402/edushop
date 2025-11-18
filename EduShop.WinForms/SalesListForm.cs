using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class SalesListForm : Form
{
    private readonly SalesService _salesService;

    private DateTimePicker _dtFrom = null!;
    private DateTimePicker _dtTo = null!;
    private Button _btnSearch = null!;
    private Button _btnClose = null!;
    private DataGridView _gridSales = null!;
    private DataGridView _gridItems = null!;
    private Label _lblSummary = null!;

    private List<SaleHeader> _currentSales = new();

    public SalesListForm(SalesService salesService)
    {
        _salesService = salesService;

        Text = "매출 현황";
        Width = 1000;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        LoadSales();
    }

    private void InitializeControls()
    {
        var lblFrom = new Label
        {
            Text = "기간",
            Left = 10,
            Top = 15,
            AutoSize = true
        };

        _dtFrom = new DateTimePicker
        {
            Left = lblFrom.Right + 5,
            Top = 10,
            Width = 120,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddMonths(-1)
        };

        var lblWave = new Label
        {
            Text = "~",
            Left = _dtFrom.Right + 5,
            Top = 15,
            AutoSize = true
        };

        _dtTo = new DateTimePicker
        {
            Left = lblWave.Right + 5,
            Top = 10,
            Width = 120,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today
        };

        _btnSearch = new Button
        {
            Text = "조회",
            Left = _dtTo.Right + 20,
            Top = 9,
            Width = 80
        };
        _btnSearch.Click += (_, _) => LoadSales();

        _gridSales = new DataGridView
        {
            Left = 10,
            Top = 45,
            Width = ClientSize.Width - 20,
            Height = (ClientSize.Height - 120) / 2,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false
        };

        _gridSales.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "일자",
            DataPropertyName = "SaleDate",
            Width = 100
        });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "고객명",
            DataPropertyName = "CustomerName",
            Width = 150
        });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "학교명",
            DataPropertyName = "SchoolName",
            Width = 200
        });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "총금액",
            DataPropertyName = "TotalAmount",
            Width = 120
        });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "총마진",
            DataPropertyName = "TotalProfit",
            Width = 120
        });

        _gridSales.SelectionChanged += (_, _) => LoadSaleItemsForSelected();

        _gridItems = new DataGridView
        {
            Left = 10,
            Top = _gridSales.Bottom + 10,
            Width = ClientSize.Width - 20,
            Height = (ClientSize.Height - 120) / 2,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
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
            Width = 250
        });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "단가",
            DataPropertyName = "UnitPrice",
            Width = 80
        });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "수량",
            DataPropertyName = "Quantity",
            Width = 60
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

        _lblSummary = new Label
        {
            Text = "합계: 0 원 / 마진: 0 원",
            Left = 10,
            Top = ClientSize.Height - 30,
            Width = 400,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };

        _btnClose = new Button
        {
            Text = "닫기",
            Left = ClientSize.Width - 90,
            Top = ClientSize.Height - 35,
            Width = 80,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnClose.Click += (_, _) => Close();

        Controls.Add(lblFrom);
        Controls.Add(_dtFrom);
        Controls.Add(lblWave);
        Controls.Add(_dtTo);
        Controls.Add(_btnSearch);
        Controls.Add(_gridSales);
        Controls.Add(_gridItems);
        Controls.Add(_lblSummary);
        Controls.Add(_btnClose);
    }

    private void LoadSales()
    {
        var from = _dtFrom.Value.Date;
        var to   = _dtTo.Value.Date;

        _currentSales = _salesService.GetSales(from, to);
        _gridSales.DataSource = null;
        _gridSales.DataSource = _currentSales;

        var summary = _salesService.GetSummary(from, to);
        _lblSummary.Text = $"합계: {summary.TotalAmount:N0} 원 / 마진: {summary.TotalProfit:N0} 원";

        LoadSaleItemsForSelected();
    }

    private void LoadSaleItemsForSelected()
    {
        if (_gridSales.CurrentRow?.DataBoundItem is not SaleHeader header)
        {
            _gridItems.DataSource = null;
            return;
        }

        var items = _salesService.GetSaleItems(header.SaleId);
        _gridItems.DataSource = null;
        _gridItems.DataSource = items;
    }
}
