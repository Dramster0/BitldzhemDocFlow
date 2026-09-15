using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitldzhemDocFlow.Models
{
    // Роли пользователей (справочник)
    [Table("Roles")]
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required, MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        public List<User> Users { get; set; } = new();
    }

    // Пользователи системы
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Login { get; set; } = string.Empty;

        // Пароль хранится в хешированном виде (BCrypt) — см. диплом, п. 1.4.4
        [Required, MaxLength(200)]
        public string PasswordHash { get; set; } = string.Empty;

        public int RoleId { get; set; }
        [ForeignKey(nameof(RoleId))]
        public Role? Role { get; set; }
    }

    // Статусы технологических карт (справочник)
    [Table("CardStatuses")]
    public class CardStatus
    {
        [Key]
        public int StatusId { get; set; }

        [Required, MaxLength(50)]
        public string StatusName { get; set; } = string.Empty;

        public List<TechnologicalCard> Cards { get; set; } = new();
    }

    // Технологические карты
    [Table("TechnologicalCards")]
    public class TechnologicalCard
    {
        [Key]
        public int CardId { get; set; }

        [Required, MaxLength(150)]
        public string CardName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string ProcessDescription { get; set; } = string.Empty;

        public int StatusId { get; set; }
        [ForeignKey(nameof(StatusId))]
        public CardStatus? Status { get; set; }

        // Кто создал карту
        public int AuthorId { get; set; }
        [ForeignKey(nameof(AuthorId))]
        public User? Author { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ApprovedDate { get; set; }

        [MaxLength(500)]
        public string DirectorComment { get; set; } = string.Empty;

        public List<CardComponent> Components { get; set; } = new();
        public List<ProductionBatch> Batches { get; set; } = new();
    }

    // Состав технологической карты (ингредиенты)
    [Table("CardComponents")]
    public class CardComponent
    {
        [Key]
        public int ComponentId { get; set; }

        public int CardId { get; set; }
        [ForeignKey(nameof(CardId))]
        public TechnologicalCard? Card { get; set; }

        [Required, MaxLength(150)]
        public string ComponentName { get; set; } = string.Empty;

        public double Quantity { get; set; }

        [MaxLength(20)]
        public string Unit { get; set; } = string.Empty;
    }

    // Партии продукции
    [Table("ProductionBatches")]
    public class ProductionBatch
    {
        [Key]
        public int BatchId { get; set; }

        public int CardId { get; set; }
        [ForeignKey(nameof(CardId))]
        public TechnologicalCard? Card { get; set; }

        [Required, MaxLength(50)]
        public string BatchNumber { get; set; } = string.Empty;

        public DateTime ProductionDate { get; set; } = DateTime.Now;

        public List<LabTest> Tests { get; set; } = new();
    }

    // Результаты лабораторных испытаний
    [Table("LabTests")]
    public class LabTest
    {
        [Key]
        public int TestId { get; set; }

        public int BatchId { get; set; }
        [ForeignKey(nameof(BatchId))]
        public ProductionBatch? Batch { get; set; }

        public DateTime TestDate { get; set; } = DateTime.Now;

        [Required, MaxLength(1000)]
        public string Result { get; set; } = string.Empty;

        // true — соответствует норме, false — не соответствует
        public bool IsCompliant { get; set; }

        public int LabAssistantId { get; set; }
        [ForeignKey(nameof(LabAssistantId))]
        public User? LabAssistant { get; set; }

        public Conclusion? Conclusion { get; set; }
    }

    // Заключения о соответствии
    [Table("Conclusions")]
    public class Conclusion
    {
        [Key]
        public int ConclusionId { get; set; }

        public int TestId { get; set; }
        [ForeignKey(nameof(TestId))]
        public LabTest? Test { get; set; }

        [Required, MaxLength(1000)]
        public string ConclusionText { get; set; } = string.Empty;

        public DateTime ConclusionDate { get; set; } = DateTime.Now;

        public int QualitySpecialistId { get; set; }
        [ForeignKey(nameof(QualitySpecialistId))]
        public User? QualitySpecialist { get; set; }
    }

    // Журнал действий пользователей
    [Table("ChangeLog")]
    public class ChangeLogEntry
    {
        [Key]
        public int LogId { get; set; }

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required, MaxLength(300)]
        public string Action { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; } = DateTime.Now;
    }
}
