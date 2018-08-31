using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IColorTokenService
    {
        string TokenStart(byte red, byte green, byte blue);
        string TokenEnd();
        string Custom(string text, byte red, byte green, byte blue);
        string Black(string text);
        string Blue(string text);
        string Gray(string text);
        string Green(string text);
        string LightPurple(string text);
        string Orange(string text);
        string Pink(string text);
        string Purple(string text);
        string Red(string text);
        string White(string text);
        string Yellow(string text);
        string Cyan(string text);
        string Combat(string text);
        string Dialog(string text);
        string DialogAction(string text);
        string DialogCheck(string text);
        string DialogHighlight(string text);
        string DialogReply(string text);
        string DM(string text);
        string GameEngine(string text);
        string SavingThrow(string text);
        string Script(string text);
        string Server(string text);
        string Shout(string text);
        string SkillCheck(string text);
        string Talk(string text);
        string Tell(string text);
        string Whisper(string text);
        string GetNamePCColor(NWObject oPC);
        string GetNameNPCColor(NWObject oNPC);
    }
}