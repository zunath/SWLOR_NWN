///////////////////////////////////////////////////////////////////////////////
// tk_odye_include.nss
//
// Created by: The Krit
// Date: 10/11/07
///////////////////////////////////////////////////////////////////////////////


///////////////////////////////////////////////////////////////////////////////
// Constants
///////////////////////////////////////////////////////////////////////////////


// The local variable that stores the current dye index on the dye.
const string DYE_INDEX = "DYE_INDEX";

// The local variable that stores the activated dye item.
const string DYE_ITEM = "TK_ODYE_ActivatedItem";

// The local variable that tracks which dye is being selected in the conversation.
const string DYE_SELECTION  = "TK_ODYE_Selection_Index";


///////////////////////////////////////////////////////////////////////////////
// Prototypes
///////////////////////////////////////////////////////////////////////////////


// Returns the name of the color(s) associated with index nColor.
string GetColorNames(int nColor);

// Renames oDye to reflect the selection of color nColor.
void SetDyeName(object oDye, int nColor);


///////////////////////////////////////////////////////////////////////////////
// Functions
///////////////////////////////////////////////////////////////////////////////


//----------------------------------------------------------
// SetDyeName
//
// Renames oDye to reflect the selection of color nColor.
//
void SetDyeName(object oDye, int nColor)
{
    SetName(oDye, GetName(oDye, TRUE) + " " + IntToString(nColor) +
            " (" + GetColorNames(nColor) + ")");
}


