using System;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;

namespace ERMS.Domain.Entities.Training;

public class ChatMessage : BaseEntity
{
    public Guid ChatConversationId { get; set; }
    public Guid EnterpriseId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual ChatConversation ChatConversation { get; set; } = null!;
    public virtual Enterprise.Enterprise Enterprise { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
