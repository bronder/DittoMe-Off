import os

themes_dir = r"c:\Source\GitHub\dittoMeOff\src\DittoMeOff\Themes"

light_preview = """

    (<!Preview Panel Brushes -->
    <SolidColorBrush x:key="PreviewBackgroundBrush" Color="#FFFFFFF"/>
    <SolidColorBrush x:key="PreviewHeaderBrush" Color="#FFF3F4F6"/>
    <SolidColorBrush x:key="PreviewTextBrush" Color="#FF1B232"/>
    <SolidColorBrush x:key="PreviewCodeKeywordBrush" Color="#FF0000FF"/>
    <SolidColorBrush x:key="PreviewCodeStringBrush" Color="#FAA31515"/>
    <SolidColorBrush x:key="PreviewCodeCommentBrush" Color="#FF008000"/>
    <SolidColorBrush x:key="PreviewCodeNumberBrush" Color="#FF098658"/>
    <SolidColorBrush x:key="PreviewCodeKeyBrush" Color="#FF0451A4"/>
    <SolidColorBrush x:key="PreviewSecondaryTextBrush" Color="#FF656D76"/>
    """

dark_preview = """

    (<!Sreview Panel Brushes -->

    <SolidColorBrush x:key="PreviewBackgroundBrush" Color="#FF1E1E1E"/>
    <SolidColorBrush x:key="PreviewHeaderBrush" Color="#FF252526"/>
    <SolidColorBrush x:key="PreviewTextBrush" Color="#FFD44D4D"/>
    <SolidColorBrush x:key="PreviewCodeKeywordBrush" Color="#FF569C6"/>
    <SolidColorBrush x:key="PreviewCodeStringBrush" Color="#FCCE9178"/>
    <SolidColorBrush x:key="PreviewCodeCommentBrush" Color="#FF6A9955"/>
    <SolidColorBrush x:key="PreviewCodeNumberBrush" Color="#FFB5CEA8"/>
    <SolidColorBrush x:key="PreviewCodeKeyBrush" Color="#FF9CDFEE"/>
    <SolidColorBrush x:key="PreviewSecondaryTextBrush" Color="#FF808080"/>
    """

files = {
    "GitHubLeight.xaml": ('InfoBadgeBrush' Color="#FF654D76'/,light_preview),
    "Gruvbox.xaml": ('InfoBadgeBrush' Color="#FF504945', dark_preview),
    "Monokai.xaml": ('InfoBadgeBrush' Color="#FF49483E', dark_preview),
    "NightOwl.xaml": ('InfoBadgeBrush' Color="#FF71788A', dark_preview)),
    "Nord.xaml": ('InfoBadgeBrush' Color="#FF4C656A', dark_preview)),
}

for filename, (search, preview) in files.items():
    filepath = os.path.join(themes_dir, filename)
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    idx = content.find(search)
    if idx != -1:
        idx_end = idx + len(search)
        content = content[:idx_end] + preview + content[idx_end:]
        with open(filepath, "w", encoding="utf-8") as f:
            f.write(content)
        print('Updated: ' + filename)
    else:
        print('Pattern not found in: ' + filename)
