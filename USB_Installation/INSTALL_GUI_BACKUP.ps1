# ============================================
# MaterialManager V01 - GUI Installer (FIXED)
# ============================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# ============================================
# VARIABLEN
# ============================================
$Script:CurrentStep = 1
$Script:TotalSteps = 5
$Script:InstallPath = "C:\Program Files\MaterialManager_V01"
$Script:SourcePath = Split-Path -Parent $MyInvocation.MyCommand.Path

# ============================================
# FORMULAR - FESTE GROESSE
# ============================================
$form = New-Object System.Windows.Forms.Form
$form.Text = 'MaterialManager V01 - Installation'
$form.Size = New-Object System.Drawing.Size(900, 650)
$form.StartPosition = 'CenterScreen'
$form.BackColor = [System.Drawing.Color]::FromArgb(20, 20, 20)
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false

# ============================================
# HEADER
# ============================================
$titleLabel = New-Object System.Windows.Forms.Label
$titleLabel.Text = 'MaterialManager V01'
$titleLabel.Font = New-Object System.Drawing.Font('Segoe UI', 24, [System.Drawing.FontStyle]::Bold)
$titleLabel.ForeColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
$titleLabel.Location = New-Object System.Drawing.Point(30, 20)
$titleLabel.Size = New-Object System.Drawing.Size(800, 40)
$form.Controls.Add($titleLabel)

$subtitleLabel = New-Object System.Windows.Forms.Label
$subtitleLabel.Text = 'Professionelle Material- und Bestandsverwaltung'
$subtitleLabel.Font = New-Object System.Drawing.Font('Segoe UI', 11)
$subtitleLabel.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
$subtitleLabel.Location = New-Object System.Drawing.Point(32, 60)
$subtitleLabel.Size = New-Object System.Drawing.Size(800, 25)
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
$nextButton.Size = New-Object System.Drawing.Size(130, 40)
$nextButton.Location = New-Object System.Drawing.Point(730, 560)
$nextButton.BackColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
$nextButton.ForeColor = [System.Drawing.Color]::White
$nextButton.FlatStyle = 'Flat'
$nextButton.Font = New-Object System.Drawing.Font('Segoe UI', 11, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($nextButton)

$backButton = New-Object System.Windows.Forms.Button
$backButton.Text = '< Zurueck'
$backButton.Size = New-Object System.Drawing.Size(130, 40)
$backButton.Location = New-Object System.Drawing.Point(580, 560)
$backButton.BackColor = [System.Drawing.Color]::FromArgb(60, 60, 60)
$backButton.ForeColor = [System.Drawing.Color]::White
$backButton.FlatStyle = 'Flat'
$backButton.Font = New-Object System.Drawing.Font('Segoe UI', 11)
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

Version: 1.0.6
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
# SCREEN 2: LIZENZ
# ============================================
function Show-LicenseScreen {
    Clear-Content
    Update-Progress 2
    
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

5.2 Bei leichter Fahrlaessigkeit haftet der Lizenzgeber NUR bei Verletzung 
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
    Update-Progress 4
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
            $shortcut = $shell.CreateShortcut("$desktop\MaterialManager V01.lnk")
            $shortcut.TargetPath = "$Script:InstallPath\MaterialManager_V01.exe"
            $shortcut.WorkingDirectory = $Script:InstallPath
            $shortcut.Save()
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

Klicken Sie auf 'Fertig' um den Installer zu schliessen.
"@
    $infoLabel.Font = New-Object System.Drawing.Font('Segoe UI', 12)
    $infoLabel.ForeColor = [System.Drawing.Color]::FromArgb(180, 180, 180)
    $infoLabel.Location = New-Object System.Drawing.Point(30, 150)
    $infoLabel.Size = New-Object System.Drawing.Size(800, 200)
    $contentPanel.Controls.Add($infoLabel)
    
    $nextButton.Text = 'Fertig'
    $nextButton.Enabled = $true
}

# ============================================
# BUTTON EVENTS
# ============================================
$nextButton.Add_Click({
    switch ($Script:CurrentStep) {
        1 { Show-LicenseScreen }
        2 { Show-PathScreen }
        3 { Show-InstallScreen }
        5 { $form.Close() }
    }
})

$backButton.Add_Click({
    switch ($Script:CurrentStep) {
        2 { Show-WelcomeScreen }
        3 { Show-LicenseScreen }
    }
})

# ============================================
# START
# ============================================
Show-WelcomeScreen
[void]$form.ShowDialog()
