using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Services;
using QuestPDF.Fluent;                 // Document.Create, Text(), Table() 등
using QuestPDF.Helpers;                // Colors
using QuestPDF.Infrastructure;         // IContainer, PageSizes 등

namespace EduShop.WinForms;

public class QuoteForm : Form
{
    private readonly ProductService _service;
    private readonly SalesService _salesService;
    private readonly UserContext _currentUser;
    private readonly AppSettings _appSettings;
    private TextBox _txtCustomer = null!;
    private TextBox _txtSchool = null!;
    private TextBox _txtContact = null!;
    private TextBox _txtMemo = null!;

    private DataGridView _grid = null!;
    private Button _btnAdd = null!;
    private Button _btnRemove = null!;
    private Button _btnClear = null!;
    private Button _btnExportCsv = null!;
    private Button _btnExportPdf = null!;
    private Button _btnSaveSale = null!;
    private Button _btnClose = null!;
    private Label _lblTotalAmount = null!;
    private Label _lblTotalProfit = null!;
    private BindingList<QuoteItem> _items = new();

    public QuoteForm(ProductService service, SalesService salesService, UserContext currentUser, AppSettings appSettings)
    {
        _service = service;
        _salesService = salesService;
        _currentUser = currentUser;
        _appSettings = appSettings;

        Text = "견적서 작성";
        Width = 900;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        UpdateTotals();
    }

    private void InitializeControls()
    {
        int top = 10;

        var lblCustomer = new Label { Text = "고객명", Left = 10, Top = top + 4, Width = 60 };
        _txtCustomer = new TextBox { Left = 80, Top = top, Width = 200 };

        var lblSchool = new Label { Text = "학교명", Left = 300, Top = top + 4, Width = 60 };
        _txtSchool = new TextBox { Left = 370, Top = top, Width = 200 };

        top += 30;

        var lblContact = new Label { Text = "연락처", Left = 10, Top = top + 4, Width = 60 };
        _txtContact = new TextBox { Left = 80, Top = top, Width = 200 };

        var lblMemo = new Label { Text = "메모", Left = 300, Top = top + 4, Width = 60 };
        _txtMemo = new TextBox { Left = 370, Top = top, Width = 300 };

        top += 40;

        // ── 그리드: 아래에 버튼/합계가 보이도록 높이를 여유 있게 설정 ──
        _grid = new DataGridView
        {
            Left = 10,
            Top = top,
            Width = ClientSize.Width - 20,
            Height = ClientSize.Height - 140,        // ← 아래 80px 정도 비워둠
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoGenerateColumns = false,
            ReadOnly = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "코드",
            DataPropertyName = "ProductCode",
            ReadOnly = true,
            Width = 100
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "상품명",
            DataPropertyName = "ProductName",
            ReadOnly = true,
            Width = 250
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "단가",
            DataPropertyName = "UnitPrice",
            Width = 80
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "수량",
            DataPropertyName = "Quantity",
            Width = 60
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "금액",
            DataPropertyName = "LineTotal",
            ReadOnly = true,
            Width = 100
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "마진",
            DataPropertyName = "LineProfit",
            ReadOnly = true,
            Width = 100
        });

        _grid.CellEndEdit += (_, __) =>
        {
            foreach (var row in _items)
            {
                if (row.Quantity < 0) row.Quantity = 0;
            }
            UpdateTotals();
        };

        // ── 하단 버튼/합계 위치 계산 (그리드 아래) ──
        int bottomTop = ClientSize.Height - 65;   // 폼 맨 아래에서 65px 위