//----------------------------------------------------------
// GetColorNames
//
// Returns the name of the color(s) associated with index nColor.
//
string GetColorNames(int nColor)
{
    // This is a basic lookup function.
    // Nested switches for efficiency (and for ease of reading -- the outer
    // switch corresponds to the rows in the GUI).
    switch ( nColor / 16 )
    {
        case  0:
            switch ( nColor % 16 )
            {
                case  0: return "light tan / light grey";
                case  1: return "tan / grey";
                case  2: return "light brown / deep grey";
                case  3: return "brown / dark grey";
                case  4: return "pale auburn / light iron";
                case  5: return "light auburn / iron";
                case  6: return "auburn / deep iron";
                case  7: return "dark auburn / dark iron";
                case  8: return "light khaki / light gold";
                case  9: return "khaki / gold";
                case 10: return "deep khaki / deep gold";
                case 11: return "dark khaki / dark gold";
                case 12: return "light ruddy grey / light brass";
                case 13: return "ruddy grey / brass";
                case 14: return "deep ruddy grey / deep brass";
                case 15: return "dark ruddy grey / dark brass";
            }
            break;

        case  1:
            switch ( nColor % 16 )
            {
                case  0: return "light olive / light copper";
                case  1: return "olive / copper";
                case  2: return "deep olive / deep copper";
                case  3: return "dark olive / dark copper";
                case  4: return "light grey / light bronze";
                case  5: return "grey / bronze";
                case  6: return "deep grey / deep bronze";
                case  7: return "dark grey / dark bronze";
                case  8: return "light ultramarine / red";
                case  9: return "ultramarine / dark red";
                case 10: return "light bright blue / rose";
                case 11: return "bright blue / dark rose";
                case 12: return "aquamarine / purple";
                case 13: return "dark aquamarine / deep purple";
                case 14: return "light green / light purple";
                case 15: return "green / dark purple";
            }
            break;

        case  2:
            switch ( nColor % 16 )
            {
                case  0: return "yellow / bright blue";
                case  1: return "dark yellow / dark bright blue";
                case  2: return "light orange / indigo";
                case  3: return "orange / dark indigo";
                case  4: return "light red / cyan";
                case  5: return "red / deep cyan";
                case  6: return "light rose / light cyan";
                case  7: return "rose / dark cyan";
                case  8: return "light purple / green";
                case  9: return "purple / jade";
                case 10: return "light violet / light jade";
                case 11: return "violet / deep jade";
                case 12: return "white charcoal / olive";
                case 13: return "charcoal / dark olive";
                case 14: return "metallic ultramarine / grey-green";
                case 15: return "metallic bright blue / dark grey-green";
            }
            break;

        case  3:
            switch ( nColor % 16 )
            {
                case  0: return "metallic aquamarine / rainbow";
                case  1: return "metallic green / dark rainbow";
                case  2: return "metallic dark yellow / rust";
                case  3: return "metallic orange / aged rust";
                case  4: return "metallic red / dark rust";
                case  5: return "metallic rose / aged dark rust";
                case  6: return "metallic purple / aged iron";
                case  7: return "metallic violet / aged deep iron";
                case  8: return "metallic light grey";
                case  9: return "metallic dark grey";
                case 10: return "light gold";
                case 11: return "metallic light brown [light bronze]";
                case 12: return "'high-contrast' grey [iron]";
                case 13: return "mirrored";
                case 14: return "pure white";
                case 15: return "pure black";
            }
            break;

        case  4:
            switch ( nColor % 16 )
            {
                case  0: return "'high-contrast' crimson";
                case  1: return "'high-contrast' brown [deep bronze]";
                case  2: return "'high-contrast' dark yellow";
                case  3: return "'high-contrast' khaki";
                case  4: return "'high-contrast' green";
                case  5: return "'high-contrast' dark grey-green";
                case  6: return "'high-contrast' violet";
                case  7: return "'high-contrast' deep cool grey";
                case  8: return "'high-contrast' purple";
                case  9: return "'high-contrast' purple-grey";
                case 10: return "'high-contrast' tan [bronze]";
                case 11: return "'high-contrast' warm grey";
                case 12: return "'high-contrast' deep jade";
                case 13: return "'high-contrast' jade-grey";
                case 14: return "'high-contrast' blue";
                case 15: return "'high-contrast' slate grey";
            }
            break;

        case  5:
            switch ( nColor % 16 )
            {
                case  0: return "'high-contrast' olive drab";
                case  1: return "'high-contrast' verdant grey";
                case  2: return "'high-contrast' ultramarine";
                case  3: return "'high-contrast' cool grey";
                case  4: return "'high-contrast' tan-grey";
                case  5: return "'high-contrast' brown-grey";
                case  6: return "'high-contrast' ruddy grey";
                case  7: return "dull fading to pink";
                case  8: return "light scarlet";
                case  9: return "scarlet";
                case 10: return "deep scarlet";
                case 11: return "dark scarlet";
                case 12: return "light goldenrod";
                case 13: return "goldenrod";
                case 14: return "deep goldenrod";
                case 15: return "dark goldenrod";
            }
            break;

        case  6:
            switch ( nColor % 16 )
            {
                case  0: return "light crimson";
                case  1: return "crimson";
                case  2: return "deep crimson";
                case  3: return "dark crimson";
                case  4: return "light persimmon";
                case  5: return "persimmon";
                case  6: return "deep persimmon";
                case  7: return "dark persimmon";
                case  8: return "light dull green";
                case  9: return "dull green";
                case 10: return "deep dull green";
                case 11: return "dark dull green";
                case 12: return "light olive drab";
                case 13: return "olive drab";
                case 14: return "deep olive drab";
                case 15: return "dark olive drab";
            }
            break;

        case  7:
            switch ( nColor % 16 )
            {
                case  0: return "light verdant grey";
                case  1: return "verdant grey";
                case  2: return "deep verdant grey";
                case  3: return "dark verdant grey";
                case  4: return "light dull auburn";
                case  5: return "dull auburn";
                case  6: return "deep dull auburn";
                case  7: return "dark dull auburn";
                case  8: return "light dull khaki";
                case  9: return "dull khaki";
                case 10: return "deep dull khaki";
                case 11: return "dark dull khaki";
                case 12: return "light warm grey";
                case 13: return "warm grey";
                case 14: return "deep warm grey";
                case 15: return "dark warm grey";
            }
            break;

        case  8:
            switch ( nColor % 16 )
            {
                case  0: return "light dull tan";
                case  1: return "dull tan";
                case  2: return "light dull brown";
                case  3: return "dull brown";
                case  4: return "light cool grey";
                case  5: return "cool grey";
                case  6: return "deep cool grey";
                case  7: return "dark cool grey";
                case  8: return "light blue";
                case  9: return "blue";
                case 10: return "deep blue";
                case 11: return "dark blue";
                case 12: return "light turquoise";
                case 13: return "turquoise";
                case 14: return "deep turquoise";
                case 15: return "dark turquoise";
            }
            break;

        case  9:
            switch ( nColor % 16 )
            {
                case  0: return "light lavender";
                case  1: return "lavender";
                case  2: return "deep lavender";
                case  3: return "dark lavender";
                case  4: return "powder blue";
                case  5: return "dark powder blue";
                case  6: return "light aquamarine";
                case  7: return "deep aquamarine";
                case  8: return "light emerald";
                case  9: return "emerald";
                case 10: return "light pear";
                case 11: return "pear";
                case 12: return "light dull orange";
                case 13: return "dull orange";
                case 14: return "chestnut";
                case 15: return "deep chestnut";
            }
            break;

        case 10:
            switch ( nColor % 16 )
            {
                case  0: return "light red-violet";
                case  1: return "red-violet";
                case  2: return "light chestnut";
                case  3: return "orchid";
                case  4: return "light blue-grey";
                case  5: return "blue-grey";
                case  6: return "white";
                case  7: return "black";
                case  8: return "light grey-green";
                case  9: return "dark jade";
                case 10: return "dark violet";
                case 11: return "deep cooler grey";
                case 12: return "dark olive-green";
                case 13: return "dark ruddy brown";
                case 14: return "dull brown";
                case 15: return "speckled gold";
            }
    }//switch (nColor/16)

    // Unknown index.
    return "unknown";
}

