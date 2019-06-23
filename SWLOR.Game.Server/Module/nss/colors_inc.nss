/*
 * colors_inc.nss
 * 
 * Access to the color tokens provided by The Krit.
 ************************************************************
 * Please use these judiciously to enhance the gaming
 * experience. (Overuse of colors detracts from it.)
 ************************************************************
 * Color tokens in a string will change the color from that
 * point on when the string is displayed on the screen.
 * Every color change should be ended by an end token,
 * supplied by ColorTokenEnd().
 ************************************************************/


///////////////////////////////////////////////////////////////////////////////
// Constants
///////////////////////////////////////////////////////////////////////////////


const string ColorArray = "	 !##$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]]^_`abcdefghijklmnopqrstuvwxyz{|}~ÄÅÇÉÑÖÜáàâäãåçéèêëíìîïñóòôöõúùûü†°¢£§•¶ß®©™´¨≠ÆØ∞±≤≥¥µ∂∑∏π∫ªºΩæø¿¡¬√ƒ≈∆«»… ÀÃÕŒœ–—“”‘’÷◊ÿŸ⁄€‹›ﬁﬂ‡·‚„‰ÂÊÁËÈÍÎÏÌÓÔÒÚÛÙıˆ˜¯˘˙˚¸˝˛˛";



///////////////////////////////////////////////////////////////////////////////
// Prototypes
///////////////////////////////////////////////////////////////////////////////


// Supplies a string that changes the text to the given RGB values.
// Valid parameter values are 0-255.
string ColorToken(int nRed, int nGreen, int nBlue);

// Supplies a string that ends an earlier color change.
string ColorTokenEnd();

///////////////////////////////////////////////////////////////////////////////

// Supplies a string that changes the text to black.
string ColorTokenBlack();

// Supplies a string that changes the text to blue.
string ColorTokenBlue();

// Supplies a string that changes the text to gray.
string ColorTokenGray();

// Supplies a string that changes the text to green.
string ColorTokenGreen();

// Supplies a string that changes the text to light purple.
string ColorTokenLightPurple();

// Supplies a string that changes the text to orange.
string ColorTokenOrange();

// Supplies a string that changes the text to pink.
string ColorTokenPink();

// Supplies a string that changes the text to purple.
string ColorTokenPurple();

// Supplies a string that changes the text to red.
string ColorTokenRed();

// Supplies a string that changes the text to white.
string ColorTokenWhite();

// Supplies a string that changes the text to yellow.
string ColorTokenYellow();

///////////////////////////////////////////////////////////////////////////////

// Supplies a string that changes the text to the color of
// combat messages.
string ColorTokenCombat();

// Supplies a string that changes the text to the color of
// dialog.
string ColorTokenDialog();

// Supplies a string that changes the text to the color of
// dialog actions.
string ColorTokenDialogAction();

// Supplies a string that changes the text to the color of
// dialog checks.
string ColorTokenDialogCheck();

// Supplies a string that changes the text to the color of
// dialog highlighting.
string ColorTokenDialogHighlight();

// Supplies a string that changes the text to the color of
// replies in the dialog window.
string ColorTokenDialogReply();

// Supplies a string that changes the text to the color of
// the DM channel.
string ColorTokenDM();

// Supplies a string that changes the text to the color of
// many game engine messages.
string ColorTokenGameEngine();

// Supplies a string that changes the text to the color of
// saving throw messages.
string ColorTokenSavingThrow();

// Supplies a string that changes the text to the color of
// messages sent from scripts.
string ColorTokenScript();

// Supplies a string that changes the text to the color of
// server messages.
string ColorTokenServer();

// Supplies a string that changes the text to the color of
// shouts.
string ColorTokenShout();

// Supplies a string that changes the text to the color of
// skill check messages.
string ColorTokenSkillCheck();

// Supplies a string that changes the text to the color of
// the talk and party talk channels.
string ColorTokenTalk();

// Supplies a string that changes the text to the color of
// tells.
string ColorTokenTell();

// Supplies a string that changes the text to the color of
// whispers.
string ColorTokenWhisper();

///////////////////////////////////////////////////////////////////////////////

// Returns the name of oPC, surrounded by color tokens, so the color of
// the name is the lighter blue often used in NWN game engine messages.
string GetNamePCColor(object oPC);

// Returns the name of oNPC, surrounded by color tokens, so the color of
// the name is the shade of purple often used in NWN game engine messages.
string GetNameNPCColor(object oNPC);



