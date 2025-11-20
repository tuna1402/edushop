using System;
using System.Drawing;
using System.Windows.Forms;

namespace EduShop.WinForms;

public class GuideForm : Form
{
    public GuideForm()
    {
        Text = "EduShop 관리 프로그램 가이드";
        Width = 700;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;

        var lblTitle = new Label
        {
            Text = "EduShop 관리 프로그램",
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            AutoSize = true,
            Left = 20,
            Top  = 20
        };

        var tb = new TextBox
        {
            Multiline  = true,
            ReadOnly   = true,
            ScrollBars = ScrollBars.Vertical,
            Left   = 20,
            Top    = lblTitle.Bottom + 10,
            Width  = ClientSize.Width - 40,
            Height = ClientSize.Height - 90,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        tb.Text =
@"이 프로그램은 EduShop의 상품·고객·계정·주문을 통합 관리하기 위한 도구입니다.

[주요 메뉴 안내]

- 관리 > 상품 관리
  · 상품 목록 조회, 등록/수정
  · 상품 엑셀 Import/Export

- 관리 > 고객 관리
  · 학교/기관 고객 정보 관리

- 관리 > 계정 관리
  · Google 계정/구독 상태 관리
  · 주문/고객과의 매핑, 사용 로그 조회

- 관리 > 주문/견적
  · 견적서 작성, 매출/납품 내역 관리

- 리포트
  · 만료 예정 계정, 매출 리포트 (점진 구현)

처음에는 '상품 관리'와 '계정 관리'부터 사용하면서
필요한 기능을 조금씩 확장하는 것을 권장합니다.";

        var btnClose = new Button
        {
            Text = "닫기",
            Width = 80,
            Left  = ClientSize.Width - 100,
            Top   = ClientSize.Height - 40,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        btnClose.Click += (_, _) => Close();

        Controls.Add(lblTitle);
        Controls.Add(tb);
        Controls.Add(btnClose);
    }
}
