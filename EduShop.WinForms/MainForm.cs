using System;
using System.Collections.Generic;
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
    private readonly ProductService _service;
    private readonly SalesService _salesService;
    private readonly UserContext _currentUser = new() { UserId = "admin", UserName = "사장" };
    private TextBox _txtNameFilter = null!;
    private ComboBox _cboStatus = null!;
    private Button _btnSearch = null!;
    private DataGridView _grid = null!;
    private Button _btnNew = null!;
    private Button _btnEdit = null!;
    private Button _btnToggle = null!;
    private Button _btnLogs = null!;
    private Button _btnExport = null!;
    private Button _btnImport = null!;
    private Button _btnQuote = null!;
    private Button _btnSales = null!;
    private Button _btnClose = null!;

    private List<Product> _currentList = new();

    public MainForm(ProductService service, SalesService salesService)
    {
        _service = service;
        _salesService = salesService;

        Text = "EduShop 상품 관리";
        Width = 1000;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;

        InitializeControls();
        LoadProducts();
    }

    private void InitializeControls()
    {
        // 상단 필터
        var lblName = new Label
        {
            Text = "상품명",
            Left = 10,
            Top = 15,
            AutoSize = true
        };
        _txtNameFilter = new TextBox
        {
            Left = lblName.Right + 5,
            Top = 10,
            Width = 200
        };

        var lblStatus = new Label
        {
            Text = "상태",
            Left = _txtNameFilter.Right + 20,
            Top = 15,
            AutoSize = true
        };
        _cboStatus = new ComboBox
        {
            Left = lblStatus.Right + 5,
            Top = 10,
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboStatus.Items.AddRange(new[] { "전체", "판매중", "판매중지" });
        _cboStatus.SelectedIndex = 0;

        _btnSearch = new Button
        {
            Text = "검색",
            Left = _cboStatus.Right + 20,
            Top = 9,
            Width = 80
        };
        _btnSearch.Click += (_, _) => LoadProducts();

        // 그리드
        _grid = new DataGridView
        {
            Left = 10,
            Top = 45,
            Width = ClientSize.Width - 20,
            Height = ClientSize.Height - 100,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
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
            Width = 100
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상품명",
            DataPropertyName = "ProductName",
            Width = 250
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "플랜",
            DataPropertyName = "PlanName",
            Width = 100
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "월(KRW)",
            DataPropertyName = "MonthlyFeeKrw",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "도매가",
            DataPropertyName = "WholesalePrice",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "소매가",
            DataPropertyName = "RetailPrice",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상태",
            DataPropertyName = "Status",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "마진",
            DataPropertyName = "Profit",
            Width = 80
        });

        _grid.CellDoubleClick += (_, _) => EditProduct();

        // 하단 버튼들
        _btnNew = new Button
        {
            Text = "신규",
            Left = 10,
            Top = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnNew.Click += (_, _) => NewProduct();

        _btnEdit = new Button
        {
            Text = "수정",
            Left = _btnNew.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnEdit.Click += (_, _) => EditProduct();

        _btnToggle = new Button
        {
            Text = "판매중/중지",
            Left = _btnEdit.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 100,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnToggle.Click += (_, _) => ToggleStatus();

        _btnLogs = new Button
        {
            Text = "로그",
            Left = _btnToggle.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnLogs.Click += (_, _) => ShowLogs();

        _btnExport = new Button
        {
            Text = "엑셀 내보내기",
            Left = _btnLogs.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 110,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnExport.Click += (_, _) => ExportToCsv();

        _btnImport = new Button
        {
            Text = "엑셀 업로드",
            Left = _btnExport.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 110,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnImport.Click += (_, _) => ImportFromCsv();

        _btnQuote = new Button
        {
            Text = "견적서",
            Left = _btnImport.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnQuote.Click += (_, _) => OpenQuote();

        _btnSales = new Button
        {
            Text = "매출현황",
            Left = _btnQuote.Right + 10,
            Top = ClientSize.Height - 45,
            Width = 90,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnSales.Click += (_, _) => OpenSalesList();

        _btnClose = new Button
        {
            Text = "닫기",
            Width = 80,
            Top = ClientSize.Height - 45,
            Left = ClientSize.Width - 90,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnClose.Click += (_, _) => Close();

        Controls.Add(lblName);
        Controls.Add(_txtNameFilter);
        Controls.Add(lblStatus);
        Controls.Add(_cboStatus);
        Controls.Add(_btnSearch);
        Controls.Add(_grid);
        Controls.Add(_btnNew);
        Controls.Add(_btnEdit);
        Controls.Add(_btnToggle);
        Controls.Add(_btnLogs);
        Controls.Add(_btnExport);
        Controls.Add(_btnImport);
        Controls.Add(_btnQuote);
        Controls.Add(_btnSales);
        Controls.Add(_btnClose);
    }

    private void LoadProducts()
    {
        var all = _service.GetAll();

        var nameFilter = _txtNameFilter.Text?.Trim();
        var statusFilter = _cboStatus.SelectedItem?.ToString();

        IEnumerable<Product> query = all;

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            query = query.Where(p =>
                p.ProductName.Contains(nameFilter, StringComparison.OrdinalIgnoreCase) ||
                p.ProductCode.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (statusFilter == "판매중")
            query = query.Where(p => p.Status == "ACTIVE");
        else if (statusFilter == "판매중지")
            query = query.Where(p => p.Status == "INACTIVE");

        _currentList = query.ToList();
        _grid.DataSource = _currentList;
    }

    private Product? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is Product p)
            return p;
        return null;
    }

    private void NewProduct()
    {
        using var dlg = new ProductDetailForm();
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Product != null)
        {
            try
            {
                _service.Create(dlg.Product, _currentUser);
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        using var dlg = new ProductDetailForm(selected);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Product != null)
        {
            try
            {
                _service.Update(dlg.Product, _currentUser);
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"수정 중 오류: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ToggleStatus()
    {
        var selected = GetSelected();
        if (selected == null)
        {
            MessageBox.Show("상태를 변경할 상품을 선택하세요.");
            return;
        }

        var newStatus = selected.Status == "ACTIVE" ? "INACTIVE" : "ACTIVE";
        var text = newStatus == "ACTIVE" ? "판매중" : "판매중지";

        if (MessageBox.Show(
                $"{selected.ProductName} 을(를) {text} 상태로 변경하시겠습니까?",
                "확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
        {
            try
            {
                _service.ChangeStatus(selected.ProductId, newStatus, _currentUser);
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"상태 변경 중 오류: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ShowLogs()
    {
        var selected = GetSelected();
        if (selected == null)
        {
            MessageBox.Show("로그를 볼 상품을 선택하세요.");
            return;
        }

        using var dlg = new ProductLogForm(_service, selected);
        dlg.ShowDialog(this);
    }

    private void ExportToCsv()
    {
        if (_currentList == null || _currentList.Count == 0)
        {
            MessageBox.Show("내보낼 상품이 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*",
            FileName = $"products_{DateTime.Now:yyyyMMddHHmm}.csv"
        };

        if (sfd.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using var writer = new StreamWriter(sfd.FileName, false, Encoding.UTF8);

            writer.WriteLine("ProductId,ProductCode,ProductName,PlanName,MonthlyFeeUsd,MonthlyFeeKrw,WholesalePrice,RetailPrice,PurchasePrice,Profit,YearlyAvailable,MinMonth,MaxMonth,Status,Remark");

            string Escape(string? s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }

            foreach (var p in _currentList)
            {
                var line = string.Join(",", new[]
                {
                    p.ProductId.ToString(),
                    Escape(p.ProductCode),
                    Escape(p.ProductName),
                    Escape(p.PlanName),
                    p.MonthlyFeeUsd?.ToString() ?? "",
                    p.MonthlyFeeKrw.ToString(),
                    p.WholesalePrice.ToString(),
                    p.RetailPrice.ToString(),
                    p.PurchasePrice.ToString(),
                    p.Profit.ToString(),
                    p.YearlyAvailable ? "Y" : "N",
                    p.MinMonth.ToString(),
                    p.MaxMonth.ToString(),
                    Escape(p.Status),
                    Escape(p.Remark)
                });

                writer.WriteLine(line);
            }

            MessageBox.Show("CSV 내보내기가 완료되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"내보내기 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportFromCsv()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*",
            Title = "상품 CSV 파일 선택"
        };

        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            string[] lines;

            // 엑셀이 열어둔 상태에서도 읽기 위해 FileShare.ReadWrite 사용 + 인코딩 자동 판별
            using (var fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] bom = new byte[3];
                int read = fs.Read(bom, 0, 3);

                bool isUtf8Bom = read == 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF;

                fs.Position = 0;

                var encoding = isUtf8Bom
                    ? new UTF8Encoding(true)
                    : Encoding.Default; // 한국 윈도우에서는 CP949

                using var reader = new StreamReader(fs, encoding, detectEncodingFromByteOrderMarks: true);

                var list = new List<string>();
                while (!reader.EndOfStream)
                {
                    list.Add(reader.ReadLine() ?? string.Empty);
                }
                lines = list.ToArray();
            }

            if (lines.Length <= 1)
            {
                MessageBox.Show("유효한 데이터가 없습니다.", "안내",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var headerCols = ParseCsvLine(lines[0]);
            var indexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headerCols.Length; i++)
            {
                indexByName[headerCols[i]] = i;
            }

            bool Has(string name) => indexByName.ContainsKey(name);
            int Idx(string name) => indexByName[name];

            List<Product> importList = new();

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = ParseCsvLine(line);
                if (cols.Length == 0) continue;

                string Get(string name)
                {
                    if (!Has(name)) return "";
                    int idx = Idx(name);
                    if (idx < 0 || idx >= cols.Length) return "";
                    return cols[idx];
                }

                long productId = 0;
                if (Has("ProductId"))
                {
                    long.TryParse(Get("ProductId"), out productId);
                }

                var code = Get("ProductCode");
                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                var name = Get("ProductName");
                var plan = Get("PlanName");

                double? monthlyUsd = null;
                if (Has("MonthlyFeeUsd"))
                {
                    if (double.TryParse(Get("MonthlyFeeUsd"), out var usd))
                        monthlyUsd = usd;
                }

                long monthlyKrw = 0;
                if (Has("MonthlyFeeKrw"))
                {
                    long.TryParse(Get("MonthlyFeeKrw"), out monthlyKrw);
                }

                long wholesale = 0;
                long retail    = 0;
                long purchase  = 0;

                if (Has("WholesalePrice"))
                    long.TryParse(Get("WholesalePrice"), out wholesale);

                if (Has("RetailPrice"))
                    long.TryParse(Get("RetailPrice"), out retail);

                if (Has("PurchasePrice"))
                    long.TryParse(Get("PurchasePrice"), out purchase);

                bool yearlyAvailable = false;
                if (Has("YearlyAvailable"))
                {
                    var y = Get("YearlyAvailable");
                    yearlyAvailable = y.Equals("Y", StringComparison.OrdinalIgnoreCase)
                                      || y.Equals("true", StringComparison.OrdinalIgnoreCase);
                }

                int minMonth = 1;
                int maxMonth = 12;
                if (Has("MinMonth"))
                    int.TryParse(Get("MinMonth"), out minMonth);
                if (Has("MaxMonth"))
                    int.TryParse(Get("MaxMonth"), out maxMonth);

                var status = Get("Status");
                if (string.IsNullOrWhiteSpace(status))
                    status = "ACTIVE";

                var remark = Get("Remark");

                var p = new Product
                {
                    ProductId       = productId,
                    ProductCode     = code.Trim(),
                    ProductName     = name.Trim(),
                    PlanName        = string.IsNullOrWhiteSpace(plan) ? null : plan.Trim(),
                    MonthlyFeeUsd   = monthlyUsd,
                    MonthlyFeeKrw   = monthlyKrw,
                    WholesalePrice  = wholesale,
                    RetailPrice     = retail,
                    PurchasePrice   = purchase,
                    YearlyAvailable = yearlyAvailable,
                    MinMonth        = minMonth,
                    MaxMonth        = maxMonth,
                    Status          = status.Trim(),
                    Remark          = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim()
                };

                importList.Add(p);
            }

            if (importList.Count == 0)
            {
                MessageBox.Show("가져올 상품이 없습니다.", "안내",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"{importList.Count}개의 행을 일괄 적용합니다.\n" +
                "상품코드/ID를 기준으로 신규/수정을 자동 판별합니다.\n계속하시겠습니까?",
                "확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            var (inserted, updated, skipped) = _service.BulkUpsert(importList, _currentUser);

            LoadProducts();

            MessageBox.Show(
                $"일괄 적용 완료.\n\n" +
                $"신규 등록: {inserted}건\n" +
                $"수정: {updated}건\n" +
                $"스킵: {skipped}건",
                "완료",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"엑셀 업로드 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenQuote()
    {
        using var dlg = new QuoteForm(_service, _salesService, _currentUser);
        dlg.ShowDialog(this);
    }
    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    sb.Append('\"');
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
        return result.ToArray();
    }

    private void OpenSalesList()
    {
        using var dlg = new SalesListForm(_salesService);
        dlg.ShowDialog(this);
    }
}