        _btnAdd = new Button
        {
            Text = "상품 추가",
            Left = 10,
            Top = bottomTop,
            Width = 90,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnAdd.Click += (_, _) => AddItem();

        _btnRemove = new Button
        {
            Text = "행 삭제",
            Left = _btnAdd.Right + 10,
            Top = bottomTop,
            Width = 90,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnRemove.Click += (_, _) => RemoveItem();

        _btnClear = new Button
        {
            Text = "모두 지우기",
            Left = _btnRemove.Right + 10,
            Top = bottomTop,
            Width = 100,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnClear.Click += (_, _) => ClearItems();

        _btnExportCsv = new Button
        {
            Text = "견적 CSV 저장",
            Left = _btnClear.Right + 10,
            Top = bottomTop,
            Width = 120,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnExportCsv.Click += (_, _) => ExportQuoteCsv();

        _btnExportPdf = new Button
        {
            Text = "견적 PDF 저장",
            Left = _btnExportCsv.Right + 10,
            Top = bottomTop,
            Width = 120,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnExportPdf.Click += (_, _) => ExportQuotePdf();

        _btnSaveSale = new Button
        {
            Text = "매출로 저장",
            Left = _btnExportPdf.Right + 10,
            Top = bottomTop,
            Width = 110,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        _btnSaveSale.Click += (_, _) => SaveAsSale();

        _btnClose = new Button
        {
            Text = "닫기",
            Left = ClientSize.Width - 90,
            Top = bottomTop,
            Width = 80,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnClose.Click += (_, _) => Close();

        _lblTotalAmount = new Label
        {
            Text = "합계: 0 원",
            Left = ClientSize.Width - 350,
            Top = bottomTop,
            Width = 200,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };

        _lblTotalProfit = new Label
        {
            Text = "예상 마진: 0 원",
            Left = ClientSize.Width - 350,
            Top = bottomTop + 25,
            Width = 200,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };

        // ── 컨트롤 추가 순서 (버튼/라벨이 그리드 위에 보이도록) ──
        Controls.Add(lblCustomer);
        Controls.Add(_txtCustomer);
        Controls.Add(lblSchool);
        Controls.Add(_txtSchool);
        Controls.Add(lblContact);
        Controls.Add(_txtContact);
        Controls.Add(lblMemo);
        Controls.Add(_txtMemo);
        Controls.Add(_grid);
        Controls.Add(_btnAdd);
        Controls.Add(_btnRemove);
        Controls.Add(_btnClear);
        Controls.Add(_btnExportCsv);
        Controls.Add(_btnExportPdf);
        Controls.Add(_btnSaveSale);
        Controls.Add(_btnClose);
        Controls.Add(_lblTotalAmount);
        Controls.Add(_lblTotalProfit);

        // 혹시라도 겹치면 그리드를 뒤로 보내기
        _grid.SendToBack();

        BindGrid();
    }


    private void BindGrid()
    {
        _grid.DataSource = _items;
    }

    private void AddItem()
    {
        using var picker = new ProductPickerForm(_service);
        if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedProduct != null)
        {
            var p = picker.SelectedProduct;

            var item = new QuoteItem
            {
                ProductId      = p.ProductId,
                ProductCode    = p.ProductCode,
                ProductName    = p.ProductName,
                UnitPrice      = p.SalePriceKrw,
                Quantity       = 1,
                PurchasePrice  = p.PurchasePriceKrw ?? 0
            };

            _items.Add(item);
            UpdateTotals();
        }
    }

    private void RemoveItem()
    {
        if (_grid.CurrentRow?.DataBoundItem is QuoteItem item)
        {
            _items.Remove(item); 
            UpdateTotals();
        }
    }

    private void ClearItems()
    {
        if (_items.Count == 0) return;

        if (MessageBox.Show("모든 항목을 삭제하시겠습니까?", "확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _items.Clear();
        UpdateTotals();
    }

    private void UpdateTotals()
    {
        long totalAmount = _items.Sum(i => i.LineTotal);
        long totalProfit = _items.Sum(i => i.LineProfit);

        _lblTotalAmount.Text = $"합계: {totalAmount:N0} 원";
        _lblTotalProfit.Text = $"예상 마진: {totalProfit:N0} 원";
    }

    private void ExportQuoteCsv()
    {
        if (_items.Count == 0)
        {
            MessageBox.Show("견적 항목이 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*",
            FileName = $"quote_{DateTime.Now:yyyyMMddHHmm}.csv",
            InitialDirectory = string.IsNullOrEmpty(_appSettings.DefaultExportFolder)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : _appSettings.DefaultExportFolder
        };

        if (sfd.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using var writer = new StreamWriter(sfd.FileName, false, Encoding.UTF8);

            string Escape(string? s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }

            // 상단 정보
            writer.WriteLine($"견적일시,{DateTime.Now:yyyy-MM-dd HH:mm}");
            writer.WriteLine($"고객명,{Escape(_txtCustomer.Text)}");
            writer.WriteLine($"학교명,{Escape(_txtSchool.Text)}");
            writer.WriteLine($"연락처,{Escape(_txtContact.Text)}");
            writer.WriteLine($"메모,{Escape(_txtMemo.Text)}");
            writer.WriteLine($"회사명,{Escape(_appSettings.CompanyName)}");
            writer.WriteLine($"담당자,{Escape(_appSettings.CompanyContact)}");
            writer.WriteLine($"전화,{Escape(_appSettings.CompanyPhone)}");
            writer.WriteLine($"이메일,{Escape(_appSettings.CompanyEmail)}");
            writer.WriteLine($"주소,{Escape(_appSettings.CompanyAddress)}");
            writer.WriteLine(); // 빈 줄

            // 헤더
            writer.WriteLine("ProductCode,ProductName,UnitPrice,Quantity,LineTotal,LineProfit");

            foreach (var item in _items)
            {
                var line = string.Join(",", new[]
                {
                    Escape(item.ProductCode),
                    Escape(item.ProductName),
                    item.UnitPrice.ToString(),
                    item.Quantity.ToString(),
                    item.LineTotal.ToString(),
                    item.LineProfit.ToString()
                });
                writer.WriteLine(line);
            }

            // 합계
            long totalAmount = _items.Sum(i => i.LineTotal);
            long totalProfit = _items.Sum(i => i.LineProfit);

            writer.WriteLine();
            writer.WriteLine($",,,합계,{totalAmount},{totalProfit}");

            MessageBox.Show("견적 CSV 저장이 완료되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"견적 저장 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportQuotePdf()
    {
        if (_items.Count == 0)
        {
            MessageBox.Show("견적 항목이 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "PDF 파일 (*.pdf)|*.pdf|모든 파일 (*.*)|*.*",
            FileName = $"quote_{DateTime.Now:yyyyMMddHHmm}.pdf",
            InitialDirectory = string.IsNullOrEmpty(_appSettings.DefaultExportFolder)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : _appSettings.DefaultExportFolder
        };

        if (sfd.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var customer = _txtCustomer.Text.Trim();
            var school   = _txtSchool.Text.Trim();
            var contact  = _txtContact.Text.Trim();
            var memo     = _txtMemo.Text.Trim();

            var items = _items.ToList();   // 스냅샷
            long totalAmount = items.Sum(i => i.LineTotal);
            long totalProfit = items.Sum(i => i.LineProfit);

            Document
                .Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header()
                            .Text("견적서")
                            .SemiBold().FontSize(18).AlignCenter();

                        page.Content().Column(col =>
                        {
                            col.Spacing(10);

                            // 상단 고객 정보
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text($"고객명: {customer}");
                                    c.Item().Text($"학교명: {school}");
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text($"연락처: {contact}");
                                    c.Item().Text($"작성일: {DateTime.Now:yyyy-MM-dd}");
                                });
                            });

                            if (!string.IsNullOrWhiteSpace(memo))
                            {
                                col.Item().Text($"메모: {memo}");
                            }

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text($"발행 회사: {_appSettings.CompanyName}");
                                    if (!string.IsNullOrWhiteSpace(_appSettings.CompanyContact))
                                        c.Item().Text($"담당자: {_appSettings.CompanyContact}");
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    if (!string.IsNullOrWhiteSpace(_appSettings.CompanyPhone))
                                        c.Item().Text($"전화: {_appSettings.CompanyPhone}");
                                    if (!string.IsNullOrWhiteSpace(_appSettings.CompanyEmail))
                                        c.Item().Text($"이메일: {_appSettings.CompanyEmail}");
                                    if (!string.IsNullOrWhiteSpace(_appSettings.CompanyAddress))
                                        c.Item().Text($"주소: {_appSettings.CompanyAddress}");
                                });
                            });

                            // 품목 테이블
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(70); // 코드
                                    columns.RelativeColumn();   // 상품명
                                    columns.ConstantColumn(60); // 단가
                                    columns.ConstantColumn(50); // 수량
                                    columns.ConstantColumn(70); // 금액
                                    columns.ConstantColumn(70); // 마진
                                });

                                // 헤더 행
                                table.Header(header =>
                                {
                                    header.Cell().Element(c =>
                                            c.PaddingVertical(4)
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Medium)
                                            .DefaultTextStyle(x => x.SemiBold()))
                                        .Text("코드");

                                    header.Cell().Element(c =>
                                            c.PaddingVertical(4)
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Medium)
                                            .DefaultTextStyle(x => x.SemiBold()))
                                        .Text("상품명");

                                    header.Cell().Element(c =>
                                            c.PaddingVertical(4)
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Medium)
                                            .DefaultTextStyle(x => x.SemiBold()))
                                        .AlignRight().Text("단가");

                                    header.Cell().Element(c =>
                                            c.PaddingVertical(4)
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Medium)
                                            .DefaultTextStyle(x => x.SemiBold()))
                                        .AlignRight().Text("수량");

                                    header.Cell().Element(c =>
                                            c.PaddingVertical(4)
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Medium)
                                            .DefaultTextStyle(x => x.SemiBold()))
                                        .AlignRight().Text("금액");

                                    header.Cell().Element(c =>
                                            c.PaddingVertical(4)
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Medium)
                                            .DefaultTextStyle(x => x.SemiBold()))
                                        .AlignRight().Text("마진");
                                });

