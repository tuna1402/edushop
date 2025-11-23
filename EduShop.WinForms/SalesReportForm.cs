using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class SalesReportForm : Form
{
    private readonly SalesService _salesService;
    private readonly CustomerService _customerService;
    private readonly ProductService _productService;
    private readonly AppSettings _appSettings;
    private readonly UserContext _currentUser;

    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private Button _btnSearch = null!;
    private Button _btnReset = null!;
    private Button _btnExportCsv = null!;

    private Label _lblTotalSales = null!;
    private Label _lblTotalCost = null!;
    private Label _lblTotalProfit = null!;

    private DataGridView _gridCustomer = null!;
    private DataGridView _gridProduct = null!;

    private List<SalesSummaryRow> _summaryRows = new();

    public SalesReportForm(
        SalesService salesService,
        CustomerService customerService,
        ProductService productService,
        AppSettings appSettings,
        UserContext currentUser)
    {
        _salesService = salesService;
        _customerService = customerService;
        _productService = productService;
        _appSettings = appSettings;
        _currentUser = currentUser;

        _ = _customerService;
        _ = _currentUser;

        Text = "매출 리포트";
        Width = 1100;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        InitDefaultRange();
        LoadData();
    }

    private void InitializeControls()
    {
        var filterPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45
        };

        var lblPeriod = new Label
        {
            Text = "기간",
            Left = 10,
            Top = 14,
            AutoSize = true
        };
        filterPanel.Controls.Add(lblPeriod);

        _dtpFrom = new DateTimePicker
        {
            Left = lblPeriod.Right + 5,
            Top = 10,
            Width = 120,
            Format = DateTimePickerFormat.Short
        };
        filterPanel.Controls.Add(_dtpFrom);

        var lblWave = new Label
        {
            Text = "~",
            Left = _dtpFrom.Right + 5,
            Top = 14,
            AutoSize = true
        };
        filterPanel.Controls.Add(lblWave);

        _dtpTo = new DateTimePicker
        {
            Left = lblWave.Right + 5,
            Top = 10,
            Width = 120,
            Format = DateTimePickerFormat.Short
        };
        filterPanel.Controls.Add(_dtpTo);

        _btnSearch = new Button
        {
            Text = "조회",
            Left = _dtpTo.Right + 20,
            Top = 9,
            Width = 80
        };
        _btnSearch.Click += (_, _) => LoadData();
        filterPanel.Controls.Add(_btnSearch);

        _btnReset = new Button
        {
            Text = "초기화",
            Left = _btnSearch.Right + 5,
            Top = 9,
            Width = 80
        };
        _btnReset.Click += (_, _) =>
        {
            InitDefaultRange();
            LoadData();
        };
        filterPanel.Controls.Add(_btnReset);

        _btnExportCsv = new Button
        {
            Text = "CSV 내보내기",
            Left = _btnReset.Right + 5,
            Top = 9,
            Width = 120
        };
        _btnExportCsv.Click += (_, _) => ExportCsv();
        filterPanel.Controls.Add(_btnExportCsv);

        Controls.Add(filterPanel);

        var summaryPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            Padding = new Padding(10)
        };

        _lblTotalSales = CreateSummaryLabel("총 매출: 0", 0);
        _lblTotalCost = CreateSummaryLabel("총 원가: 0", 1);
        _lblTotalProfit = CreateSummaryLabel("총 이익: 0", 2);

        summaryPanel.Controls.Add(_lblTotalSales);
        summaryPanel.Controls.Add(_lblTotalCost);
        summaryPanel.Controls.Add(_lblTotalProfit);

        Controls.Add(summaryPanel);

        var tab = new TabControl
        {
            Dock = DockStyle.Fill
        };

        var tpCustomer = new TabPage("고객별 매출");
        _gridCustomer = CreateGrid();
        _gridCustomer.Dock = DockStyle.Fill;
        tpCustomer.Controls.Add(_gridCustomer);
        tab.TabPages.Add(tpCustomer);

        var tpProduct = new TabPage("상품별 매출");
        _gridProduct = CreateGrid();
        _gridProduct.Dock = DockStyle.Fill;
        tpProduct.Controls.Add(_gridProduct);
        tab.TabPages.Add(tpProduct);

        Controls.Add(tab);

        BuildCustomerColumns();
        BuildProductColumns();
    }

    private static Label CreateSummaryLabel(string text, int index)
    {
        return new Label
        {
            Text = text,
            Left = 10 + index * 220,
            Top = 20,
            Width = 210,
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 11, FontStyle.Bold)
        };
    }

    private static DataGridView CreateGrid()
    {
        return new DataGridView
        {
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false
        };
    }

    private void BuildCustomerColumns()
    {
        _gridCustomer.Columns.Clear();
        _gridCustomer.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "고객명",
            DataPropertyName = "CustomerName",
            Width = 200
        });
        _gridCustomer.Columns.Add(CreateNumberColumn("총 매출", "TotalSales"));
        _gridCustomer.Columns.Add(CreateNumberColumn("총 원가", "TotalCost"));
        _gridCustomer.Columns.Add(CreateNumberColumn("총 이익", "TotalProfit"));
        _gridCustomer.Columns.Add(CreateNumberColumn("총 수량", "QtyTotal"));
    }

    private void BuildProductColumns()
    {
        _gridProduct.Columns.Clear();
        _gridProduct.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상품명",
            DataPropertyName = "ProductName",
            Width = 250
        });
        _gridProduct.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "플랜",
            DataPropertyName = "PlanName",
            Width = 150
        });
        _gridProduct.Columns.Add(CreateNumberColumn("총 매출", "TotalSales"));
        _gridProduct.Columns.Add(CreateNumberColumn("총 원가", "TotalCost"));
        _gridProduct.Columns.Add(CreateNumberColumn("총 이익", "TotalProfit"));
        _gridProduct.Columns.Add(CreateNumberColumn("총 수량", "QtyTotal"));
    }

    private static DataGridViewTextBoxColumn CreateNumberColumn(string header, string property)
    {
        var col = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            Width = 120,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
        };
        col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        return col;
    }

    private void InitDefaultRange()
    {
        _dtpTo.Value = DateTime.Today;
        _dtpFrom.Value = DateTime.Today.AddMonths(-1);
    }

    private void LoadData()
    {
        var from = _dtpFrom.Value.Date;
        var to = _dtpTo.Value.Date;

        _summaryRows = _salesService.GetSummary(from, to);

        long totalSales = _summaryRows.Sum(x => x.SalesAmt);
        long totalCost = _summaryRows.Sum(x => x.CostAmt);
        long totalProfit = _summaryRows.Sum(x => x.ProfitAmt);

        _lblTotalSales.Text = $"총 매출: {totalSales:N0}";
        _lblTotalCost.Text = $"총 원가: {totalCost:N0}";
        _lblTotalProfit.Text = $"총 이익: {totalProfit:N0}";

        var byCustomer = _summaryRows
            .GroupBy(x => x.Customer)
            .Select(g => new
            {
                CustomerName = string.IsNullOrWhiteSpace(g.Key) ? "(미지정)" : g.Key,
                QtyTotal = g.Sum(x => x.Qty),
                TotalSales = g.Sum(x => x.SalesAmt),
                TotalCost = g.Sum(x => x.CostAmt),
                TotalProfit = g.Sum(x => x.ProfitAmt)
            })
            .OrderByDescending(x => x.TotalSales)
            .ToList();

        _gridCustomer.DataSource = byCustomer;

        var productPlanMap = _productService.GetAll()
            .ToDictionary(p => p.ProductId, p => p.PlanName ?? "");

        var byProduct = _summaryRows
            .GroupBy(x => new { x.ProductId, x.Product })
            .Select(g => new
            {
                ProductName = string.IsNullOrWhiteSpace(g.Key.Product) ? "(미지정)" : g.Key.Product,
                PlanName = g.Key.ProductId.HasValue && productPlanMap.TryGetValue(g.Key.ProductId.Value, out var plan)
                    ? plan
                    : "",
                QtyTotal = g.Sum(x => x.Qty),
                TotalSales = g.Sum(x => x.SalesAmt),
                TotalCost = g.Sum(x => x.CostAmt),
                TotalProfit = g.Sum(x => x.ProfitAmt)
            })
            .OrderByDescending(x => x.TotalSales)
            .ToList();

        _gridProduct.DataSource = byProduct;
    }

    private void ExportCsv()
    {
        if (_summaryRows == null || _summaryRows.Count == 0)
        {
            MessageBox.Show("내보낼 데이터가 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*",
            FileName = $"sales_report_{DateTime.Now:yyyyMMddHHmm}.csv",
            InitialDirectory = string.IsNullOrEmpty(_appSettings.DefaultExportFolder)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : _appSettings.DefaultExportFolder
        };

        if (sfd.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using var writer = new StreamWriter(
                sfd.FileName,
                false,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            writer.WriteLine(string.Join(",",
                "Date",
                "Customer",
                "Product",
                "Qty",
                "SalesAmt",
                "CostAmt",
                "ProfitAmt"));

            foreach (var row in _summaryRows)
            {
                var line = string.Join(",",
                    EscapeCsv(row.Date.ToString("yyyy-MM-dd")),
                    EscapeCsv(row.Customer),
                    EscapeCsv(row.Product),
                    row.Qty.ToString(CultureInfo.InvariantCulture),
                    row.SalesAmt.ToString(CultureInfo.InvariantCulture),
                    row.CostAmt.ToString(CultureInfo.InvariantCulture),
                    row.ProfitAmt.ToString(CultureInfo.InvariantCulture));

                writer.WriteLine(line);
            }

            MessageBox.Show("매출 리포트 CSV 저장이 완료되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"CSV 저장 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string EscapeCsv(string? value)
    {
        if (value == null) return "";
        if (!value.Contains(',') && !value.Contains('\"') && !value.Contains('\n'))
            return value;

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
