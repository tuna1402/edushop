using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class AccountListForm : Form
{
    private readonly AccountService _accountService;
    private readonly ProductService _productService;
    private readonly SalesService   _salesService;
    private readonly CustomerService  _customerService;
    private readonly CardService    _cardService;
    private readonly UserContext    _currentUser;
    private readonly AppSettings    _appSettings;
    private readonly bool          _expiringModeLocked;
    private readonly bool          _expiringOnly;
    private readonly string?       _initialStatus;

    private TextBox        _txtEmail = null!;
    private ComboBox       _cboStatus = null!;
    private ComboBox       _cboProduct = null!;
    private CheckBox       _chkUseDate = null!;
    private ComboBox       _cboDateField = null!;
    private DateTimePicker _dtFrom = null!;
    private DateTimePicker _dtTo = null!;
    private Button         _btnSearch = null!;
    private Button         _btnReset = null!;
    private Button         _btnExportCsv = null!;
    private Button         _btnMore = null!;

    private DataGridView   _grid = null!;
    private Button         _btnNew = null!;
    private Button         _btnEdit = null!;
    private Button         _btnDetail = null!;
    private Button         _btnReuse = null!;
    private Button         _btnExtend = null!;
    private Button         _btnCancel = null!;
    private Button         _btnClose = null!;

    private ContextMenuStrip _ctxRowMenu = null!;
    private ContextMenuStrip _ctxMoreMenu = null!;

    private List<Product>  _products = new();
    private List<Customer> _customers = new();
    private List<Card>     _cards = new();
    private List<Account>  _currentAccounts = new();
    private bool          _expiringFilterOn = false;
    private Label         _lblExpiringNotice = null!;

    private class AccountRow
    {
        public long     AccountId   { get; set; }
        public string   Email       { get; set; } = "";
        public string   Product     { get; set; } = "";
        public string   CardName    { get; set; } = "";
        public string   Status      { get; set; } = "";
        public string   StatusDisplay { get; set; } = "";
        public DateTime StartDate   { get; set; }
        public DateTime? EndDate     { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public long?    CustomerId  { get; set; }
        public long?    OrderId     { get; set; }
        public string?  OrderCode   { get; set; }
        public string?  Memo        { get; set; }
    }

    public AccountListForm(
        AccountService  accountService,
        ProductService  productService,
        SalesService    salesService,
        CustomerService customerService,
        CardService     cardService,
        UserContext     currentUser,
        AppSettings     appSettings,
        bool            expiringOnly = false)
        : this(accountService, productService, salesService, customerService, cardService, currentUser, appSettings, expiringOnly, null)
    {
    }

    public AccountListForm(
        AccountService  accountService,
        ProductService  productService,
        SalesService    salesService,
        CustomerService customerService,
        CardService     cardService,
        UserContext     currentUser,
        AppSettings     appSettings,
        bool            expiringOnly,
        string?         initialStatus)
    {
        _accountService    = accountService;
        _productService    = productService;
        _salesService      = salesService;
        _customerService   = customerService;
        _cardService       = cardService;
        _currentUser       = currentUser;
        _appSettings       = appSettings;
        _expiringModeLocked = expiringOnly;
        _expiringOnly      = expiringOnly;
        _expiringFilterOn  = expiringOnly;
        _initialStatus     = initialStatus;

        Text = _expiringOnly ? "만료 예정 계정 목록" : "계정 목록";
        Width = 1100;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        LoadProducts();
        LoadCards();
        ReloadData();
    }

    // ─────────────────────────────────────────────────────────────
    //  UI 초기화
    // ─────────────────────────────────────────────────────────────
    private void InitializeControls()
    {
        // 검색 영역
        var lblEmail = new Label
        {
            Text = "이메일",
            Left = 10,
            Top = 15,
            Width = 60
        };
        _txtEmail = new TextBox
        {
            Left = lblEmail.Right + 5,
            Top = 10,
            Width = 180
        };

        var lblStatus = new Label
        {
            Text = "상태",
            Left = _txtEmail.Right + 20,
            Top = 15,
            Width = 40
        };
        _cboStatus = new ComboBox
        {
            Left = lblStatus.Right + 5,
            Top = 10,
            Width = 130,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboStatus.DisplayMember = "Display";
        _cboStatus.ValueMember   = "Code";
        _cboStatus.DataSource    = AccountStatusHelper.GetAllWithEmpty().Select(x => new { x.Code, x.Display }).ToList();
        if (!string.IsNullOrEmpty(_initialStatus) && _cboStatus.Items.Count > 0)
        {
            var found = AccountStatusHelper
                .GetAllWithEmpty()
                .Any(x => string.Equals(x.Code, _initialStatus, StringComparison.OrdinalIgnoreCase));

            if (found)
            {
                _cboStatus.SelectedValue = _initialStatus;
            }
            else
            {
                _cboStatus.SelectedIndex = 0;
            }
        }
        else if (_cboStatus.Items.Count > 0)
        {
            _cboStatus.SelectedIndex = 0;
        }

        var lblProduct = new Label
        {
            Text = "상품",
            Left = _cboStatus.Right + 20,
            Top = 15,
            Width = 40
        };
        _cboProduct = new ComboBox
        {
            Left = lblProduct.Right + 5,
            Top = 10,
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _chkUseDate = new CheckBox
        {
            Text = "기간",
            Left = 10,
            Top = 45,
            Width = 50
        };

        _cboDateField = new ComboBox
        {
            Left = _chkUseDate.Right + 5,
            Top = 42,
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboDateField.Items.Add("구독 만료일");
        _cboDateField.Items.Add("구독 시작일");
        _cboDateField.Items.Add("납품일");
        _cboDateField.SelectedIndex = 0;

        _dtFrom = new DateTimePicker
        {
            Left = _cboDateField.Right + 10,
            Top = 42,
            Width = 120,
            Format = DateTimePickerFormat.Short
        };

        var lblWave = new Label
        {
            Text = "~",
            Left = _dtFrom.Right + 5,
            Top = 46,
            Width = 20
        };

        _dtTo = new DateTimePicker
        {
            Left = lblWave.Right + 5,
            Top = 42,
            Width = 120,
            Format = DateTimePickerFormat.Short
        };

        _btnSearch = new Button
        {
            Text = "조회",
            Left = _dtTo.Right + 15,
            Top = 40,
            Width = 80
        };
        _btnSearch.Click += (_, _) => ReloadData();

        _btnReset = new Button
        {
            Text = "초기화",
            Left = _btnSearch.Right + 10,
            Top = 40,
            Width = 80
        };
        _btnReset.Click += (_, _) => ResetFilters();

        _btnExportCsv = new Button
        {
            Text = "엑셀 다운로드(계정 CSV Export)",
            Left = _btnReset.Right + 10,
            Top = 40,
            Width = 190
        };
        _btnExportCsv.Click += (_, _) => ExportAccountsCsv();

        _btnMore = new Button
        {
            Text = "더보기 ▼",
            Left = _btnExportCsv.Right + 10,
            Top = 40,
            Width = 90
        };
        _btnMore.Click += (_, _) => ShowMoreMenu();

        _lblExpiringNotice = new Label
        {
            AutoSize = true,
            Left = 10,
            Top = 60,
            Text = $"※ 오늘 기준 {GetExpiringDays()}일 이내 만료 예정인 계정만 표시합니다.",
            Visible = _expiringOnly
        };

        // 그리드
        _grid = new DataGridView
        {
            Left = 10,
            Top = 90,
            Width = ClientSize.Width - 20,
            Height = ClientSize.Height - 140,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,  // 여러 계정 선택 가능 (납품용 CSV 등)
            AutoGenerateColumns = false
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "ID",
            DataPropertyName = "AccountId",
            Width = 60
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "이메일",
            DataPropertyName = "Email",
            Width = 240
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상품",
            DataPropertyName = "Product",
            Width = 260
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "결제 카드",
            DataPropertyName = "CardName",
            Width = 180
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상태",
            DataPropertyName = "StatusDisplay",
            Width = 100
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "시작일",
            DataPropertyName = "StartDate",
            Width = 90,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "만료일",
            DataPropertyName = "EndDate",
            Width = 90,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "납품일",
            DataPropertyName = "DeliveryDate",
            Width = 90,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "CustomerId",
            DataPropertyName = "CustomerId",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "OrderId",
            DataPropertyName = "OrderId",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "주문/견적 번호",
            DataPropertyName = "OrderCode",
            Width = 120
        });

        _grid.DoubleClick += (_, _) => ShowDetail();
        _grid.KeyDown += GridOnKeyDown;

        // 하단 버튼 (심플하게 3개만)
        _btnNew = new Button
        {
            Text = "신규",
            Left = 10,
            Top = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnNew.Click += (_, _) => CreateNew();

        _btnEdit = new Button
        {
            Text = "수정",
            Left = _btnNew.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnEdit.Click += (_, _) => EditSelected();

        _btnDetail = new Button
        {
            Text = "상세 보기",
            Left = _btnEdit.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 100,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnDetail.Click += (_, _) => ShowDetail();

        _btnReuse = new Button
        {
            Text = "계정 재사용...",
            Left = _btnDetail.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 110,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnReuse.Click += (_, _) => ReuseSelectedAccount();

        _btnExtend = new Button
        {
            Text = "구독 기간 연장...",
            Left = _btnReuse.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 130,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnExtend.Click += (_, _) => ExtendSubscriptionSelected();

        _btnCancel = new Button
        {
            Text = _expiringOnly ? "만료 예정 구독 취소" : "구독 취소",
            Left = _btnExtend.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 120,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnCancel.Click += (_, _) => CancelSelectedAccounts();

        _btnClose = new Button
        {
            Text = "닫기",
            Left = ClientSize.Width - 100,
            Top = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnClose.Click += (_, _) => Close();

        // 컨텍스트 메뉴 (그리드 우클릭)
        _ctxRowMenu = new ContextMenuStrip();
        _ctxRowMenu.Items.Add("계정 상세", null, (_, _) => ShowDetail());
        _ctxRowMenu.Items.Add("계정 수정(&E)", null, (_, _) => EditSelected());
        _ctxRowMenu.Items.Add("계정 삭제(비활성화)", null, (_, _) => DeleteSelected());
        _ctxRowMenu.Items.Add(new ToolStripSeparator());
        _ctxRowMenu.Items.Add("해당 주문/견적 열기(&O)", null, (_, _) => OpenOrderForSelectedAccount());
        _ctxRowMenu.Items.Add(new ToolStripSeparator());
        _ctxRowMenu.Items.Add("납품 처리", null, (_, _) => DeliverSelected());
        _ctxRowMenu.Items.Add(_expiringOnly ? "만료 예정 구독 취소" : "구독 취소", null, (_, _) => CancelSelectedAccounts());
        _ctxRowMenu.Items.Add("구독 기간 연장...", null, (_, _) => ExtendSubscriptionSelected());
        _ctxRowMenu.Items.Add("계정 재사용...", null, (_, _) => ReuseSelectedAccount());
        _ctxRowMenu.Items.Add("재사용 준비", null, (_, _) => ResetReadySelected());
        _ctxRowMenu.Items.Add(new ToolStripSeparator());
        _ctxRowMenu.Items.Add("결제 카드 연결/변경...", null, (_, _) => AssignCardToSelected());
        _ctxRowMenu.Items.Add("결제 카드 해제", null, (_, _) => UnsetCardForSelected());
        _ctxRowMenu.Items.Add(new ToolStripSeparator());
        _ctxRowMenu.Items.Add("선택 계정만 엑셀 다운로드", null, (_, _) => ExportAccountsCsv(true));
        _ctxRowMenu.Items.Add("선택 계정 납품용 엑셀", null, (_, _) => ExportDeliveryCsv());

        _grid.ContextMenuStrip = _ctxRowMenu;

        // "더보기" 메뉴
        _ctxMoreMenu = new ContextMenuStrip();
        _ctxMoreMenu.Items.Add($"만료 예정({GetExpiringDays()}일) 보기", null, (_, _) => ShowExpiring());
        _ctxMoreMenu.Items.Add("엑셀 다운로드(계정 CSV Export)", null, (_, _) => ExportAccountsCsv());
        _ctxMoreMenu.Items.Add("엑셀 업로드(계정 CSV Import)", null, (_, _) => ImportAccountsCsv());

        Controls.Add(lblEmail);
        Controls.Add(_txtEmail);
        Controls.Add(lblStatus);
        Controls.Add(_cboStatus);
        Controls.Add(lblProduct);
        Controls.Add(_cboProduct);
        Controls.Add(_chkUseDate);
        Controls.Add(_cboDateField);
        Controls.Add(_dtFrom);
        Controls.Add(lblWave);
        Controls.Add(_dtTo);
        Controls.Add(_btnSearch);
        Controls.Add(_btnReset);
        Controls.Add(_btnExportCsv);
        Controls.Add(_btnMore);
        Controls.Add(_lblExpiringNotice);
        Controls.Add(_grid);
        Controls.Add(_btnNew);
        Controls.Add(_btnEdit);
        Controls.Add(_btnDetail);
        Controls.Add(_btnReuse);
        Controls.Add(_btnExtend);
        Controls.Add(_btnCancel);
        Controls.Add(_btnClose);
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
            EditSelected();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Delete)
        {
            DeleteSelected();
            e.Handled = true;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  데이터 로딩/필터
    // ─────────────────────────────────────────────────────────────
    private void LoadProducts()
    {
        _products = _productService.GetAll();
        _customers = _customerService.GetAll();

        _cboProduct.Items.Clear();
        _cboProduct.Items.Add(""); // 전체

        foreach (var p in _products)
        {
            _cboProduct.Items.Add($"{p.ProductName} / {p.PlanName}");
        }

        _cboProduct.SelectedIndex = 0;
    }

    private void LoadCards()
    {
        _cards = _cardService.GetAll();
    }

    private void ResetFilters()
    {
        _txtEmail.Text = "";
        if (!string.IsNullOrEmpty(_initialStatus) && _cboStatus.Items.Count > 0)
        {
            var found = AccountStatusHelper
                .GetAllWithEmpty()
                .Any(x => string.Equals(x.Code, _initialStatus, StringComparison.OrdinalIgnoreCase));

            if (found)
            {
                _cboStatus.SelectedValue = _initialStatus;
            }
            else
            {
                _cboStatus.SelectedIndex = 0;
            }
        }
        else
        {
            _cboStatus.SelectedIndex = 0;
        }
        _cboProduct.SelectedIndex = 0;
        _chkUseDate.Checked = false;
        _dtFrom.Value = DateTime.Today;
        _dtTo.Value = DateTime.Today;
        _expiringFilterOn = _expiringModeLocked;
        _lblExpiringNotice.Visible = _expiringOnly;
        ReloadData();
    }

    private void ReloadData()
    {
        LoadCards();
        _currentAccounts = _expiringOnly
            ? _accountService.GetExpiring(DateTime.Today, GetExpiringDays())
            : _accountService.GetAll();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<Account> query = _currentAccounts;

        if (!string.IsNullOrEmpty(_initialStatus))
        {
            query = query.Where(a => string.Equals(a.Status, _initialStatus, StringComparison.OrdinalIgnoreCase));
        }

        if (_expiringFilterOn)
        {
            var today = DateTime.Today;
            var limit = today.AddDays(GetExpiringDays());

            query = query
                .Where(a =>
                {
                    var endDate = a.SubscriptionEndDate.Date;
                    return endDate >= today && endDate <= limit;
                })
                .Where(a => a.Status != AccountStatus.Canceled && a.Status != AccountStatus.ResetReady);
        }

        var email = _txtEmail.Text.Trim();
        if (!string.IsNullOrEmpty(email))
        {
            query = query.Where(a =>
                a.Email.Contains(email, StringComparison.OrdinalIgnoreCase));
        }

        if (_cboStatus.SelectedIndex > 0)
        {
            var st = (string)_cboStatus.SelectedValue!;
            query = query.Where(a => a.Status == st);
        }

        if (_cboProduct.SelectedIndex > 0)
        {
            var index = _cboProduct.SelectedIndex;
            // 첫 번째는 "", 그 뒤부터 _products 순서
            var product = _products[index - 1];
            query = query.Where(a => a.ProductId == product.ProductId);
        }

        if (_chkUseDate.Checked)
        {
            var from = _dtFrom.Value.Date;
            var to   = _dtTo.Value.Date;

            switch (_cboDateField.SelectedIndex)
            {
                case 0: // 만료일
                    query = query.Where(a =>
                        a.SubscriptionEndDate.Date >= from &&
                        a.SubscriptionEndDate.Date <= to);
                    break;
                case 1: // 시작일
                    query = query.Where(a =>
                        a.SubscriptionStartDate.Date >= from &&
                        a.SubscriptionStartDate.Date <= to);
                    break;
                case 2: // 납품일
                    query = query.Where(a =>
                        a.DeliveryDate is DateTime delivery &&
                        delivery.Date >= from &&
                        delivery.Date <= to);
                    break;
            }
        }

        var ordered = query.OrderBy(a => a.SubscriptionEndDate);
        if (!_expiringFilterOn)
        {
            ordered = ordered.ThenBy(a => a.AccountId);
        }

        var cardMap = _cards.ToDictionary(c => c.CardId, GetCardDisplay);

        var list = ordered
            .Select(a =>
            {
                var product = _products.FirstOrDefault(p => p.ProductId == a.ProductId);
                var productName = product == null
                    ? $"#{a.ProductId}"
                    : $"{product.ProductName} / {product.PlanName}";

                return new AccountRow
                {
                    AccountId    = a.AccountId,
                    Email        = a.Email,
                    Product      = productName,
                    CardName     = a.CardId.HasValue && cardMap.TryGetValue(a.CardId.Value, out var cardName)
                        ? cardName
                        : "",
                    Status       = a.Status,
                    StatusDisplay = AccountStatusHelper.ToDisplay(a.Status),
                    StartDate    = a.SubscriptionStartDate,
                    EndDate      = a.SubscriptionEndDate,
                    DeliveryDate = a.DeliveryDate,
                    CustomerId   = a.CustomerId,
                    OrderId      = a.OrderId,
                    OrderCode    = a.OrderId?.ToString(),
                    Memo         = a.Memo
                };
            })
            .ToList();

        _grid.DataSource = list;
    }

    private static string GetCardDisplay(Card card)
    {
        var company = string.IsNullOrWhiteSpace(card.CardCompany) ? "" : $" ({card.CardCompany})";
        var last4 = string.IsNullOrWhiteSpace(card.Last4Digits) ? "" : $" - {card.Last4Digits}";
        return $"{card.CardName}{company}{last4}";
    }

    private Account? GetSelectedAccount()
    {
        if (_grid.CurrentRow?.DataBoundItem is not AccountRow row)
            return null;

        return _currentAccounts.FirstOrDefault(a => a.AccountId == row.AccountId);
    }

    private List<Account> GetSelectedAccounts()
    {
        if (_grid.SelectedRows.Count == 0)
            return new List<Account>();

        var ids = _grid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => ((AccountRow)r.DataBoundItem).AccountId)
            .ToHashSet();

        return _currentAccounts
            .Where(a => ids.Contains(a.AccountId))
            .ToList();
    }

    // ─────────────────────────────────────────────────────────────
    //  CRUD / 상태 변경 액션
    // ─────────────────────────────────────────────────────────────
    private void CreateNew()
    {
        using var dlg = new AccountEditForm(_accountService, _productService, _customerService, _currentUser, null);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            ReloadData();
        }
    }

    private void EditSelected()
    {
        var acc = GetSelectedAccount();
        if (acc == null) return;

        using var dlg = new AccountEditForm(_accountService, _productService, _customerService, _currentUser, acc.AccountId);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            ReloadData();
        }
    }

    private void ShowDetail()
    {
        var acc = GetSelectedAccount();
        if (acc == null) return;

        using var dlg = new AccountDetailForm(_accountService, _productService, _customerService, acc.AccountId, _currentUser);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            ReloadData();
        }
    }

    private void OpenOrderForSelectedAccount()
    {
        if (_grid.CurrentRow?.DataBoundItem is not AccountRow row)
        {
            MessageBox.Show("계정을 선택하세요.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (row.OrderId == null)
        {
            MessageBox.Show("연결된 주문/견적 정보가 없습니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var sale = _salesService.GetSale(row.OrderId.Value);
        if (sale == null)
        {
            MessageBox.Show("해당 주문/견적을 찾을 수 없습니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaleDetailForm(_salesService, _accountService, _currentUser, sale.SaleId);
        dlg.ShowDialog(this);
    }

    private void DeleteSelected()
    {
        var acc = GetSelectedAccount();
        if (acc == null) return;

        var result = MessageBox.Show(
            $"계정 [{acc.Email}] 을(를) 삭제(비활성) 하시겠습니까?",
            "확인",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        _accountService.SoftDelete(acc.AccountId, _currentUser);
        ReloadData();
    }

    private void DeliverSelected()
    {
        var acc = GetSelectedAccount();
        if (acc == null) return;

        try
        {
            _accountService.Deliver(acc.AccountId, DateTime.Today, _currentUser);
            ReloadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReuseSelectedAccount()
    {
        var acc = GetSelectedAccount();
        if (acc == null)
        {
            MessageBox.Show("재사용할 계정을 선택하세요.");
            return;
        }

        if (!string.Equals(acc.Status, AccountStatus.ResetReady, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("재사용 가능한 상태(RESET_READY)의 계정만 재사용할 수 있습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new AccountReuseForm(_customerService, _productService);
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _accountService.ReuseAccount(
                acc.AccountId,
                dlg.SelectedCustomerId,
                dlg.SelectedProductId,
                dlg.StartDate,
                dlg.EndDate,
                dlg.SelectedOrderId,
                dlg.DeliveryDate,
                _currentUser);

            MessageBox.Show("계정 재사용 처리 완료.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            ReloadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"계정 재사용 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExtendSubscriptionSelected()
    {
        var selectedRows = _grid.SelectedRows;
        if (selectedRows.Count == 0)
        {
            MessageBox.Show("연장할 계정을 선택하세요.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var ids = new List<long>();
        foreach (DataGridViewRow row in selectedRows)
        {
            if (row.DataBoundItem is AccountRow ar)
                ids.Add(ar.AccountId);
        }

        using var dlg = new ExtendSubscriptionForm(ids.Count);
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        var months = dlg.Months;
        if (months <= 0)
            return;

        try
        {
            _accountService.ExtendSubscription(ids, months, _currentUser);
            ReloadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"구독 기간 연장 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CancelSelectedAccounts()
    {
        var selected = GetSelectedAccounts();
        if (selected.Count == 0)
        {
            MessageBox.Show("구독 취소할 계정을 선택하세요.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var cancellable = selected
            .Where(a => a.Status != AccountStatus.Canceled && a.Status != AccountStatus.ResetReady)
            .ToList();

        if (cancellable.Count == 0)
        {
            MessageBox.Show("선택된 계정은 이미 취소되었거나 재사용 준비 상태입니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(
            $"선택한 {cancellable.Count}개 계정의 구독을 취소(CANCELED) 상태로 변경하시겠습니까?",
            "구독 취소 확인",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        var errors = new List<string>();

        foreach (var acc in cancellable)
        {
            try
            {
                _accountService.CancelSubscription(acc.AccountId, _currentUser);
            }
            catch (Exception ex)
            {
                errors.Add($"{acc.Email}: {ex.Message}");
            }
        }

        ReloadData();

        if (errors.Count > 0)
        {
            MessageBox.Show("일부 계정 처리 중 오류:\n" + string.Join("\n", errors.Take(5)), "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            MessageBox.Show("구독 취소가 완료되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ResetReadySelected()
    {
        var acc = GetSelectedAccount();
        if (acc == null) return;

        var result = MessageBox.Show(
            $"계정 [{acc.Email}] 을(를) 재사용 준비 상태로 전환하시겠습니까?",
            "재사용 준비 확인",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        try
        {
            _accountService.MarkResetReady(acc.AccountId, _currentUser);
            ReloadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AssignCardToSelected()
    {
        var selected = GetSelectedAccounts();
        if (selected.Count == 0)
        {
            MessageBox.Show("결제 카드를 연결할 계정을 선택하세요.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        LoadCards();
        if (_cards.Count == 0)
        {
            MessageBox.Show("등록된 카드가 없습니다. 먼저 카드 정보를 등록하세요.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var initialCardId = GetCommonCardId(selected);
        var selectedCard = ShowCardSelectionDialog(initialCardId);
        if (selectedCard == null)
            return;

        try
        {
            _accountService.UpdateCardAssignments(selected.Select(a => a.AccountId), selectedCard.CardId, _currentUser);
            ReloadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"카드 연결 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UnsetCardForSelected()
    {
        var selected = GetSelectedAccounts();
        if (selected.Count == 0)
        {
            MessageBox.Show("카드를 해제할 계정을 선택하세요.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(
            $"선택한 {selected.Count}개 계정의 카드 연결을 해제하시겠습니까?",
            "카드 해제 확인",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        try
        {
            _accountService.UpdateCardAssignments(selected.Select(a => a.AccountId), null, _currentUser);
            ReloadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"카드 해제 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private long? GetCommonCardId(List<Account> accounts)
    {
        if (accounts.Count == 0) return null;
        var first = accounts[0].CardId;
        return accounts.All(a => a.CardId == first) ? first : null;
    }

    private Card? ShowCardSelectionDialog(long? initialCardId)
    {
        using var dlg = new Form
        {
            Text = "결제 카드 선택",
            Width = 360,
            Height = 160,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lbl = new Label
        {
            Text = "카드",
            Left = 10,
            Top = 20,
            Width = 60
        };

        var options = _cards
            .Select(c => new CardOption
            {
                CardId = c.CardId,
                Display = GetCardDisplay(c)
            })
            .ToList();

        var cbo = new ComboBox
        {
            Left = lbl.Right + 5,
            Top = 16,
            Width = 240,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DataSource = options,
            DisplayMember = nameof(CardOption.Display),
            ValueMember = nameof(CardOption.CardId)
        };

        if (cbo.Items.Count == 0)
        {
            MessageBox.Show("등록된 카드가 없습니다. 먼저 카드 정보를 등록하세요.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        TrySetComboSelection(cbo, initialCardId);

        var btnOk = new Button
        {
            Text = "확인",
            Left = cbo.Left,
            Top = cbo.Bottom + 15,
            Width = 80,
            DialogResult = DialogResult.OK
        };

        var btnCancel = new Button
        {
            Text = "취소",
            Left = btnOk.Right + 10,
            Top = btnOk.Top,
            Width = 80,
            DialogResult = DialogResult.Cancel
        };

        dlg.Controls.Add(lbl);
        dlg.Controls.Add(cbo);
        dlg.Controls.Add(btnOk);
        dlg.Controls.Add(btnCancel);
        dlg.AcceptButton = btnOk;
        dlg.CancelButton = btnCancel;

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return null;

        var selectedId = (long)cbo.SelectedValue!;
        return _cards.FirstOrDefault(c => c.CardId == selectedId);
    }

    private static void TrySetComboSelection(ComboBox combo, long? initialCardId)
    {
        if (combo.Items.Count == 0)
            return;

        if (initialCardId.HasValue)
        {
            combo.SelectedValue = initialCardId.Value;
            if (combo.SelectedIndex >= 0)
                return;
        }

        combo.SelectedIndex = 0;
    }

    private class CardOption
    {
        public long   CardId { get; set; }
        public string Display { get; set; } = "";
    }

    private int GetExpiringDays()
    {
        return _appSettings.ExpiringDays > 0 ? _appSettings.ExpiringDays : 30;
    }

    private void ShowExpiring()
    {
        _expiringFilterOn = true;
        ReloadData();
    }

    // ─────────────────────────────────────────────────────────────
    //  CSV 유틸 & Export / Import
    // ─────────────────────────────────────────────────────────────
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

    private void ExportAccountsCsv(bool onlySelected = false)
    {
        List<AccountRow> targetRows;

        if (onlySelected && _grid.SelectedRows.Count > 0)
        {
            targetRows = new List<AccountRow>();
            foreach (DataGridViewRow r in _grid.SelectedRows)
            {
                if (r.DataBoundItem is AccountRow ar)
                    targetRows.Add(ar);
            }
        }
        else
        {
            if (_grid.DataSource is not List<AccountRow> rows || rows.Count == 0)
            {
                MessageBox.Show("내보낼 계정 데이터가 없습니다.", "안내",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            targetRows = rows;
        }

        if (targetRows.Count == 0)
        {
            MessageBox.Show("내보낼 계정 데이터가 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var productDict = _products.ToDictionary(p => p.ProductId, p => p);
        var customerDict = _customers.ToDictionary(c => c.CustomerId, c => c);

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*",
            FileName = $"accounts_{DateTime.Now:yyyyMMddHHmm}.csv",
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
                "Email",
                "ProductCode",
                "CustomerName",
                "SubscriptionStartDate",
                "SubscriptionEndDate",
                "Status",
                "OrderCode",
                "DeliveryDate",
                "Memo"));

            foreach (var row in targetRows)
            {
                var acc = _currentAccounts.FirstOrDefault(a => a.AccountId == row.AccountId);
                if (acc == null) continue;

                var productCode = productDict.TryGetValue(acc.ProductId, out var product)
                    ? product.ProductCode
                    : acc.ProductId.ToString(CultureInfo.InvariantCulture);

                var customerName = acc.CustomerId.HasValue &&
                                   customerDict.TryGetValue(acc.CustomerId.Value, out var customer)
                    ? customer.CustomerName
                    : "";

                var line = string.Join(",",
                    EscapeCsv(acc.Email),
                    EscapeCsv(productCode),
                    EscapeCsv(customerName),
                    EscapeCsv(acc.SubscriptionStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    EscapeCsv(acc.SubscriptionEndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    EscapeCsv(acc.Status),
                    EscapeCsv(acc.OrderId?.ToString(CultureInfo.InvariantCulture) ?? ""),
                    EscapeCsv(acc.DeliveryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? ""),
                    EscapeCsv(acc.Memo ?? ""));

                writer.WriteLine(line);
            }

            MessageBox.Show("계정 목록 CSV 저장이 완료되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"CSV 저장 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportAccountsCsv()
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
        var errors = new List<string>();

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
            if (headerCols.Count == 0 || !headerCols[0].Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("헤더 형식이 예상과 다릅니다. (첫 컬럼은 Email이어야 합니다.)", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int lineNo = 1;

            var products = _productService.GetAll();
            var productDict = products.ToDictionary(p => p.ProductCode, StringComparer.OrdinalIgnoreCase);
            var customers = _customerService.GetAll();
            var customerDict = customers.ToDictionary(c => c.CustomerName, StringComparer.OrdinalIgnoreCase);
            var accountDict = _accountService
                .GetAll()
                .ToDictionary(a => a.Email, StringComparer.OrdinalIgnoreCase);

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                lineNo++;
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var cols = SplitCsvLine(line);

                string email       = cols.ElementAtOrDefault(0)?.Trim() ?? "";
                string productCode = cols.ElementAtOrDefault(1)?.Trim() ?? "";
                string customerNm  = cols.ElementAtOrDefault(2)?.Trim() ?? "";
                string startStr    = cols.ElementAtOrDefault(3)?.Trim() ?? "";
                string endStr      = cols.ElementAtOrDefault(4)?.Trim() ?? "";
                string statusStr   = cols.ElementAtOrDefault(5)?.Trim() ?? "";
                string orderCode   = cols.ElementAtOrDefault(6)?.Trim() ?? "";
                string deliveryStr = cols.ElementAtOrDefault(7)?.Trim() ?? "";
                string memo        = cols.ElementAtOrDefault(8) ?? "";

                if (string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(productCode) ||
                    string.IsNullOrWhiteSpace(customerNm) ||
                    string.IsNullOrWhiteSpace(startStr) ||
                    string.IsNullOrWhiteSpace(endStr))
                {
                    errors.Add($"라인 {lineNo}: 필수 값(Email, ProductCode, CustomerName, 시작/만료일) 중 일부가 비어 있습니다.");
                    skipped++;
                    continue;
                }

                if (!productDict.TryGetValue(productCode, out var product))
                {
                    errors.Add($"라인 {lineNo}: 알 수 없는 ProductCode [{productCode}].");
                    skipped++;
                    continue;
                }

                if (!customerDict.TryGetValue(customerNm, out var customer))
                {
                    errors.Add($"라인 {lineNo}: 알 수 없는 CustomerName [{customerNm}].");
                    skipped++;
                    continue;
                }

                if (!DateTime.TryParse(startStr, out var startDate))
                {
                    errors.Add($"라인 {lineNo}: 시작일 형식이 잘못되었습니다. (값: {startStr})");
                    skipped++;
                    continue;
                }

                if (!DateTime.TryParse(endStr, out var endDate))
                {
                    errors.Add($"라인 {lineNo}: 만료일 형식이 잘못되었습니다. (값: {endStr})");
                    skipped++;
                    continue;
                }

                var allowedStatuses = new[]
                {
                    AccountStatus.Created,
                    AccountStatus.SubsActive,
                    AccountStatus.Delivered,
                    AccountStatus.InUse,
                    AccountStatus.Expiring,
                    AccountStatus.Canceled,
                    AccountStatus.ResetReady
                };

                string status = string.IsNullOrWhiteSpace(statusStr)
                    ? AccountStatus.SubsActive
                    : statusStr;

                if (!allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"라인 {lineNo}: 알 수 없는 상태 값 [{statusStr}].");
                    skipped++;
                    continue;
                }

                status = allowedStatuses
                    .First(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));

                DateTime? deliveryDate = null;
                if (!string.IsNullOrWhiteSpace(deliveryStr))
                {
                    if (!DateTime.TryParse(deliveryStr, out var d))
                    {
                        errors.Add($"라인 {lineNo}: DeliveryDate 형식 오류. (값: {deliveryStr})");
                        skipped++;
                        continue;
                    }
                    deliveryDate = d;
                }

                long? orderId = null;
                var finalMemo = memo;
                if (!string.IsNullOrWhiteSpace(orderCode))
                {
                    if (long.TryParse(orderCode, out var oid))
                    {
                        orderId = oid;
                    }
                    else
                    {
                        finalMemo = string.IsNullOrWhiteSpace(finalMemo)
                            ? $"OrderCode:{orderCode}"
                            : $"{finalMemo} (OrderCode:{orderCode})";
                    }
                }

                try
                {
                    if (!accountDict.TryGetValue(email, out var existing))
                    {
                        var acc = new Account
                        {
                            Email                 = email,
                            ProductId             = product.ProductId,
                            CustomerId            = customer.CustomerId,
                            SubscriptionStartDate = startDate.Date,
                            SubscriptionEndDate   = endDate.Date,
                            Status                = status,
                            OrderId               = orderId,
                            DeliveryDate          = deliveryDate?.Date,
                            Memo                  = finalMemo
                        };

                        var newId = _accountService.Create(acc, _currentUser);
                        acc.AccountId = newId;
                        accountDict[email] = acc;
                        created++;
                    }
                    else
                    {
                        existing.ProductId             = product.ProductId;
                        existing.CustomerId            = customer.CustomerId;
                        existing.SubscriptionStartDate = startDate.Date;
                        existing.SubscriptionEndDate   = endDate.Date;
                        existing.Status                = status;
                        existing.OrderId               = orderId;
                        existing.DeliveryDate          = deliveryDate?.Date;
                        existing.Memo                  = finalMemo;

                        _accountService.Update(existing, _currentUser);
                        updated++;
                    }
                }
                catch (Exception exRow)
                {
                    errors.Add($"라인 {lineNo}: 저장 중 오류 - {exRow.Message}");
                    skipped++;
                }
            }

            ReloadData();

            var msg = $"계정 Import 완료:\n" +
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

    private void ExportDeliveryCsv()
    {
        List<AccountRow> targetRows;

        if (_grid.SelectedRows.Count > 0)
        {
            targetRows = new List<AccountRow>();
            foreach (DataGridViewRow gridRow in _grid.SelectedRows)
            {
                if (gridRow.DataBoundItem is AccountRow row)
                    targetRows.Add(row);
            }
        }
        else
        {
            if (_grid.DataSource is not List<AccountRow> all || all.Count == 0)
            {
                MessageBox.Show("납품용으로 내보낼 계정이 없습니다.", "안내",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            targetRows = all;
        }

        if (targetRows.Count == 0)
        {
            MessageBox.Show("납품용으로 내보낼 계정이 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*",
            FileName = $"delivery_accounts_{DateTime.Now:yyyyMMddHHmm}.csv",
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
                "Email",
                "ProductNamePlan",
                "StartDate",
                "EndDate",
                "CustomerId",
                "OrderId"));

            foreach (var row in targetRows)
            {
                var acc = _currentAccounts.FirstOrDefault(a => a.AccountId == row.AccountId);
                if (acc == null) continue;

                var product = _products.FirstOrDefault(p => p.ProductId == acc.ProductId);
                var productNamePlan = product == null
                    ? $"#{acc.ProductId}"
                    : $"{product.ProductName} / {product.PlanName}";

                string startDate = acc.SubscriptionStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string endDate   = acc.SubscriptionEndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                var line = string.Join(",",
                    EscapeCsv(acc.Email),
                    EscapeCsv(productNamePlan),
                    EscapeCsv(startDate),
                    EscapeCsv(endDate),
                    EscapeCsv(acc.CustomerId?.ToString(CultureInfo.InvariantCulture) ?? ""),
                    EscapeCsv(acc.OrderId?.ToString(CultureInfo.InvariantCulture) ?? ""));

                writer.WriteLine(line);
            }

            MessageBox.Show("납품용 CSV 저장이 완료되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"납품용 CSV 저장 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