                                // 데이터 행
                                foreach (var item in items)
                                {
                                    table.Cell().Element(c => c.PaddingVertical(2))
                                        .Text(item.ProductCode);

                                    table.Cell().Element(c => c.PaddingVertical(2))
                                        .Text(item.ProductName);

                                    table.Cell().Element(c => c.PaddingVertical(2))
                                        .AlignRight().Text(item.UnitPrice.ToString("N0"));

                                    table.Cell().Element(c => c.PaddingVertical(2))
                                        .AlignRight().Text(item.Quantity.ToString());

                                    table.Cell().Element(c => c.PaddingVertical(2))
                                        .AlignRight().Text(item.LineTotal.ToString("N0"));

                                    table.Cell().Element(c => c.PaddingVertical(2))
                                        .AlignRight().Text(item.LineProfit.ToString("N0"));
                                }

                                // 합계 행
                                table.Cell().ColumnSpan(3)
                                    .Element(c =>
                                        c.PaddingVertical(4)
                                        .BorderTop(1)
                                        .BorderColor(Colors.Grey.Medium)
                                        .DefaultTextStyle(x => x.SemiBold()))
                                    .AlignRight().Text("합계");

                                table.Cell().Element(c =>
                                        c.PaddingVertical(4)
                                        .BorderTop(1)
                                        .BorderColor(Colors.Grey.Medium)
                                        .DefaultTextStyle(x => x.SemiBold()))
                                    .Text(""); // 수량 합계는 생략

