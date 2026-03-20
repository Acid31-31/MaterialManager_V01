# ============================================
# MaterialManager V01 - GUI Installer
# ============================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# ============================================
# VARIABLEN
# ============================================
$Script:CurrentStep = 1
$Script:TotalSteps = 6
$Script:InstallPath = "C:\Program Files\MaterialManager_V01"
$Script:SourcePath = Split-Path -Parent $MyInvocation.MyCommand.Path
$Script:VersionApiUrl = "https://api.github.com/repos/Acid31-31/MaterialManager_V01/releases/latest"
$Script:FallbackVersion = "1.0.29"

function Get-NormalizedVersion {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $Script:FallbackVersion
    }

    $match = [regex]::Match($Value, '\d+(\.\d+){0,3}')
    if ($match.Success) {
        return $match.Value
    }

    return $Script:FallbackVersion
}

function Get-PackageVersion {
    $packageExe = Join-Path $Script:SourcePath 'MaterialManager\MaterialManager_V01.exe'
    if (Test-Path $packageExe) {
        try {
            $versionInfo = (Get-Item $packageExe).VersionInfo
            if (-not [string]::IsNullOrWhiteSpace($versionInfo.ProductVersion)) {
                return Get-NormalizedVersion $versionInfo.ProductVersion
            }
            if (-not [string]::IsNullOrWhiteSpace($versionInfo.FileVersion)) {
                return Get-NormalizedVersion $versionInfo.FileVersion
            }
        } catch { }
    }

    return $Script:FallbackVersion
}

$Script:PackageVersion = Get-PackageVersion

function Get-LatestOnlineVersion {
    try {
        $response = Invoke-RestMethod -Uri $Script:VersionApiUrl -Headers @{ 'User-Agent' = 'MaterialManager-V01-Installer'; 'Accept' = 'application/vnd.github+json' } -TimeoutSec 8
        if ($response -and $response.tag_name) {
            return Get-NormalizedVersion $response.tag_name
        }
    } catch { }

    return $Script:PackageVersion
}

$Script:LatestOnlineVersion = Get-LatestOnlineVersion
$Script:DesignWidth = 1000
$Script:DesignHeight = 750
$workingArea = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$Script:formWidth = [int][Math]::Min($Script:DesignWidth, [Math]::Max(880, $workingArea.Width - 40))
$Script:formHeight = [int][Math]::Min($Script:DesignHeight, [Math]::Max(650, $workingArea.Height - 40))
$Script:scaleX = $Script:formWidth / [double]$Script:DesignWidth
$Script:scaleY = $Script:formHeight / [double]$Script:DesignHeight

function Set-ControlResponsiveLayout {
    param([System.Windows.Forms.Control]$Control)

    if ($null -eq $Control) { return }

    $meta = $Control.Tag
    if (-not ($meta -is [pscustomobject] -and $meta.ResponsiveMarker -eq $true)) {
        $meta = [pscustomobject]@{
            ResponsiveMarker = $true
            Left = $Control.Left
            Top = $Control.Top
            Width = $Control.Width
            Height = $Control.Height
            FontName = if ($Control.Font) { $Control.Font.FontFamily.Name } else { $null }
            FontSize = if ($Control.Font) { $Control.Font.Size } else { 0 }
            FontStyle = if ($Control.Font) { [int]$Control.Font.Style } else { 0 }
        }
        $Control.Tag = $meta
    }

    $Control.Left = [int][Math]::Round($meta.Left * $Script:scaleX)
    $Control.Top = [int][Math]::Round($meta.Top * $Script:scaleY)
    $Control.Width = [int][Math]::Round($meta.Width * $Script:scaleX)
    $Control.Height = [int][Math]::Round($meta.Height * $Script:scaleY)

    if ($meta.FontName -and $meta.FontSize -gt 0) {
        $scaledFontSize = [float][Math]::Max(8, [Math]::Round($meta.FontSize * [Math]::Min($Script:scaleX, $Script:scaleY), 1))
        if ($null -eq $Control.Font -or [Math]::Abs($Control.Font.Size - $scaledFontSize) -gt 0.1) {
            $Control.Font = New-Object System.Drawing.Font($meta.FontName, $scaledFontSize, [System.Drawing.FontStyle]$meta.FontStyle)
        }
    }

    if ($Control -is [System.Windows.Forms.ScrollableControl]) {
        $Control.AutoScroll = $true
    }

    foreach ($child in $Control.Controls) {
        Set-ControlResponsiveLayout $child
    }
}

