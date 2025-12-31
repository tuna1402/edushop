using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EduShop.Core.Models;
using EduShop.Core.Services;

namespace EduShop.WinForms;

public class AccountReuseForm : Form
{
    private readonly CustomerService _customerService;
    private readonly ProductService  _productService;

    private ComboBox _cboCustomer = null!;
    private ComboBox _cboProduct  = null!;
    private DateTimePicker _dtStart = null!;
    private DateTimePicker _dtEnd   = null!;
    private TextBox _txtOrderId = null!;
    private CheckBox _chkDelivery = null!;
    private DateTimePicker _dtDelivery = null!;
    private Button _btnOk = null!;
    private Button _btnCancel = null!;

    private List<Customer> _customers = new();
    private List<Product> _products   = new();

    public long SelectedCustomerId { get; private set; }
    public long SelectedProductId  { get; private set; }
    public DateTime StartDate      { get; private set; }
    public DateTime EndDate        { get; private set; }
    public long? SelectedOrderId   { get; private set; }
    public DateTime? DeliveryDate  { get; private set; }

    public AccountReuseForm(CustomerService customerService, ProductService productService)
    {
        _customerService = customerService;
        _productService  = productService;

        Text = "계정 재사용";
        Width = 420;
        Height = 320;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        InitializeControls();
        LoadLookups();
    }

    private void InitializeControls()
    {
        var lblCustomer = new Label { Text = "고객", Left = 20, Top = 20, Width = 80 };
        _cboCustomer = new ComboBox
        {
            Left = 110,
            Top = 16,
            Width = 250,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var lblProduct = new Label { Text = "상품", Left = 20, Top = 55, Width = 80 };
        _cboProduct = new ComboBox
        {
            Left = 110,
            Top = 51,
            Width = 250,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var lblStart = new Label { Text = "시작일", Left = 20, Top = 90, Width = 80 };
        _dtStart = new DateTimePicker
        {
            Left = 110,
            Top = 86,
            Width = 120,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today
        };

        var lblEnd = new Label { Text = "만료일", Left = 20, Top = 125, Width = 80 };
        _dtEnd = new DateTimePicker
        {
            Left = 110,
            Top = 121,
            Width = 120,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddMonths(3)
        };

        var lblOrder = new Label { Text = "주문번호", Left = 20, Top = 160, Width = 80 };
        _txtOrderId = new TextBox
        {
            Left = 110,
            Top = 156,
            Width = 120
        };

        _chkDelivery = new CheckBox
        {
            Text = "납품일 지정",
            Left = 20,
            Top = 190,
            Width = 100
        };
        _chkDelivery.CheckedChanged += (_, _) => _dtDelivery.Enabled = _chkDelivery.Checked;

        _dtDelivery = new DateTimePicker
        {
            Left = 140,
            Top = 186,
            Width = 120,
            Format = DateTimePickerFormat.Short,
            Enabled = false,
            Value = DateTime.Today
        };

        _btnOk = new Button
        {
            Text = "확인",
            Left = 170,
            Top = 230,
            Width = 80,
            DialogResult = DialogResult.OK
        };
        _btnOk.Click += (_, _) => Confirm();

        _btnCancel = new Button
        {
            Text = "취소",
            Left = 260,
            Top = 230,
            Width = 80,
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[]
        {
            lblCustomer, _cboCustomer,
            lblProduct, _cboProduct,
            lblStart, _dtStart,
            lblEnd, _dtEnd,
            lblOrder, _txtOrderId,
            _chkDelivery, _dtDelivery,
            _btnOk, _btnCancel
        });
    }

    private void LoadLookups()
    {
        _customers = _customerService.GetAll();
        _products  = _productService.GetAll();

        _cboCustomer.DataSource = _customers
            .Select(c => new { c.CustomerId, Name = c.SchoolName })
            .ToList();
        _cboCustomer.DisplayMember = "Name";
        _cboCustomer.ValueMember   = "CustomerId";

        _cboProduct.DataSource = _products
            .Select(p => new { p.ProductId, Name = $"{p.ProductName} / {p.PlanName}" })
            .ToList();
        _cboProduct.DisplayMember = "Name";
        _cboProduct.ValueMember   = "ProductId";
    }

    private void Confirm()
    {
        if (_cboCustomer.SelectedValue is not long customerId)
        {
            MessageBox.Show("고객을 선택하세요.");
            DialogResult = DialogResult.None;
            return;
        }

        if (_cboProduct.SelectedValue is not long productId)
        {
            MessageBox.Show("상품을 선택하세요.");
            DialogResult = DialogResult.None;
            return;
        }

        if (_dtEnd.Value.Date < _dtStart.Value.Date)
        {
            MessageBox.Show("만료일은 시작일 이후여야 합니다.");
            DialogResult = DialogResult.None;
            return;
        }

        SelectedCustomerId = customerId;
        SelectedProductId  = productId;
        StartDate          = _dtStart.Value.Date;
        EndDate            = _dtEnd.Value.Date;

        if (long.TryParse(_txtOrderId.Text.Trim(), out var orderId))
        {
            SelectedOrderId = orderId;
        }
        else
        {
            SelectedOrderId = null;
        }

        DeliveryDate = _chkDelivery.Checked ? _dtDelivery.Value.Date : null;
    }
}
