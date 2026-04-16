using System;
using System.Collections.Generic;
using ERMS.Domain.Common;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;

namespace ERMS.Domain.Entities.Training;

public class ChatConversation : BaseEntity
{
    public Guid EnterpriseId { get; set; }
    public Guid UserId { get; set; }
    public int DepartmentId { get; set; }
    public string? Title { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual Enterprise.Enterprise Enterprise { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Department Department { get; set; } = null!;
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
