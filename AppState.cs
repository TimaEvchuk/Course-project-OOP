namespace Plantify
{
    public static class AppState
    {
        /// <summary>
        /// A session-wide flag to indicate if the "needs care" notification has already been generated.
        /// This prevents it from re-appearing every time the user navigates back to the MyGarden view.
        /// It should be reset on logout.
        /// </summary>
        public static bool NeedsCareNotifiedToday { get; set; } = false;
    }
}
