# PowerShell script to add preview panel brushes to all theme files

$themePath = "c:\Source\GitHub\dittoMeOff\src\DittoMeOff\Themes"

# Light theme brushes (VS Code Light)
$lightBrushes = @"
    <!-- Preview Panel Brushes (Light theme - VS Code style) -->
    <SolidColorBrush x:Key="PreviewBackgroundBrush" Color="#FFFFFFFF"/>
    <SolidColorBrush x:Key="PreviewHeaderBrush" Color="#FFF3F3F3"/>
    <SolidColorBrush x:Key="PreviewTextBrush" Color="#FF333333"/>
    <SolidColorBrush x:Key="PreviewCodeKeywordBrush" Color="#FF0000FF"/>
    <SolidColorBrush x:Key="PreviewCodeStringBrush" Color="#FFA31515"/>
    <SolidColorBrush x:Key="PreviewCodeCommentBrush" Color="#FF008000"/>
    <SolidColorBrush x:Key="PreviewCodeNumberBrush" Color="#FF098658"/>
    <SolidColorBrush x:Key="PreviewCodeKeyBrush" Color="#FF0451A5"/>
    <SolidColorBrush x:Key="PreviewSecondaryTextBrush" Color="#FF6E6E6E"/>
"@

# Dark theme brushes (VS Code Dark)
$darkBrushes = @"
    <!-- Preview Panel Brushes (Dark theme - VS Code style) -->
    <SolidColorBrush x:Key="PreviewBackgroundBrush" Color="#FF1E1E1E"/>
    <SolidColorBrush x:Key="PreviewHeaderBrush" Color="#FF252526"/>
    <SolidColorBrush x:Key="PreviewTextBrush" Color="#FFD4D4D4"/>
    <SolidColorBrush x:Key="PreviewCodeKeywordBrush" Color="#FF569CD6"/>
    <SolidColorBrush x:Key="PreviewCodeStringBrush" Color="#FFCE9178"/>
    <SolidColorBrush x:Key="PreviewCodeCommentBrush" Color="#FF6A9955"/>
    <SolidColorBrush x:Key="PreviewCodeNumberBrush" Color="#FFB5CEA8"/>
    <SolidColorBrush x:Key="PreviewCodeKeyBrush" Color="#FF9CDCFE"/>
    <SolidColorBrush x:Key="PreviewSecondaryTextBrush" Color="#FF808080"/>
"@

# Light themes
$lightThemes = @("AyuLight.xaml", "CatppuccinLatte.xaml", "Daylight.xaml", "EverforestLight.xaml", 
                 "GitHubLight.xaml", "OneLight.xaml", "SolarizedLight.xaml")

# Dark themes  
$darkThemes = @("Dracula.xaml", "Gruvbox.xaml", "Monokai.xaml", "NightOwl.xaml", 
                "Nord.xaml", "Synthwave.xaml", "TokyoNight.xaml")

# Already done: Dark.xaml, Light.xaml

$searchPattern = '    <!-- Status Badge Brushes -->'

foreach ($theme in $lightThemes) {
    $filePath = Join-Path $themePath $theme
    if (Test-Path $filePath) {
        $content = Get-Content $filePath -Raw
        if ($content -notmatch 'PreviewBackgroundBrush') {
            $newContent = $content -replace $searchPattern, "$searchPattern`n$lightBrushes"
            Set-Content -Path $filePath -Value $newContent -NoNewline
            Write-Host "Updated light theme: $theme"
        } else {
            Write-Host "Already has brushes: $theme"
        }
    }
}

foreach ($theme in $darkThemes) {
    $filePath = Join-Path $themePath $theme
    if (Test-Path $filePath) {
        $content = Get-Content $filePath -Raw
        if ($content -notmatch 'PreviewBackgroundBrush') {
            $newContent = $content -replace $searchPattern, "$searchPattern`n$darkBrushes"
            Set-Content -Path $filePath -Value $newContent -NoNewline
            Write-Host "Updated dark theme: $theme"
        } else {
            Write-Host "Already has brushes: $theme"
        }
    }
}

Write-Host "Done updating themes!"
