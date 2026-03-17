# ============================================
# MaterialManager V01 - GUI Installer (FIXED)
# ============================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# ============================================
# VARIABLEN
# ============================================
$Script:CurrentStep = 1
$Script:TotalSteps = 6  # Erhöht von 5 auf 6 (neuer Preis-Screen)
$Script:InstallPath = "C:\Program Files\MaterialManager_V01"
$Script:SourcePath = Split-Path -Parent $MyInvocation.MyCommand.Path

# ============================================
# FORMULAR - 90% BILDSCHIRMGROESSE
# ============================================
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$formWidth = [int]($screen.Width * 0.9)
$formHeight = [int]($screen.Height * 0.9)

$form = New-Object System.Windows.Forms.Form
$form.Text = 'MaterialManager V01 - Installation'
$form.ClientSize = New-Object System.Drawing.Size($formWidth, $formHeight)
$form.StartPosition = 'CenterScreen'
$form.BackColor = [System.Drawing.Color]::FromArgb(20, 20, 20)
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false

# ============================================
# HEADER (DYNAMISCH ANGEPASST)
# ============================================
$titleLabel = New-Object System.Windows.Forms.Label
$titleLabel.Text = 'MaterialManager V01'
$titleLabel.Font = New-Object System.Drawing.Font('Segoe UI', 24, [System.Drawing.FontStyle]::Bold)
$titleLabel.ForeColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
$titleLabel.Location = New-Object System.Drawing.Point(30, 20)
$titleLabel.Size = New-Object System.Drawing.Size($formWidth - 60, 40)
$form.Controls.Add($titleLabel)

$subtitleLabel = New-Object System.Windows.Forms.Label
$subtitleLabel.Text = 'Professionelle Material- und Bestandsverwaltung'
$subtitleLabel.Font = New-Object System.Drawing.Font('Segoe UI', 11)
$subtitleLabel.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
$subtitleLabel.Location = New-Object System.Drawing.Point(32, 60)
$subtitleLabel.Size = New-Object System.Drawing.Size($formWidth - 60, 25)
$form.Controls.Add($subtitleLabel)

# ============================================
# CONTENT PANEL
# ============================================
$contentPanel = New-Object System.Windows.Forms.Panel
$contentPanel.Location = New-Object System.Drawing.Point(0, 100)
$contentPanel.Size = New-Object System.Drawing.Size(900, 400)
$contentPanel.BackColor = [System.Drawing.Color]::FromArgb(20, 20, 20)
$form.Controls.Add($contentPanel)

# ============================================
# PROGRESS BAR (FEST BEI Y=520)
# ============================================
$progressBar = New-Object System.Windows.Forms.ProgressBar
$progressBar.Location = New-Object System.Drawing.Point(30, 520)
$progressBar.Size = New-Object System.Drawing.Size(830, 8)
$progressBar.Value = 20
$form.Controls.Add($progressBar)

$progressLabel = New-Object System.Windows.Forms.Label
$progressLabel.Text = 'Schritt 1 von 5'
$progressLabel.Font = New-Object System.Drawing.Font('Segoe UI', 9)
$progressLabel.ForeColor = [System.Drawing.Color]::FromArgb(120, 120, 120)
$progressLabel.Location = New-Object System.Drawing.Point(30, 535)
$progressLabel.Size = New-Object System.Drawing.Size(200, 20)
$form.Controls.Add($progressLabel)

