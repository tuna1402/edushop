using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class HomeDashboardForm : Form
{
    private readonly ProductService  _productService;
    private readonly CustomerService _customerService;
    private readonly AccountService  _accountService;
    private readonly SalesService?   _salesService;
    private readonly UserContext     _currentUser;

    private Label _lblProductCount = null!;
    private Label _lblCustomerCount = null!;
    private Label _lblAccountTotal = null!;
    private Label _lblAccountActive = null!;

    private Label _lblExpiringTitle = null!;
    private Label _lblRecentSalesTitle = null!;
    private DataGridView _dgvExpiringAccounts = null!;
    private DataGridView _dgvRecentSales = null!;

    public HomeDashboardForm(
        ProductService  productService,
        CustomerService customerService,
        AccountService  accountService,
        SalesService?   salesService,
        UserContext     currentUser)
    {
        _productService  = productService;
        _customerService = customerService;
        _accountService  = accountService;
        _salesService    = salesService;
        _currentUser     = currentUser;

        Text = "홈 대시보드";
        TopLevel = false;
        FormBorderStyle = FormBorderStyle.None;
        Dock = DockStyle.Fill;

        InitializeControls();
        LoadSummary();
    }

    private void InitializeControls()
    {
        var summaryPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            Padding = new Padding(10)
        };

        var summaryLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1
        };
        summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        summaryLayout.Controls.Add(CreateSummaryCard("상품 수", out _lblProductCount), 0, 0);
        summaryLayout.Controls.Add(CreateSummaryCard("고객 수", out _lblCustomerCount), 1, 0);
        summaryLayout.Controls.Add(CreateSummaryCard("계정 수", out _lblAccountTotal), 2, 0);
        summaryLayout.Controls.Add(CreateSummaryCard("활성 계정", out _lblAccountActive), 3, 0);

        summaryPanel.Controls.Add(summaryLayout);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(10)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        // 만료 예정 계정 영역
        var expiringPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(5)
        };

        _lblExpiringTitle = new Label
        {
            Text = "만료 예정 계정",
            Dock = DockStyle.Top,
            Font = new Font(Font, FontStyle.Bold),
            Height = 24
        };

        _dgvExpiringAccounts = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        _dgvExpiringAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "이메일",
            DataPropertyName = nameof(ExpiringAccountRow.Email),
            Width = 180
        });
        _dgvExpiringAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "고객명",
            DataPropertyName = nameof(ExpiringAccountRow.CustomerName),
            Width = 140
        });
        _dgvExpiringAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상품",
            DataPropertyName = nameof(ExpiringAccountRow.ProductName),
            Width = 170
        });
        _dgvExpiringAccounts.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "만료일",
            DataPropertyName = nameof(ExpiringAccountRow.SubscriptionEndDate),
            Width = 100
        });

        expiringPanel.Controls.Add(_dgvExpiringAccounts);
        expiringPanel.Controls.Add(_lblExpiringTitle);

        // 최근 매출 영역
        var salesPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(5)
        };

        _lblRecentSalesTitle = new Label
        {
            Text = "최근 매출",
            Dock = DockStyle.Top,
            Font = new Font(Font, FontStyle.Bold),
            Height = 24
        };

        _dgvRecentSales = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        _dgvRecentSales.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "일자",
            DataPropertyName = nameof(RecentSaleRow.SaleDate),
            Width = 90
        });
        _dgvRecentSales.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "고객명",
            DataPropertyName = nameof(RecentSaleRow.CustomerName),
            Width = 150
        });
        _dgvRecentSales.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "금액",
            DataPropertyName = nameof(RecentSaleRow.TotalAmount),
            Width = 100
        });

        salesPanel.Controls.Add(_dgvRecentSales);
        salesPanel.Controls.Add(_lblRecentSalesTitle);

        mainLayout.Controls.Add(expiringPanel, 0, 0);
        mainLayout.Controls.Add(salesPanel, 1, 0);

        Controls.Add(mainLayout);
        Controls.Add(summaryPanel);
    }

    private Control CreateSummaryCard(string title, out Label valueLabel)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font(Font, FontStyle.Bold)
        };

        valueLabel = new Label
        {
            Text = "0",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            ForeColor = Color.Navy,
            TextAlign = ContentAlignment.MiddleLeft
        };

        panel.Controls.Add(valueLabel);
        panel.Controls.Add(titleLabel);

        return panel;
    }

    private void LoadSummary()
    {
        var products = _productService.GetAll();
        var customers = _customerService.GetAll();
        var accounts = _accountService.GetAll();

        var activeStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AccountStatus.SubsActive,
            AccountStatus.InUse,
            AccountStatus.Delivered
        };

        _lblProductCount.Text  = products.Count.ToString("N0");
        _lblCustomerCount.Text = customers.Count.ToString("N0");
        _lblAccountTotal.Text  = accounts.Count.ToString("N0");
        _lblAccountActive.Text = accounts.Count(a => activeStatuses.Contains(a.Status)).ToString("N0");

        LoadExpiringAccounts(accounts, products, customers);
        LoadRecentSales();
    }

    private void LoadExpiringAccounts(List<Account> accounts, List<Product> products, List<Customer> customers)
    {
        var today = DateTime.Today;
        var expiringDays = AppSettingsManager.Current.ExpiringDays > 0
            ? AppSettingsManager.Current.ExpiringDays
            : 30;
        _lblExpiringTitle.Text = $"만료 예정 계정 ({expiringDays}일 이내)";

        var productMap = products.ToDictionary(p => p.ProductId, p => p, EqualityComparer<long>.Default);
        var customerMap = customers.ToDictionary(c => c.CustomerId, c => c, EqualityComparer<long>.Default);

        var limit = today.AddDays(expiringDays);

        var rows = accounts
            .Where(a => a.SubscriptionEndDate.Date >= today && a.SubscriptionEndDate.Date <= limit)
            .Where(a => a.Status != AccountStatus.Canceled && a.Status != AccountStatus.ResetReady)
            .OrderBy(a => a.SubscriptionEndDate)
            .Take(5)
            .Select(a => new ExpiringAccountRow
            {
                Email = a.Email,
                CustomerName = a.CustomerId.HasValue && customerMap.TryGetValue(a.CustomerId.Value, out var customer)
                    ? customer.CustomerName
                    : "-",
                ProductName = productMap.TryGetValue(a.ProductId, out var product)
                    ? product.ProductName
                    : $"#{a.ProductId}",
                SubscriptionEndDate = a.SubscriptionEndDate.ToString("yyyy-MM-dd"),
                StatusDisplay = AccountStatusHelper.ToDisplay(a.Status)
            })
            .ToList();

        _dgvExpiringAccounts.DataSource = rows;
    }

    private void LoadRecentSales()
    {
        if (_salesService == null)
        {
            _lblRecentSalesTitle.Text = "최근 매출 (서비스 미연결)";
            _dgvRecentSales.Visible = false;
            return;
        }

        var recent = _salesService.GetRecent(5);
        _lblRecentSalesTitle.Text = "최근 매출";

        var rows = recent
            .Select(s => new RecentSaleRow
            {
                SaleDate = s.SaleDate.ToString("yyyy-MM-dd"),
                CustomerName = s.CustomerName ?? s.SchoolName ?? "-",
                TotalAmount = s.TotalAmount.ToString("N0")
            })
            .ToList();

        _dgvRecentSales.DataSource = rows;
    }

    private class ExpiringAccountRow
    {
        public string Email { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string SubscriptionEndDate { get; set; } = "";
        public string StatusDisplay { get; set; } = "";
    }

    private class RecentSaleRow
    {
        public string SaleDate { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string TotalAmount { get; set; } = "";
    }
}
