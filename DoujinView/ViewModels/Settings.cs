namespace DoujinView.ViewModels;

public static class Settings {
    public static Setting<string> LastOpenedFilePath  { get; set; } = new("LastOpenedFilePath");
    public static Setting<int>    LastOpenedPageIndex { get; set; } = new("LastOpenedPageIndex");
    public static Setting<bool>   ResumeLastArchive   { get; set; } = new("ResumeLastArchive");
    public static Setting<bool>   ResumeLastPage      { get; set; } = new("ResumeLastPage");
    public static Setting<bool>   LoadNextArchive     { get; set; } = new("LoadNextArchive");
    public static Setting<bool>   JapaneseMode        { get; set; } = new("JapaneseMode");
    public static Setting<bool>   CreationOrder       { get; set; } = new("CreationOrder");
    public static Setting<int>    WindowState         { get; set; } = new("WindowState");
    public static Setting<int>    WindowsPositionX    { get; set; } = new("WindowPosX");
    public static Setting<int>    WindowsPositionY    { get; set; } = new("WindowPosY");
}