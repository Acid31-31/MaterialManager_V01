# ============================================
# MaterialManager V01 - GUI Deinstaller v1.0.31
# ============================================

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# ============================================
# VARIABLEN
# ============================================
$Script:CurrentStep = 1
$Script:TotalSteps = 4
$Script:InstallPath = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not (Test-Path (Join-Path $Script:InstallPath 'MaterialManager_V01.exe'))) {
    $Script:InstallPath = "C:\Program Files\MaterialManager_V01"
}
$Script:UserDataPath = "$env:LOCALAPPDATA\MaterialManager_V01"
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
# ADMIN-CHECK
# ============================================
$isAdmin = [System.Security.Principal.WindowsIdentity]::GetCurrent().Groups -contains 'S-1-5-32-544'
if (-not $isAdmin) {
    [System.Windows.Forms.MessageBox]::Show(
        'Dieses Programm muss mit Administrator-Rechten ausgeführt werden!',
        'Admin-Fehler',
        'OK',
        'Error'
    )
    exit 1
}

# ============================================
# FORMULAR - FESTE GROESSE 1000x750
# ============================================
$form = New-Object System.Windows.Forms.Form
$form.Text = 'MaterialManager V01 - Deinstallation'
$form.ClientSize = New-Object System.Drawing.Size($Script:formWidth, $Script:formHeight)
$form.StartPosition = 'CenterScreen'
$form.BackColor = [System.Drawing.Color]::FromArgb(20, 20, 20)
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false
$form.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Dpi

$Script:formWidth = 1000
$Script:formHeight = 750

# ============================================
# HEADER - FESTE WERTE (identisch mit INSTALL_GUI)
# ============================================
$titleLabel = New-Object System.Windows.Forms.Label
$titleLabel.Text = 'MaterialManager V01'
$titleLabel.Font = New-Object System.Drawing.Font('Segoe UI', 24, [System.Drawing.FontStyle]::Bold)
$titleLabel.ForeColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
$titleLabel.Location = New-Object System.Drawing.Point(40, 20)
$titleLabel.Size = New-Object System.Drawing.Size(920, 50)
$form.Controls.Add($titleLabel)

$subtitleLabel = New-Object System.Windows.Forms.Label
$subtitleLabel.Text = 'Professionelle Material- und Bestandsverwaltung'
$subtitleLabel.Font = New-Object System.Drawing.Font('Segoe UI', 12)
$subtitleLabel.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
$subtitleLabel.Location = New-Object System.Drawing.Point(42, 75)
$subtitleLabel.Size = New-Object System.Drawing.Size(920, 30)
$form.Controls.Add($subtitleLabel)

# ============================================
# CONTENT PANEL - FESTE WERTE
# ============================================
$contentPanel = New-Object System.Windows.Forms.Panel
$contentPanel.Location = New-Object System.Drawing.Point(0, 120)
$contentPanel.Size = New-Object System.Drawing.Size(1000, 480)
$contentPanel.BackColor = [System.Drawing.Color]::FromArgb(20, 20, 20)
$form.Controls.Add($contentPanel)

# ============================================
# PROGRESS BAR - FESTE WERTE
# ============================================
$progressBar = New-Object System.Windows.Forms.ProgressBar
$progressBar.Location = New-Object System.Drawing.Point(40, 630)
$progressBar.Size = New-Object System.Drawing.Size(920, 10)
$progressBar.Value = 0
$form.Controls.Add($progressBar)

$progressLabel = New-Object System.Windows.Forms.Label
$progressLabel.Text = 'Schritt 1 von 4'
$progressLabel.Font = New-Object System.Drawing.Font('Segoe UI', 9)
$progressLabel.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
$progressLabel.Location = New-Object System.Drawing.Point(40, 645)
$progressLabel.Size = New-Object System.Drawing.Size(400, 25)
$form.Controls.Add($progressLabel)