function Apply-ResponsiveLayout {
    $form.ClientSize = New-Object System.Drawing.Size($Script:formWidth, $Script:formHeight)
    $contentPanel.AutoScroll = $true

    foreach ($control in $form.Controls) {
        Set-ControlResponsiveLayout $control
    }
}

# ============================================
# FORMULAR - FESTE GROESSE 1000x750 (vergroessert!)
# ============================================
$form = New-Object System.Windows.Forms.Form
$form.Text = 'MaterialManager V01 - Installation'
$form.ClientSize = New-Object System.Drawing.Size($Script:formWidth, $Script:formHeight)
$form.StartPosition = 'CenterScreen'
$form.BackColor = [System.Drawing.Color]::FromArgb(20, 20, 20)
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false
$form.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Dpi

# ============================================
# HEADER - FESTE WERTE (kein $formWidth mehr!)
# ============================================
$titleLabel = New-Object System.Windows.Forms.Label
$titleLabel.Text = 'MaterialManager V01'
$titleLabel.Font = New-Object System.Drawing.Font('Segoe UI', 24, [System.Drawing.FontStyle]::Bold)
$titleLabel.ForeColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
$titleLabel.Location = New-Object System.Drawing.Point(40, 20)
$titleLabel.Size = New-Object System.Drawing.Size(920, 50)  # ✅ 1000-80=920
$form.Controls.Add($titleLabel)

$subtitleLabel = New-Object System.Windows.Forms.Label
$subtitleLabel.Text = 'Professionelle Material- und Bestandsverwaltung'
$subtitleLabel.Font = New-Object System.Drawing.Font('Segoe UI', 12)
$subtitleLabel.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
$subtitleLabel.Location = New-Object System.Drawing.Point(42, 75)
$subtitleLabel.Size = New-Object System.Drawing.Size(920, 30)  # ✅ 1000-80=920
$form.Controls.Add($subtitleLabel)

# ============================================
# CONTENT PANEL - FESTE WERTE
# ============================================
$contentPanel = New-Object System.Windows.Forms.Panel
$contentPanel.Location = New-Object System.Drawing.Point(0, 120)
$contentPanel.Size = New-Object System.Drawing.Size(1000, 480)  # ✅ 750-270=480
$contentPanel.BackColor = [System.Drawing.Color]::FromArgb(20, 20, 20)
$form.Controls.Add($contentPanel)

# ============================================
# PROGRESS BAR - FESTE WERTE
# ============================================
$progressBar = New-Object System.Windows.Forms.ProgressBar
$progressBar.Location = New-Object System.Drawing.Point(40, 630)  # ✅ 750-120=630
$progressBar.Size = New-Object System.Drawing.Size(920, 10)  # ✅ 1000-80=920
$progressBar.Value = 0
$form.Controls.Add($progressBar)

$progressLabel = New-Object System.Windows.Forms.Label
$progressLabel.Text = 'Schritt 1 von 6'
$progressLabel.Font = New-Object System.Drawing.Font('Segoe UI', 9)
$progressLabel.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
$progressLabel.Location = New-Object System.Drawing.Point(40, 645)  # ✅ 630+15=645
$progressLabel.Size = New-Object System.Drawing.Size(400, 25)
$form.Controls.Add($progressLabel)

