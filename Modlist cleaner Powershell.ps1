Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

function Show-Help {
    [System.Windows.Forms.MessageBox]::Show("To use:

1. Put the broken save into [TARGET SAVE]

2. Output will automatically be named the same as the Target save, but can be changed manually

3. Click 'Clear Modlist' to remove all mods while preserving save file size", "Help - Instructions")
}

function Get-ModlistBlock {
    param([byte[]]$Data)
    $content = [System.Text.Encoding]::ASCII.GetString($Data)
    $regex = [regex]::new("<UsedMods>.*?</UsedMods>", "Singleline")
    return $regex.Match($content)
}

function Replace-Modlist {
    param (
        [string]$TargetPath,
        [string]$OutputPath
    )

    $binData = [System.IO.File]::ReadAllBytes($TargetPath)
    $match = Get-ModlistBlock -Data $binData

    if (-not $match.Success) {
        [System.Windows.Forms.MessageBox]::Show("Error: <UsedMods> section not found in the target file.", "Error")
        return
    }

    $emptyMods = "<UsedMods></UsedMods>"
    $replacement = $emptyMods + (" " * ($match.Length - $emptyMods.Length))
    $replacementBytes = [System.Text.Encoding]::ASCII.GetBytes($replacement)

    [Array]::Copy($replacementBytes, 0, $binData, $match.Index, $replacementBytes.Length)
    [System.IO.File]::WriteAllBytes($OutputPath, $binData)

    [System.Windows.Forms.MessageBox]::Show("Modlist cleared successfully!`nSaved to:`n$OutputPath", "Success")
}

# === FORM SETUP ===
$form = New-Object System.Windows.Forms.Form
$form.Text = "Kingdom Come Modlist Remover Tool"
$form.Size = New-Object System.Drawing.Size(800, 250)
$form.MinimumSize = New-Object System.Drawing.Size(600, 250)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "Sizable"

$font = New-Object System.Drawing.Font("Segoe UI", 9)

# === UI ELEMENTS ===
$label1 = New-Object System.Windows.Forms.Label
$label1.Text = "Target Save (modlist will be cleared):"
$label1.Location = '10,20'
$label1.Size = '250,22'
$form.Controls.Add($label1)

$targetBox = New-Object System.Windows.Forms.TextBox
$targetBox.Location = '270,20'
$targetBox.Size = '360,22'
$targetBox.Anchor = 'Top,Left,Right'
$targetBox.Font = $font
$form.Controls.Add($targetBox)

$browseTarget = New-Object System.Windows.Forms.Button
$browseTarget.Text = "Browse"
$browseTarget.Size = '100,22'
$browseTarget.Location = '640,20'
$form.Controls.Add($browseTarget)
$browseTarget.Add_Click({
    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Filter = "WH Save Files (*.whs)|*.whs"
    if ($dialog.ShowDialog() -eq "OK") {
        $targetBox.Text = $dialog.FileName
        $outputBox.Text = $dialog.FileName
    }
})

$label2 = New-Object System.Windows.Forms.Label
$label2.Text = "Output Save (auto or custom):"
$label2.Location = '10,60'
$label2.Size = '250,22'
$form.Controls.Add($label2)

$outputBox = New-Object System.Windows.Forms.TextBox
$outputBox.Location = '270,60'
$outputBox.Size = '360,22'
$outputBox.Anchor = 'Top,Left,Right'
$outputBox.Font = $font
$form.Controls.Add($outputBox)

$browseOutput = New-Object System.Windows.Forms.Button
$browseOutput.Text = "Browse"
$browseOutput.Size = '100,22'
$browseOutput.Location = '640,60'
$form.Controls.Add($browseOutput)
$browseOutput.Add_Click({
    $dialog = New-Object System.Windows.Forms.SaveFileDialog
    $dialog.Filter = "WH Save Files (*.whs)|*.whs"
    if ($dialog.ShowDialog() -eq "OK") {
        $outputBox.Text = $dialog.FileName
    }
})

$button = New-Object System.Windows.Forms.Button
$button.Text = "Run Tool"
$button.Size = '200,40'
$button.Location = '300,100'
$button.BackColor = "IndianRed"
$button.ForeColor = "White"
$button.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($button)
$button.Add_Click({
    if (-not ($targetBox.Text -and $outputBox.Text)) {
        [System.Windows.Forms.MessageBox]::Show("Please select both Target and Output file paths.", "Error")
        return
    }
    Replace-Modlist -TargetPath $targetBox.Text -OutputPath $outputBox.Text
})

# === MENU ===
$menu = New-Object System.Windows.Forms.MenuStrip
$helpMenu = New-Object System.Windows.Forms.ToolStripMenuItem("Help")
$helpItem = New-Object System.Windows.Forms.ToolStripMenuItem("How to Use")
$helpItem.Add_Click({ Show-Help })
$helpMenu.DropDownItems.Add($helpItem)
$menu.Items.Add($helpMenu)
$form.MainMenuStrip = $menu
$form.Controls.Add($menu)

# === RESIZE HANDLING ===
$form.Add_Resize({
    $formWidth = $form.ClientSize.Width
    $padding = 10
    $buttonWidth = 100
    $labelWidth = [Math]::Min(260, $formWidth * 0.30)
    $textBoxWidth = $formWidth - $labelWidth - $buttonWidth - ($padding * 4)

    $label1.Width = $labelWidth
    $label2.Width = $labelWidth

    $targetBox.Width = $textBoxWidth
    $outputBox.Width = $textBoxWidth

    $targetBox.Location = New-Object Drawing.Point(($label1.Location.X + $labelWidth + $padding), $targetBox.Location.Y)
    $outputBox.Location = New-Object Drawing.Point(($label2.Location.X + $labelWidth + $padding), $outputBox.Location.Y)

    $browseTarget.Location = New-Object Drawing.Point(($targetBox.Location.X + $textBoxWidth + $padding), $browseTarget.Location.Y)
    $browseOutput.Location = New-Object Drawing.Point(($outputBox.Location.X + $textBoxWidth + $padding), $browseOutput.Location.Y)
})

# === START GUI ===
$form.ShowDialog()
