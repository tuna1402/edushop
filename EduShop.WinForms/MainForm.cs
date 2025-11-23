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
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class MainForm : Form
{
    private readonly ProductService  _service;
    private readonly SalesService    _salesService;
    private readonly AccountService  _accountService;
    private readonly CustomerService _customerService;
    private readonly UserContext     _currentUser;
    private readonly AppSettings     _appSettings;

    // 메뉴바
    private MenuStrip _menuStrip = null!;
    private ToolStripMenuItem _fileMenu = null!;
    private ToolStripMenuItem _manageMenu = null!;
    private ToolStripMenuItem _reportMenu = null!;
    private ToolStripMenuItem _toolsMenu = null!;
    private ToolStripMenuItem _helpMenu = null!;

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
        public double MonthlyFeeUsd   { get; set; }
        public long   MonthlyFeeKrw   { get; set; }
        public long   WholesalePrice  { get; set; }
        public long   RetailPrice     { get; set; }
        public long   PurchasePrice   { get; set; }
        public bool   YearlyAvailable { get; set; }
        public int    MinMonth        { get; set; }
        public int    MaxMonth        { get; set; }
        public string Status          { get; set; } = "";
        public string? Remark         { get; set; }
    }

    public MainForm(
        ProductService  service,
        SalesService    salesService,
        AccountService  accountService,
        CustomerService customerService,
        UserContext     currentUser,
        AppSettings     appSettings)
    {
        _service         = service;
        _salesService    = salesService;
        _accountService  = accountService;
        _customerService = customerService;
        _currentUser     = currentUser;
        _appSettings     = appSettings;

        Text = "EduShop 관리 프로그램";
        Width = 1200;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        InitializeControls();
        InitializeMenu();
        
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
        _cboStatus.Items.Add("");          // 전체
        _cboStatus.Items.Add("ACTIVE");
        _cboStatus.Items.Add("INACTIVE");
        _cboStatus.Items.Add("STOPPED");
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
            HeaderText = "월 구독료(USD)",
            DataPropertyName = "MonthlyFeeUsd",
            Width = 110,
            DefaultCellStyle = { Format = "N2" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "월 구독료(원)",
            DataPropertyName = "MonthlyFeeKrw",
            Width = 110,
            DefaultCellStyle = { Format = "N0" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "도매가",
            DataPropertyName = "WholesalePrice",
            Width = 90,
            DefaultCellStyle = { Format = "N0" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "소매가",
            DataPropertyName = "RetailPrice",
            Width = 90,
            DefaultCellStyle = { Format = "N0" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "매입가",
            DataPropertyName = "PurchasePrice",
            Width = 90,
            DefaultCellStyle = { Format = "N0" }
        });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "연 구독",
            DataPropertyName = "YearlyAvailable",
            Width = 70
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "개월(최소)",
            DataPropertyName = "MinMonth",
            Width = 70
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "개월(최대)",
            DataPropertyName = "MaxMonth",
            Width = 70
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상태",
            DataPropertyName = "Status",
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
        _ctxRowMenu.Items.Add("상품 삭제(상태 INACTIVE)", null, (_, _) => DeleteProduct());
        _ctxRowMenu.Items.Add(new ToolStripSeparator());
        _ctxRowMenu.Items.Add("상태: ACTIVE로 변경", null, (_, _) => SetStatusSelected("ACTIVE"));
        _ctxRowMenu.Items.Add("상태: INACTIVE로 변경", null, (_, _) => SetStatusSelected("INACTIVE"));
        _ctxRowMenu.Items.Add("상태: STOPPED로 변경", null, (_, _) => SetStatusSelected("STOPPED"));
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

        _reportMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            mnuSalesReport,
            new ToolStripSeparator(),
            mnuExpiringAccounts,
            mnuStatusSummary
        });

        // ── 도구(T) ────────────────────────────────
        _toolsMenu = new ToolStripMenuItem("도구(&T)");

        var mnuCodeMaster = new ToolStripMenuItem("코드/상태 값 관리...", null,
            (_, _) => OpenCodeMaster());
        var mnuDbBackup = new ToolStripMenuItem("데이터베이스 백업/복원...", null,
            (_, _) => OpenDbBackup());

        _toolsMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            mnuCodeMaster,
            new ToolStripSeparator(),
            mnuDbBackup
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
        var f = new CustomerListForm(_customerService, _currentUser);
        ShowEmbeddedForm(f);
    }

    private void OpenAccountListForm()
    {
        var f = new AccountListForm(_accountService, _service, _customerService, _currentUser, _appSettings);
        ShowEmbeddedForm(f);
    }

    private void OpenQuoteForm()
    {
        var f = new QuoteForm(_service, _salesService, _currentUser, _appSettings);
        ShowEmbeddedForm(f);
    }

    private void OpenSalesListForm()
    {
        var f = new SalesListForm(_salesService);
        ShowEmbeddedForm(f);
    }

    private void OpenSalesReport()
    {
        var f = new QuoteForm(_service, _salesService, _currentUser, _appSettings);
        ShowEmbeddedForm(f);
    }

    private void OpenExpiringAccountList()
    {
        var f = new AccountListForm(_accountService, _service, _customerService, _currentUser, _appSettings, expiringOnly: true);
        ShowEmbeddedForm(f);
    }

    private void OpenAccountStatusSummary()
    {
        using var dlg = new AccountStatusSummaryForm(
            _accountService,
            _service,
            _customerService,
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
            var status = (string)_cboStatus.SelectedItem!;
            query = query.Where(p => p.Status == status);
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
                MonthlyFeeUsd   = p.MonthlyFeeUsd ?? 0d,
                MonthlyFeeKrw   = p.MonthlyFeeKrw,
                WholesalePrice  = p.WholesalePrice,
                RetailPrice     = p.RetailPrice,
                PurchasePrice   = p.PurchasePrice,
                YearlyAvailable = p.YearlyAvailable,
                MinMonth        = p.MinMonth,
                MaxMonth        = p.MaxMonth,
                Status          = p.Status,
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
            $"상태 INACTIVE로 변경하시겠습니까?",
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
            $"상품 [{selected.ProductCode}] 상태를 '{selected.Status}' → '{newStatus}' 로 변경하시겠습니까?",
            "상태 변경 확인",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        selected.Status = newStatus;
        _service.Update(selected, _currentUser);
        LoadProducts();
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

    private static long ParseLongOrDefault(string? value, long defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;
        return defaultValue;
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
                "PlanName",
                "MonthlyFeeUsd",
                "MonthlyFeeKrw",
                "WholesalePrice",
                "RetailPrice",
                "PurchasePrice",
                "YearlyAvailable",
                "MinMonth",
                "MaxMonth",
                "Status",
                "Remark"));

            foreach (var row in rows)
            {
                var line = string.Join(",",
                    EscapeCsv(row.ProductCode),
                    EscapeCsv(row.ProductName),
                    EscapeCsv(row.PlanName),
                    EscapeCsv(row.MonthlyFeeUsd.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(row.MonthlyFeeKrw.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(row.WholesalePrice.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(row.RetailPrice.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(row.PurchasePrice.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(row.YearlyAvailable ? "1" : "0"),
                    EscapeCsv(row.MinMonth.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(row.MaxMonth.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(row.Status),
                    EscapeCsv(row.Remark ?? "")
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
            if (headerCols.Count < 3 || !headerCols[0].Equals("ProductCode", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("헤더 형식이 예상과 다릅니다. (첫 컬럼은 ProductCode여야 합니다.)", "오류",
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

                string productCode     = cols.ElementAtOrDefault(0)?.Trim() ?? "";
                string productName     = cols.ElementAtOrDefault(1)?.Trim() ?? "";
                string planName        = cols.ElementAtOrDefault(2)?.Trim() ?? "";
                string monthlyUsdStr   = cols.ElementAtOrDefault(3)?.Trim() ?? "";
                string monthlyKrwStr   = cols.ElementAtOrDefault(4)?.Trim() ?? "";
                string wholesaleStr    = cols.ElementAtOrDefault(5)?.Trim() ?? "";
                string retailStr       = cols.ElementAtOrDefault(6)?.Trim() ?? "";
                string purchaseStr     = cols.ElementAtOrDefault(7)?.Trim() ?? "";
                string yearlyStr       = cols.ElementAtOrDefault(8)?.Trim() ?? "";
                string minMonthStr     = cols.ElementAtOrDefault(9)?.Trim() ?? "";
                string maxMonthStr     = cols.ElementAtOrDefault(10)?.Trim() ?? "";
                string statusStr       = cols.ElementAtOrDefault(11)?.Trim() ?? "";
                string remark          = cols.ElementAtOrDefault(12) ?? "";

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

                if (!double.TryParse(monthlyUsdStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var monthlyUsd))
                    monthlyUsd = 0;

                long monthlyKrw  = ParseLongOrDefault(monthlyKrwStr);
                long wholesale   = ParseLongOrDefault(wholesaleStr);
                long retail      = ParseLongOrDefault(retailStr);
                long purchase    = ParseLongOrDefault(purchaseStr);
                bool yearlyAvail = yearlyStr == "1" || yearlyStr.Equals("true", StringComparison.OrdinalIgnoreCase);

                int minMonth = ParseIntOrDefault(minMonthStr, 1);
                int maxMonth = ParseIntOrDefault(maxMonthStr, 12);

                string status = string.IsNullOrWhiteSpace(statusStr) ? "ACTIVE" : statusStr;

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
                            PlanName        = planName,
                            MonthlyFeeUsd   = monthlyUsd,
                            MonthlyFeeKrw   = monthlyKrw,
                            WholesalePrice  = wholesale,
                            RetailPrice     = retail,
                            PurchasePrice   = purchase,
                            YearlyAvailable = yearlyAvail,
                            MinMonth        = minMonth,
                            MaxMonth        = maxMonth,
                            Status          = status,
                            Remark          = remark
                        };

                        _service.Create(p, _currentUser);
                        created++;
                    }
                    else
                    {
                        existing.ProductName     = productName;
                        existing.PlanName        = planName;
                        existing.MonthlyFeeUsd   = monthlyUsd;
                        existing.MonthlyFeeKrw   = monthlyKrw;
                        existing.WholesalePrice  = wholesale;
                        existing.RetailPrice     = retail;
                        existing.PurchasePrice   = purchase;
                        existing.YearlyAvailable = yearlyAvail;
                        existing.MinMonth        = minMonth;
                        existing.MaxMonth        = maxMonth;
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
}
