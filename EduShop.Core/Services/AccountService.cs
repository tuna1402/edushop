using System;
using System.Collections.Generic;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Repositories;

namespace EduShop.Core.Services;

public class AccountService
{
    private readonly AccountRepository _accountRepo;
    private readonly AccountUsageLogRepository _logRepo;
    public AccountService(AccountRepository accountRepo, AccountUsageLogRepository logRepo)
    {
        _accountRepo = accountRepo;
        _logRepo     = logRepo;
    }

    public List<Account> GetAll() =>
        _accountRepo.GetAll();

    public Account? Get(long accountId) =>
        _accountRepo.GetById(accountId);

    public Account? GetById(long accountId) =>
        _accountRepo.GetById(accountId);

    public List<AccountUsageLog> GetUsageLogs(
        long accountId,
        DateTime? from = null,
        DateTime? to = null,
        long? customerId = null,
        long? productId = null) =>
        _logRepo.GetForAccount(accountId, from, to, customerId, productId);

    public long Create(Account acc, UserContext user)
    {
        // 기본 상태값: SUBS_ACTIVE (명세에 맞게 필요하면 Created로 수정 가능)
        if (string.IsNullOrWhiteSpace(acc.Status))
            acc.Status = AccountStatus.SubsActive;

        var newId = _accountRepo.Insert(acc, user.UserName);

        // 사용 로그: 생성
        _logRepo.Insert(new AccountUsageLog
        {
            AccountId   = newId,
            CustomerId  = acc.CustomerId,
            ProductId   = acc.ProductId,
            ActionType  = AccountActionType.Create,
            RequestDate = acc.SubscriptionStartDate,
            ExpireDate  = acc.SubscriptionEndDate,
            Description = $"계정 생성: {acc.Email}"
        }, user.UserName);

        return newId;
    }

    public void Update(Account acc, UserContext user)
    {
        _accountRepo.Update(acc, user.UserName);

        _logRepo.Insert(new AccountUsageLog
        {
            AccountId   = acc.AccountId,
            CustomerId  = acc.CustomerId,
            ProductId   = acc.ProductId,
            ActionType  = AccountActionType.StatusChange,
            RequestDate = acc.SubscriptionStartDate,
            ExpireDate  = acc.SubscriptionEndDate,
            Description = "계정 정보 수정"
        }, user.UserName);
    }

    public void SoftDelete(long accountId, UserContext user)
    {
        var acc = _accountRepo.GetById(accountId);
        if (acc == null) return;

        _accountRepo.SoftDelete(accountId, user.UserName);

        _logRepo.Insert(new AccountUsageLog
        {
            AccountId   = accountId,
            CustomerId  = acc.CustomerId,
            ProductId   = acc.ProductId,
            ActionType  = AccountActionType.StatusChange,
            RequestDate = acc.SubscriptionStartDate,
            ExpireDate  = acc.SubscriptionEndDate,
            Description = "계정 삭제(비활성화)"
        }, user.UserName);
    }

    public void ChangeStatus(long accountId, string newStatus, UserContext user, string? description = null)
    {
        var acc = _accountRepo.GetById(accountId);
        if (acc == null) return;

        if (acc.Status == newStatus) return;

        var oldStatus = acc.Status;
        acc.Status = newStatus;

        // 만료 예정 상태로 전환되면, 필요하면 SubscriptionEndDate 기준으로만 사용
        _accountRepo.Update(acc, user.UserName);

        _logRepo.Insert(new AccountUsageLog
        {
            AccountId   = accountId,
            CustomerId  = acc.CustomerId,
            ProductId   = acc.ProductId,
            ActionType  = AccountActionType.StatusChange,
            RequestDate = acc.SubscriptionStartDate,
            ExpireDate  = acc.SubscriptionEndDate,
            Description = description ?? $"상태 변경: {oldStatus} → {newStatus}"
        }, user.UserName);
    }

