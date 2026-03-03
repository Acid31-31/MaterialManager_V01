# Copilot Instructions

## Project Guidelines
- User requires full project-level backups (complete folder snapshots) in the Backup directory before changes, not single-file backups in separate ad-hoc locations. This applies to all modifications to the codebase. Always create backups first, then make changes. Never delete or modify without a backup first. Always show what will be done and ask for confirmation on risky operations.

## License Key Generation
- The project uses two different license key generation mechanisms that MUST be unified:
  - `LicenseKeyGenerator.cs` uses HMAC-SHA256 with "MM_V01_MASTER_SECRET_2025_PRODUCTION" and produces `MM-XXXX-XXXX-XXXX-XXXX` format.
  - `LicenseService.cs` had its own `GenerateLicenseKey()` using plain SHA256, which created incompatibility.
- To ensure consistency, `LicenseService.ActivateFullLicense()` must call `LicenseKeyGenerator.GenerateLicenseKey()`.
