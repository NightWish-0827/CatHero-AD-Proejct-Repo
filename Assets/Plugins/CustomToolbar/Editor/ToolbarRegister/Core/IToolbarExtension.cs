namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 툴바 확장 기능을 위한 인터페이스
    /// </summary>
    public interface IToolbarExtension
    {
        /// <summary>
        /// 툴바 GUI 렌더링 메서드
        /// </summary>
        void OnToolbarGUI();
    }

    /// <summary>
    /// 왼쪽 툴바 확장 기능을 위한 인터페이스
    /// </summary>
    public interface ILeftToolbarExtension : IToolbarExtension
    {
    }

    /// <summary>
    /// 오른쪽 툴바 확장 기능을 위한 인터페이스
    /// </summary>
    public interface IRightToolbarExtension : IToolbarExtension
    {
    }
}