# ============================================
# BUTTONS - FESTE WERTE
# ============================================
$nextButton = New-Object System.Windows.Forms.Button
$nextButton.Text = 'Weiter >'
$nextButton.Size = New-Object System.Drawing.Size(160, 45)
$nextButton.Location = New-Object System.Drawing.Point(800, 680)  # ✅ 1000-200=800, 750-70=680
$nextButton.BackColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
$nextButton.ForeColor = [System.Drawing.Color]::White
$nextButton.FlatStyle = 'Flat'
$nextButton.Font = New-Object System.Drawing.Font('Segoe UI', 12, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($nextButton)

$backButton = New-Object System.Windows.Forms.Button
$backButton.Text = '< Zurueck'
$backButton.Size = New-Object System.Drawing.Size(160, 45)
$backButton.Location = New-Object System.Drawing.Point(620, 680)  # ✅ 1000-380=620, 750-70=680
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

Online aktuellste Version: $($Script:LatestOnlineVersion)
Paket-Version dieses Installers: $($Script:PackageVersion)
Hersteller: Alexander Hoelzer
Copyright (c) 2026

Diese Software installiert MaterialManager V01 auf Ihrem Computer.

MaterialManager V01 ist eine professionelle Loesung zur
Material- und Bestandsverwaltung fuer Industriebetriebe.

Klicken Sie auf 'Weiter' um fortzufahren.
"@
    $welcomeLabel.Font = New-Object System.Drawing.Font('Segoe UI', 14)
    $welcomeLabel.ForeColor = [System.Drawing.Color]::FromArgb(200, 200, 200)
    $welcomeLabel.Location = New-Object System.Drawing.Point(60, 80)
    $welcomeLabel.Size = New-Object System.Drawing.Size(880, 320)
    $contentPanel.Controls.Add($welcomeLabel)
    
    $backButton.Enabled = $false
    Apply-ResponsiveLayout
}

