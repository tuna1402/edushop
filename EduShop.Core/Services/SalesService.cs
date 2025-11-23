using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Repositories;

namespace EduShop.Core.Services;

public class SalesService
{
    private readonly SalesRepository _salesRepo;

    public SalesService(SalesRepository salesRepo)
    {
        _salesRepo = salesRepo;
    }

    // 견적/화면에서 만들어진 매출 헤더 & 항목을 그대로 저장
    public long CreateSale(SaleHeader header, List<SaleItem> items, UserContext user)
    {
        // 필요하면 여기서 도메인 검증(빈 항목, 0수량, 음수 금액 등) 추가 가능
        return _salesRepo.InsertSale(header, items, user.UserName);
    }

    public List<SaleHeader> GetSales(DateTime? from, DateTime? to)
        => _salesRepo.GetSales(from, to);

    public List<SaleItem> GetSaleItems(long saleId)
        => _salesRepo.GetSaleItems(saleId);

    public SalesSummary GetSummary(DateTime? from, DateTime? to)
        => _salesRepo.GetSummary(from, to);

    public List<SaleHeader> GetRecent(int count)
        => _salesRepo.GetRecent(count);
}
