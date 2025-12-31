using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Infrastructure.Supabase;
using EduShop.Core.Models;
using EduShop.Core.Repositories.Remote;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class CustomerListForm : Form
{
    private readonly CustomerService _customerService;
    private readonly UserContext     _currentUser;
    private readonly SupabaseSessionState _supabaseState;

    private TextBox      _txtName = null!;
    private Button       _btnSearch = null!;
    private Button       _btnReset = null!;
    private Button       _btnExportCsv = null!;
    private Button       _btnImportCsv = null!;
    private DataGridView _grid = null!;
    private Button       _btnNew = null!;
    private Button       _btnEdit = null!;
    private Button       _btnDelete = null!;
    private Button       _btnClose = null!;
    private ContextMenuStrip _ctxMenu = null!;

    private List<Customer> _customers = new();

    private class CustomerRow
    {
        public long   CustomerId   { get; set; }
        public string SchoolName { get; set; } = "";
        public string? ContactName { get; set; }
        public string? Phone1      { get; set; }
        public string? Phone2      { get; set; }
        public string? Email1      { get; set; }
        public string? Email2      { get; set; }
        public string? Address     { get; set; }
        public string? Memo        { get; set; }
    }

    public CustomerListForm(CustomerService customerService, UserContext currentUser, SupabaseSessionState supabaseState)
    {
        _customerService = customerService;
        _currentUser     = currentUser;
        _supabaseState   = supabaseState;

        Text = "고객 관리";
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        ReloadData();
    }

    private void InitializeControls()
    {
        var lblName = new Label
        {
            Text = "학교명",
            Left = 10,
            Top  = 15,
            Width = 60
        };
        _txtName = new TextBox
        {
            Left = lblName.Right + 5,
            Top  = 10,
            Width = 200
        };

        _btnSearch = new Button
        {
            Text = "조회",
            Left = _txtName.Right + 10,
            Top  = 8,
            Width = 80
        };
        _btnSearch.Click += (_, _) => ReloadData();

        _btnReset = new Button
        {
            Text = "초기화",
            Left = _btnSearch.Right + 10,
            Top  = 8,
            Width = 80
        };
        _btnReset.Click += (_, _) => ResetFilters();

        _btnExportCsv = new Button
        {
            Text = "엑셀 다운로드(고객 CSV Export)",
            Left = _btnReset.Right + 10,
            Top  = 8,
            Width = 200
        };
        _btnExportCsv.Click += (_, _) => ExportCustomersCsv();

        _btnImportCsv = new Button
        {
            Text = "엑셀 업로드(고객 CSV Import)",
            Left = _btnExportCsv.Right + 10,
            Top  = 8,
            Width = 200
        };
        _btnImportCsv.Click += (_, _) => ImportCustomersCsv();

        _grid = new DataGridView
        {
            Left = 10,
            Top  = 45,
            Width  = ClientSize.Width - 20,
            Height = ClientSize.Height - 110,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect   = false,
            AutoGenerateColumns = false
        };

        _ctxMenu = new ContextMenuStrip();
        _ctxMenu.Items.Add("선택 고객만 엑셀 다운로드", null, (_, _) => ExportCustomersCsv(true));
        _ctxMenu.Items.Add("엑셀 다운로드(고객 CSV Export)", null, (_, _) => ExportCustomersCsv());
        _ctxMenu.Items.Add("엑셀 업로드(고객 CSV Import)", null, (_, _) => ImportCustomersCsv());
        _grid.ContextMenuStrip = _ctxMenu;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "ID",
            DataPropertyName = "CustomerId",
            Width = 60
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "학교명",
            DataPropertyName = "SchoolName",
            Width = 200
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "담당자",
            DataPropertyName = "ContactName",
            Width = 120
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "전화1",
            DataPropertyName = "Phone1",
            Width = 120
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "전화2",
            DataPropertyName = "Phone2",
            Width = 120
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "이메일1",
            DataPropertyName = "Email1",
            Width = 180
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "이메일2",
            DataPropertyName = "Email2",
            Width = 180
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "주소",
            DataPropertyName = "Address",
            Width = 180
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "메모",
            DataPropertyName = "Memo",
            Width = 200
        });

        _grid.DoubleClick += (_, _) => EditSelected();
        _grid.KeyDown += GridOnKeyDown;

        _btnNew = new Button
        {
            Text = "신규",
            Left = 10,
            Top  = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnNew.Click += (_, _) => CreateNew();

        _btnEdit = new Button
        {
            Text = "수정",
            Left = _btnNew.Right + 10,
            Top  = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnEdit.Click += (_, _) => EditSelected();

        _btnDelete = new Button
        {
            Text = "삭제",
            Left = _btnEdit.Right + 10,
            Top  = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnDelete.Click += (_, _) => DeleteSelected();

        _btnClose = new Button
        {
            Text = "닫기",
            Left = ClientSize.Width - 100,
            Top  = ClientSize.Height - 45,
            Width = 80,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnClose.Click += (_, _) => Close();

        Controls.Add(lblName);
        Controls.Add(_txtName);
        Controls.Add(_btnSearch);
        Controls.Add(_btnReset);
        Controls.Add(_btnExportCsv);
        Controls.Add(_btnImportCsv);
        Controls.Add(_grid);
        Controls.Add(_btnNew);
        Controls.Add(_btnEdit);
        Controls.Add(_btnDelete);
        Controls.Add(_btnClose);
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

    private void ResetFilters()
    {
        _txtName.Text = "";
        ReloadData();
    }

    private async void ReloadData()
    {
        _customers = await LoadCustomersAsync();
        ApplyFilter();
    }

    private async Task<List<Customer>> LoadCustomersAsync()
    {
        if (!_supabaseState.IsSupabaseActive)
        {
            SetRemoteMode(false);
            return _customerService.GetAll();
        }

        try
        {
            SetRemoteMode(true);
            var client = await SupabaseClientFactory.CreateAsync(_supabaseState.Config, _supabaseState.AccessToken);
            var repo = new RemoteCustomerRepository(client);
            return await repo.GetAllAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Supabase 고객 조회 실패: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetRemoteMode(false);
            return _customerService.GetAll();
        }
    }

    private void SetRemoteMode(bool enabled)
    {
        _btnNew.Enabled = !enabled;
        _btnEdit.Enabled = !enabled;
        _btnDelete.Enabled = !enabled;
        _btnImportCsv.Enabled = !enabled;
    }

    private void ApplyFilter()
    {
        IEnumerable<Customer> query = _customers;

        var name = _txtName.Text.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(c =>
                c.SchoolName.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        var list = query
            .OrderBy(c => c.SchoolName)
            .ThenBy(c => c.CustomerId)
            .Select(c => new CustomerRow
            {
                CustomerId   = c.CustomerId,
                SchoolName   = c.SchoolName,
                ContactName  = c.ContactName,
                Phone1       = c.Phone1,
                Phone2       = c.Phone2,
                Email1       = c.Email1,
                Email2       = c.Email2,
                Address      = c.Address,
                Memo         = c.Memo
            })
            .ToList();

        _grid.DataSource = list;
    }

    private Customer? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is not CustomerRow row) return null;
        return _customers.FirstOrDefault(c => c.CustomerId == row.CustomerId);
    }

    private void CreateNew()
    {
        if (_supabaseState.IsSupabaseActive)
        {
            MessageBox.Show("Supabase 연결 모드에서는 고객 등록을 할 수 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new CustomerEditForm(_customerService, _currentUser, null);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            ReloadData();
        }
    }

    private void EditSelected()
    {
        if (_supabaseState.IsSupabaseActive)
        {
            MessageBox.Show("Supabase 연결 모드에서는 고객 수정을 할 수 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var c = GetSelected();
        if (c == null) return;

        using var dlg = new CustomerEditForm(_customerService, _currentUser, c.CustomerId);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            ReloadData();
        }
    }

    private void DeleteSelected()
    {
        if (_supabaseState.IsSupabaseActive)
        {
            MessageBox.Show("Supabase 연결 모드에서는 고객 삭제를 할 수 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var c = GetSelected();
        if (c == null) return;

        var result = MessageBox.Show(
            $"고객 [{c.SchoolName}] 을(를) 삭제(비활성) 하시겠습니까?",
            "확인",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        _customerService.SoftDelete(c.CustomerId, _currentUser);
        ReloadData();
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
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

    private void ExportCustomersCsv(bool onlySelected = false)
    {
        List<CustomerRow> rows;

        if (onlySelected && _grid.SelectedRows.Count > 0)
        {
            rows = new List<CustomerRow>();
            foreach (DataGridViewRow r in _grid.SelectedRows)
            {
                if (r.DataBoundItem is CustomerRow cr)
                    rows.Add(cr);
            }
        }
        else
        {
            if (_grid.DataSource is not List<CustomerRow> all || all.Count == 0)
            {
                MessageBox.Show("내보낼 고객 데이터가 없습니다.", "안내",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            rows = all;
        }

        if (rows.Count == 0)
        {
            MessageBox.Show("내보낼 고객 데이터가 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*",
            FileName = $"customers_{DateTime.Now:yyyyMMddHHmm}.csv"
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
                "SchoolName",
                "ContactName",
                "Phone1",
                "Phone2",
                "Email1",
                "Email2",
                "Address",
                "Memo"));

            foreach (var row in rows)
            {
                var line = string.Join(",",
                    EscapeCsv(row.SchoolName ?? ""),
                    EscapeCsv(row.ContactName ?? ""),
                    EscapeCsv(row.Phone1 ?? ""),
                    EscapeCsv(row.Phone2 ?? ""),
                    EscapeCsv(row.Email1 ?? ""),
                    EscapeCsv(row.Email2 ?? ""),
                    EscapeCsv(row.Address ?? ""),
                    EscapeCsv(row.Memo ?? ""));

                writer.WriteLine(line);
            }

            MessageBox.Show("고객 목록 CSV 저장이 완료되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"CSV 저장 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportCustomersCsv()
    {
        if (_supabaseState.IsSupabaseActive)
        {
            MessageBox.Show("Supabase 연결 모드에서는 고객 Import를 할 수 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

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

            if (!columnMap.ContainsKey("SchoolName") && !columnMap.ContainsKey("CustomerName"))
            {
                MessageBox.Show("헤더 형식이 예상과 다릅니다. (SchoolName 또는 CustomerName 컬럼이 필요합니다.)", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int lineNo = 1;
            _customers = _customerService.GetAll();
            var byName = _customers.ToDictionary(c => c.SchoolName, StringComparer.OrdinalIgnoreCase);

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                lineNo++;
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var cols = SplitCsvLine(line);

                string schoolName  = GetCsvValue(cols, columnMap, "SchoolName");
                if (string.IsNullOrWhiteSpace(schoolName))
                    schoolName = GetCsvValue(cols, columnMap, "CustomerName");
                string contactName = GetCsvValue(cols, columnMap, "ContactName");
                string phone1      = GetCsvValue(cols, columnMap, "Phone1");
                string phone2      = GetCsvValue(cols, columnMap, "Phone2");
                string email1      = GetCsvValue(cols, columnMap, "Email1");
                string email2      = GetCsvValue(cols, columnMap, "Email2");
                string address     = GetCsvValue(cols, columnMap, "Address");
                string memo        = GetCsvValue(cols, columnMap, "Memo");

                if (string.IsNullOrEmpty(schoolName))
                {
                    errors.Add($"라인 {lineNo}: SchoolName이 비어 있습니다.");
                    skipped++;
                    continue;
                }

                try
                {
                    if (!byName.TryGetValue(schoolName, out var existing))
                    {
                        var c = new Customer
                        {
                            SchoolName   = schoolName,
                            ContactName  = contactName,
                            Phone1       = phone1,
                            Phone2       = phone2,
                            Email1       = email1,
                            Email2       = email2,
                            Address      = address,
                            Memo         = memo
                        };

                        var newId = _customerService.Create(c, _currentUser);
                        c.CustomerId = newId;
                        byName[schoolName] = c;
                        created++;
                    }
                    else
                    {
                        existing.SchoolName   = schoolName;
                        existing.ContactName  = contactName;
                        existing.Phone1       = phone1;
                        existing.Phone2       = phone2;
                        existing.Email1       = email1;
                        existing.Email2       = email2;
                        existing.Address      = address;
                        existing.Memo         = memo;

                        _customerService.Update(existing, _currentUser);
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

            var msg = $"고객 Import 완료:\n" +
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
}