# ============================================
# SCREEN 2: PREISE (INTERAKTIV)
# ============================================
function Show-PricingScreen {
    Clear-Content
    Update-Progress 2
    
    $titleLabel = New-Object System.Windows.Forms.Label
    $titleLabel.Text = 'Lizenzmodell waehlen'
    $titleLabel.Font = New-Object System.Drawing.Font('Segoe UI', 20, [System.Drawing.FontStyle]::Bold)
    $titleLabel.ForeColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
    $titleLabel.Location = New-Object System.Drawing.Point(40, 20)
    $titleLabel.Size = New-Object System.Drawing.Size(920, 40)  # ✅ 1000-80=920
    $contentPanel.Controls.Add($titleLabel)
    
    $infoLabel = New-Object System.Windows.Forms.Label
    $infoLabel.Text = 'Waehlen Sie Ihr gewuenschtes Lizenzmodell:'
    $infoLabel.Font = New-Object System.Drawing.Font('Segoe UI', 11)
    $infoLabel.ForeColor = [System.Drawing.Color]::FromArgb(180, 180, 180)
    $infoLabel.Location = New-Object System.Drawing.Point(40, 70)
    $infoLabel.Size = New-Object System.Drawing.Size(920, 30)  # ✅ 1000-80=920
    $contentPanel.Controls.Add($infoLabel)
    
    # RADIO BUTTON 1: DEMO
    $Script:radioDEMO = New-Object System.Windows.Forms.RadioButton
    $Script:radioDEMO.Text = 'DEMO-VERSION (30 Tage kostenlos)'
    $Script:radioDEMO.Checked = $true
    $Script:radioDEMO.ForeColor = [System.Drawing.Color]::White
    $Script:radioDEMO.Font = New-Object System.Drawing.Font('Segoe UI', 13, [System.Drawing.FontStyle]::Bold)
    $Script:radioDEMO.Location = New-Object System.Drawing.Point(50, 120)
    $Script:radioDEMO.Size = New-Object System.Drawing.Size(900, 30)  # ✅ Volle Breite
    $contentPanel.Controls.Add($Script:radioDEMO)
    
    $demoInfo = New-Object System.Windows.Forms.Label
    $demoInfo.Text = '   Vollstaendig funktionsfaehig | Keine Kreditkarte | Upgrade jederzeit moeglich'
    $demoInfo.Font = New-Object System.Drawing.Font('Segoe UI', 10)
    $demoInfo.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
    $demoInfo.Location = New-Object System.Drawing.Point(50, 150)
    $demoInfo.Size = New-Object System.Drawing.Size(900, 25)  # ✅ 1000-100=900
    $contentPanel.Controls.Add($demoInfo)
    
    # RADIO BUTTON 2: EINZELPLATZ
    $Script:radioSINGLE = New-Object System.Windows.Forms.RadioButton
    $Script:radioSINGLE.Text = 'EINZELPLATZ-LIZENZ - 299,00 EUR'
    $Script:radioSINGLE.ForeColor = [System.Drawing.Color]::White
    $Script:radioSINGLE.Font = New-Object System.Drawing.Font('Segoe UI', 13, [System.Drawing.FontStyle]::Bold)
    $Script:radioSINGLE.Location = New-Object System.Drawing.Point(50, 190)
    $Script:radioSINGLE.Size = New-Object System.Drawing.Size(900, 30)  # ✅ Volle Breite
    $contentPanel.Controls.Add($Script:radioSINGLE)
    
    $singleInfo = New-Object System.Windows.Forms.Label
    $singleInfo.Text = '   1 PC | 12 Monate Support | Updates (1 Jahr) | Hardware-gebunden'
    $singleInfo.Font = New-Object System.Drawing.Font('Segoe UI', 10)
    $singleInfo.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
    $singleInfo.Location = New-Object System.Drawing.Point(50, 220)
    $singleInfo.Size = New-Object System.Drawing.Size(900, 25)  # ✅ 1000-100=900
    $contentPanel.Controls.Add($singleInfo)
    
    # RADIO BUTTON 3: MEHRPLATZ
    $Script:radioMULTI = New-Object System.Windows.Forms.RadioButton
    $Script:radioMULTI.Text = 'MEHRPLATZ-LIZENZ (5 PCs) - 1.199,00 EUR'
    $Script:radioMULTI.ForeColor = [System.Drawing.Color]::White
    $Script:radioMULTI.Font = New-Object System.Drawing.Font('Segoe UI', 13, [System.Drawing.FontStyle]::Bold)
    $Script:radioMULTI.Location = New-Object System.Drawing.Point(50, 260)
    $Script:radioMULTI.Size = New-Object System.Drawing.Size(900, 30)  # ✅ Volle Breite
    $contentPanel.Controls.Add($Script:radioMULTI)
    
    $multiInfo = New-Object System.Windows.Forms.Label
    $multiInfo.Text = '   5 Lizenzen (je 239,80 EUR/PC) | 12 Monate Support | Netzwerk-Modus'
    $multiInfo.Font = New-Object System.Drawing.Font('Segoe UI', 10)
    $multiInfo.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
    $multiInfo.Location = New-Object System.Drawing.Point(50, 290)
    $multiInfo.Size = New-Object System.Drawing.Size(900, 25)  # ✅ 1000-100=900
    $contentPanel.Controls.Add($multiInfo)
    
    # RADIO BUTTON 4: UNTERNEHMEN
    $Script:radioENT = New-Object System.Windows.Forms.RadioButton
    $Script:radioENT.Text = 'UNTERNEHMENSLIZENZ (10+ PCs) - Auf Anfrage'
    $Script:radioENT.ForeColor = [System.Drawing.Color]::White
    $Script:radioENT.Font = New-Object System.Drawing.Font('Segoe UI', 13, [System.Drawing.FontStyle]::Bold)
    $Script:radioENT.Location = New-Object System.Drawing.Point(50, 330)
    $Script:radioENT.Size = New-Object System.Drawing.Size(900, 30)  # ✅ Volle Breite
    $contentPanel.Controls.Add($Script:radioENT)
    
    $entInfo = New-Object System.Windows.Forms.Label
    $entInfo.Text = '   Individuelles Angebot | Prioritaets-Support | Schulungen'
    $entInfo.Font = New-Object System.Drawing.Font('Segoe UI', 10)
    $entInfo.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
    $entInfo.Location = New-Object System.Drawing.Point(50, 360)
    $entInfo.Size = New-Object System.Drawing.Size(900, 25)  # ✅ 1000-100=900
    $contentPanel.Controls.Add($entInfo)
    
    # HINWEIS BOX - UNTEN
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
    $hinweisLabel.Location = New-Object System.Drawing.Point(40, 400)  # ✅ 480-80=400
    $hinweisLabel.Size = New-Object System.Drawing.Size(920, 80)  # ✅ 1000-80=920
    $hinweisLabel.BorderStyle = 'FixedSingle'
    $contentPanel.Controls.Add($hinweisLabel)
    
    $backButton.Enabled = $true
    Apply-ResponsiveLayout
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
    $licenseBox.Location = New-Object System.Drawing.Point(40, 20)
    $licenseBox.Size = New-Object System.Drawing.Size(920, 360)  # ✅ 1000-80=920, 480-120=360
    $licenseBox.BackColor = [System.Drawing.Color]::FromArgb(30, 30, 30)
    $licenseBox.ForeColor = [System.Drawing.Color]::FromArgb(200, 200, 200)
    $licenseBox.Font = New-Object System.Drawing.Font('Consolas', 9)
    $licenseBox.Text = @"
================================================================================
        END-USER LICENSE AGREEMENT (EULA) - LIZENZVEREINBARUNG
                 MaterialManager V01 Paket-Version $($Script:PackageVersion)
================================================================================

COPYRIGHT (c) 2026 Alexander Hoelzer. Alle Rechte vorbehalten.

LIZENZGEBER:
Alexander Hoelzer
Pfarrer-Rosenkranz-Str. 9
56642 Kruft
Deutschland
E-Mail: info@hoelzer.de
Privat: hoelzer_alex@yahoo.de

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

3.1 ERLAUBTIST:
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

Stand: Maerz 2026
"@
    $contentPanel.Controls.Add($licenseBox)
    
    $Script:acceptCheckbox = New-Object System.Windows.Forms.CheckBox
    $Script:acceptCheckbox.Text = 'Ich akzeptiere die Lizenzvereinbarung'
    $Script:acceptCheckbox.ForeColor = [System.Drawing.Color]::White
    $Script:acceptCheckbox.Font = New-Object System.Drawing.Font('Segoe UI', 12)
    $Script:acceptCheckbox.Location = New-Object System.Drawing.Point(40, 390)  # ✅ 480-90=390
    $Script:acceptCheckbox.Size = New-Object System.Drawing.Size(600, 35)
    $contentPanel.Controls.Add($Script:acceptCheckbox)
    
    $backButton.Enabled = $true
    Apply-ResponsiveLayout
}

