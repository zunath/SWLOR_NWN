using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>
        /// Temporary, removed with the rest of <see cref="GameServices"/>: the content factory for code that is not
        /// converted yet, so it creates converted content with the text converted code uses (<c>Content.Create&lt;X, A1&gt;(a1)</c>).
        /// Converted content takes <see cref="IContentFactory"/> through its constructor instead.
        /// </summary>
        public static IContentFactory Content => FacadeAccessor.Resolve<IContentFactory>();
    }
}
