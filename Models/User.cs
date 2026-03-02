using System;
using System.Collections.Generic;
using System.Linq;

namespace MaterialManager_V01.Models
{
    /// <summary>
    /// Benutzer-Modell für Multi-User-Verwaltung
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string PasswordHash { get; set; } = "";  // bcrypt hash (NICHT plain text!)
        public UserRole Role { get; set; } = UserRole.Lagerarbeiter;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }
        public string Email { get; set; } = "";
    }

    /// <summary>
    /// Rollen für Zugriffskontrolle
    /// </summary>
    public enum UserRole
    {
        Admin = 0,           // Vollzugriff + Setup
        Manager = 1,         // Berichte, Freigaben, Analysen
        Lagerarbeiter = 2,   // Material hinzufügen/ändern
        ReadOnly = 3         // Nur anschauen (Audits, Inspektionen)
    }

    /// <summary>
    /// Audit-Log Eintrag für Compliance
    /// </summary>
    public class AuditLogEntry
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Action { get; set; } = "";  // "CREATE", "UPDATE", "DELETE", "LOGIN"
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string TableName { get; set; } = "";  // z.B. "MaterialItem"
        public string RecordId { get; set; } = "";   // Material ID
        public string OldValue { get; set; } = "";   // Alt-Wert
        public string NewValue { get; set; } = "";   // Neu-Wert
        public string Reason { get; set; } = "";     // Kommentar/Grund
        public string IPAddress { get; set; } = "";
        public string Result { get; set; } = "SUCCESS";  // SUCCESS, ERROR

        public override string ToString()
            => $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Username} - {Action} {TableName} (ID:{RecordId}) - {Reason}";
    }
}