# ============================================
# SCREEN 4: PFAD
# ============================================
function Show-PathScreen {
    if (-not $Script:acceptCheckbox.Checked) {
        [System.Windows.Forms.MessageBox]::Show('Bitte akzeptieren Sie die Lizenzvereinbarung!', 'Fehler', 'OK', 'Warning')
        return
    }
    
    Clear-Content
    Update-Progress 4
    
    $pathLabel = New-Object System.Windows.Forms.Label
    $pathLabel.Text = 'Installationsordner:'
    $pathLabel.Font = New-Object System.Drawing.Font('Segoe UI', 13)
    $pathLabel.ForeColor = [System.Drawing.Color]::White
    $pathLabel.Location = New-Object System.Drawing.Point(40, 60)
    $pathLabel.Size = New-Object System.Drawing.Size(400, 35)
    $contentPanel.Controls.Add($pathLabel)
    
    $Script:pathTextBox = New-Object System.Windows.Forms.TextBox
    $Script:pathTextBox.Text = $Script:InstallPath
    $Script:pathTextBox.Location = New-Object System.Drawing.Point(40, 110)
    $Script:pathTextBox.Size = New-Object System.Drawing.Size(720, 35)  # ✅ 1000-280=720
    $Script:pathTextBox.Font = New-Object System.Drawing.Font('Consolas', 12)
    $Script:pathTextBox.BackColor = [System.Drawing.Color]::FromArgb(40, 40, 40)
    $Script:pathTextBox.ForeColor = [System.Drawing.Color]::White
    $contentPanel.Controls.Add($Script:pathTextBox)
    
    $browseButton = New-Object System.Windows.Forms.Button
    $browseButton.Text = 'Durchsuchen'
    $browseButton.Location = New-Object System.Drawing.Point(780, 107)  # ✅ 1000-220=780
    $browseButton.Size = New-Object System.Drawing.Size(180, 42)
    $browseButton.BackColor = [System.Drawing.Color]::FromArgb(60, 60, 60)
    $browseButton.ForeColor = [System.Drawing.Color]::White
    $browseButton.FlatStyle = 'Flat'
    $browseButton.Font = New-Object System.Drawing.Font('Segoe UI', 11)
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
    $Script:desktopCheckbox.Font = New-Object System.Drawing.Font('Segoe UI', 12)
    $Script:desktopCheckbox.Location = New-Object System.Drawing.Point(40, 180)
    $Script:desktopCheckbox.Size = New-Object System.Drawing.Size(600, 35)
    $contentPanel.Controls.Add($Script:desktopCheckbox)
    Apply-ResponsiveLayout
}

