# Regal-Konzept - MaterialManager R03

## Übersicht

Das System verwaltet 10 spezialisierte Regale plus EU Paletten für verschiedene Materialtypen.

## Regal-Anordnung

### Regal A - Mittelformat (MF)
- **Kapazität**: 800 kg
- **Materialien**: Nur Mittelformat (MF) aller Art

### Regal B - Aluminium/Edelstahl ab 6mm
- **Kapazität**: 1.500 kg
- **Materialien**: 
  - Aluminium (alle Legierungen) mit Stärke ≥ 6mm
  - Edelstahl (alle Legierungen) mit Stärke ≥ 6mm
- **Priorität**: HÖCHSTE (wird vor allen anderen Regeln geprüft)

### Regal C - Hochfeste Stähle (GF)
- **Kapazität**: 1.000 kg
- **Materialien**: 
  - Nur Großformat (GF)
  - Nur Stahl mit Legierungen: S355, S460, HB400, HB500

### Regal D - Niedrigfeste Stähle (GF)
- **Kapazität**: 1.000 kg
- **Materialien**: 
  - Nur Großformat (GF)
  - Nur Stahl mit Legierung: S235

### Regal E - Großformat Sonstige
- **Kapazität**: 1.000 kg
- **Materialien**: 
  - Großformat (GF) von Aluminium oder Edelstahl
  - GF von unbekannten Stahl-Legierungen

### Regal F - Mittelformat (MF)
- **Kapazität**: 800 kg
- **Materialien**: Nur Mittelformat (MF) aller Art

### Regal G - Kleinformat (KF)
- **Kapazität**: 600 kg
- **Materialien**: Nur Kleinformat (KF) aller Art

### Regal H - Kleinformat (KF)
- **Kapazität**: 600 kg
- **Materialien**: Nur Kleinformat (KF) aller Art

### Regal I - Kleinformat (KF)
- **Kapazität**: 600 kg
- **Materialien**: Nur Kleinformat (KF) aller Art

### Regal J - Mittelformat (MF)
- **Kapazität**: 800 kg
- **Materialien**: Nur Mittelformat (MF) aller Art

### EU Palette
- **Kapazität**: 2.000 kg
- **Materialien**: 
  - Reste (ohne feste Form)
  - Kleine Materialien (≤ 1500x1300mm)
  - Fallback für nicht zuordenbare Materialien

## Zuordnungslogik (Priorität von oben nach unten)

```
1. Form = "MF"
   => Regal A, F oder J (automatisch das am wenigsten ausgelastete)
   (GILT IMMER, auch wenn Dimensionen klein sind!)

2. Form = "KF"
   => Regal G, H oder I (automatisch das am wenigsten ausgelastete)
   (GILT IMMER, auch wenn Dimensionen klein sind!)

3. Dimensionen ≤ 1500x1300mm
   => EU Palette
   (Für Reste und Materialien ohne feste Form)

4. Aluminium ODER Edelstahl UND Stärke ≥ 6mm
   => Regal B

5. Form = "GF" UND MaterialArt = "Stahl":
   - Legierung IN [S355, S460, HB400, HB500]
     => Regal C
   - Legierung = S235
     => Regal D
   - Andere Stahl-Legierung oder keine Angabe
     => Regal E

6. Form = "GF" UND MaterialArt ≠ "Stahl"
   => Regal E

7. Form = "Rest" mit großen Dimensionen (> 1500x1300):
   - Stahl-Reste:
     * Legierung IN [S355, S460, HB400, HB500] => Regal C
     * Legierung = S235 => Regal D
     * Sonstige Stahl-Legierungen => Regal E
   - Alu/Edelstahl-Reste:
     * Stärke ≥ 6mm => Regal B
     * Stärke < 6mm => Regal E

8. Standard (keine Form, keine Dimensionen)
   => EU Palette
```

## Beispiele

| Material | Legierung | Form | Stärke | Maß | Ziel | Grund |
|----------|-----------|------|--------|-----|------|-------|
| Stahl | S235 | MF | 3mm | 2500x1250 | Regal A/F/J | MF hat absolute Priorität |
| Stahl | S235 | MF | 3mm | 1400x1200 | Regal A/F/J | MF hat Vorrang vor Dimensionen! |
| Alu | EN AW-5754 | KF | 4mm | 2000x1000 | Regal G/H/I | KF hat absolute Priorität |
| Alu | EN AW-5754 | KF | 4mm | 1200x800 | Regal G/H/I | KF hat Vorrang vor Dimensionen! |
| Stahl | S235 | Rest | 5mm | 1400x1200 | EU Palette | Rest mit kleinen Dimensionen |
| Stahl | S355 | Rest | 10mm | 2000x1500 | Regal C | Großer Rest + S355 |
| Stahl | S235 | Rest | 8mm | 1800x1400 | Regal D | Großer Rest + S235 |
| Alu | EN AW-5754 | Rest | 8mm | 1200x800 | EU Palette | Rest mit kleinen Dimensionen |
| Alu | EN AW-5754 | Rest | 10mm | 2000x1500 | Regal B | Großer Rest + Alu ≥6mm |
| Alu | EN AW-5754 | Rest | 4mm | 2000x1500 | Regal E | Großer Rest + Alu <6mm |
| Alu | EN AW-5754 | MF | 8mm | 2500x1250 | Regal A/F/J | MF-Form hat Vorrang! |
| Edelstahl | 1.4301 | KF | 10mm | 2000x1000 | Regal G/H/I | KF-Form hat Vorrang! |
| Alu | EN AW-5754 | GF | 8mm | 3000x1500 | Regal B | GF + Alu ≥6mm |
| Stahl | S355 | GF | 2mm | 3000x1500 | Regal C | GF + S355 |
| Stahl | S235 | GF | 3mm | 3000x1500 | Regal D | GF + S235 |
| Edelstahl | 1.4301 | GF | 4mm | 3000x1500 | Regal E | GF + Edelstahl <6mm |