    public void Deliver(long accountId, DateTime? deliveryDate, UserContext user)
    {
        var acc = _accountRepo.GetById(accountId);
        if (acc == null) return;

        if (acc.Status != AccountStatus.SubsActive &&
            acc.Status != AccountStatus.Created)
        {
            throw new InvalidOperationException("CREATED 또는 SUBS_ACTIVE 상태에서만 납품 처리할 수 있습니다.");
        }

        if (acc.CustomerId == null || acc.OrderId == null)
        {
            throw new InvalidOperationException("납품 처리 전에 CustomerId / OrderId가 지정되어 있어야 합니다.");
        }

        var oldStatus = acc.Status;
        acc.Status = AccountStatus.Delivered;
        acc.DeliveryDate = deliveryDate?.Date ?? DateTime.Today;

        _accountRepo.Update(acc, user.UserName);

        _logRepo.Insert(new AccountUsageLog
        {
            AccountId   = acc.AccountId,
            CustomerId  = acc.CustomerId,
            ProductId   = acc.ProductId,
            ActionType  = AccountActionType.Deliver,
            RequestDate = acc.SubscriptionStartDate,
            ExpireDate  = acc.SubscriptionEndDate,
            Description = $"납품 처리: {oldStatus} → {acc.Status}"
        }, user.UserName);
    }

    public void CancelSubscription(long accountId, UserContext user)
    {
        var acc = _accountRepo.GetById(accountId);
        if (acc == null) return;

        if (acc.Status == AccountStatus.Canceled ||
            acc.Status == AccountStatus.ResetReady)
        {
            return;
        }

        var cancellableStatuses = new HashSet<string>
        {
            AccountStatus.InUse,
            AccountStatus.Expiring,
            AccountStatus.SubsActive,
            AccountStatus.Delivered,
            AccountStatus.Created
        };

        if (!cancellableStatuses.Contains(acc.Status))
        {
            return;
        }

        var oldStatus = acc.Status;
        acc.Status = AccountStatus.Canceled;

        _accountRepo.Update(acc, user.UserName);

        _logRepo.Insert(new AccountUsageLog
        {
            AccountId   = acc.AccountId,
            CustomerId  = acc.CustomerId,
            ProductId   = acc.ProductId,
            ActionType  = AccountActionType.Cancel,
            RequestDate = DateTime.Today,
            ExpireDate  = acc.SubscriptionEndDate,
            Description = $"구독 취소: {oldStatus} → {acc.Status}"
        }, user.UserName);
    }

    public void MarkResetReady(long accountId, UserContext user)
    {
        var acc = _accountRepo.GetById(accountId);
        if (acc == null) return;

        if (acc.Status != AccountStatus.Canceled)
        {
            throw new InvalidOperationException("CANCELED 상태에서만 재사용 준비로 전환할 수 있습니다.");
        }

        var oldStatus = acc.Status;
        acc.Status = AccountStatus.ResetReady;

        _accountRepo.Update(acc, user.UserName);

        _logRepo.Insert(new AccountUsageLog
        {
            AccountId   = acc.AccountId,
            CustomerId  = acc.CustomerId,
            ProductId   = acc.ProductId,
            ActionType  = AccountActionType.PasswordReset,
            RequestDate = acc.SubscriptionStartDate,
            ExpireDate  = acc.SubscriptionEndDate,
            Description = $"재사용 준비: {oldStatus} → {acc.Status}"
        }, user.UserName);
    }

    public void ReuseAccount(
        long accountId,
        long newCustomerId,
        long newProductId,
        DateTime newStartDate,
        DateTime newEndDate,
        long? newOrderId,
        DateTime? newDeliveryDate,
        UserContext user)
    {
        var acc = _accountRepo.GetById(accountId);
        if (acc == null)
            throw new InvalidOperationException("계정을 찾을 수 없습니다.");

        if (acc.Status != AccountStatus.ResetReady)
        {
            throw new InvalidOperationException("RESET_READY 상태에서만 재사용할 수 있습니다.");
        }

        acc.CustomerId            = newCustomerId;
        acc.ProductId             = newProductId;
        acc.SubscriptionStartDate = newStartDate.Date;
        acc.SubscriptionEndDate   = newEndDate.Date;
        acc.OrderId               = newOrderId;
        acc.DeliveryDate          = newDeliveryDate?.Date;
        acc.Status                = acc.DeliveryDate.HasValue
            ? AccountStatus.Delivered
            : AccountStatus.SubsActive;

        _accountRepo.Update(acc, user.UserName);

        _logRepo.Insert(new AccountUsageLog
        {
            AccountId   = acc.AccountId,
            CustomerId  = acc.CustomerId,
            ProductId   = acc.ProductId,
            ActionType  = AccountActionType.Reuse,
            RequestDate = acc.SubscriptionStartDate,
            ExpireDate  = acc.SubscriptionEndDate,
            Description = "계정 재사용"
        }, user.UserName);
    }
    public List<Account> GetExpiring(DateTime referenceDate, int days)
        => _accountRepo.GetExpiring(referenceDate, days);
        
    public Account? GetByEmail(string email) =>
    _accountRepo.GetByEmail(email);
}
