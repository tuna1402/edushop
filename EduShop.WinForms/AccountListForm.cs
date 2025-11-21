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
    private const int ExpiringDays = 30;
    private readonly AccountService _accountService;
    private readonly ProductService _productService;
    private readonly CustomerService  _customerService;
    private readonly UserContext    _currentUser;
    private readonly bool          _expiringModeLocked;

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
    private Button         _btnClose = null!;

    private ContextMenuStrip _ctxRowMenu = null!;
    private ContextMenuStrip _ctxMoreMenu = null!;

    private List<Product> _products = new();
    private List<Account> _currentAccounts = new();
    private bool          _expiringOnly;

    private class AccountRow
    {
        public long     AccountId   { get; set; }
        public string   Email       { get; set; } = "";
        public string   Product     { get; set; } = "";
        public string   Status      { get; set; } = "";
        public DateTime StartDate   { get; set; }
        public DateTime EndDate     { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public long?    CustomerId  { get; set; }
        public long?    OrderId     { get; set; }
        public string?  Memo        { get; set; }
    }

    public AccountListForm(
        AccountService accountService, 
        ProductService productService, 
        CustomerService customerService, 
        UserContext currentUser,
        bool expiringOnly = false
        )
    {
        _accountService = accountService;
        _productService = productService;
        _customerService = customerService;
        _currentUser    = currentUser;
        _expiringModeLocked = expiringOnly;
        _expiringOnly = expiringOnly;

        Text = _expiringOnly ? "만료 예정 계정 목록" : "계정 목록";
        Width = 1100;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        LoadProducts();
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
        _cboStatus.Items.Add(""); // 전체
        _cboStatus.Items.Add(AccountStatus.Created);
        _cboStatus.Items.Add(AccountStatus.SubsActive);
        _cboStatus.Items.Add(AccountStatus.Delivered);
        _cboStatus.Items.Add(AccountStatus.InUse);
        _cboStatus.Items.Add(AccountStatus.Expiring);
        _cboStatus.Items.Add(AccountStatus.Canceled);
        _cboStatus.Items.Add(AccountStatus.ResetReady);
        _cboStatus.SelectedIndex = 0;

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
            Text = "엑셀 다운로드",
            Left = _btnReset.Right + 10,
            Top = 40,
            Width = 110
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
            Text = $"※ 오늘 기준 {AppSettingsManager.Current.ExpiringDays}일 이내 만료 예정인 계정만 표시합니다.",
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
            HeaderText = "상태",
            DataPropertyName = "Status",
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

        _grid.DoubleClick += (_, _) => EditSelected();
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
        _ctxRowMenu.Items.Add("계정 수정(&E)", null, (_, _) => EditSelected());
        _ctxRowMenu.Items.Add("계정 삭제(비활성화)", null, (_, _) => DeleteSelected());
        _ctxRowMenu.Items.Add(new ToolStripSeparator());
        _ctxRowMenu.Items.Add("납품 처리", null, (_, _) => DeliverSelected());
        _ctxRowMenu.Items.Add("구독 취소", null, (_, _) => CancelSelected());
        _ctxRowMenu.Items.Add("재사용 준비", null, (_, _) => ResetReadySelected());
        _ctxRowMenu.Items.Add(new ToolStripSeparator());
        _ctxRowMenu.Items.Add("선택 계정 납품용 엑셀", null, (_, _) => ExportDeliveryCsv());

        _grid.ContextMenuStrip = _ctxRowMenu;

        // "더보기" 메뉴
        _ctxMoreMenu = new ContextMenuStrip();
        _ctxMoreMenu.Items.Add($"만료 예정({AppSettingsManager.Current.ExpiringDays}일) 보기", null, (_, _) => ShowExpiring());
        _ctxMoreMenu.Items.Add("엑셀 등록 (Import)", null, (_, _) => ImportAccountsCsv());

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

        _cboProduct.Items.Clear();
        _cboProduct.Items.Add(""); // 전체

        foreach (var p in _products)
        {
            _cboProduct.Items.Add($"{p.ProductName} / {p.PlanName}");
        }

        _cboProduct.SelectedIndex = 0;
    }

    private void ResetFilters()
    {
        _txtEmail.Text = "";
        _cboStatus.SelectedIndex = 0;
        _cboProduct.SelectedIndex = 0;
        _chkUseDate.Checked = false;
        _dtFrom.Value = DateTime.Today;
        _dtTo.Value = DateTime.Today;
        _expiringOnly = _expiringModeLocked;
        ReloadData();
    }

    private void ReloadData()
    {
        _currentAccounts = _expiringOnly
            ? _accountService.GetExpiring(DateTime.Today, ExpiringDays)
            : _accountService.GetAll();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<Account> query = _currentAccounts;

        if (_expiringFilterOn)
        {
            var today = DateTime.Today;
            var limit = today.AddDays(AppSettingsManager.Current.ExpiringDays);

            query = query
                .Where(a => a.SubscriptionEndDate.HasValue)
                .Where(a => a.SubscriptionEndDate.Value.Date >= today &&
                            a.SubscriptionEndDate.Value.Date <= limit)
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
            var st = (string)_cboStatus.SelectedItem!;
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
                        a.DeliveryDate.HasValue &&
                        a.DeliveryDate.Value.Date >= from &&
                        a.DeliveryDate.Value.Date <= to);
                    break;
            }
        }

        var ordered = query.OrderBy(a => a.SubscriptionEndDate);
        if (!_expiringFilterOn)
        {
            ordered = ordered.ThenBy(a => a.AccountId);
        }

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
                    Status       = a.Status,
                    StartDate    = a.SubscriptionStartDate,
                    EndDate      = a.SubscriptionEndDate,
                    DeliveryDate = a.DeliveryDate,
                    CustomerId   = a.CustomerId,
                    OrderId      = a.OrderId,
                    Memo         = a.Memo
                };
            })
            .ToList();

        _grid.DataSource = list;
    }

    private Account? GetSelectedAccount()
    {
        if (_grid.CurrentRow?.DataBoundItem is not AccountRow row)
            return null;

        return _currentAccounts.FirstOrDefault(a => a.AccountId == row.AccountId);
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

    private void CancelSelected()
    {
        var acc = GetSelectedAccount();
        if (acc == null) return;

        var result = MessageBox.Show(
            $"계정 [{acc.Email}] 구독을 취소하시겠습니까?",
            "구독 취소 확인",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        try
        {
            _accountService.CancelSubscription(acc.AccountId, _currentUser);
            ReloadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    private void ShowExpiring()
    {
        _expiringOnly = true;
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

    private void ExportAccountsCsv()
    {
        if (_grid.DataSource is not List<AccountRow> rows || rows.Count == 0)
        {
            MessageBox.Show("내보낼 계정 데이터가 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*",
            FileName = $"accounts_{DateTime.Now:yyyyMMddHHmm}.csv"
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
                "StartDate",
                "EndDate",
                "Status",
                "CustomerId",
                "OrderId",
                "DeliveryDate",
                "LastPaymentDate",
                "Memo"));

            foreach (var row in rows)
            {
                var acc = _currentAccounts.FirstOrDefault(a => a.AccountId == row.AccountId);
                if (acc == null) continue;

                var product = _products.FirstOrDefault(p => p.ProductId == acc.ProductId);
                var productCode = product?.ProductCode ?? acc.ProductId.ToString(CultureInfo.InvariantCulture);

                string startDate = acc.SubscriptionStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string endDate   = acc.SubscriptionEndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string delivery  = acc.DeliveryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
                string lastPay   = acc.LastPaymentDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

                var line = string.Join(",",
                    EscapeCsv(acc.Email),
                    EscapeCsv(productCode),
                    EscapeCsv(startDate),
                    EscapeCsv(endDate),
                    EscapeCsv(acc.Status),
                    EscapeCsv(acc.CustomerId?.ToString(CultureInfo.InvariantCulture) ?? ""),
                    EscapeCsv(acc.OrderId?.ToString(CultureInfo.InvariantCulture) ?? ""),
                    EscapeCsv(delivery),
                    EscapeCsv(lastPay),
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
            if (headerCols.Count < 4 || !headerCols[0].Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("헤더 형식이 예상과 다릅니다. (첫 컬럼은 Email이어야 합니다.)", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int lineNo = 1;

            _products = _productService.GetAll();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                lineNo++;
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var cols = SplitCsvLine(line);
                if (cols.Count < 4)
                {
                    errors.Add($"라인 {lineNo}: 열 개수가 부족합니다.");
                    skipped++;
                    continue;
                }

                string email       = cols[0].Trim();
                string productCode = cols.Count > 1 ? cols[1].Trim() : "";
                string startStr    = cols.Count > 2 ? cols[2].Trim() : "";
                string endStr      = cols.Count > 3 ? cols[3].Trim() : "";
                string statusStr   = cols.Count > 4 ? cols[4].Trim() : "";
                string customerStr = cols.Count > 5 ? cols[5].Trim() : "";
                string orderStr    = cols.Count > 6 ? cols[6].Trim() : "";
                string deliveryStr = cols.Count > 7 ? cols[7].Trim() : "";
                string lastPayStr  = cols.Count > 8 ? cols[8].Trim() : "";
                string memo        = cols.Count > 9 ? cols[9] : "";

                if (string.IsNullOrEmpty(email))
                {
                    errors.Add($"라인 {lineNo}: Email이 비어 있습니다.");
                    skipped++;
                    continue;
                }

                var product = _products.FirstOrDefault(p =>
                    p.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase));

                if (product == null)
                {
                    errors.Add($"라인 {lineNo}: 알 수 없는 ProductCode [{productCode}].");
                    skipped++;
                    continue;
                }

                if (!DateTime.TryParseExact(startStr, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var startDate))
                {
                    errors.Add($"라인 {lineNo}: 시작일 형식이 잘못되었습니다. (값: {startStr})");
                    skipped++;
                    continue;
                }

                if (!DateTime.TryParseExact(endStr, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var endDate))
                {
                    errors.Add($"라인 {lineNo}: 만료일 형식이 잘못되었습니다. (값: {endStr})");
                    skipped++;
                    continue;
                }

                string status;
                if (string.IsNullOrWhiteSpace(statusStr))
                {
                    status = AccountStatus.SubsActive;
                }
                else
                {
                    var allowed = new[]
                    {
                        AccountStatus.Created,
                        AccountStatus.SubsActive,
                        AccountStatus.Delivered,
                        AccountStatus.InUse,
                        AccountStatus.Expiring,
                        AccountStatus.Canceled,
                        AccountStatus.ResetReady
                    };

                    if (!allowed.Contains(statusStr))
                    {
                        errors.Add($"라인 {lineNo}: 알 수 없는 상태 값 [{statusStr}].");
                        skipped++;
                        continue;
                    }

                    status = statusStr;
                }

                long? customerId = null;
                if (!string.IsNullOrEmpty(customerStr))
                {
                    if (!long.TryParse(customerStr, out var cid))
                    {
                        errors.Add($"라인 {lineNo}: CustomerId가 숫자가 아닙니다. (값: {customerStr})");
                        skipped++;
                        continue;
                    }
                    customerId = cid;
                }

                long? orderId = null;
                if (!string.IsNullOrEmpty(orderStr))
                {
                    if (!long.TryParse(orderStr, out var oid))
                    {
                        errors.Add($"라인 {lineNo}: OrderId가 숫자가 아닙니다. (값: {orderStr})");
                        skipped++;
                        continue;
                    }
                    orderId = oid;
                }

                DateTime? deliveryDate = null;
                if (!string.IsNullOrEmpty(deliveryStr))
                {
                    if (!DateTime.TryParseExact(deliveryStr, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var d))
                    {
                        errors.Add($"라인 {lineNo}: DeliveryDate 형식 오류. (값: {deliveryStr})");
                        skipped++;
                        continue;
                    }
                    deliveryDate = d;
                }

                DateTime? lastPayDate = null;
                if (!string.IsNullOrEmpty(lastPayStr))
                {
                    if (!DateTime.TryParseExact(lastPayStr, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var d))
                    {
                        errors.Add($"라인 {lineNo}: LastPaymentDate 형식 오류. (값: {lastPayStr})");
                        skipped++;
                        continue;
                    }
                    lastPayDate = d;
                }

                try
                {
                    var existing = _accountService.GetByEmail(email);

                    if (existing == null)
                    {
                        var acc = new Account
                        {
                            Email                 = email,
                            ProductId             = product.ProductId,
                            SubscriptionStartDate = startDate,
                            SubscriptionEndDate   = endDate,
                            Status                = status,
                            CustomerId            = customerId,
                            OrderId               = orderId,
                            DeliveryDate          = deliveryDate,
                            LastPaymentDate       = lastPayDate,
                            Memo                  = memo
                        };

                        _accountService.Create(acc, _currentUser);
                        created++;
                    }
                    else
                    {
                        existing.ProductId             = product.ProductId;
                        existing.SubscriptionStartDate = startDate;
                        existing.SubscriptionEndDate   = endDate;
                        existing.Status                = status;
                        existing.CustomerId            = customerId;
                        existing.OrderId               = orderId;
                        existing.DeliveryDate          = deliveryDate;
                        existing.LastPaymentDate       = lastPayDate;
                        existing.Memo                  = memo;

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

            var msg = $"Import 완료:\n" +
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
            FileName = $"delivery_accounts_{DateTime.Now:yyyyMMddHHmm}.csv"
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
