namespace AnimCancel.Core
{
    public static class CancelEmotes
    {
        public static string Trigger(CancelEmote e) => e == CancelEmote.Cheer ? "emote_cheer" : "emote_nonono";

        /// <summary>Any State transition blend into the emote, from Player_animator.</summary>
        public static float BlendSeconds(CancelEmote e) => e == CancelEmote.Cheer ? 0.19f : 0.11f;
    }
}
