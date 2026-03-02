using System;
using System.Collections.Generic;
using System.Linq;
using MaterialManager_V01.Models;

namespace MaterialManager_V01.Services
{
    /// <summary>
    /// Benutzer-Management Service
    /// Verwaltet Login, Passwörter, Rollen
    /// </summary>
    public static class UserService
    {
        private static List<User> _users = new();
        private static User _currentUser = null;
        private static DateTime _lastActivityTime = DateTime.Now;
        private const int SESSION_TIMEOUT_MINUTES = 15;

        static UserService()
        {
            InitializeDefaultUsers();
        }

        /// <summary>
        /// Initialisiere Standard-Benutzer (nur beim ersten Start)
        /// </summary>
        private static void InitializeDefaultUsers()
        {
            if (_users.Count == 0)
            {
                // Default Admin für Setup
                _users.Add(new User
                {
                    Id = 1,
                    Username = "admin",
                    DisplayName = "Administrator",
                    PasswordHash = BCryptHashPassword("admin123"),  // ÄNDERE DAS!
                    Role = UserRole.Admin,
                    IsActive = true,
                    Email = "admin@example.com"
                });

                // Default Manager
                _users.Add(new User
                {
                    Id = 2,
                    Username = "manager",
                    DisplayName = "Manager",
                    PasswordHash = BCryptHashPassword("manager123"),  // ÄNDERE DAS!
                    Role = UserRole.Manager,
                    IsActive = true,
                    Email = "manager@example.com"
                });

                // Default Lagerarbeiter
                _users.Add(new User
                {
                    Id = 3,
                    Username = "lager",
                    DisplayName = "Lagerarbeiter",
                    PasswordHash = BCryptHashPassword("lager123"),  // ÄNDERE DAS!
                    Role = UserRole.Lagerarbeiter,
                    IsActive = true,
                    Email = "lager@example.com"
                });
            }
        }

        /// <summary>
        /// Benutzer-Login mit Passwort-Validierung
        /// </summary>
        public static bool Login(string username, string password)
        {
            var user = _users.FirstOrDefault(u => u.Username == username && u.IsActive);
            if (user == null)
                return false;

            // Passwort prüfen
            if (!VerifyPassword(password, user.PasswordHash))
                return false;

            // Login erfolgreich
            _currentUser = user;
            _lastActivityTime = DateTime.Now;
            user.LastLogin = DateTime.Now;

            // Log Login
            AuditLogService.LogAction(
                username: user.Username,
                action: "LOGIN",
                tableName: "Users",
                recordId: user.Id.ToString(),
                newValue: $"User '{user.Username}' with role '{user.Role}'",
                reason: "User login"
            );

            return true;
        }

        /// <summary>
        /// Benutzer abmelden
        /// </summary>
        public static void Logout()
        {
            if (_currentUser != null)
            {
                AuditLogService.LogAction(
                    username: _currentUser.Username,
                    action: "LOGOUT",
                    tableName: "Users",
                    recordId: _currentUser.Id.ToString(),
                    reason: "User logout"
                );

                _currentUser = null;
            }
        }

        /// <summary>
        /// Aktueller angemeldeter Benutzer
        /// </summary>
        public static User GetCurrentUser() => _currentUser;

        /// <summary>
        /// Prüfe ob Benutzer angemeldet ist
        /// </summary>
        public static bool IsLoggedIn => _currentUser != null;

        /// <summary>
        /// Session-Timeout prüfen (15 Min Inaktivität)
        /// </summary>
        public static bool CheckSessionTimeout()
        {
            if (_currentUser == null)
                return false;

            var inactiveMinutes = (DateTime.Now - _lastActivityTime).TotalMinutes;
            if (inactiveMinutes > SESSION_TIMEOUT_MINUTES)
            {
                Logout();
                return true;  // Timeout occurred
            }

            _lastActivityTime = DateTime.Now;
            return false;
        }

        /// <summary>
        /// Berechtigungsprüfung für Funktion
        /// </summary>
        public static bool HasPermission(string permission)
        {
            if (_currentUser == null)
                return false;

            return _currentUser.Role switch
            {
                UserRole.Admin => true,  // Admin: Alles erlaubt
                UserRole.Manager => permission != "DELETE_USER" && permission != "SYSTEM_SETUP",
                UserRole.Lagerarbeiter => permission == "ADD_MATERIAL" || permission == "EDIT_MATERIAL" || permission == "VIEW",
                UserRole.ReadOnly => permission == "VIEW",
                _ => false
            };
        }

        /// <summary>
        /// Alle Benutzer auflisten (nur für Admin)
        /// </summary>
        public static List<User> GetAllUsers()
        {
            if (_currentUser?.Role != UserRole.Admin)
                return new List<User>();  // Nur Admin darf sehen

            return _users;
        }

        /// <summary>
        /// Neuen Benutzer erstellen
        /// </summary>
        public static bool CreateUser(string username, string password, string displayName, UserRole role)
        {
            if (_currentUser?.Role != UserRole.Admin)
                return false;  // Nur Admin

            if (_users.Any(u => u.Username == username))
                return false;  // Existiert schon

            var newUser = new User
            {
                Id = _users.Count + 1,
                Username = username,
                DisplayName = displayName,
                PasswordHash = BCryptHashPassword(password),
                Role = role,
                IsActive = true
            };

            _users.Add(newUser);

            AuditLogService.LogAction(
                username: _currentUser.Username,
                action: "CREATE",
                tableName: "Users",
                recordId: newUser.Id.ToString(),
                newValue: $"Username: {username}, Role: {role}",
                reason: "New user created"
            );

            return true;
        }

        /// <summary>
        /// Passwort ändern
        /// </summary>
        public static bool ChangePassword(string oldPassword, string newPassword)
        {
            if (_currentUser == null)
                return false;

            // Alt-Passwort prüfen
            if (!VerifyPassword(oldPassword, _currentUser.PasswordHash))
                return false;

            // Neues Passwort setzen
            _currentUser.PasswordHash = BCryptHashPassword(newPassword);

            AuditLogService.LogAction(
                username: _currentUser.Username,
                action: "UPDATE",
                tableName: "Users",
                recordId: _currentUser.Id.ToString(),
                newValue: "Password changed",
                reason: "User changed password"
            );

            return true;
        }

        // ═══════════════════════════════════════════════════════════════
        // PASSWORT-HASHING (SHA256 für jetzt, bcrypt später)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Passwort hashen (SHA256 Standard, bcrypt optional)
        /// </summary>
        private static string BCryptHashPassword(string password)
        {
            // Verwende SHA256 (einfach, ohne extra Package)
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return System.Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// Passwort-Validierung
        /// </summary>
        private static bool VerifyPassword(string password, string hash)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hashOfInput = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var hashString = System.Convert.ToBase64String(hashOfInput);
                return hashString == hash;
            }
        }
    }
}