# ============================================
# SCREEN 5: INSTALLATION
# ============================================
function Show-InstallScreen {
    $Script:InstallPath = $Script:pathTextBox.Text
    
    Clear-Content
    Update-Progress 5
    $nextButton.Enabled = $false
    $backButton.Enabled = $false
    
    $statusLabel = New-Object System.Windows.Forms.Label
    $statusLabel.Text = 'Installation laeuft...'
    $statusLabel.Font = New-Object System.Drawing.Font('Segoe UI', 18, [System.Drawing.FontStyle]::Bold)
    $statusLabel.ForeColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
    $statusLabel.Location = New-Object System.Drawing.Point(40, 60)
    $statusLabel.Size = New-Object System.Drawing.Size(920, 45)  # ✅ 1000-80=920
    $contentPanel.Controls.Add($statusLabel)
    
    $installProgress = New-Object System.Windows.Forms.ProgressBar
    $installProgress.Location = New-Object System.Drawing.Point(40, 140)
    $installProgress.Size = New-Object System.Drawing.Size(920, 35)  # ✅ 1000-80=920
    $installProgress.Style = 'Continuous'
    $contentPanel.Controls.Add($installProgress)
    Apply-ResponsiveLayout
    
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
        
        $installProgress.Value = 60
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
            [System.Runtime.Interopservices.Marshal]::ReleaseComObject($shell) | Out-Null
        }
        
        $installProgress.Value = 75
        $statusLabel.Text = 'Erstelle Deinstaller...'
        $form.Refresh()
        
        # DEINSTALLER
        $uninstallScript = @"
@echo off
echo.
echo MaterialManager V01 - DEINSTALLATION
echo.
pause

net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo FEHLER: Administrator-Rechte erforderlich!
    pause
    exit /b 1
)

taskkill /F /IM MaterialManager_V01.exe >nul 2>&1
del "%USERPROFILE%\Desktop\MaterialManager V01.lnk" >nul 2>&1
reg delete "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MaterialManager_V01" /f >nul 2>&1

echo.
choice /C JN /M "Benutzerdaten auch loeschen"
if errorlevel 1 rd /S /Q "%LOCALAPPDATA%\MaterialManager_V01" >nul 2>&1

cd /d "%TEMP%"
rd /S /Q "$($Script:InstallPath)" >nul 2>&1