## Intelligente Last-Verteilung

### MF-Regale (A, F, J)
Bei jedem neuen MF-Material wird automatisch das Regal mit der **niedrigsten prozentualen Auslastung** gewählt:
- Beispiel: Regal A: 60%, Regal F: 45%, Regal J: 70% => Nächstes MF geht nach Regal F

### KF-Regale (G, H, I)
Bei jedem neuen KF-Material wird automatisch das Regal mit der **niedrigsten prozentualen Auslastung** gewählt:
- Beispiel: Regal G: 80%, Regal H: 60%, Regal I: 75% => Nächstes KF geht nach Regal H

## Technische Implementation

### Services/RegalService.cs
- Zentrale Verwaltung aller 10 Regale
- Kapazitätsberechnung pro Regal
- Automatische Zuordnung basierend auf Prioritäts-Regeln
- Intelligente Last-Verteilung für MF und KF
- Stahl-Klassifikation (niedrig/hoch)
- Persistierung der Kapazitäten

### Methoden
- `DetermineLagerort()`: Bestimmt automatisch das passende Regal basierend auf allen Parametern
- `FindBestMFRegal()`: Findet das am wenigsten ausgelastete MF-Regal (A, F, J)
- `FindBestKFRegal()`: Findet das am wenigsten ausgelastete KF-Regal (G, H, I)
- `GetRegalDescription()`: Liefert Beschreibung für jedes Regal
- `CalculateCurrentLoad()`: Berechnet aktuelle Beladung
- `GetAllLagerorte()`: Liefert alle verfügbaren Lagerorte
- `GetRegalGroups()`: Gruppiert Regale für die Anzeige

## Besondere Regeln

### Regel 1: Form-Zuordnung hat absolute Priorität für MF und KF
- **MF (Mittelformat)** geht IMMER in Regal A, F oder J
  - Auch wenn ein MF-Material nur 1400x1200mm hat (kleiner als Schwelle)
  - Die Form "MF" überschreibt die Dimensionsprüfung
  - Standard-Maß: 2500x1250mm

- **KF (Kleinformat)** geht IMMER in Regal G, H oder I
  - Auch wenn ein KF-Material nur 1200x800mm hat (kleiner als Schwelle)
  - Die Form "KF" überschreibt die Dimensionsprüfung
  - Standard-Maß: 2000x1000mm

### Regel 2: Dimensionsprüfung nur für Reste und GF
- Die 1500x1300mm Schwelle gilt nur für:
  - Reste (Form = "Rest")
  - GF-Materialien ohne spezielle Eigenschaften
  - Materialien ohne definierte Form

### Regel 3: GF-Stähle werden nach Festigkeit sortiert
- Hochfeste Stähle (S355+) => Regal C
- Baustähle (S235) => Regal D
- Verhindert Vermischung unterschiedlicher Qualitäten

### Regel 4: Aluminium/Edelstahl ≥6mm
- Gilt nur für GF-Materialien
- MF und KF gehen in ihre Standard-Regale, unabhängig von Stärke

### Regel 5: Last-Verteilung
- MF wird auf Regal A, F, J verteilt (am wenigsten ausgelastet)
- KF wird auf Regal G, H, I verteilt (am wenigsten ausgelastet)
- Automatische Auswahl des am wenigsten ausgelasteten Regals
- Vermeidet Überlastung einzelner Regale

## Regal-Übersicht

```
┌────────────┬─────────────────────────────────────────┬──────────┐
│ Regal      │ Zweck                                   │ Kapazität│
├────────────┼─────────────────────────────────────────┼──────────┤
│ A          │ MF (Mittelformat)                       │  800 kg  │
│ B          │ Alu/Edelstahl ≥6mm (ALLE Formen!)      │ 1500 kg  │
│ C          │ GF - Hochfeste Stähle (S355+)          │ 1000 kg  │
│ D          │ GF - Baustähle (S235)                  │ 1000 kg  │
│ E          │ GF - Sonstige                          │ 1000 kg  │
│ F          │ MF (Mittelformat)                       │  800 kg  │
│ G          │ KF (Kleinformat)                        │  600 kg  │
│ H          │ KF (Kleinformat)                        │  600 kg  │
│ I          │ KF (Kleinformat)                        │  600 kg  │
│ J          │ MF (Mittelformat)                       │  800 kg  │
│ EU Palette │ Reste & Kleine Materialien             │ 2000 kg  │
└────────────┴─────────────────────────────────────────┴──────────┘

Gesamt-Kapazität: 10.100 kg
```