# ============================================
# BUTTONS - FESTE WERTE (identisch mit INSTALL_GUI)
# ============================================
$nextButton = New-Object System.Windows.Forms.Button
$nextButton.Text = 'Weiter >'
$nextButton.Size = New-Object System.Drawing.Size(160, 45)
$nextButton.Location = New-Object System.Drawing.Point(800, 680)
$nextButton.BackColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
$nextButton.ForeColor = [System.Drawing.Color]::White
$nextButton.FlatStyle = 'Flat'
$nextButton.Font = New-Object System.Drawing.Font('Segoe UI', 12, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($nextButton)

$backButton = New-Object System.Windows.Forms.Button
$backButton.Text = '< Zurueck'
$backButton.Size = New-Object System.Drawing.Size(160, 45)
$backButton.Location = New-Object System.Drawing.Point(620, 680)
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
# SCREEN 1: WILLKOMMEN / WARNUNG
# ============================================
function Show-WelcomeScreen {
    Clear-Content
    Update-Progress 1
    
    $welcomeLabel = New-Object System.Windows.Forms.Label
    $welcomeLabel.Text = @"
Willkommen beim MaterialManager V01 Deinstaller!

Version: 1.0.7
Hersteller: Alexander Hoelzer
Copyright (c) 2026

Dieses Programm deinstalliert MaterialManager V01 von Ihrem Computer.

WARNUNG: Dies wird folgende Dateien löschen:
  • Programmdateien: C:\Program Files\MaterialManager_V01
  • Desktop-Verknüpfung
  • Registry-Einträge

Ihre Benutzerdaten bleiben standardmäßig erhalten!

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
# SCREEN 2: OPTIONEN
# ============================================
function Show-OptionsScreen {
    Clear-Content
    Update-Progress 2
    
    $titleLabel = New-Object System.Windows.Forms.Label
    $titleLabel.Text = 'Deinstallationsoptionen'
    $titleLabel.Font = New-Object System.Drawing.Font('Segoe UI', 20, [System.Drawing.FontStyle]::Bold)
    $titleLabel.ForeColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
    $titleLabel.Location = New-Object System.Drawing.Point(40, 20)
    $titleLabel.Size = New-Object System.Drawing.Size(920, 40)
    $contentPanel.Controls.Add($titleLabel)
    
    $infoLabel = New-Object System.Windows.Forms.Label
    $infoLabel.Text = 'Was moechten Sie entfernen?'
    $infoLabel.Font = New-Object System.Drawing.Font('Segoe UI', 11)
    $infoLabel.ForeColor = [System.Drawing.Color]::FromArgb(180, 180, 180)
    $infoLabel.Location = New-Object System.Drawing.Point(40, 70)
    $infoLabel.Size = New-Object System.Drawing.Size(920, 30)
    $contentPanel.Controls.Add($infoLabel)
    
    # CHECKBOX 1: PROGRAMMDATEIEN
    $Script:checkboxProgram = New-Object System.Windows.Forms.CheckBox
    $Script:checkboxProgram.Text = 'Programmdateien entfernen (erforderlich)'
    $Script:checkboxProgram.Checked = $true
    $Script:checkboxProgram.Enabled = $false
    $Script:checkboxProgram.ForeColor = [System.Drawing.Color]::White
    $Script:checkboxProgram.Font = New-Object System.Drawing.Font('Segoe UI', 12, [System.Drawing.FontStyle]::Bold)
    $Script:checkboxProgram.Location = New-Object System.Drawing.Point(50, 120)
    $Script:checkboxProgram.Size = New-Object System.Drawing.Size(900, 30)
    $contentPanel.Controls.Add($Script:checkboxProgram)
    
    $programInfo = New-Object System.Windows.Forms.Label
    $programInfo.Text = '   Entfernt alle Dateien aus: C:\Program Files\MaterialManager_V01'
    $programInfo.Font = New-Object System.Drawing.Font('Segoe UI', 10)
    $programInfo.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
    $programInfo.Location = New-Object System.Drawing.Point(50, 150)
    $programInfo.Size = New-Object System.Drawing.Size(900, 25)
    $contentPanel.Controls.Add($programInfo)
    
    # CHECKBOX 2: DESKTOP-VERKNÜPFUNG
    $Script:checkboxDesktop = New-Object System.Windows.Forms.CheckBox
    $Script:checkboxDesktop.Text = 'Desktop-Verknuepfung entfernen'
    $Script:checkboxDesktop.Checked = $true
    $Script:checkboxDesktop.ForeColor = [System.Drawing.Color]::White
    $Script:checkboxDesktop.Font = New-Object System.Drawing.Font('Segoe UI', 12)
    $Script:checkboxDesktop.Location = New-Object System.Drawing.Point(50, 200)
    $Script:checkboxDesktop.Size = New-Object System.Drawing.Size(900, 30)
    $contentPanel.Controls.Add($Script:checkboxDesktop)
    
    $desktopInfo = New-Object System.Windows.Forms.Label
    $desktopInfo.Text = '   Entfernt die Verknuepfung: Desktop\MaterialManager V01.lnk'
    $desktopInfo.Font = New-Object System.Drawing.Font('Segoe UI', 10)
    $desktopInfo.ForeColor = [System.Drawing.Color]::FromArgb(150, 150, 150)
    $desktopInfo.Location = New-Object System.Drawing.Point(50, 230)
    $desktopInfo.Size = New-Object System.Drawing.Size(900, 25)
    $contentPanel.Controls.Add($desktopInfo)
    
    # CHECKBOX 3: BENUTZERDATEN
    $Script:checkboxUserData = New-Object System.Windows.Forms.CheckBox
    $Script:checkboxUserData.Text = 'Benutzerdaten auch entfernen'
    $Script:checkboxUserData.Checked = $false
    $Script:checkboxUserData.ForeColor = [System.Drawing.Color]::FromArgb(255, 193, 7)
    $Script:checkboxUserData.Font = New-Object System.Drawing.Font('Segoe UI', 12, [System.Drawing.FontStyle]::Bold)
    $Script:checkboxUserData.Location = New-Object System.Drawing.Point(50, 280)
    $Script:checkboxUserData.Size = New-Object System.Drawing.Size(900, 30)
    $contentPanel.Controls.Add($Script:checkboxUserData)
    
    $userDataInfo = New-Object System.Windows.Forms.Label
    $userDataInfo.Text = "   WARNUNG: Dies kann nicht rueckgaengig gemacht werden!`n   Entfernt: Konfigurationen, Lizenzdaten, Cache`n   Pfad: $env:LOCALAPPDATA\MaterialManager_V01"
    $userDataInfo.Font = New-Object System.Drawing.Font('Segoe UI', 10)
    $userDataInfo.ForeColor = [System.Drawing.Color]::FromArgb(255, 152, 0)
    $userDataInfo.Location = New-Object System.Drawing.Point(50, 310)
    $userDataInfo.Size = New-Object System.Drawing.Size(900, 50)
    $contentPanel.Controls.Add($userDataInfo)
    
    $backButton.Enabled = $true
    Apply-ResponsiveLayout
}

# ============================================
# SCREEN 3: DEINSTALLATION
# ============================================
function Show-UninstallScreen {
    Clear-Content
    Update-Progress 3
    $nextButton.Enabled = $false
    $backButton.Enabled = $false
    
    $statusLabel = New-Object System.Windows.Forms.Label
    $statusLabel.Text = 'Deinstallation laeuft...'
    $statusLabel.Font = New-Object System.Drawing.Font('Segoe UI', 18, [System.Drawing.FontStyle]::Bold)
    $statusLabel.ForeColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
    $statusLabel.Location = New-Object System.Drawing.Point(40, 60)
    $statusLabel.Size = New-Object System.Drawing.Size(920, 45)
    $contentPanel.Controls.Add($statusLabel)
    
    $uninstallProgress = New-Object System.Windows.Forms.ProgressBar
    $uninstallProgress.Location = New-Object System.Drawing.Point(40, 140)
    $uninstallProgress.Size = New-Object System.Drawing.Size(920, 35)
    $uninstallProgress.Style = 'Continuous'
    $contentPanel.Controls.Add($uninstallProgress)
    
    $logBox = New-Object System.Windows.Forms.TextBox
    $logBox.Multiline = $true
    $logBox.ScrollBars = 'Vertical'
    $logBox.ReadOnly = $true
    $logBox.Location = New-Object System.Drawing.Point(40, 200)
    $logBox.Size = New-Object System.Drawing.Size(920, 230)
    $logBox.BackColor = [System.Drawing.Color]::FromArgb(30, 30, 30)
    $logBox.ForeColor = [System.Drawing.Color]::FromArgb(200, 200, 200)
    $logBox.Font = New-Object System.Drawing.Font('Consolas', 9)
    $contentPanel.Controls.Add($logBox)
    Apply-ResponsiveLayout
    
    function Add-Log {
        param([string]$message)
        $logBox.AppendText("$message`r`n")
        $logBox.ScrollToCaret()
        $form.Refresh()
    }
    
    try {
        # 1. PROZESS STOPPEN
        Add-Log "[1/5] Stoppe MaterialManager Prozesse..."
        $uninstallProgress.Value = 10
        taskkill /F /IM MaterialManager_V01.exe 2>$null | Out-Null
        Start-Sleep -Milliseconds 500
        Add-Log "  ✓ Prozesse gestoppt"
        
        # 2. DESKTOP-VERKNÜPFUNG
        Add-Log "[2/5] Entferne Desktop-Verknuepfung..."
        $uninstallProgress.Value = 30
        if ($Script:checkboxDesktop.Checked) {
            $desktopLink = "$env:USERPROFILE\Desktop\MaterialManager V01.lnk"
            if (Test-Path $desktopLink) {
                Remove-Item -Path $desktopLink -Force -ErrorAction SilentlyContinue
                Add-Log "  ✓ Desktop-Verknuepfung entfernt"
            } else {
                Add-Log "  ⓘ Desktop-Verknuepfung nicht gefunden"
            }
        }
        
        # 3. REGISTRY
        Add-Log "[3/5] Entferne Registry-Eintraege..."
        $uninstallProgress.Value = 50
        try {
            $regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MaterialManager_V01"
            if (Test-Path $regPath) {
                Remove-Item -Path $regPath -Force -ErrorAction SilentlyContinue
                Add-Log "  ✓ Registry-Eintraege entfernt"
            } else {
                Add-Log "  ⓘ Registry-Eintraege nicht gefunden"
            }
        } catch {
            Add-Log "  ⚠ Fehler beim Loeschen der Registry: $_"
        }
        
        # 4. PROGRAMMDATEIEN
        Add-Log "[4/5] Entferne Programmdateien..."
        $uninstallProgress.Value = 70
        if (Test-Path $Script:InstallPath) {
            Remove-Item -Path $Script:InstallPath -Recurse -Force -ErrorAction SilentlyContinue
            Add-Log "  ✓ Programmdateien entfernt"
        } else {
            Add-Log "  ⓘ Installationsverzeichnis nicht gefunden"
        }
        
        # 5. BENUTZERDATEN (optional)
        Add-Log "[5/5] Abschliessend..."
        $uninstallProgress.Value = 85
        if ($Script:checkboxUserData.Checked) {
            Add-Log "  → Entferne Benutzerdaten..."
            if (Test-Path $Script:UserDataPath) {
                Remove-Item -Path $Script:UserDataPath -Recurse -Force -ErrorAction SilentlyContinue
                Add-Log "  ✓ Benutzerdaten entfernt"
            } else {
                Add-Log "  ⓘ Benutzerdaten nicht gefunden"
            }
        }
        
        $uninstallProgress.Value = 100
        Start-Sleep -Milliseconds 500
        Add-Log ""
        Add-Log "✓ Deinstallation erfolgreich abgeschlossen!"
        
        Show-CompletionScreen
    } catch {
        Add-Log ""
        Add-Log "✗ FEHLER: $_"
        $nextButton.Enabled = $true
        $backButton.Enabled = $true
    }
}

# ============================================
# SCREEN 4: FERTIG
# ============================================
function Show-CompletionScreen {
    Clear-Content
    Update-Progress 4
    
    $doneLabel = New-Object System.Windows.Forms.Label
    $doneLabel.Text = 'Deinstallation erfolgreich abgeschlossen!'
    $doneLabel.Font = New-Object System.Drawing.Font('Segoe UI', 20, [System.Drawing.FontStyle]::Bold)
    $doneLabel.ForeColor = [System.Drawing.Color]::FromArgb(76, 175, 80)
    $doneLabel.Location = New-Object System.Drawing.Point(40, 80)
    $doneLabel.Size = New-Object System.Drawing.Size(920, 55)
    $contentPanel.Controls.Add($doneLabel)
    
    $infoLabel = New-Object System.Windows.Forms.Label
    $infoLabel.Text = @"
MaterialManager V01 wurde erfolgreich deinstalliert.

Folgende Komponenten wurden entfernt:
  ✓ Programmdateien
  ✓ Desktop-Verknuepfung
  ✓ Registry-Eintraege"@
    if ($Script:checkboxUserData.Checked) {
        $infoLabel.Text += "`n  ✓ Benutzerdaten und Konfigurationen"
    } else {
        $infoLabel.Text += "`n  ⓘ Benutzerdaten erhalten (manuell loeschbar)"
    }
    
    $infoLabel.Font = New-Object System.Drawing.Font('Segoe UI', 13)
    $infoLabel.ForeColor = [System.Drawing.Color]::FromArgb(180, 180, 180)
    $infoLabel.Location = New-Object System.Drawing.Point(40, 160)
    $infoLabel.Size = New-Object System.Drawing.Size(920, 200)
    $contentPanel.Controls.Add($infoLabel)
    
    $nextButton.Text = 'Fertig'
    $nextButton.Enabled = $true
    Apply-ResponsiveLayout
}

# ============================================
# BUTTON EVENTS
# ============================================
$nextButton.Add_Click({
    switch ($Script:CurrentStep) {
        1 { Show-OptionsScreen }
        2 { Show-UninstallScreen }
        4 { $form.Close() }
    }
})

$backButton.Add_Click({
    switch ($Script:CurrentStep) {
        2 { Show-WelcomeScreen }
    }
})

# ============================================
# START
# ============================================
Show-WelcomeScreen
[void]$form.ShowDialog()
