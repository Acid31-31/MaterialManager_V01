#!/usr/bin/env python3
"""
MaterialManager V01 - Lizenz-Verwaltung
Verwaltet und dokumentiert ausgegebene Lizenzen
"""

import json
import csv
import os
from datetime import datetime
from pathlib import Path

class LicenseManager:
    def __init__(self, db_file="licenses_issued.json"):
        self.db_file = db_file
        self.licenses = self.load_licenses()

    def load_licenses(self):
        """Lade Lizenzdatenbank"""
        if os.path.exists(self.db_file):
            with open(self.db_file, 'r', encoding='utf-8') as f:
                return json.load(f)
        return []

    def save_licenses(self):
        """Speichere Lizenzdatenbank"""
        with open(self.db_file, 'w', encoding='utf-8') as f:
            json.dump(self.licenses, f, indent=2, ensure_ascii=False)

    def add_license(self, hardware_id, company_name, license_key, years, notes=""):
        """Füge neue Lizenz hinzu"""
        from datetime import datetime, timedelta
        
        license_entry = {
            "id": len(self.licenses) + 1,
            "hardware_id": hardware_id,
            "company_name": company_name,
            "license_key": license_key,
            "issued_date": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
            "expiry_date": (datetime.now() + timedelta(days=365*years)).strftime("%Y-%m-%d"),
            "years": years,
            "notes": notes,
            "status": "active"
        }
        self.licenses.append(license_entry)
        self.save_licenses()
        return license_entry

    def list_licenses(self):
        """Liste alle Lizenzen auf"""
        print("\n┌──────────────────────────────────────────────────────────────────────┐")
        print("│ Ausgegebene Lizenzen                                                 │")
        print("├──────────────────────────────────────────────────────────────────────┤")
        
        if not self.licenses:
            print("│ Keine Lizenzen vorhanden                                             │")
        else:
            print(f"│ {'ID':<4} {'Firma':<25} {'Ablauf':<12} {'Status':<10} {'Noten':<15} │")
            print("├──────────────────────────────────────────────────────────────────────┤")
            
            for lic in self.licenses:
                company = lic['company_name'][:24]
                expiry = lic['expiry_date']
                status = lic['status']
                notes = lic.get('notes', '')[:14]
                print(f"│ {lic['id']:<4} {company:<25} {expiry:<12} {status:<10} {notes:<15} │")
        
        print("└──────────────────────────────────────────────────────────────────────┘\n")

    def get_license_details(self, license_id):
        """Zeige Lizenzdetails"""
        for lic in self.licenses:
            if lic['id'] == license_id:
                print("\n┌──────────────────────────────────────────────────────────────┐")
                print("│ Lizenzdetails                                                  │")
                print("├──────────────────────────────────────────────────────────────┤")
                print(f"│ ID:              {lic['id']}")
                print(f"│ Hardware-ID:     {lic['hardware_id']}")
                print(f"│ Firma:           {lic['company_name']}")
                print(f"│ Lizenzschlüssel: {lic['license_key']}")
                print(f"│ Ausstellungsdatum: {lic['issued_date']}")
                print(f"│ Ablaufdatum:     {lic['expiry_date']}")
                print(f"│ Laufzeit:        {lic['years']} Jahr(e)")
                print(f"│ Status:          {lic['status']}")
                print(f"│ Notizen:         {lic.get('notes', 'Keine')}")
                print("└──────────────────────────────────────────────────────────────┘\n")
                return
        
        print(f"✗ Lizenz mit ID {license_id} nicht gefunden!")

    def export_csv(self, filename="licenses_export.csv"):
        """Exportiere Lizenzen als CSV"""
        if not self.licenses:
            print("✗ Keine Lizenzen zum Exportieren!")
            return
        
        with open(filename, 'w', newline='', encoding='utf-8') as f:
            fieldnames = ['id', 'hardware_id', 'company_name', 'license_key', 
                         'issued_date', 'expiry_date', 'years', 'status', 'notes']
            writer = csv.DictWriter(f, fieldnames=fieldnames)
            writer.writeheader()
            writer.writerows(self.licenses)
        
        print(f"✓ Lizenzen exportiert: {filename}")

    def check_expiry(self):
        """Prüfe ablaufende Lizenzen"""
        from datetime import datetime, timedelta
        
        today = datetime.now()
        warning_date = today + timedelta(days=30)
        
        print("\n┌──────────────────────────────────────────────────────────────┐")
        print("│ Ablaufende Lizenzen (in den nächsten 30 Tagen)               │")
        print("├──────────────────────────────────────────────────────────────┤")
        
        expiring = []
        for lic in self.licenses:
            expiry = datetime.strptime(lic['expiry_date'], "%Y-%m-%d")
            if today <= expiry <= warning_date and lic['status'] == 'active':
                expiring.append(lic)
        
        if not expiring:
            print("│ Keine ablaufenden Lizenzen!                                  │")
        else:
            for lic in expiring:
                days_left = (datetime.strptime(lic['expiry_date'], "%Y-%m-%d") - today).days
                company = lic['company_name']
                print(f"│ {company:<30} - {days_left} Tage                │")
        
        print("└──────────────────────────────────────────────────────────────┘\n")


def main():
    manager = LicenseManager()
    
    while True:
        print("\n╔════════════════════════════════════════════════════════════════╗")
        print("║      MaterialManager V01 - Lizenzenverwaltung                  ║")
        print("╚════════════════════════════════════════════════════════════════╝\n")
        print("1) Lizenz hinzufügen")
        print("2) Alle Lizenzen anzeigen")
        print("3) Lizenzdetails anzeigen")
        print("4) Ablaufende Lizenzen prüfen")
        print("5) Als CSV exportieren")
        print("6) Beenden")
        print()
        
        choice = input("Wahl (1-6): ").strip()
        
        if choice == "1":
            print("\n--- Neue Lizenz hinzufügen ---")
            hw_id = input("Hardware-ID: ").strip()
            company = input("Firmenname: ").strip()
            license_key = input("Lizenzschlüssel: ").strip()
            try:
                years = int(input("Laufzeit (Jahre): ").strip())
                notes = input("Notizen (optional): ").strip()
                
                lic = manager.add_license(hw_id, company, license_key, years, notes)
                print(f"✓ Lizenz #{lic['id']} hinzugefügt!")
            except ValueError:
                print("✗ Ungültige Eingabe!")
        
        elif choice == "2":
            manager.list_licenses()
        
        elif choice == "3":
            manager.list_licenses()
            try:
                lic_id = int(input("Lizenz-ID anzeigen: ").strip())
                manager.get_license_details(lic_id)
            except ValueError:
                print("✗ Ungültige ID!")
        
        elif choice == "4":
            manager.check_expiry()
        
        elif choice == "5":
            filename = input("Dateiname (Standard: licenses_export.csv): ").strip()
            if not filename:
                filename = "licenses_export.csv"
            manager.export_csv(filename)
        
        elif choice == "6":
            print("Auf Wiedersehen!")
            break
        
        else:
            print("✗ Ungültige Wahl!")


if __name__ == "__main__":
    main()
