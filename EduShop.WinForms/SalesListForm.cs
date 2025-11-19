using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Models;
using EduShop.Core.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EduShop.WinForms;

public class SalesListForm : Form
{
    private readonly SalesService _salesService;

    private DateTimePicker _dtFrom = null!;
    private DateTimePicker _dtTo = null!;
    private Button _btnSearch = null!;
    private Button _btnClose = null!;
    private Button _btnExportPdf = null!;
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

        _btnExportPdf = new Button
        {
            Text = "리포트 PDF",
            Left = ClientSize.Width - 90 - 110,   // 닫기 버튼 왼쪽에 위치
            Top = ClientSize.Height - 35,
            Width = 100,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnExportPdf.Click += (_, _) => ExportSalesReportPdf();

        Controls.Add(lblFrom);
        Controls.Add(_dtFrom);
        Controls.Add(lblWave);
        Controls.Add(_dtTo);
        Controls.Add(_btnSearch);
        Controls.Add(_gridSales);
        Controls.Add(_gridItems);
        Controls.Add(_lblSummary);
        Controls.Add(_btnExportPdf);
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
    private void ExportSalesReportPdf()
    {
        if (_currentSales == null || _currentSales.Count == 0)
        {
            MessageBox.Show("리포트로 저장할 매출 데이터가 없습니다.", "안내",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "PDF 파일 (*.pdf)|*.pdf|모든 파일 (*.*)|*.*",
            FileName = $"sales_report_{DateTime.Now:yyyyMMddHHmm}.pdf"
        };

        if (sfd.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var from = _dtFrom.Value.Date;
            var to   = _dtTo.Value.Date;

            var sales   = _currentSales.OrderBy(s => s.SaleDate).ThenBy(s => s.SaleId).ToList();
            var summary = _salesService.GetSummary(from, to);

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
                            .Text("매출 리포트")
                            .SemiBold().FontSize(18).AlignCenter();

                        page.Content().Column(col =>
                        {
                            col.Spacing(10);

                            // 기간 + 합계 정보
                            col.Item().Text($"기간: {from:yyyy-MM-dd} ~ {to:yyyy-MM-dd}");
                            col.Item().Text(
                                $"총 매출: {summary.TotalAmount:N0} 원 / 총 마진: {summary.TotalProfit:N0} 원");

                            // 매출 목록 테이블
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(90); // 일자
                                    columns.RelativeColumn();   // 고객/학교
                                    columns.ConstantColumn(100); // 총금액
                                    columns.ConstantColumn(100); // 총마진
                                });

                                // 헤더
                                table.Header(header =>
                                {
                                    header.Cell().Element(c =>
                                            c.PaddingVertical(4)
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Medium)
                                            .DefaultTextStyle(x => x.SemiBold()))
                                          .Text("일자");

                                    header.Cell().Element(c =>
                                            c.PaddingVertical(4)
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Medium)
                                            .DefaultTextStyle(x => x.SemiBold()))
                                          .Text("고객 / 학교");

                                    header.Cell().Element(c =>
                                            c.PaddingVertical(4)
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Medium)
                                            .DefaultTextStyle(x => x.SemiBold()))
                                          .AlignRight().Text("총금액");

                                    header.Cell().Element(c =>
                                            c.PaddingVertical(4)
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Medium)
                                            .DefaultTextStyle(x => x.SemiBold()))
                                          .AlignRight().Text("총마진");
                                });

                                // 데이터 행
                                foreach (var s in sales)
                                {
                                    var title = $"{s.CustomerName ?? ""} / {s.SchoolName ?? ""}".Trim(' ', '/');

                                    table.Cell().Element(c => c.PaddingVertical(2))
                                        .Text(s.SaleDate.ToString("yyyy-MM-dd"));

                                    table.Cell().Element(c => c.PaddingVertical(2))
                                        .Text(string.IsNullOrWhiteSpace(title) ? "(무기명)" : title);

                                    table.Cell().Element(c => c.PaddingVertical(2))
                                        .AlignRight().Text(s.TotalAmount.ToString("N0"));

                                    table.Cell().Element(c => c.PaddingVertical(2))
                                        .AlignRight().Text(s.TotalProfit.ToString("N0"));
                                }

                                // 합계 행
                                table.Cell().ColumnSpan(2)
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
                                    .AlignRight().Text(summary.TotalAmount.ToString("N0"));

                                table.Cell().Element(c =>
                                        c.PaddingVertical(4)
                                        .BorderTop(1)
                                        .BorderColor(Colors.Grey.Medium)
                                        .DefaultTextStyle(x => x.SemiBold()))
                                    .AlignRight().Text(summary.TotalProfit.ToString("N0"));
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

            MessageBox.Show("매출 리포트 PDF 저장이 완료되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"매출 리포트 PDF 저장 중 오류: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

}
