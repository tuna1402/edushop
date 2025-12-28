using System;
using EduShop.Core.Common;
using EduShop.Core.Models;
using EduShop.Core.Repositories;

namespace EduShop.Core.Services;

public class CardService
{
    private readonly CardRepository _cardRepo;
    private readonly AuditLogRepository _logRepo;

    public CardService(CardRepository cardRepo, AuditLogRepository logRepo)
    {
        _cardRepo = cardRepo;
        _logRepo = logRepo;
    }

    public List<Card> GetAll() => _cardRepo.GetAll();

    public Card? GetById(long cardId) => _cardRepo.GetById(cardId);

    public long Create(Card card, UserContext user)
    {
        if (string.IsNullOrWhiteSpace(card.Status))
            card.Status = "ACTIVE";

        var newId = _cardRepo.Insert(card, user.UserName);

        _logRepo.Insert(new AuditLogEntry
        {
            UserId = user.UserId,
            UserName = user.UserName,
            ActionType = "CARD_CREATE",
            TableName = "Card",
            TargetId = newId,
            TargetCode = card.CardName,
            Description = $"카드 등록 - {card.CardName}"
        });

        return newId;
    }

    public void Update(Card card, UserContext user)
    {
        var existing = _cardRepo.GetById(card.CardId);
        if (existing == null)
            throw new InvalidOperationException($"카드(ID={card.CardId})을(를) 찾을 수 없습니다.");

        _cardRepo.Update(card, user.UserName);

        _logRepo.Insert(new AuditLogEntry
        {
            UserId = user.UserId,
            UserName = user.UserName,
            ActionType = "CARD_UPDATE",
            TableName = "Card",
            TargetId = card.CardId,
            TargetCode = card.CardName,
            Description = $"카드 수정 - {card.CardName}"
        });
    }

    public void ChangeStatus(long cardId, string newStatus, UserContext user)
    {
        var existing = _cardRepo.GetById(cardId);
        if (existing == null) return;

        if (string.Equals(existing.Status, newStatus, StringComparison.OrdinalIgnoreCase))
            return;

        _cardRepo.UpdateStatus(cardId, newStatus, user.UserName);

        _logRepo.Insert(new AuditLogEntry
        {
            UserId = user.UserId,
            UserName = user.UserName,
            ActionType = "CARD_STATUS_CHANGE",
            TableName = "Card",
            TargetId = cardId,
            TargetCode = existing.CardName,
            Description = $"카드 상태 변경 - {existing.Status} → {newStatus}"
        });
    }
}