echo.
echo Deinstallation abgeschlossen!
pause
exit
"@
        [System.IO.File]::WriteAllText("$Script:InstallPath\UNINSTALL.bat", $uninstallScript)
        
        $installProgress.Value = 85
        $statusLabel.Text = 'Registriere in Systemsteuerung...'
        $form.Refresh()
        
        # REGISTRY
        try {
            $regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MaterialManager_V01"
            if (-not (Test-Path $regPath)) {
                New-Item -Path $regPath -Force | Out-Null
            }
            
            Set-ItemProperty -Path $regPath -Name "DisplayName" -Value "MaterialManager V01" -Type String
            Set-ItemProperty -Path $regPath -Name "DisplayVersion" -Value $Script:PackageVersion -Type String
            Set-ItemProperty -Path $regPath -Name "Publisher" -Value "Alexander Hoelzer" -Type String
            Set-ItemProperty -Path $regPath -Name "InstallLocation" -Value $Script:InstallPath -Type String
            Set-ItemProperty -Path $regPath -Name "UninstallString" -Value "`"$Script:InstallPath\UNINSTALL.bat`"" -Type String
            Set-ItemProperty -Path $regPath -Name "DisplayIcon" -Value "$Script:InstallPath\MaterialManager_V01.exe,0" -Type String
            Set-ItemProperty -Path $regPath -Name "NoModify" -Value 1 -Type DWord
            Set-ItemProperty -Path $regPath -Name "NoRepair" -Value 1 -Type DWord
            Set-ItemProperty -Path $regPath -Name "EstimatedSize" -Value 50000 -Type DWord
        } catch { }

        $installProgress.Value = 100
        Start-Sleep -Milliseconds 500
        
        Show-CompletionScreen
    } catch {
        [System.Windows.Forms.MessageBox]::Show("Fehler: $_", 'Installation fehlgeschlagen', 'OK', 'Error')
        $nextButton.Enabled = $true
    }
}

# ============================================
# SCREEN 6: FERTIG
# ============================================
function Show-CompletionScreen {
    Clear-Content
    Update-Progress 6
    
    $doneLabel = New-Object System.Windows.Forms.Label
    $doneLabel.Text = 'Installation erfolgreich abgeschlossen!'
    $doneLabel.Font = New-Object System.Drawing.Font('Segoe UI', 20, [System.Drawing.FontStyle]::Bold)
    $doneLabel.ForeColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
    $doneLabel.Location = New-Object System.Drawing.Point(40, 80)
    $doneLabel.Size = New-Object System.Drawing.Size(920, 55)  # ✅ 1000-80=920
    $contentPanel.Controls.Add($doneLabel)
    
    $infoLabel = New-Object System.Windows.Forms.Label
    $infoLabel.Text = @"
MaterialManager V01 ist jetzt installiert.

Installationsort: $Script:InstallPath

Sie koennen das Programm ueber die Desktop-Verknuepfung starten.
"@
    $infoLabel.Font = New-Object System.Drawing.Font('Segoe UI', 13)
    $infoLabel.ForeColor = [System.Drawing.Color]::FromArgb(180, 180, 180)
    $infoLabel.Location = New-Object System.Drawing.Point(40, 160)
    $infoLabel.Size = New-Object System.Drawing.Size(920, 180)  # ✅ 1000-80=920
    $contentPanel.Controls.Add($infoLabel)
    
    $Script:autoStartCheckbox = New-Object System.Windows.Forms.CheckBox
    $Script:autoStartCheckbox.Text = 'MaterialManager V01 jetzt starten'
    $Script:autoStartCheckbox.Checked = $true
    $Script:autoStartCheckbox.ForeColor = [System.Drawing.Color]::White
    $Script:autoStartCheckbox.Font = New-Object System.Drawing.Font('Segoe UI', 13, [System.Drawing.FontStyle]::Bold)
    $Script:autoStartCheckbox.Location = New-Object System.Drawing.Point(40, 360)  # ✅ 480-120=360
    $Script:autoStartCheckbox.Size = New-Object System.Drawing.Size(700, 40)
    $contentPanel.Controls.Add($Script:autoStartCheckbox)
    
    $nextButton.Text = 'Fertig'
    $nextButton.Enabled = $true
    Apply-ResponsiveLayout
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
        6 { 
            if ($Script:autoStartCheckbox.Checked) {
                try {
                    $exePath = "$Script:InstallPath\MaterialManager_V01.exe"
                    if (Test-Path $exePath) {
                        Start-Process $exePath -WorkingDirectory $Script:InstallPath
                    }
                } catch {
                    [System.Windows.Forms.MessageBox]::Show(
                        "App konnte nicht automatisch gestartet werden.`nBitte starten Sie die App manuell.",
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