                                table.Cell().Element(c =>
                                        c.PaddingVertical(4)
                                        .BorderTop(1)
                                        .BorderColor(Colors.Grey.Medium)
                                        .DefaultTextStyle(x => x.SemiBold()))
                                    .AlignRight().Text(totalAmount.ToString("N0"));

                                table.Cell().Element(c =>
                                        c.PaddingVertical(4)
                                        .BorderTop(1)
                                        .BorderColor(Colors.Grey.Medium)
                                        .DefaultTextStyle(x => x.SemiBold()))
                                    .AlignRight().Text(totalProfit.ToString("N0"));
                            });

                            col.Item()
                                .AlignRight()
                                .Text(x =>
                                {
                                    x.Span($"합계: {totalAmount:N0} 원   /   ");
                                    x.Span($"예상 마진: {totalProfit:N0} 원");
                                });
                        });

                        page.Footer()
                            .AlignRight()
                            .Text(x =>
                            {
                                x.Span("페이지 ");
                                x.CurrentPageNumber();
                                x.Span(" / ");
                                x.TotalPages();
                            });
                    });
                })
                .GeneratePdf(sfd.FileName);

            MessageBox.Show("견적 PDF 저장이 완료되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"견적 PDF 저장 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveAsSale()
    {
        if (_items.Count == 0)
        {
            MessageBox.Show("매출로 저장할 항목이 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var header = new SaleHeader
        {
            SaleDate     = DateTime.Today,
            CustomerName = string.IsNullOrWhiteSpace(_txtCustomer.Text) ? null : _txtCustomer.Text.Trim(),
            SchoolName   = string.IsNullOrWhiteSpace(_txtSchool.Text)   ? null : _txtSchool.Text.Trim(),
            Contact      = string.IsNullOrWhiteSpace(_txtContact.Text)  ? null : _txtContact.Text.Trim(),
            Memo         = string.IsNullOrWhiteSpace(_txtMemo.Text)     ? null : _txtMemo.Text.Trim()
        };

        var items = _items.Select(q => new SaleItem
        {
            ProductId   = q.ProductId == 0 ? null : q.ProductId,
            ProductCode = q.ProductCode,
            ProductName = q.ProductName,
            UnitPrice   = q.UnitPrice,
            Quantity    = q.Quantity,
            LineTotal   = q.LineTotal,
            LineProfit  = q.LineProfit
        }).ToList();

        try
        {
            var saleId = _salesService.CreateSale(header, items, _currentUser);
            MessageBox.Show($"매출로 저장되었습니다. (ID = {saleId})", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"매출 저장 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private class QuoteItem
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public long UnitPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public long PurchasePrice { get; set; }

        public long LineTotal => UnitPrice * Quantity;
        public long LineProfit => (UnitPrice - PurchasePrice) * Quantity;
    }
}