///////////////////////////////////////////////////////////////////////////////
// Basic Functions
///////////////////////////////////////////////////////////////////////////////


///////////////////////////////////////////////////////////////////////////////
// ColorToken()
//
// Supplies a string that changes the text to the given RGB values.
// Valid parameter values are 0-255.
//
string ColorToken(int nRed, int nGreen, int nBlue)
{
    return "<c" + GetSubString(ColorArray, nRed, 1) +
            GetSubString(ColorArray, nGreen, 1) +
            GetSubString(ColorArray, nBlue, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenEnd()
//
// Supplies a string that ends an earlier color change.
//
string ColorTokenEnd()
{
 return "</c>";
}



///////////////////////////////////////////////////////////////////////////////
// Functions by Color
///////////////////////////////////////////////////////////////////////////////


///////////////////////////////////////////////////////////////////////////////
// ColorTokenBlack()
//
// Supplies a string that changes the text to black.
//
string ColorTokenBlack()
{
    return "<c" + GetSubString(ColorArray, 0, 1) +
            GetSubString(ColorArray, 0, 1) +
            GetSubString(ColorArray, 0, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenBlue()
//
// Supplies a string that changes the text to blue.
//
string ColorTokenBlue()
{
    return "<c" + GetSubString(ColorArray, 0, 1) +
            GetSubString(ColorArray,   0, 1) +
            GetSubString(ColorArray, 255, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenGray()
//
// Supplies a string that changes the text to gray.
//
string ColorTokenGray()
{
    return "<c" + GetSubString(ColorArray, 127, 1) +
            GetSubString(ColorArray, 127, 1) +
            GetSubString(ColorArray, 127, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenGreen()
//
// Supplies a string that changes the text to green.
//
string ColorTokenGreen()
{
    return "<c" + GetSubString(ColorArray, 0, 1) +
            GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray,   0, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenLightPurple()
//
// Supplies a string that changes the text to light purple.
//
string ColorTokenLightPurple()
{
    return "<c" + GetSubString(ColorArray, 175, 1) +
            GetSubString(ColorArray,  48, 1) +
            GetSubString(ColorArray, 255, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenOrange()
//
// Supplies a string that changes the text to orange.
//
string ColorTokenOrange()
{
    return "<c" + GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray, 127, 1) +
            GetSubString(ColorArray, 0, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenPink()
//
// Supplies a string that changes the text to pink.
//
string ColorTokenPink()
{
    return "<c" + GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray,   0, 1) +
            GetSubString(ColorArray, 255, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenPurple()
//
// Supplies a string that changes the text to purple.
//
string ColorTokenPurple()
{
    return "<c" + GetSubString(ColorArray, 127, 1) +
            GetSubString(ColorArray,   0, 1) +
            GetSubString(ColorArray, 255, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenRed()
//
// Supplies a string that changes the text to red.
//
string ColorTokenRed()
{
    return "<c" + GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray, 0, 1) +
            GetSubString(ColorArray, 0, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenWhite()
//
// Supplies a string that changes the text to white.
//
string ColorTokenWhite()
{
    return "<c" + GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray, 255, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenYellow()
//
// Supplies a string that changes the text to yellow.
//
string ColorTokenYellow()
{
    return "<c" + GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray,   0, 1) + ">";
}



///////////////////////////////////////////////////////////////////////////////
// Functions by Purpose
///////////////////////////////////////////////////////////////////////////////


///////////////////////////////////////////////////////////////////////////////
// ColorTokenCombat()
//
// Supplies a string that changes the text to the color of
// combat messages.
//
string ColorTokenCombat()
{
    return "<c" + GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray, 102, 1) +
            GetSubString(ColorArray,   0, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenDialog()
//
// Supplies a string that changes the text to the color of
// dialog.
//
string ColorTokenDialog()
{
    return "<c" + GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray, 255, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenDialogAction()
//
// Supplies a string that changes the text to the color of
// dialog actions.
//
string ColorTokenDialogAction()
{
    return "<c" + GetSubString(ColorArray, 1, 1) +
            GetSubString(ColorArray, 254, 1) +
            GetSubString(ColorArray,   1, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenDialogCheck()
//
// Supplies a string that changes the text to the color of
// dialog checks.
//
string ColorTokenDialogCheck()
{
    return "<c" + GetSubString(ColorArray, 254, 1) +
            GetSubString(ColorArray, 1, 1) +
            GetSubString(ColorArray, 1, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenDialogHighlight()
//
// Supplies a string that changes the text to the color of
// dialog highlighting.
//
string ColorTokenDialogHighlight()
{
    return "<c" + GetSubString(ColorArray, 1, 1) +
            GetSubString(ColorArray,   1, 1) +
            GetSubString(ColorArray, 254, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenDialogReply()
//
// Supplies a string that changes the text to the color of
// replies in the dialog window.
//
string ColorTokenDialogReply()
{
    return "<c" + GetSubString(ColorArray, 102, 1) +
            GetSubString(ColorArray, 178, 1) +
            GetSubString(ColorArray, 255, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenDM()
//
// Supplies a string that changes the text to the color of
// the DM channel.
//
string ColorTokenDM()
{
    return "<c" + GetSubString(ColorArray, 16, 1) +
            GetSubString(ColorArray, 223, 1) +
            GetSubString(ColorArray, 255, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenGameEngine()
//
// Supplies a string that changes the text to the color of
// many game engine messages.
//
string ColorTokenGameEngine()
{
    return "<c" + GetSubString(ColorArray, 204, 1) +
            GetSubString(ColorArray, 119, 1) +
            GetSubString(ColorArray, 255, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenSavingThrow()
//
// Supplies a string that changes the text to the color of
// saving throw messages.
//
string ColorTokenSavingThrow()
{
    return "<c" + GetSubString(ColorArray, 102, 1) +
            GetSubString(ColorArray, 204, 1) +
            GetSubString(ColorArray, 255, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenScript()
//
// Supplies a string that changes the text to the color of
// messages sent from scripts.
//
string ColorTokenScript()
{
    return "<c" + GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray, 0, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenServer()
//
// Supplies a string that changes the text to the color of
// server messages.
//
string ColorTokenServer()
{
    return "<c" + GetSubString(ColorArray, 176, 1) +
            GetSubString(ColorArray, 176, 1) +
            GetSubString(ColorArray, 176, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenShout()
//
// Supplies a string that changes the text to the color of
// shouts.
//
string ColorTokenShout()
{
    return "<c" + GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray, 239, 1) +
            GetSubString(ColorArray,  80, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenSkillCheck()
//
// Supplies a string that changes the text to the color of
// skill check messages.
//
string ColorTokenSkillCheck()
{
    return "<c" + GetSubString(ColorArray, 0, 1) +
            GetSubString(ColorArray, 102, 1) +
            GetSubString(ColorArray, 255, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenTalk()
//
// Supplies a string that changes the text to the color of
// the talk and party talk channels.
//
string ColorTokenTalk()
{
    return "<c" + GetSubString(ColorArray, 240, 1) +
            GetSubString(ColorArray, 240, 1) +
            GetSubString(ColorArray, 240, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenTell()
//
// Supplies a string that changes the text to the color of
// tells.
//
string ColorTokenTell()
{
    return "<c" + GetSubString(ColorArray,  32, 1) +
            GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray,  32, 1) + ">";
}

///////////////////////////////////////////////////////////////////////////////
// ColorTokenWhisper()
//
// Supplies a string that changes the text to the color of
// whispers.
//
string ColorTokenWhisper()
{
    return "<c" + GetSubString(ColorArray, 128, 1) +
            GetSubString(ColorArray, 128, 1) +
            GetSubString(ColorArray, 128, 1) + ">";
}



///////////////////////////////////////////////////////////////////////////////
// Colored Name Functions
///////////////////////////////////////////////////////////////////////////////


///////////////////////////////////////////////////////////////////////////////
// GetNamePCColor()
//
// Returns the name of oPC, surrounded by color tokens, so the color of
// the name is the lighter blue often used in NWN game engine messages.
// 
//
string GetNamePCColor(object oPC)
{
    return "<c" + GetSubString(ColorArray, 153, 1) +
            GetSubString(ColorArray, 255, 1) +
            GetSubString(ColorArray, 255, 1) + ">" +
            GetName(oPC) + "</c>";
}

///////////////////////////////////////////////////////////////////////////////
// GetNameNPCColor()
//
// Returns the name of oNPC, surrounded by color tokens, so the color of
// the name is the shade of purple often used in NWN game engine messages.
//
string GetNameNPCColor(object oNPC)
{
    return "<c" + GetSubString(ColorArray, 204, 1) +
            GetSubString(ColorArray, 153, 1) +
            GetSubString(ColorArray, 204, 1) + ">" +
            GetName(oNPC) + "</c>";
}
