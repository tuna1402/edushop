using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Repositories;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class MainForm : Form
{
    private readonly ProductService  _service;
    private readonly SalesService    _salesService;
    private readonly AccountService  _accountService;
    private readonly CustomerService _customerService;
    private readonly CardService     _cardService;
    private readonly AuditLogRepository _auditRepo;
    private readonly AccountUsageLogRepository _usageRepo;
    private readonly UserContext     _currentUser;
    private readonly AppSettings     _appSettings;
    private readonly SupabaseSessionState _supabaseState;

    // 메뉴바
    private MenuStrip _menuStrip = null!;
    private ToolStripMenuItem _fileMenu = null!;
    private ToolStripMenuItem _manageMenu = null!;
    private ToolStripMenuItem _reportMenu = null!;
    private ToolStripMenuItem _toolsMenu = null!;
    private ToolStripMenuItem _helpMenu = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _dbStatusLabel = null!;

    // 가이드 초기 1회 표시용
    private bool _guideShown = false;

    // 검색 영역
    private TextBox  _txtKeyword = null!;
    private ComboBox _cboStatus = null!;
    private Button   _btnSearch = null!;
    private Button   _btnReset = null!;
    private Button   _btnExportCsv = null!;
    private Button   _btnMore = null!;

    // 그리드 + 하단 버튼
    private DataGridView _grid = null!;
    private Button       _btnNew = null!;
    private Button       _btnEdit = null!;
    private Button       _btnClose = null!;
    private Panel _contentPanel = null!;
    private Form? _currentEmbeddedForm;
    private Panel _productPagePanel = null!;

    // 컨텍스트 메뉴
    private ContextMenuStrip _ctxRowMenu = null!;
    private ContextMenuStrip _ctxMoreMenu = null!;

    // 데이터
    private List<Product> _products = new();

    private class ProductRow
    {
        public long   ProductId       { get; set; }
        public string ProductCode     { get; set; } = "";
        public string ProductName     { get; set; } = "";
        public string PlanName        { get; set; } = "";
        public int    DurationMonths  { get; set; }
        public double? PurchasePriceUsd { get; set; }
        public long?  PurchasePriceKrw { get; set; }
        public long   SalePriceKrw    { get; set; }
        public string StatusLabel     { get; set; } = "";
        public string? Remark         { get; set; }
    }

    private sealed class StatusOption
    {
        public StatusOption(string value, string text)
        {
            Value = value;
            Text = text;
        }

        public string Value { get; }
        public string Text { get; }
        public override string ToString() => Text;
    }

    public MainForm(
        ProductService  service,
        SalesService    salesService,
        AccountService  accountService,
        CustomerService customerService,
        CardService     cardService,
        AuditLogRepository auditRepo,
        AccountUsageLogRepository usageRepo,
        UserContext     currentUser,
        AppSettings     appSettings,
        SupabaseSessionState supabaseState)
    {
        _service         = service;
        _salesService    = salesService;
        _accountService  = accountService;
        _customerService = customerService;
        _cardService     = cardService;
        _auditRepo       = auditRepo;
        _usageRepo       = usageRepo;
        _currentUser     = currentUser;
        _appSettings     = appSettings;
        _supabaseState   = supabaseState;

        Text = "EduShop 관리 프로그램";
        Width = 1200;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        InitializeControls();
        InitializeMenu();
        InitializeStatusStrip();
        
        LoadProducts();

        Load += (_, _) =>
        {
            ShowGuideOnStartup();
            ShowHomeDashboard();
        };
    }

    // ─────────────────────────────────────────────────────
    // UI 초기화
    // ─────────────────────────────────────────────────────
    private void InitializeControls()
    {
        // ── 검색 영역 컨트롤 생성 ─────────────────────
        var lblKeyword = new Label
        {
            Text = "검색",
            Left = 10,
            Top  = 15,
            Width = 40
        };
        _txtKeyword = new TextBox
        {
            Left = lblKeyword.Right + 5,
            Top  = 12,
            Width = 220
        };

        var lblStatus = new Label
        {
            Text = "상태",
            Left = _txtKeyword.Right + 20,
            Top  = 15,
            Width = 40
        };
        _cboStatus = new ComboBox
        {
            Left = lblStatus.Right + 5,
            Top  = 12,
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboStatus.Items.Add(new StatusOption("", "전체"));
        _cboStatus.Items.Add(new StatusOption("ACTIVE", "판매중"));
        _cboStatus.Items.Add(new StatusOption("INACTIVE", "품절"));
        _cboStatus.SelectedIndex = 0;

        _btnSearch = new Button
        {
            Text = "조회",
            Left = _cboStatus.Right + 20,
            Top  = 10,
            Width = 80
        };
        _btnSearch.Click += (_, _) => LoadProducts();

        _btnReset = new Button
        {
            Text = "초기화",
            Left = _btnSearch.Right + 10,
            Top  = 10,
            Width = 80
        };
        _btnReset.Click += (_, _) => ResetFilters();

        _btnExportCsv = new Button
        {
            Text = "엑셀 다운로드",
            Left = _btnReset.Right + 10,
            Top  = 10,
            Width = 110
        };
        _btnExportCsv.Click += (_, _) => ExportProductsCsv();

        _btnMore = new Button
        {
            Text = "더보기 ▼",
            Left = _btnExportCsv.Right + 10,
            Top  = 10,
            Width = 90
        };
        _btnMore.Click += (_, _) => ShowMoreMenu();

        // ── 상품 그리드 ──────────────────────────────
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect   = true,
            AutoGenerateColumns = false
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "ID",
            DataPropertyName = "ProductId",
            Width = 60
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "코드",
            DataPropertyName = "ProductCode",
            Width = 90
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상품명",
            DataPropertyName = "ProductName",
            Width = 260
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "플랜",
            DataPropertyName = "PlanName",
            Width = 120
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "기간(개월)",
            DataPropertyName = "DurationMonths",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "매입가(USD)",
            DataPropertyName = "PurchasePriceUsd",
            Width = 90,
            DefaultCellStyle = { Format = "N2" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "매입가(원)",
            DataPropertyName = "PurchasePriceKrw",
            Width = 90,
            DefaultCellStyle = { Format = "N0" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "판매가(원)",
            DataPropertyName = "SalePriceKrw",
            Width = 90,
            DefaultCellStyle = { Format = "N0" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상태",
            DataPropertyName = "StatusLabel",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "메모",
            DataPropertyName = "Remark",
            Width = 200
        });

        _grid.DoubleClick += (_, _) => EditProduct();
        _grid.KeyDown += GridOnKeyDown;

        // ── 하단 버튼 ───────────────────────────────
        _btnNew = new Button
        {
            Text = "신규",
            Width = 80,
            Left = 10,
            Top  = 10
        };
        _btnNew.Click += (_, _) => NewProduct();

        _btnEdit = new Button
        {
            Text = "수정",
            Width = 80,
            Left = _btnNew.Right + 10,
            Top  = 10
        };
        _btnEdit.Click += (_, _) => EditProduct();

        _btnClose = new Button
        {
            Text = "닫기",
            Width = 80,
            Left = _btnEdit.Right + 10,
            Top  = 10
        };
        _btnClose.Click += (_, _) => Close();

        // ── 콘텐츠 영역 패널(다른 페이지 포함) ────────
        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill   // 메뉴바 아래 전체 영역
        };
        Controls.Add(_contentPanel);

        // ── 상품 페이지용 패널 구성 ───────────────────
        var searchPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45
        };
        searchPanel.Controls.Add(lblKeyword);
        searchPanel.Controls.Add(_txtKeyword);
        searchPanel.Controls.Add(lblStatus);
        searchPanel.Controls.Add(_cboStatus);
        searchPanel.Controls.Add(_btnSearch);
        searchPanel.Controls.Add(_btnReset);
        searchPanel.Controls.Add(_btnExportCsv);
        searchPanel.Controls.Add(_btnMore);

        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 45
        };
        bottomPanel.Controls.Add(_btnNew);
        bottomPanel.Controls.Add(_btnEdit);
        bottomPanel.Controls.Add(_btnClose);

        var gridPanel = new Panel
        {
            Dock = DockStyle.Fill
        };
        gridPanel.Controls.Add(_grid);

        _productPagePanel = new Panel
        {
            Dock = DockStyle.Fill
        };
        _productPagePanel.Controls.Add(gridPanel);
        _productPagePanel.Controls.Add(bottomPanel);
        _productPagePanel.Controls.Add(searchPanel);

        // 처음에는 상품 페이지를 표시
        _contentPanel.Controls.Add(_productPagePanel);

        // ── 그리드 우클릭 메뉴 ───────────────────────
        _ctxRowMenu = new ContextMenuStrip();
        _ctxRowMenu.Items.Add("상품 수정(&E)", null, (_, _) => EditProduct());
        _ctxRowMenu.Items.Add("상품 삭제(품절)", null, (_, _) => DeleteProduct());
        _ctxRowMenu.Items.Add(new ToolStripSeparator());
        _ctxRowMenu.Items.Add("상태: 판매중으로 변경", null, (_, _) => SetStatusSelected("ACTIVE"));
        _ctxRowMenu.Items.Add("상태: 품절로 변경", null, (_, _) => SetStatusSelected("INACTIVE"));
        _ctxRowMenu.Items.Add(new ToolStripSeparator());
        _ctxRowMenu.Items.Add("선택 상품만 엑셀 다운로드", null, (_, _) => ExportProductsCsv(true));

        _grid.ContextMenuStrip = _ctxRowMenu;

        // "더보기" 메뉴 (글로벌/부가기능)
        _ctxMoreMenu = new ContextMenuStrip();
        _ctxMoreMenu.Items.Add("엑셀 등록 (Import)", null, (_, _) => ImportProductsCsv());
        _ctxMoreMenu.Items.Add(new ToolStripSeparator());
        _ctxMoreMenu.Items.Add("견적서", null, (_, _) => OpenQuoteForm());
        _ctxMoreMenu.Items.Add("매출 현황", null, (_, _) => OpenSalesListForm());
        _ctxMoreMenu.Items.Add("계정 관리", null, (_, _) => OpenAccountListForm());
        _ctxMoreMenu.Items.Add("고객 관리", null, (_, _) => OpenCustomerListForm());
        _ctxMoreMenu.Items.Add("카드 관리", null, (_, _) => OpenCardListForm());
    }

    private void InitializeStatusStrip()
    {
        _statusStrip = new StatusStrip();
        _dbStatusLabel = new ToolStripStatusLabel();
        _statusStrip.Items.Add(_dbStatusLabel);
        Controls.Add(_statusStrip);
        UpdateDatabaseStatus();
    }


    private void InitializeMenu()
    {
        _menuStrip = new MenuStrip();

        // ── 파일(F) ────────────────────────────────
        _fileMenu = new ToolStripMenuItem("파일(&F)");

        var mnuHome = new ToolStripMenuItem("홈 화면(&H)", null,
            (_, _) => ShowHomeDashboard());
        var mnuSettings = new ToolStripMenuItem("환경 설정(&S)...", null,
            (_, _) => OpenSettings());
        var mnuExit = new ToolStripMenuItem("종료(&X)", null,
            (_, _) => Close());

        _fileMenu.DropDownItems.Add(mnuHome);
        _fileMenu.DropDownItems.Add(new ToolStripSeparator());
        _fileMenu.DropDownItems.Add(mnuSettings);
        _fileMenu.DropDownItems.Add(new ToolStripSeparator());
        _fileMenu.DropDownItems.Add(mnuExit);

        // ── 관리(M) ────────────────────────────────
        _manageMenu = new ToolStripMenuItem("관리(&M)");

        var mnuProductList = new ToolStripMenuItem("상품 목록(&P)...", null,
            (_, _) => FocusProductList());
        var mnuProductCsv = new ToolStripMenuItem("상품 엑셀 Import/Export...", null,
            (_, _) => ShowProductCsvGuide());

        var mnuCustomerList = new ToolStripMenuItem("고객 목록(&C)...", null,
            (_, _) => OpenCustomerListForm());
        var mnuAccountList = new ToolStripMenuItem("계정 목록(&A)...", null,
            (_, _) => OpenAccountListForm());
        var mnuCardList = new ToolStripMenuItem("카드 목록(&D)...", null,
            (_, _) => OpenCardListForm());
        var mnuQuote = new ToolStripMenuItem("견적서 작성(&Q)...", null,
            (_, _) => OpenQuoteForm());
        var mnuSalesList = new ToolStripMenuItem("매출/납품 내역(&O)...", null,
            (_, _) => OpenSalesListForm());

        _manageMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            mnuProductList,
            mnuProductCsv,
            new ToolStripSeparator(),
            mnuCustomerList,
            mnuAccountList,
            mnuCardList,
            new ToolStripSeparator(),
            mnuQuote,
            mnuSalesList
        });

        // ── 리포트(R) ────────────────────────────────
        _reportMenu = new ToolStripMenuItem("리포트(&R)");

        var mnuSalesReport = new ToolStripMenuItem("매출 리포트(&M)...", null,
            (_, _) => OpenSalesReport());
        var mnuExpiringAccounts = new ToolStripMenuItem("만료 예정 계정 목록(&E)...", null,
            (_, _) => OpenExpiringAccountList());
        var mnuStatusSummary = new ToolStripMenuItem("계정 상태별 통계(&S)...", null,
            (_, _) => OpenAccountStatusSummary());
        var mnuLogViewer = new ToolStripMenuItem("로그/이력 조회(&L)...", null,
            (_, _) => OpenLogViewer());

        _reportMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            mnuSalesReport,
            new ToolStripSeparator(),
            mnuExpiringAccounts,
            mnuStatusSummary,
            new ToolStripSeparator(),
            mnuLogViewer
        });

        // ── 도구(T) ────────────────────────────────
        _toolsMenu = new ToolStripMenuItem("도구(&T)");

        var mnuCodeMaster = new ToolStripMenuItem("코드/상태 값 관리...", null,
            (_, _) => OpenCodeMaster());
        var mnuDbBackup = new ToolStripMenuItem("데이터베이스 백업/복원...", null,
            (_, _) => OpenDbBackup());
        var mnuSupabaseSettings = new ToolStripMenuItem("Supabase 설정...", null,
            (_, _) => OpenSupabaseSettings());

        _toolsMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            mnuCodeMaster,
            new ToolStripSeparator(),
            mnuDbBackup,
            new ToolStripSeparator(),
            mnuSupabaseSettings
        });

        // ── 도움말(H) ────────────────────────────────
        _helpMenu = new ToolStripMenuItem("도움말(&H)");

        var mnuGuide = new ToolStripMenuItem("프로그램 가이드(&G)...", null,
            (_, _) => ShowGuide());
        var mnuShortcuts = new ToolStripMenuItem("사용 팁/단축키(&K)...", null,
            (_, _) => ShowTips());
        var mnuAbout = new ToolStripMenuItem("정보(&A)...", null,
            (_, _) => ShowAbout());

        _helpMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            mnuGuide,
            mnuShortcuts,
            new ToolStripSeparator(),
            mnuAbout
        });

        // 상단 메뉴바에 등록
        _menuStrip.Items.AddRange(new ToolStripItem[]
        {
            _fileMenu,
            _manageMenu,
            _reportMenu,
            _toolsMenu,
            _helpMenu
        });

        MainMenuStrip = _menuStrip;
        Controls.Add(_menuStrip);
    }

    // ── 가이드 표시 ───────────────────────────────
    private void ShowGuideOnStartup()
    {
        if (_guideShown) return;
        _guideShown = true;
        ShowGuide();
    }

    private void ShowGuide()
    {
        using var guide = new GuideForm();
        guide.ShowDialog(this);
    }

    // ── 파일/도구 관련 ────────────────────────────
    private void OpenSettings()
    {
        using var dlg = new SettingsForm(_appSettings);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            MessageBox.Show("설정을 저장했습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void OpenLogViewer()
    {
        using var dlg = new LogViewerForm(
            _auditRepo,
            _usageRepo,
            _service,
            _customerService,
            _accountService,
            _currentUser);
        dlg.ShowDialog(this);
    }

    // 현재 화면이 이미 상품 목록이라 포커스만 주면 됨
    private void FocusProductList()
    {
        if (_currentEmbeddedForm != null)
        {
            _currentEmbeddedForm.Close();
            _currentEmbeddedForm.Dispose();
            _currentEmbeddedForm = null;
        }

        _contentPanel.Controls.Clear();
        _contentPanel.Controls.Add(_productPagePanel);
        _grid.Focus();
    }

    private void ShowHomeDashboard()
    {
        var dashboard = new HomeDashboardForm(
            _service,
            _customerService,
            _accountService,
            _salesService,
            _currentUser,
            _appSettings);

        ShowEmbeddedForm(dashboard);
    }

    private void ShowProductCsvGuide()
    {
        MessageBox.Show("상품 화면 하단의 엑셀 Import/Export 버튼을 사용하세요.", "안내",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ── 관리: 다른 폼 열기 ───────────────────────
    private void OpenCustomerListForm()
    {
        var f = new CustomerListForm(_customerService, _currentUser, _supabaseState);
        ShowEmbeddedForm(f);
    }

    private void OpenSupabaseSettings()
    {
        using var dlg = new SupabaseSettingsForm(_supabaseState);
        dlg.ShowDialog(this);
        UpdateDatabaseStatus();
    }

    private void OpenAccountListForm()
    {
        var f = new AccountListForm(_accountService, _service, _salesService, _customerService, _cardService, _currentUser, _appSettings);
        ShowEmbeddedForm(f);
    }

    private void OpenCardListForm()
    {
        var f = new CardListForm(_cardService, _currentUser);
        ShowEmbeddedForm(f);
    }

    private void OpenQuoteForm()
    {
        var f = new QuoteForm(_service, _salesService, _currentUser, _appSettings);
        ShowEmbeddedForm(f);
    }

    private void OpenSalesListForm()
    {
        var f = new SalesListForm(_salesService, _accountService);
        ShowEmbeddedForm(f);
    }

    private void OpenSalesReport()
    {
        using var dlg = new SalesReportForm(
            _salesService,
            _customerService,
            _service,
            _appSettings,
            _currentUser);

        dlg.ShowDialog(this);
    }

    private void OpenExpiringAccountList()
    {
        var f = new AccountListForm(_accountService, _service, _salesService, _customerService, _cardService, _currentUser, _appSettings, expiringOnly: true);
        ShowEmbeddedForm(f);
    }

    private void OpenAccountStatusSummary()
    {
        using var dlg = new AccountStatusSummaryForm(
            _accountService,
            _service,
            _salesService,
            _customerService,
            _cardService,
            _currentUser,
            _appSettings);

        dlg.ShowDialog(this);
    }

    private void OpenCodeMaster()
    {
        MessageBox.Show("코드/상태 값 관리 화면은 추후 구현 예정입니다.", "안내",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenDbBackup()
    {
        MessageBox.Show("DB 백업/복원 기능은 추후 구현 예정입니다.", "안내",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowTips()
    {
        MessageBox.Show("자주 쓰는 기능:\n- 그리드 더블클릭: 선택 항목 수정\n- Delete: 선택 항목 삭제(비활성)\n- 컨텍스트 메뉴: 우클릭", 
            "사용 팁",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowAbout()
    {
        MessageBox.Show("EduShop 관리 프로그램\n버전 1.0\n\nGitHub: tuna1402/edushop", 
            "정보",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }


    private void ShowMoreMenu()
    {
        if (_ctxMoreMenu == null) return;
        var location = new Point(0, _btnMore.Height);
        _ctxMoreMenu.Show(_btnMore, location);
    }

    private void GridOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            EditProduct();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Delete)
        {
            DeleteProduct();
            e.Handled = true;
        }
    }

    // ─────────────────────────────────────────────────────
    // 데이터 로딩/필터
    // ─────────────────────────────────────────────────────
    private void ResetFilters()
    {
        _txtKeyword.Text = "";
        _cboStatus.SelectedIndex = 0;
        LoadProducts();
    }

    private void LoadProducts()
    {
        _products = _service.GetAll();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<Product> query = _products;

        var keyword = _txtKeyword.Text.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(p =>
                (p.ProductName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (p.ProductCode ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (_cboStatus.SelectedIndex > 0)
        {
            var status = (_cboStatus.SelectedItem as StatusOption)?.Value;
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }
        }

        var list = query
            .OrderBy(p => p.ProductName)
            .ThenBy(p => p.PlanName)
            .Select(p => new ProductRow
            {
                ProductId       = p.ProductId,
                ProductCode     = p.ProductCode,
                ProductName     = p.ProductName,
                PlanName        = p.PlanName ?? "",
                DurationMonths  = p.DurationMonths,
                PurchasePriceUsd = p.PurchasePriceUsd,
                PurchasePriceKrw = p.PurchasePriceKrw,
                SalePriceKrw    = p.SalePriceKrw,
                StatusLabel     = GetStatusLabel(p.Status),
                Remark          = p.Remark
            })
            .ToList();

        _grid.DataSource = list;
    }

    private Product? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is not ProductRow row)
            return null;

        return _products.FirstOrDefault(p => p.ProductId == row.ProductId);
    }

    // ─────────────────────────────────────────────────────
    // 네가 준 NewProduct / EditProduct 그대로 사용
    // ─────────────────────────────────────────────────────
    private void NewProduct()
    {
        // 폼 안에서 직접 _service.Create(...) 호출하도록 설계했으므로
        // 여기서는 ProductService/UserContext만 넘겨주고, 결과만 확인
        using var dlg = new ProductDetailForm(_service, _currentUser, null);

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            // 저장은 폼 안에서 이미 처리했으니 목록만 갱신
            LoadProducts();
        }
    }

    private void EditProduct()
    {
        var selected = GetSelected();
        if (selected == null)
        {
            MessageBox.Show("수정할 상품을 선택하세요.");
            return;
        }

        using var dlg = new ProductDetailForm(_service, _currentUser, selected);

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            // 수정 처리도 폼에서 이미 수행
            LoadProducts();
        }
    }

    private void DeleteProduct()
    {
        var selected = GetSelected();
        if (selected == null)
        {
            MessageBox.Show("삭제할 상품을 선택하세요.");
            return;
        }

        var result = MessageBox.Show(
            $"상품 [{selected.ProductCode}] {selected.ProductName} 을(를)\n" +
            $"상태 품절로 변경하시겠습니까?",
            "삭제(비활성) 확인",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        selected.Status = "INACTIVE";
        _service.Update(selected, _currentUser);
        LoadProducts();
    }

    private void SetStatusSelected(string newStatus)
    {
        var selected = GetSelected();
        if (selected == null) return;

        if (selected.Status == newStatus) return;

        var result = MessageBox.Show(
            $"상품 [{selected.ProductCode}] 상태를 '{GetStatusLabel(selected.Status)}' → '{GetStatusLabel(newStatus)}' 로 변경하시겠습니까?",
            "상태 변경 확인",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        selected.Status = newStatus;
        _service.Update(selected, _currentUser);
        LoadProducts();
    }

    private static string GetStatusLabel(string status)
    {
        return status == "ACTIVE" ? "판매중" : "품절";
    }

    private static string GetStatusValue(string label)
    {
        return label == "판매중" ? "ACTIVE" : "INACTIVE";
    }

    // ─────────────────────────────────────────────────────
    // CSV 유틸
    // ─────────────────────────────────────────────────────
    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(line))
        {
            result.Add(string.Empty);
            return result;
        }

        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        result.Add(sb.ToString());
        return result;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        var needsQuote = value.Contains(',') || value.Contains('"') ||
                         value.Contains('\n') || value.Contains('\r');

        if (!needsQuote)
            return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string GetCsvValue(IReadOnlyList<string> cols, IReadOnlyDictionary<string, int> columnMap, string columnName)
    {
        if (!columnMap.TryGetValue(columnName, out var index))
            return "";

        return index >= 0 && index < cols.Count ? cols[index].Trim() : "";
    }

    private static long ParseLongOrDefault(string? value, long defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;
        return defaultValue;
    }

    private static double? ParseDoubleOrDefault(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out v))
            return v;
        return null;
    }

    private static int ParseIntOrDefault(string? value, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;
        return defaultValue;
    }

    // ─────────────────────────────────────────────────────
    // 상품 엑셀 Export
    // ─────────────────────────────────────────────────────
    private void ExportProductsCsv(bool onlySelected = false)
    {
        List<ProductRow> rows;

        if (onlySelected && _grid.SelectedRows.Count > 0)
        {
            rows = new List<ProductRow>();
            foreach (DataGridViewRow r in _grid.SelectedRows)
            {
                if (r.DataBoundItem is ProductRow pr)
                    rows.Add(pr);
            }
        }
        else
        {
            if (_grid.DataSource is not List<ProductRow> all || all.Count == 0)
            {
                MessageBox.Show("내보낼 상품 데이터가 없습니다.", "안내",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            rows = all;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*",
            FileName = $"products_{DateTime.Now:yyyyMMddHHmm}.csv",
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
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)); // UTF-8 BOM

            writer.WriteLine(string.Join(",",
                "ProductCode",
                "ProductName",
                "DurationMonths",
                "PurchasePriceUsd",
                "PurchasePriceKrw",
                "SalePriceKrw",
                "Status"));

            foreach (var row in rows)
            {
                var line = string.Join(",",
                    EscapeCsv(row.ProductCode),
                    EscapeCsv(row.ProductName),
                    EscapeCsv(row.DurationMonths.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(row.PurchasePriceUsd?.ToString(CultureInfo.InvariantCulture) ?? ""),
                    EscapeCsv(row.PurchasePriceKrw?.ToString(CultureInfo.InvariantCulture) ?? ""),
                    EscapeCsv(row.SalePriceKrw.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(GetStatusValue(row.StatusLabel))
                );

                writer.WriteLine(line);
            }

            MessageBox.Show("상품 목록 CSV 저장이 완료되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"CSV 저장 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ─────────────────────────────────────────────────────
    // 상품 엑셀 Import
    // ─────────────────────────────────────────────────────
    private void ImportProductsCsv()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*"
        };

        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        int created = 0;
        int updated = 0;
        int skipped = 0;
        var errors  = new List<string>();

        try
        {
            using var reader = new StreamReader(ofd.FileName, Encoding.UTF8);
            string? header = reader.ReadLine();
            if (header == null)
            {
                MessageBox.Show("파일이 비어 있습니다.", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var headerCols = SplitCsvLine(header);
            var columnMap = headerCols
                .Select((name, index) => new { Name = name.Trim(), Index = index })
                .ToDictionary(x => x.Name, x => x.Index, StringComparer.OrdinalIgnoreCase);

            if (!columnMap.ContainsKey("ProductCode"))
            {
                MessageBox.Show("헤더 형식이 예상과 다릅니다. (ProductCode 컬럼이 필요합니다.)", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!columnMap.ContainsKey("ProductName") || !columnMap.ContainsKey("DurationMonths"))
            {
                MessageBox.Show("헤더 형식이 예상과 다릅니다. (ProductName, DurationMonths 컬럼이 필요합니다.)", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int lineNo = 1;
            // 최신 목록 기준으로 중복 판단
            _products = _service.GetAll();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                lineNo++;
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var cols = SplitCsvLine(line);

                string productCode     = GetCsvValue(cols, columnMap, "ProductCode");
                string productName     = GetCsvValue(cols, columnMap, "ProductName");
                string durationStr     = GetCsvValue(cols, columnMap, "DurationMonths");
                string purchaseUsdStr  = GetCsvValue(cols, columnMap, "PurchasePriceUsd");
                string purchaseKrwStr  = GetCsvValue(cols, columnMap, "PurchasePriceKrw");
                string saleKrwStr      = GetCsvValue(cols, columnMap, "SalePriceKrw");
                string statusStr       = GetCsvValue(cols, columnMap, "Status");
                string remark          = GetCsvValue(cols, columnMap, "Remark");

                if (string.IsNullOrEmpty(productCode))
                {
                    errors.Add($"라인 {lineNo}: ProductCode가 비어 있습니다.");
                    skipped++;
                    continue;
                }

                if (string.IsNullOrEmpty(productName))
                {
                    errors.Add($"라인 {lineNo}: ProductName이 비어 있습니다.");
                    skipped++;
                    continue;
                }

                int durationMonths = ParseIntOrDefault(durationStr, 0);
                var purchaseUsd = ParseDoubleOrDefault(purchaseUsdStr);
                var purchaseKrw = string.IsNullOrWhiteSpace(purchaseKrwStr)
                    ? (long?)null
                    : ParseLongOrDefault(purchaseKrwStr);
                long saleKrw = ParseLongOrDefault(saleKrwStr);
                string status = string.IsNullOrWhiteSpace(statusStr) ? "ACTIVE" : statusStr;
                if (status == "STOPPED")
                    status = "INACTIVE";

                try
                {
                    var existing = _products.FirstOrDefault(p =>
                        p.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase));

                    if (existing == null)
                    {
                        var p = new Product
                        {
                            ProductCode     = productCode,
                            ProductName     = productName,
                            DurationMonths  = durationMonths,
                            PurchasePriceUsd = purchaseUsd,
                            PurchasePriceKrw = purchaseKrw,
                            SalePriceKrw    = saleKrw,
                            Status          = status,
                            Remark          = remark
                        };

                        _service.Create(p, _currentUser);
                        created++;
                    }
                    else
                    {
                        existing.ProductName     = productName;
                        existing.DurationMonths  = durationMonths;
                        existing.PurchasePriceUsd = purchaseUsd;
                        existing.PurchasePriceKrw = purchaseKrw;
                        existing.SalePriceKrw    = saleKrw;
                        existing.Status          = status;
                        existing.Remark          = remark;

                        _service.Update(existing, _currentUser);
                        updated++;
                    }
                }
                catch (Exception exRow)
                {
                    errors.Add($"라인 {lineNo}: 저장 중 오류 - {exRow.Message}");
                    skipped++;
                }
            }

            LoadProducts();

            var msg = $"상품 Import 완료:\n" +
                      $"- 신규 등록: {created}건\n" +
                      $"- 수정: {updated}건\n" +
                      $"- 건너뜀: {skipped}건";

            if (errors.Count > 0)
            {
                msg += "\n\n[일부 오류 예시]\n" +
                       string.Join("\n", errors.Take(10));
            }

            MessageBox.Show(msg, "Import 결과",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"CSV Import 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowEmbeddedForm(Form form)
    {
        // 기존 뷰 정리
        if (_currentEmbeddedForm != null)
        {
            _currentEmbeddedForm.Close();
            _currentEmbeddedForm.Dispose();
            _currentEmbeddedForm = null;
        }

        // 새 뷰 설정
        _currentEmbeddedForm = form;
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;

        _contentPanel.Controls.Clear();
        _contentPanel.Controls.Add(form);
        form.Show();
    }

    private void UpdateDatabaseStatus()
    {
        var label = _supabaseState.IsSupabaseActive ? "DB: Supabase" : "DB: Local";
        _dbStatusLabel.Text = label;
    }
}