# ============================================
# BUTTONS (FEST BEI Y=560)
# ============================================
$nextButton = New-Object System.Windows.Forms.Button
$nextButton.Text = 'Weiter >'
$nextButton.Size = New-Object System.Drawing.Size(150, 50)
$nextButton.Location = New-Object System.Drawing.Point($formWidth - 180, $buttonY)
$nextButton.BackColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
$nextButton.ForeColor = [System.Drawing.Color]::White
$nextButton.FlatStyle = 'Flat'
$nextButton.Font = New-Object System.Drawing.Font('Segoe UI', 12, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($nextButton)

$backButton = New-Object System.Windows.Forms.Button
$backButton.Text = '< Zurueck'
$backButton.Size = New-Object System.Drawing.Size(150, 50)
$backButton.Location = New-Object System.Drawing.Point($formWidth - 350, $buttonY)
$backButton.BackColor = [System.Drawing.Color]::FromArgb(60, 60, 60)
$backButton.ForeColor = [System.Drawing.Color]::White
$backButton.FlatStyle = 'Flat'
$backButton.Font = New-Object System.Drawing.Font('Segoe UI', 12)
$backButton.Enabled = $false
$form.Controls.Add($backButton)

# ============================================
# FUNKTIONEN
# ============================================
function Update-Progress {
    param([int]$Step)
    $Script:CurrentStep = $Step
    $progressBar.Value = ($Step / $Script:TotalSteps) * 100
    $progressLabel.Text = "Schritt $Step von $Script:TotalSteps"
}

function Clear-Content {
    $contentPanel.Controls.Clear()
}

# ============================================
# SCREEN 1: WILLKOMMEN
# ============================================
function Show-WelcomeScreen {
    Clear-Content
    Update-Progress 1
    
    $welcomeLabel = New-Object System.Windows.Forms.Label
    $welcomeLabel.Text = @"
Willkommen beim MaterialManager V01 Installer!

Version: 1.0.7
Hersteller: Alexander Hoelzer
Copyright (c) 2026

Diese Software installiert MaterialManager V01 auf Ihrem Computer.

MaterialManager V01 ist eine professionelle Loesung zur
Material- und Bestandsverwaltung fuer Industriebetriebe.

Klicken Sie auf 'Weiter' um fortzufahren.
"@
    $welcomeLabel.Font = New-Object System.Drawing.Font('Segoe UI', 13)
    $welcomeLabel.ForeColor = [System.Drawing.Color]::FromArgb(200, 200, 200)
    $welcomeLabel.Location = New-Object System.Drawing.Point(50, 50)
    $welcomeLabel.Size = New-Object System.Drawing.Size(800, 300)
    $contentPanel.Controls.Add($welcomeLabel)
    
    $backButton.Enabled = $false
}

# ============================================
# SCREEN 2: PREISE & LIZENZMODELLE (INTERAKTIV)
# ============================================
function Show-PricingScreen {
    Clear-Content
    Update-Progress 2
    
    $titleLabel = New-Object System.Windows.Forms.Label
    $titleLabel.Text = 'Lizenzmodell waehlen'
    $titleLabel.Font = New-Object System.Drawing.Font('Segoe UI', 22, [System.Drawing.FontStyle]::Bold)
    $titleLabel.ForeColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
    $titleLabel.Location = New-Object System.Drawing.Point(30, 20)
    $titleLabel.Size = New-Object System.Drawing.Size(800, 40)
    $contentPanel.Controls.Add($titleLabel)
    
    $infoLabel = New-Object System.Windows.Forms.Label
    $infoLabel.Text = 'Waehlen Sie Ihr gewuenschtes Lizenzmodell:'
    $infoLabel.Font = New-Object System.Drawing.Font('Segoe UI', 12)
    $infoLabel.ForeColor = [System.Drawing.Color]::FromArgb(180, 180, 180)
    $infoLabel.Location = New-Object System.Drawing.Point(30, 70)
    $infoLabel.Size = New-Object System.Drawing.Size(800, 30)
    $contentPanel.Controls.Add($infoLabel)
    
    # RADIO BUTTON 1: DEMO
    $Script:radioDEMO = New-Object System.Windows.Forms.RadioButton
    $Script:radioDEMO.Text = 'DEMO-VERSION (30 Tage kostenlos)'
    $Script:radioDEMO.Checked = $true
    $Script:radioDEMO.ForeColor = [System.Drawing.Color]::White
    $Script:radioDEMO.Font = New-Object System.Drawing.Font('Segoe UI', 14, [System.Drawing.FontStyle]::Bold)
    $Script:radioDEMO.Location = New-Object System.Drawing.Point(50, 120)
    $Script:radioDEMO.Size = New-Object System.Drawing.Size(500, 30)
    $contentPanel.Controls.Add($Script:radioDEMO)
    
    $demoInfo = New-Object System.Windows.Forms.Label
    $demoInfo.Text = '   Vollstaendig funktionsfaehig | Keine Kreditkarte | Upgrade jederzeit moeglich'
    $demoInfo.Font = New-Object System.Drawing.Font('Segoe UI', 10)
    $demoInfo.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
    $demoInfo.Location = New-Object System.Drawing.Point(50, 150)
    $demoInfo.Size = New-Object System.Drawing.Size(800, 20)
    $contentPanel.Controls.Add($demoInfo)
    
    # RADIO BUTTON 2: EINZELPLATZ
    $Script:radioSINGLE = New-Object System.Windows.Forms.RadioButton
    $Script:radioSINGLE.Text = 'EINZELPLATZ-LIZENZ - 299,00 EUR'
    $Script:radioSINGLE.ForeColor = [System.Drawing.Color]::White
    $Script:radioSINGLE.Font = New-Object System.Drawing.Font('Segoe UI', 14, [System.Drawing.FontStyle]::Bold)
    $Script:radioSINGLE.Location = New-Object System.Drawing.Point(50, 190)
    $Script:radioSINGLE.Size = New-Object System.Drawing.Size(500, 30)
    $contentPanel.Controls.Add($Script:radioSINGLE)
    
    $singleInfo = New-Object System.Windows.Forms.Label
    $singleInfo.Text = '   1 PC | 12 Monate Support | Updates (1 Jahr) | Hardware-gebunden'
    $singleInfo.Font = New-Object System.Drawing.Font('Segoe UI', 10)
    $singleInfo.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
    $singleInfo.Location = New-Object System.Drawing.Point(50, 220)
    $singleInfo.Size = New-Object System.Drawing.Size(800, 20)
    $contentPanel.Controls.Add($singleInfo)
    
    # RADIO BUTTON 3: MEHRPLATZ
    $Script:radioMULTI = New-Object System.Windows.Forms.RadioButton
    $Script:radioMULTI.Text = 'MEHRPLATZ-LIZENZ (5 PCs) - 1.199,00 EUR'
    $Script:radioMULTI.ForeColor = [System.Drawing.Color]::White
    $Script:radioMULTI.Font = New-Object System.Drawing.Font('Segoe UI', 14, [System.Drawing.FontStyle]::Bold)
    $Script:radioMULTI.Location = New-Object System.Drawing.Point(50, 260)
    $Script:radioMULTI.Size = New-Object System.Drawing.Size(600, 30)
    $contentPanel.Controls.Add($Script:radioMULTI)
    
    $multiInfo = New-Object System.Windows.Forms.Label
    $multiInfo.Text = '   5 Lizenzen (je 239,80 EUR/PC) | 12 Monate Support | Netzwerk-Modus'
    $multiInfo.Font = New-Object System.Drawing.Font('Segoe UI', 10)
    $multiInfo.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
    $multiInfo.Location = New-Object System.Drawing.Point(50, 290)
    $multiInfo.Size = New-Object System.Drawing.Size(800, 20)
    $contentPanel.Controls.Add($multiInfo)
    
    # RADIO BUTTON 4: UNTERNEHMEN
    $Script:radioENT = New-Object System.Windows.Forms.RadioButton
    $Script:radioENT.Text = 'UNTERNEHMENSLIZENZ (10+ PCs) - Auf Anfrage'
    $Script:radioENT.ForeColor = [System.Drawing.Color]::White
    $Script:radioENT.Font = New-Object System.Drawing.Font('Segoe UI', 14, [System.Drawing.FontStyle]::Bold)
    $Script:radioENT.Location = New-Object System.Drawing.Point(50, 330)
    $Script:radioENT.Size = New-Object System.Drawing.Size(650, 30)
    $contentPanel.Controls.Add($Script:radioENT)
    
    $entInfo = New-Object System.Windows.Forms.Label
    $entInfo.Text = '   Individuelles Angebot | Prioritaets-Support | Schulungen'
    $entInfo.Font = New-Object System.Drawing.Font('Segoe UI', 10)
    $entInfo.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
    $entInfo.Location = New-Object System.Drawing.Point(50, 360)
    $entInfo.Size = New-Object System.Drawing.Size(800, 20)
    $contentPanel.Controls.Add($entInfo)
    
    # HINWEIS BOX
    $hinweisLabel = New-Object System.Windows.Forms.Label
    $hinweisLabel.Text = @"
Kontakt fuer Lizenzbestellung:
Alexander Hoelzer | Pfarrer-Rosenkranz-Str. 9 | 56642 Kruft
E-Mail: info@hoelzer.de | Privat: hoelzer_alex@yahoo.de

RABATT: 10% bei Bestellung innerhalb 7 Tagen! (Code: DEMO2026)
"@
    $hinweisLabel.Font = New-Object System.Drawing.Font('Consolas', 9)
    $hinweisLabel.ForeColor = [System.Drawing.Color]::FromArgb(255, 215, 0)
    $hinweisLabel.BackColor = [System.Drawing.Color]::FromArgb(40, 40, 40)
    $hinweisLabel.Location = New-Object System.Drawing.Point(30, 395)
    $hinweisLabel.Size = New-Object System.Drawing.Size(840, 60)
    $hinweisLabel.BorderStyle = 'FixedSingle'
    $contentPanel.Controls.Add($hinweisLabel)
    
    $backButton.Enabled = $true
}

# ============================================
# SCREEN 3: LIZENZ
# ============================================
function Show-LicenseScreen {
    Clear-Content
    Update-Progress 3
    
    $licenseBox = New-Object System.Windows.Forms.TextBox
    $licenseBox.Multiline = $true
    $licenseBox.ScrollBars = 'Vertical'
    $licenseBox.ReadOnly = $true
    $licenseBox.Location = New-Object System.Drawing.Point(30, 20)
    $licenseBox.Size = New-Object System.Drawing.Size(840, 320)
    $licenseBox.BackColor = [System.Drawing.Color]::FromArgb(30, 30, 30)
    $licenseBox.ForeColor = [System.Drawing.Color]::FromArgb(200, 200, 200)
    $licenseBox.Font = New-Object System.Drawing.Font('Consolas', 9)
    $licenseBox.Text = @"
================================================================================
        END-USER LICENSE AGREEMENT (EULA) - LIZENZVEREINBARUNG
                    MaterialManager V01 Version 1.0.6
================================================================================

COPYRIGHT (c) 2026 Alexander Hoelzer. Alle Rechte vorbehalten.

LIZENZGEBER:
Alexander Hoelzer
Pfarrer-Rosenkranz-Str. 9
56642 Kruft
Deutschland
E-Mail: info@hoelzer.de

================================================================================
1. VERTRAGSGEGENSTAND UND LIZENZGEWAEHRUNG
================================================================================

1.1 Diese Lizenzvereinbarung regelt die Nutzung der Software "MaterialManager 
    V01" (nachfolgend "Software") zwischen dem Lizenzgeber Alexander Hoelzer 
    und dem Endnutzer (nachfolgend "Lizenznehmer").

1.2 Der Lizenzgeber gewaehrt dem Lizenznehmer das nicht-ausschliessliche, 
    nicht uebertragbare und zeitlich beschraenkte Recht, die Software auf 
    einem (1) Computer zu installieren und zu nutzen.

1.3 Jede Installation erfordert einen individuellen, hardware-gebundenen 
    Lizenzschluessel. Ein Lizenzschluessel berechtigt zur Nutzung auf 
    genau einem Computer.

================================================================================
2. URHEBERRECHT UND EIGENTUMSRECHTE
================================================================================

2.1 Die Software ist urheberrechtlich geschuetzt. Saemtliche Rechte an der 
    Software, einschliesslich Quellcode, Dokumentation und Datenbank-Schema, 
    verbleiben beim Lizenzgeber.

2.2 Der Lizenznehmer erwirbt lediglich ein eingeschraenktes Nutzungsrecht. 
    Ein Eigentumserwerb an der Software findet nicht statt.

================================================================================
3. NUTZUNGSRECHTE UND NUTZUNGSBESCHRAENKUNGEN
================================================================================

3.1 ERLAUBT IST:
    - Installation auf einem (1) Computer fuer geschaeftliche Zwecke
    - Erstellung von Sicherungskopien fuer eigene Archivzwecke
    - Nutzung durch autorisierte Mitarbeiter des Lizenznehmers

3.2 STRENG VERBOTEN IST:
    - Vervielfaeltigung, Verbreitung oder oeffentliche Zugaenglichmachung
    - Weitergabe des Lizenzschluessels an Dritte
    - Dekompilierung, Disassemblierung oder Reverse Engineering
    - Entfernung oder Veraenderung von Copyright-Vermerken
    - Vermietung, Verleih oder Lizenzierung an Dritte
    - Nutzung auf mehr als einem Computer pro Lizenz

3.3 Verstoesse gegen diese Nutzungsbeschraenkungen berechtigen den Lizenzgeber 
    zur fristlosen Kuendigung und Geltendmachung von Schadensersatz.

================================================================================
4. GEWAEHRLEISTUNG UND MAENGELRECHTE
================================================================================

4.1 Der Lizenzgeber gewaehrleistet, dass die Software bei vertragsgemaeßer 
    Nutzung die dokumentierten Funktionen erbringt.

4.2 Gesetzliche Gewaehrleistungsrechte (§§ 434 ff. BGB) bleiben ungekuehrt:
    - Bei Unternehmern (B2B): 12 Monate ab Lieferung
    - Bei Verbrauchern (B2C): 24 Monate ab Lieferung (gesetzlich)

4.3 Der Lizenzgeber leistet Gewaehrleistung durch Fehlerbehebung oder 
    Bereitstellung einer korrigierten Version. Bei Fehlschlag steht dem 
    Lizenznehmer das Recht auf Minderung oder Ruecktritt zu.

4.4 KEINE Gewaehrleistung besteht bei:
    - Unsachgemaesser Bedienung oder Installation
    - Veraenderungen durch Dritte oder den Lizenznehmer
    - Inkompatibilitaet mit Drittsoftware
    - Hardware-Defekten oder Betriebssystemfehlern

================================================================================
5. HAFTUNGSBESCHRAENKUNG
================================================================================

5.1 Der Lizenzgeber haftet UNBESCHRAENKT bei:
    - Vorsatz und grober Fahrlaessigkeit
    - Verletzung von Leben, Koerper oder Gesundheit
    - Nach dem Produkthaftungsgesetz
    - Bei Uebernahme einer Garantie

5.2 Bei erster Fahrlaessigkeit haftet der Lizenzgeber NUR bei Verletzung 
    wesentlicher Vertragspflichten (Kardinalpflichten) und beschraenkt auf 
    den vorhersehbaren, vertragstypischen Schaden.

5.3 Datenverluste werden nur ersetzt, soweit sie bei ordnungsgemaesser 
    Datensicherung nicht wiederherstellbar gewesen waeren.

5.4 Diese Haftungsbeschraenkung gilt auch fuer persoenliche Haftung von 
    Mitarbeitern, Vertretern und Erfuellungsgehilfen.

================================================================================
6. TECHNISCHER SUPPORT UND WARTUNG
================================================================================

6.1 Der Lizenznehmer erhaelt 12 Monate technischen Support ab Kaufdatum:
    - E-Mail-Support: info@hoelzer.de
    - Reaktionszeit: 2 Werktage
    - Kostenlose Updates und Fehlerbehebungen

6.2 Support umfasst NICHT:
    - Schulungen oder Anpassungen
    - Fehler durch unsachgemaesse Nutzung
    - Kosten fuer Drittanbieter oder Hardware

================================================================================
7. WIDERRUFSRECHT (NUR VERBRAUCHER)
================================================================================

7.1 Verbraucher (Privatpersonen) haben ein 14-taegiges Widerrufsrecht ab 
    Vertragsschluss (§ 312g BGB, § 355 BGB).

7.2 Das Widerrufsrecht ERLISCHT vorzeitig bei Download digitaler Inhalte, 
    wenn der Verbraucher ausdruecklich zugestimmt hat und seine Kenntnis 
    bestaetigt, dass er das Widerrufsrecht verliert (§ 356 Abs. 5 BGB).

7.3 UNTERNEHMER haben KEIN Widerrufsrecht.

================================================================================
8. DATENSCHUTZ (DSGVO)
================================================================================

8.1 Die Software speichert folgende Daten lokal:
    - Lizenzschluessel und Hardware-ID (zur Lizenzvalidierung)
    - Bestandsdaten und Materialien (Geschaeftsdaten des Nutzers)
    - Protokolldateien (Fehlerdiagnose)

8.2 Es erfolgt KEINE Datenuebertragung an Dritte oder ins Internet, 
    ausser bei Lizenzaktivierung (einmalig Hardware-ID an Lizenzserver).

8.3 Verantwortlich fuer Datenverarbeitung:
    Alexander Hoelzer, Pfarrer-Rosenkranz-Str. 9, 56642 Kruft
    Datenschutzanfragen: info@hoelzer.de

8.4 Betroffenenrechte (Art. 15-22 DSGVO): Auskunft, Berichtigung, Loeschung, 
    Einschraenkung, Datenportabilitaet, Widerspruch.
    Beschwerderecht bei Datenschutzbehoerde.

================================================================================
9. LAUFZEIT UND KUENDIGUNG
================================================================================

9.1 Die Lizenz ist zeitlich UNBEGRENZT, sofern nicht gekuendigt.

9.2 Ordentliche Kuendigung durch den Lizenznehmer jederzeit moeglich 
    durch Deinstallation und Vernichtung aller Kopien.

9.3 Ausserordentliche Kuendigung durch Lizenzgeber bei:
    - Schwerwiegendem Verstoss gegen Nutzungsbedingungen (Ziff. 3.2)
    - Zahlungsverzug
    - Insolvenz des Lizenznehmers

9.4 Bei Kuendigung erlischt das Nutzungsrecht sofort. Der Lizenznehmer muss 
    alle Kopien der Software loeschen und dies auf Verlangen schriftlich 
    bestaetigen.

================================================================================
10. SCHLUSSBESTIMMUNGEN
================================================================================

10.1 ANWENDBARES RECHT:
     Es gilt deutsches Recht unter Ausschluss des UN-Kaufrechts (CISG).

10.2 GERICHTSSTAND:
     - Bei Unternehmern: Amtsgericht/Landgericht Koblenz
     - Bei Verbrauchern: Gesetzlicher Gerichtsstand des Verbrauchers

10.3 SALVATORISCHE KLAUSEL:
     Sollten einzelne Bestimmungen unwirksam sein, bleibt die Wirksamkeit 
     der uebrigen Bestimmungen ungekuehrt. Unwirksame Klauseln werden durch 
     wirksame ersetzt, die dem wirtschaftlichen Zweck am naechsten kommen.

10.4 VERTRAGSSPRACHE:
     Diese Vereinbarung ist in deutscher Sprache verfasst. Bei Uebersetzungen 
     in andere Sprachen ist die deutsche Version massgeblich.

================================================================================
KONTAKT UND SUPPORT
================================================================================

Alexander Hoelzer
Pfarrer-Rosenkranz-Str. 9
56642 Kruft
Deutschland

E-Mail: info@hoelzer.de
Privat: hoelzer_alex@yahoo.de
Telefon: [Bitte bei Bedarf einfuegen]
Web: [Bitte bei Bedarf einfuegen]

Support-Zeiten: Montag - Freitag, 09:00 - 17:00 Uhr

================================================================================

Durch Klicken auf "Ich akzeptiere die Lizenzvereinbarung" erklaeren Sie sich 
mit allen Bedingungen einverstanden und schliessen einen rechtlich bindenden 
Vertrag mit dem Lizenzgeber.

Stand: Maerz 2026
"@
    $contentPanel.Controls.Add($licenseBox)
    
    $Script:acceptCheckbox = New-Object System.Windows.Forms.CheckBox
    $Script:acceptCheckbox.Text = 'Ich akzeptiere die Lizenzvereinbarung'
    $Script:acceptCheckbox.ForeColor = [System.Drawing.Color]::White
    $Script:acceptCheckbox.Font = New-Object System.Drawing.Font('Segoe UI', 11)
    $Script:acceptCheckbox.Location = New-Object System.Drawing.Point(30, 360)
    $Script:acceptCheckbox.Size = New-Object System.Drawing.Size(500, 30)
    $contentPanel.Controls.Add($Script:acceptCheckbox)
    
    $backButton.Enabled = $true
}

# ============================================
# SCREEN 3: PFAD
# ============================================
function Show-PathScreen {
    if (-not $Script:acceptCheckbox.Checked) {
        [System.Windows.Forms.MessageBox]::Show('Bitte akzeptieren Sie die Lizenz!', 'Fehler', 'OK', 'Warning')
        return
    }
    
    Clear-Content
    Update-Progress 3
    
    $pathLabel = New-Object System.Windows.Forms.Label
    $pathLabel.Text = 'Installationsordner:'
    $pathLabel.Font = New-Object System.Drawing.Font('Segoe UI', 12)
    $pathLabel.ForeColor = [System.Drawing.Color]::White
    $pathLabel.Location = New-Object System.Drawing.Point(30, 50)
    $pathLabel.Size = New-Object System.Drawing.Size(300, 30)
    $contentPanel.Controls.Add($pathLabel)
    
    $Script:pathTextBox = New-Object System.Windows.Forms.TextBox
    $Script:pathTextBox.Text = $Script:InstallPath
    $Script:pathTextBox.Location = New-Object System.Drawing.Point(30, 90)
    $Script:pathTextBox.Size = New-Object System.Drawing.Size(640, 30)
    $Script:pathTextBox.Font = New-Object System.Drawing.Font('Consolas', 11)
    $Script:pathTextBox.BackColor = [System.Drawing.Color]::FromArgb(40, 40, 40)
    $Script:pathTextBox.ForeColor = [System.Drawing.Color]::White
    $contentPanel.Controls.Add($Script:pathTextBox)
    
    $browseButton = New-Object System.Windows.Forms.Button
    $browseButton.Text = 'Durchsuchen'
    $browseButton.Location = New-Object System.Drawing.Point(690, 88)
    $browseButton.Size = New-Object System.Drawing.Size(150, 35)
    $browseButton.BackColor = [System.Drawing.Color]::FromArgb(60, 60, 60)
    $browseButton.ForeColor = [System.Drawing.Color]::White
    $browseButton.FlatStyle = 'Flat'
    $browseButton.Add_Click({
        $folderBrowser = New-Object System.Windows.Forms.FolderBrowserDialog
        if ($folderBrowser.ShowDialog() -eq 'OK') {
            $Script:pathTextBox.Text = $folderBrowser.SelectedPath + '\MaterialManager_V01'
        }
    })
    $contentPanel.Controls.Add($browseButton)
    
    $Script:desktopCheckbox = New-Object System.Windows.Forms.CheckBox
    $Script:desktopCheckbox.Text = 'Desktop-Verknuepfung erstellen'
    $Script:desktopCheckbox.Checked = $true
    $Script:desktopCheckbox.ForeColor = [System.Drawing.Color]::White
    $Script:desktopCheckbox.Font = New-Object System.Drawing.Font('Segoe UI', 11)
    $Script:desktopCheckbox.Location = New-Object System.Drawing.Point(30, 150)
    $Script:desktopCheckbox.Size = New-Object System.Drawing.Size(400, 30)
    $contentPanel.Controls.Add($Script:desktopCheckbox)
}

# ============================================
# SCREEN 4: INSTALLATION
# ============================================
function Show-InstallScreen {
    $Script:InstallPath = $Script:pathTextBox.Text
    
    Clear-Content
    Update-Progress 5
    $nextButton.Enabled = $false
    $backButton.Enabled = $false
    
    $statusLabel = New-Object System.Windows.Forms.Label
    $statusLabel.Text = 'Installation laeuft...'
    $statusLabel.Font = New-Object System.Drawing.Font('Segoe UI', 16, [System.Drawing.FontStyle]::Bold)
    $statusLabel.ForeColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
    $statusLabel.Location = New-Object System.Drawing.Point(30, 50)
    $statusLabel.Size = New-Object System.Drawing.Size(800, 40)
    $contentPanel.Controls.Add($statusLabel)
    
    $installProgress = New-Object System.Windows.Forms.ProgressBar
    $installProgress.Location = New-Object System.Drawing.Point(30, 120)
    $installProgress.Size = New-Object System.Drawing.Size(840, 30)
    $installProgress.Style = 'Continuous'
    $contentPanel.Controls.Add($installProgress)
    
    try {
        $sourceDir = "$Script:SourcePath\MaterialManager"
        $statusLabel.Text = 'Erstelle Verzeichnis...'
        $form.Refresh()
        
        if (-not (Test-Path $Script:InstallPath)) {
            New-Item -ItemType Directory -Path $Script:InstallPath -Force | Out-Null
        }
        $installProgress.Value = 30
        Start-Sleep -Milliseconds 300
        
        $statusLabel.Text = 'Kopiere Dateien...'
        $form.Refresh()
        Copy-Item -Path "$sourceDir\*" -Destination $Script:InstallPath -Recurse -Force
        
        $installProgress.Value = 70
        Start-Sleep -Milliseconds 400
        
        $statusLabel.Text = 'Erstelle Verknuepfung...'
        $form.Refresh()
        if ($Script:desktopCheckbox.Checked) {
            $shell = New-Object -ComObject WScript.Shell
            $desktop = [Environment]::GetFolderPath('Desktop')
            $shortcutPath = "$desktop\MaterialManager V01.lnk"
            $shortcut = $shell.CreateShortcut($shortcutPath)
            $shortcut.TargetPath = "$Script:InstallPath\MaterialManager_V01.exe"
            $shortcut.WorkingDirectory = $Script:InstallPath
            $shortcut.Description = "MaterialManager V01 - Material- und Bestandsverwaltung"
            $shortcut.IconLocation = "$Script:InstallPath\MaterialManager_V01.exe,0"
            $shortcut.WindowStyle = 1
            $shortcut.Save()
            
            # Freigabe COM-Objekt
            [System.Runtime.Interopservices.Marshal]::ReleaseComObject($shell) | Out-Null
        }
        
        $installProgress.Value = 80
        $statusLabel.Text = 'Erstelle Deinstaller...'
        $form.Refresh()
        
        # DEINSTALLER erstellen
        $uninstallScript = @"
@echo off
REM ============================================
REM MaterialManager V01 - DEINSTALLER
REM ============================================

echo.
echo ========================================
echo MaterialManager V01 - DEINSTALLATION
echo ========================================
echo.
echo Dieses Programm entfernt MaterialManager V01 vollstaendig von Ihrem Computer.
echo.
pause

REM Pruefen ob Admin-Rechte vorhanden
net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo.
    echo FEHLER: Administrator-Rechte erforderlich!
    echo Bitte Rechtsklick -^> Als Administrator ausfuehren.
    echo.
    pause
    exit /b 1
)

echo.
echo [1/5] Stoppe laufende Prozesse...
taskkill /F /IM MaterialManager_V01.exe >nul 2>&1

echo [2/5] Loesche Desktop-Verknuepfung...
del "%USERPROFILE%\Desktop\MaterialManager V01.lnk" >nul 2>&1

echo [3/5] Loesche Registry-Eintrag...
reg delete "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MaterialManager_V01" /f >nul 2>&1

echo [4/5] Loesche Benutzerdaten (optional)...
echo.
choice /C JN /M "Moechten Sie auch Ihre Benutzerdaten (Datenbank, Backups, Lizenzen) loeschen"
if errorlevel 2 goto KeepUserData
if errorlevel 1 goto DeleteUserData

:DeleteUserData
rd /S /Q "%LOCALAPPDATA%\MaterialManager_V01" >nul 2>&1
echo    - Benutzerdaten geloescht
goto DeleteInstallFolder

:KeepUserData
echo    - Benutzerdaten behalten
goto DeleteInstallFolder

:DeleteInstallFolder
echo [5/5] Loesche Installationsordner...
cd /d "%TEMP%"
rd /S /Q "$($Script:InstallPath)" >nul 2>&1

echo.
echo ========================================
echo DEINSTALLATION ABGESCHLOSSEN!
echo ========================================
echo.
echo MaterialManager V01 wurde erfolgreich entfernt.
echo.
echo Vielen Dank, dass Sie MaterialManager V01 verwendet haben!
echo Bei Fragen: info@hoelzer.de
echo.
pause
exit
"@
        [System.IO.File]::WriteAllText("$Script:InstallPath\UNINSTALL.bat", $uninstallScript)
        
        $installProgress.Value = 90
        $statusLabel.Text = 'Registriere in Systemsteuerung...'
        $form.Refresh()
        
        # REGISTRY-EINTRAG für "Programme & Features"
        try {
            $regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MaterialManager_V01"
            if (-not (Test-Path $regPath)) {
                New-Item -Path $regPath -Force | Out-Null
            }
            
            Set-ItemProperty -Path $regPath -Name "DisplayName" -Value "MaterialManager V01" -Type String
            Set-ItemProperty -Path $regPath -Name "DisplayVersion" -Value "1.0.6" -Type String
            Set-ItemProperty -Path $regPath -Name "Publisher" -Value "Alexander Hoelzer" -Type String
            Set-ItemProperty -Path $regPath -Name "InstallLocation" -Value $Script:InstallPath -Type String
            Set-ItemProperty -Path $regPath -Name "UninstallString" -Value "`"$Script:InstallPath\UNINSTALL.bat`"" -Type String
            Set-ItemProperty -Path $regPath -Name "DisplayIcon" -Value "$Script:InstallPath\MaterialManager_V01.exe,0" -Type String
            Set-ItemProperty -Path $regPath -Name "NoModify" -Value 1 -Type DWord
            Set-ItemProperty -Path $regPath -Name "NoRepair" -Value 1 -Type DWord
            Set-ItemProperty -Path $regPath -Name "EstimatedSize" -Value 50000 -Type DWord
        } catch {
            # Registry-Fehler nicht kritisch, Installation kann fortgesetzt werden
        }

        $installProgress.Value = 100
        Start-Sleep -Milliseconds 500
        
        Show-CompletionScreen
    } catch {
        [System.Windows.Forms.MessageBox]::Show("Fehler: $_", 'Installation fehlgeschlagen', 'OK', 'Error')
        $nextButton.Enabled = $true
    }
}

# ============================================
# SCREEN 5: FERTIG
# ============================================
function Show-CompletionScreen {
    Clear-Content
    Update-Progress 5
    
    $doneLabel = New-Object System.Windows.Forms.Label
    $doneLabel.Text = 'Installation erfolgreich abgeschlossen!'
    $doneLabel.Font = New-Object System.Drawing.Font('Segoe UI', 18, [System.Drawing.FontStyle]::Bold)
    $doneLabel.ForeColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
    $doneLabel.Location = New-Object System.Drawing.Point(30, 80)
    $doneLabel.Size = New-Object System.Drawing.Size(800, 40)
    $contentPanel.Controls.Add($doneLabel)
    
    $infoLabel = New-Object System.Windows.Forms.Label
    $infoLabel.Text = @"
MaterialManager V01 ist jetzt installiert.

Installationsort: $Script:InstallPath

Sie koennen das Programm ueber die Desktop-Verknuepfung starten.
"@
    $infoLabel.Font = New-Object System.Drawing.Font('Segoe UI', 12)
    $infoLabel.ForeColor = [System.Drawing.Color]::FromArgb(180, 180, 180)
    $infoLabel.Location = New-Object System.Drawing.Point(30, 150)
    $infoLabel.Size = New-Object System.Drawing.Size(800, 150)
    $contentPanel.Controls.Add($infoLabel)
    
    # AUTO-START CHECKBOX
    $Script:autoStartCheckbox = New-Object System.Windows.Forms.CheckBox
    $Script:autoStartCheckbox.Text = 'MaterialManager V01 jetzt starten'
    $Script:autoStartCheckbox.Checked = $true
    $Script:autoStartCheckbox.ForeColor = [System.Drawing.Color]::White
    $Script:autoStartCheckbox.Font = New-Object System.Drawing.Font('Segoe UI', 12, [System.Drawing.FontStyle]::Bold)
    $Script:autoStartCheckbox.Location = New-Object System.Drawing.Point(30, 320)
    $Script:autoStartCheckbox.Size = New-Object System.Drawing.Size(500, 30)
    $contentPanel.Controls.Add($Script:autoStartCheckbox)
    
    $nextButton.Text = 'Fertig'
    $nextButton.Enabled = $true
}

# ============================================
# BUTTON EVENTS
# ============================================
$nextButton.Add_Click({
    switch ($Script:CurrentStep) {
        1 { Show-PricingScreen }
        2 { Show-LicenseScreen }
        3 { Show-PathScreen }
        4 { Show-InstallScreen }
        5 { 
            # FERTIG-Button: Auto-Start wenn Checkbox aktiviert
            if ($Script:autoStartCheckbox.Checked) {
                try {
                    $exePath = "$Script:InstallPath\MaterialManager_V01.exe"
                    if (Test-Path $exePath) {
                        Start-Process $exePath -WorkingDirectory $Script:InstallPath
                    }
                } catch {
                    [System.Windows.Forms.MessageBox]::Show(
                        "App konnte nicht automatisch gestartet werden.`nBitte starten Sie die App manuell über die Desktop-Verknüpfung.",
                        "Info",
                        'OK',
                        'Information'
                    )
                }
            }
            $form.Close() 
        }
    }
})

$backButton.Add_Click({
    switch ($Script:CurrentStep) {
        2 { Show-WelcomeScreen }
        3 { Show-PricingScreen }
        4 { Show-LicenseScreen }
    }
})

# ============================================
# START
# ============================================
Show-WelcomeScreen
[void]$form.ShowDialog()
