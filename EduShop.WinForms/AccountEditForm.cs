using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class AccountEditForm : Form
{
    private readonly AccountService   _accountService;
    private readonly ProductService   _productService;
    private readonly CustomerService  _customerService;
    private readonly UserContext      _currentUser;

    private Account? _account;

    private TextBox        _txtEmail = null!;
    private ComboBox       _cboProduct = null!;
    private DateTimePicker _dtStart = null!;
    private DateTimePicker _dtEnd   = null!;
    private ComboBox       _cboStatus = null!;
    private ComboBox       _cboCustomer = null!;
    private TextBox        _txtOrderId = null!;
    private DateTimePicker _dtDelivery = null!;
    private DateTimePicker _dtLastPayment = null!;
    private TextBox        _txtMemo = null!;
    private Button         _btnSave = null!;
    private Button         _btnCancel = null!;

    private DataGridView   _gridLogs = null!;

    private List<Product>  _products  = new();
    private List<Customer> _customers = new();

    public AccountEditForm(
        AccountService  accountService,
        ProductService  productService,
        CustomerService customerService,
        UserContext     currentUser,
        long?           accountId)
    {
        _accountService  = accountService;
        _productService  = productService;
        _customerService = customerService;
        _currentUser     = currentUser;

        if (accountId.HasValue)
        {
            _account = _accountService.Get(accountId.Value);
        }

        Text = _account == null ? "계정 등록" : "계정 수정";
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;

        InitializeControls();
        LoadProducts();
        LoadCustomers();
        BindAccount();
        LoadLogs();
    }

    private void InitializeControls()
    {
        int leftLabel = 10;
        int leftInput = 110;
        int top       = 15;
        int rowHeight = 28;

        // 이메일
        var lblEmail = new Label
        {
            Text  = "이메일",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtEmail = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 300
        };
        top += rowHeight;

        // 상품
        var lblProduct = new Label
        {
            Text  = "상품",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _cboProduct = new ComboBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 350,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        top += rowHeight;

        // 구독 시작/만료
        var lblStart = new Label
        {
            Text  = "구독 시작일",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _dtStart = new DateTimePicker
        {
            Left  = leftInput,
            Top   = top,
            Width = 150,
            Format = DateTimePickerFormat.Short
        };

        var lblEnd = new Label
        {
            Text  = "만료일",
            Left  = _dtStart.Right + 20,
            Top   = top + 4,
            Width = 60
        };
        _dtEnd = new DateTimePicker
        {
            Left  = lblEnd.Right + 5,
            Top   = top,
            Width = 150,
            Format = DateTimePickerFormat.Short
        };
        top += rowHeight;

        // 상태
        var lblStatus = new Label
        {
            Text  = "상태",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _cboStatus = new ComboBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 150,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboStatus.Items.Add(AccountStatus.Created);
        _cboStatus.Items.Add(AccountStatus.SubsActive);
        _cboStatus.Items.Add(AccountStatus.Delivered);
        _cboStatus.Items.Add(AccountStatus.InUse);
        _cboStatus.Items.Add(AccountStatus.Expiring);
        _cboStatus.Items.Add(AccountStatus.Canceled);
        _cboStatus.Items.Add(AccountStatus.ResetReady);
        _cboStatus.SelectedIndex = 1; // SUBS_ACTIVE
        top += rowHeight;

        // 고객 (콤보박스)
        var lblCustomer = new Label
        {
            Text  = "고객",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _cboCustomer = new ComboBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 300,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        top += rowHeight;

        // OrderId (일단 숫자 입력)
        var lblOrder = new Label
        {
            Text  = "OrderId",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtOrderId = new TextBox
        {
            Left  = leftInput,
            Top   = top,
            Width = 150
        };
        top += rowHeight;

        // 납품일
        var lblDelivery = new Label
        {
            Text  = "납품일",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _dtDelivery = new DateTimePicker
        {
            Left  = leftInput,
            Top   = top,
            Width = 150,
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true
        };
        top += rowHeight;

        // 마지막 결제일
        var lblLastPay = new Label
        {
            Text  = "마지막 결제일",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _dtLastPayment = new DateTimePicker
        {
            Left  = leftInput,
            Top   = top,
            Width = 150,
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true
        };
        top += rowHeight;

        // 메모
        var lblMemo = new Label
        {
            Text  = "메모",
            Left  = leftLabel,
            Top   = top + 4,
            Width = 90
        };
        _txtMemo = new TextBox
        {
            Left     = leftInput,
            Top      = top,
            Width    = 500,
            Height   = 60,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        top += 70;

        _btnSave = new Button
        {
            Text = "저장",
            Left = Width - 220,
            Top  = top,
            Width = 80,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnSave.Click += (_, _) => SaveAccount();

        _btnCancel = new Button
        {
            Text = "취소",
            Left = Width - 130,
            Top  = top,
            Width = 80,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        var lblLogs = new Label
        {
            Text = "사용 로그",
            Left = 10,
            Top  = top + 35,
            Width = 100
        };

        _gridLogs = new DataGridView
        {
            Left = 10,
            Top  = lblLogs.Bottom + 5,
            Width  = ClientSize.Width - 20,
            Height = ClientSize.Height - (lblLogs.Bottom + 15),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect   = false,
            AutoGenerateColumns = false
        };

        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "일시",
            DataPropertyName = "CreatedAt",
            Width = 140,
            DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" }
        });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "액션",
            DataPropertyName = "ActionType",
            Width = 120
        });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "요청일",
            DataPropertyName = "RequestDate",
            Width = 100,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "만료일",
            DataPropertyName = "ExpireDate",
            Width = 100,
            DefaultCellStyle = { Format = "yyyy-MM-dd" }
        });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "설명",
            DataPropertyName = "Description",
            Width = 300
        });
        _gridLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "작성자",
            DataPropertyName = "CreatedBy",
            Width = 80
        });

        Controls.Add(lblEmail);
        Controls.Add(_txtEmail);
        Controls.Add(lblProduct);
        Controls.Add(_cboProduct);
        Controls.Add(lblStart);
        Controls.Add(_dtStart);
        Controls.Add(lblEnd);
        Controls.Add(_dtEnd);
        Controls.Add(lblStatus);
        Controls.Add(_cboStatus);
        Controls.Add(lblCustomer);
        Controls.Add(_cboCustomer);
        Controls.Add(lblOrder);
        Controls.Add(_txtOrderId);
        Controls.Add(lblDelivery);
        Controls.Add(_dtDelivery);
        Controls.Add(lblLastPay);
        Controls.Add(_dtLastPayment);
        Controls.Add(lblMemo);
        Controls.Add(_txtMemo);
        Controls.Add(_btnSave);
        Controls.Add(_btnCancel);
        Controls.Add(lblLogs);
        Controls.Add(_gridLogs);
    }

    private void LoadProducts()
    {
        _products = _productService.GetAll();

        _cboProduct.Items.Clear();
        foreach (var p in _products)
        {
            _cboProduct.Items.Add($"{p.ProductName} / {p.PlanName}");
        }

        if (_cboProduct.Items.Count > 0)
            _cboProduct.SelectedIndex = 0;
    }

    private void LoadCustomers()
    {
        _customers = _customerService.GetAll()
            .OrderBy(c => c.SchoolName)
            .ToList();

        _cboCustomer.Items.Clear();
        _cboCustomer.Items.Add(""); // 미지정

        foreach (var c in _customers)
        {
            _cboCustomer.Items.Add(c.SchoolName);
        }

        _cboCustomer.SelectedIndex = 0;
    }

    private void BindAccount()
    {
        if (_account == null)
        {
            _dtStart.Value = DateTime.Today;
            _dtEnd.Value   = DateTime.Today.AddMonths(3);
            return;
        }

        _txtEmail.Text = _account.Email;

        var pIdx = _products.FindIndex(p => p.ProductId == _account.ProductId);
        if (pIdx >= 0) _cboProduct.SelectedIndex = pIdx;

        _dtStart.Value = _account.SubscriptionStartDate;
        _dtEnd.Value   = _account.SubscriptionEndDate;

        var stIndex = _cboStatus.Items.IndexOf(_account.Status);
        if (stIndex >= 0) _cboStatus.SelectedIndex = stIndex;

        if (_account.CustomerId.HasValue)
        {
            var idx = _customers.FindIndex(c => c.CustomerId == _account.CustomerId.Value);
            _cboCustomer.SelectedIndex = idx >= 0 ? idx + 1 : 0;
        }
        else
        {
            _cboCustomer.SelectedIndex = 0;
        }

        _txtOrderId.Text = _account.OrderId?.ToString(CultureInfo.InvariantCulture) ?? "";

        if (_account.DeliveryDate.HasValue)
        {
            _dtDelivery.Value   = _account.DeliveryDate.Value;
            _dtDelivery.Checked = true;
        }
        else _dtDelivery.Checked = false;

        if (_account.LastPaymentDate.HasValue)
        {
            _dtLastPayment.Value   = _account.LastPaymentDate.Value;
            _dtLastPayment.Checked = true;
        }
        else _dtLastPayment.Checked = false;

        _txtMemo.Text = _account.Memo ?? "";
    }

    private void LoadLogs()
    {
        if (_account == null || _account.AccountId == 0)
        {
            _gridLogs.DataSource = null;
            return;
        }

        var logs = _accountService.GetUsageLogs(_account.AccountId);

        var view = logs
            .OrderBy(l => l.CreatedAt)
            .Select(l => new
            {
                l.CreatedAt,
                l.ActionType,
                l.RequestDate,
                l.ExpireDate,
                l.Description,
                l.CreatedBy
            })
            .ToList();

        _gridLogs.DataSource = view;
    }

    private void SaveAccount()
    {
        var email = _txtEmail.Text.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            MessageBox.Show("이메일을 입력하세요.", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _txtEmail.Focus();
            return;
        }

        if (_cboProduct.SelectedIndex < 0)
        {
            MessageBox.Show("구독 상품을 선택하세요.", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _cboProduct.Focus();
            return;
        }

        if (_dtEnd.Value.Date < _dtStart.Value.Date)
        {
            MessageBox.Show("만료일은 시작일 이후여야 합니다.", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _dtEnd.Focus();
            return;
        }

        long? customerId = null;
        if (_cboCustomer.SelectedIndex > 0)
        {
            var cust = _customers[_cboCustomer.SelectedIndex - 1];
            customerId = cust.CustomerId;
        }

        long? orderId = null;
        if (!string.IsNullOrWhiteSpace(_txtOrderId.Text))
        {
            if (!long.TryParse(_txtOrderId.Text.Trim(), out var tmp))
            {
                MessageBox.Show("OrderId는 숫자여야 합니다.", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _txtOrderId.Focus();
                return;
            }
            orderId = tmp;
        }

        var product = _products[_cboProduct.SelectedIndex];

        DateTime? delivery = _dtDelivery.Checked ? _dtDelivery.Value.Date : null;
        DateTime? lastPay  = _dtLastPayment.Checked ? _dtLastPayment.Value.Date : null;

        var status = (string)_cboStatus.SelectedItem!;

        if (_account == null)
        {
            var acc = new Account
            {
                Email                 = email,
                ProductId             = product.ProductId,
                SubscriptionStartDate = _dtStart.Value.Date,
                SubscriptionEndDate   = _dtEnd.Value.Date,
                Status                = status,
                CustomerId            = customerId,
                OrderId               = orderId,
                DeliveryDate          = delivery,
                LastPaymentDate       = lastPay,
                Memo                  = _txtMemo.Text
            };

            var newId = _accountService.Create(acc, _currentUser);
            _account = _accountService.Get(newId);

            MessageBox.Show("계정이 등록되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
        }
        else
        {
            _account.Email                 = email;
            _account.ProductId             = product.ProductId;
            _account.SubscriptionStartDate = _dtStart.Value.Date;
            _account.SubscriptionEndDate   = _dtEnd.Value.Date;
            _account.Status                = status;
            _account.CustomerId            = customerId;
            _account.OrderId               = orderId;
            _account.DeliveryDate          = delivery;
            _account.LastPaymentDate       = lastPay;
            _account.Memo                  = _txtMemo.Text;

            _accountService.Update(_account, _currentUser);

            MessageBox.Show("계정 정보가 수정되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
        }

        Close();
    }
}
