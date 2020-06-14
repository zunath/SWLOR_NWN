// Krit's Omnidye

// Sets the dye based on the color selection.

#include "tk_odye_include"


// Converts the encoded color selection into a NWN color index.
// Returns -nSelection on error.
int SelectionToIndex(int nSelection);


void main()
{
    // Get the dye that will be set.
    object oDye = GetLocalObject(OBJECT_SELF, DYE_ITEM);

    // Get the selected index.
    int nColor = GetLocalInt(OBJECT_SELF, DYE_SELECTION);

    // Convert the index to a standard index.
    nColor = SelectionToIndex(nColor);

    // Set the color index to be used by the dying scripts.
    SetLocalInt(oDye, DYE_INDEX, nColor);

    // Rename the dye to be informative.
    SetDyeName(oDye, nColor);
}


//----------------------------------------------------------
// Converts the encoded color selection into a NWN color index.
// Returns -nSelection on error.
int SelectionToIndex(int nSelection)
{
    // Decompose the code.
    int nFamily =  nSelection / 100;
    int nHue    = (nSelection / 10) % 10;
    int nShade  =  nSelection % 10;

    // Translate the code into a color.
    switch ( nFamily )
    {
        case 1: // Browns and yellows
            switch ( nHue )
            {
                case 1: // Brown / bronze
                    switch ( nShade )
                    {
                        case 1: return   0; // Light tan
                        case 2: return   1; // Tan
                        case 3: return   2; // Light brown
                        case 4: return   3; // Brown
                        case 5: return  74; // "Hi-C" tan / "Hi-C" bronze
                        case 6: return  65; // "Hi-C" brown / "Hi-C" deep bronze
                        case 7: return  59; // Metal light brown / light bronze
                        // Metal colors.
                        case 8: return  21; // Bronze
                        case 9: return  22; // Deep bronze
                        case 0: return  23; // Dark bronze
                    }
                    break;

                case 2: // Dull or ruddy brown
                    switch ( nShade )
                    {
                        case 1: return 128; // Light dull tan
                        case 2: return 129; // Dull tan
                        case 3: return 130; // Light dull brown
                        case 4: return 131; // Dull brown
                        case 5: return 173; // Dark ruddy brown
                    }
                    break;

                case 3: // Auburn / brass
                    switch ( nShade )
                    {
                        case 1: return   4; // Pale auburn
                        case 2: return   5; // Light auburn
                        case 3: return   6; // Auburn
                        case 4: return   7; // Dark auburn
                        // Metal colors.
                        case 7: return  12; // Light brass
                        case 8: return  13; // Brass
                        case 9: return  14; // Deep brass
                        case 0: return  15; // Dark brass
                    }
                    break;

                case 4: // Dull auburn
                    switch ( nShade )
                    {
                        case 1: return 116; // Pale dull auburn
                        case 2: return 117; // Light dull auburn
                        case 3: return 118; // Dull auburn
                        case 4: return 119; // Dark dull auburn
                    }
                    break;

                case 5: // Yellow or gold
                    switch ( nShade )
                    {
                        case 1: return  32; // Yellow
                        case 2: return  33; // Dark yellow
                        case 3: return  66; // "Hi-C" dark yellow
                        case 4: return  50; // Metal dark yellow
                        case 5: return  58; // Light gold
                        case 6: return 175; // Speckled gold
                        // Metal colors.
                        case 8: return   9; // Gold
                        case 9: return  10; // Deep gold
                        case 0: return  11; // Dark gold
                    }
                    break;

                case 6: // Goldenrod
                    switch ( nShade )
                    {
                        case 1: return  92; // Light goldenrod
                        case 2: return  93; // Goldenrod
                        case 3: return  94; // Deep goldenrod
                        case 4: return  95; // Dark goldenrod
                    }
                    break;

                case 7: // Pear
                    switch ( nShade )
                    {
                        case 1: return 154; // Light pear
                        case 2: return 155; // Pear
                    }
            }//switch(nHue) - browns and yellows
            break;

        case 2: // Oranges, reds, and red-violets
            switch ( nHue )
            {
                case 1: // Orange / copper
                    switch ( nShade )
                    {
                        case 1: return  34; // Light orange
                        case 2: return  35; // Orange
                        case 3: return  51; // Metal orange
                        // Metal colors.
                        case 7: return  16; // Light copper
                        case 8: return  17; // Copper
                        case 9: return  18; // Deep copper
                        case 0: return  19; // Dark copper
                    }
                    break;

                case 2: // Dull orange
                    switch ( nShade )
                    {
                        case 1: return 156; // Light dull orange
                        case 2: return 157; // Dull orange
                    }
                    break;

                case 3: // Persimmon
                    switch ( nShade )
                    {
                        case 1: return 100; // Light persimmon
                        case 2: return 101; // Persimmon
                        case 3: return 102; // Deep persimmon
                        case 4: return 103; // Dark persimmon
                    }
                    break;

                case 4: // Red or chestnut
                    switch ( nShade )
                    {
                        case 1: return  36; // Light red
                        case 2: return  37; // Red
                        case 3: return  52; // Metal red
                        case 4: return 162; // Light chestnut
                        case 5: return 158; // Chestnut
                        case 6: return 159; // Deep chestnut
                        // Metal colors.
                        case 9: return  24; // Red
                        case 0: return  25; // Metal red
                    }
                    break;

                case 5: // Crimson
                    switch ( nShade )
                    {
                        case 1: return  96; // Light crimson
                        case 2: return  97; // Crimson
                        case 3: return  98; // Deep crimson
                        case 4: return  99; // Dark crimson
                        case 5: return  64; // "Hi-C" crimson
                    }
                    break;

                case 6: // Scarlet
                    switch ( nShade )
                    {
                        case 1: return  88; // Light scarlet
                        case 2: return  89; // Scarlet
                        case 3: return  90; // Deep scarlet
                        case 4: return  91; // Dark scarlet
                    }
                    break;

                case 7: // Red-violet
                    switch ( nShade )
                    {
                        case 1: return  38; // Light rose
                        case 2: return  39; // Rose
                        case 3: return  53; // Metal rose
                        case 4: return 160; // Light red-violet
                        case 5: return 161; // red-violet
                        case 6: return 163; // orchid
                        // Metal colors.
                        case 9: return  26; // Rose
                        case 0: return  27; // Dark rose
                    }
            }//switch(nHue) - oranges, reds, and red-violets
            break;

        case 3: // Purples and blues
            switch ( nHue )
            {
                case 1: // Lavender
                    switch ( nShade )
                    {
                        case 1: return 144; // Light lavender
                        case 2: return 145; // Lavender
                        case 3: return 146; // Deep lavender
                        case 4: return 147; // Dark lavender
                    }
                    break;

                case 2: // Purple
                    switch ( nShade )
                    {
                        case 1: return  40; // Light purple
                        case 2: return  41; // Purple
                        case 3: return  72; // "Hi-C" purple
                        case 4: return  54; // Metal purple
                        // Metal colors
                        case 7: return  30; // Light purple
                        case 8: return  28; // Purple
                        case 9: return  29; // Deep purple
                        case 0: return  31; // Dark purple
                    }
                    break;

                case 3: // Violet
                    switch ( nShade )
                    {
                        case 1: return  42; // Light violet
                        case 2: return  43; // Violet
                        case 3: return 170; // Dark violet
                        case 4: return  70; // "Hi-C" violet
                        case 5: return  55; // Metal violet
                        // Metal colors.
                        case 9: return  34; // Indigo
                        case 0: return  35; // Dark indigo
                    }
                    break;

                case 4: // Blue or powder blue
                    switch ( nShade )
                    {
                        case 1: return 136; // Light blue
                        case 2: return 137; // Blue
                        case 3: return 138; // Deep blue
                        case 4: return 139; // Dark blue
                        case 5: return  78; // "Hi-C" blue
                        case 6: return 148; // Powder blue
                        case 7: return 149; // Dark powder blue
                    }
                    break;

                case 5: // Bright blue or ultramarine
                    switch ( nShade )
                    {
                        case 1: return  26; // Light bright blue
                        case 2: return  27; // Bright blue
                        case 3: return  47; // Metal bright blue
                        case 4: return  24; // Light ultramarine
                        case 5: return  25; // Ultramarine
                        case 6: return  82; // "Hi-C" ultramarine
                        case 7: return  46; // Metal ultramarine
                        // Metal colors.
                        case 9: return  32; // Bright blue
                        case 0: return  33; // Dark bright blue
                    }
                    break;

                case 6: // Turquoise
                    switch ( nShade )
                    {
                        case 1: return 140; // Light turquoise
                        case 2: return 141; // Turquoise
                        case 3: return 142; // Deep turquoise
                        case 4: return 143; // Dark turquoise
                    }
                    break;

                case 7: // Aquamarine
                    switch ( nShade )
                    {
                        case 1: return 150; // Light aquamarine
                        case 2: return  28; // Aquamarine
                        case 3: return 151; // Deep aquamarine
                        case 4: return  29; // Dark aquamarine
                        case 5: return  48; // Metal aquamarine
                        // Metal colors.
                        case 7: return  38; // Light cyan
                        case 8: return  36; // Cyan
                        case 9: return  37; // Deep cyan
                        case 0: return  39; // Dark cyan
                    }
            }//switch(nHue) - purples and blues
            break;

        case 4: // Greens and khakis
            switch ( nHue )
            {
                case 1: // Green
                    switch ( nShade )
                    {
                        case 1: return  30; // Light green
                        case 2: return  31; // Green
                        case 3: return  68; // "Hi-C" green
                        case 4: return  49; // Metal green
                        // Metal colors.
                        case 9: return  40; // Green
                        case 0: return  41; // Dark green
                    }
                    break;

                case 2: // Dull green
                    switch ( nShade )
                    {
                        case 1: return 104; // Light dull green
                        case 2: return 105; // Dull green
                        case 3: return 106; // Deep dull green
                        case 4: return 107; // Dark dull green
                    }
                    break;

                case 3: // Emerald or jade
                    switch ( nShade )
                    {
                        case 1: return 152; // Light emerald
                        case 2: return 153; // Emerald
                        case 3: return 169; // Dark jade
                        case 4: return  76; // "Hi-C" deep jade
                        // Metal colors.
                        case 9: return  42; // Light jade
                        case 0: return  43; // Deep jade
                    }
                    break;

                case 4: // Olive
                    switch ( nShade )
                    {
                        case 1: return  16; // Light olive
                        case 2: return  17; // Olive
                        case 3: return  18; // Deep olive
                        case 4: return  19; // Dark olive
                        case 5: return 172; // Dark olive-green
                        // Metal colors.
                        case 9: return  44; // Olive
                        case 0: return  45; // Dark olive
                    }
                    break;

                case 5: // Olive drab
                    switch ( nShade )
                    {
                        case 1: return 108; // Light olive drab
                        case 2: return 109; // Olive drab
                        case 3: return 110; // Deep olive drab
                        case 4: return 111; // Dark olive drab
                        case 5: return  80; // "Hi-C" olive drab
                    }
                    break;

                case 6: // Grey-green or khaki
                    switch ( nShade )
                    {
                        case 1: return 168; // Light grey-green
                        case 2: return  69; // "Hi-C" dark grey-green
                        case 3: return   8; // Light khaki
                        case 4: return   9; // Khaki
                        case 5: return  10; // Deep khaki
                        case 6: return  11; // Dark khaki
                        case 7: return  67; // "Hi-C" khaki
                        // Metal colors.
                        case 9: return  46; // Grey-green
                        case 0: return  47; // Dark grey-green
                    }
                    break;

                case 7: // Dull khaki
                    switch ( nShade )
                    {
                        case 1: return 120; // Light dull khaki
                        case 2: return 121; // Dull khaki
                        case 3: return 122; // Deep dull khaki
                        case 4: return 123; // Dark dull khaki
                    }
            }//switch(nHue) - greens and khakis
            break;

        case 5: // Hued greys
            switch ( nHue )
            {
                case 1: // Cool grey
                    switch ( nShade )
                    {
                        case 1: return 132; // Light cool grey
                        case 2: return 133; // Cool grey
                        case 3: return 134; // Deep cool grey
                        case 4: return 135; // Dark cool grey
                        case 5: return 171; // Deep cooler grey
                        case 6: return  83; // "Hi-C" cool grey
                        case 7: return  71; // "Hi-C" deep cool grey
                    }
                    break;

                case 2: // Blue-grey
                    switch ( nShade )
                    {
                        case 1: return 164; // Light blue-grey
                        case 2: return 165; // Blue-grey
                        case 3: return  79; // "Hi-C" slate grey
                        case 4: return  73; // "Hi-C" purple-grey
                    }
                    break;

                case 3: // Green/brown-grey
                    switch ( nShade )
                    {
                        case 1: return  84; // "Hi-C" tan-grey
                        case 2: return  85; // "Hi-C" brown-grey
                        case 3: return  77; // "Hi-C" jade-grey
                    }
                    break;

                case 4: // Verdant grey
                    switch ( nShade )
                    {
                        case 1: return 112; // Light verdant grey
                        case 2: return 113; // Verdant grey
                        case 3: return 114; // Deep verdant grey
                        case 4: return 115; // Dark verdant grey
                        case 5: return  81; // "Hi-C" verdant grey
                    }
                    break;

                case 5: // Warm grey
                    switch ( nShade )
                    {
                        case 1: return 124; // Light warm grey
                        case 2: return 125; // Warm grey
                        case 3: return 126; // Deep warm grey
                        case 4: return 127; // Dark warm grey
                        case 5: return  75; // "Hi-C" warm grey
                    }
                    break;

                case 6: // Ruddy grey
                    switch ( nShade )
                    {
                        case 1: return  12; // Light ruddy grey
                        case 2: return  13; // Ruddy grey
                        case 3: return  14; // Deep ruddy grey
                        case 4: return  15; // Dark ruddy grey
                        case 5: return  86; // "Hi-C" ruddy grey
                    }
            }//switch(nHue) - hued greys
            break;

        case 6: // True greys and multi-chromatics
            switch ( nHue )
            {
                case 1: // Grey
                    switch ( nShade )
                    {
                        case 1: return  20; // Light grey
                        case 2: return  21; // Grey
                        case 3: return  22; // Deep grey
                        case 4: return  23; // Dark grey
                        case 5: return  60; // "Hi-C" grey
                        case 6: return  56; // Metal light grey / light grey
                        case 7: return  57; // Metal dark grey / dark grey
                        // Metal colors
                        case 9: return   1; // Grey
                        case 0: return   2; // Deep grey
                    }
                    break;

                case 2: // Black or white
                    switch ( nShade )
                    {
                        case 1: return  45; // Charcoal
                        case 2: return 167; // Black
                        case 3: return  63; // Pure black
                        case 4: return  44; // White charcoal
                        case 5: return 166; // White
                        case 6: return  62; // Pure white
                    }
                    break;

                case 3: // Multi-chromatic
                    switch ( nShade )
                    {
                        case 1: return  87; // Dull fading to pink
                        case 2: return 175; // Speckled gold
                        case 3: return  61; // Mirrored
                        // Metal colors.
                        case 9: return  48; // Rainbow
                        case 0: return  49; // Dark rainbow
                    }
                    break;

                case 4: // Iron
                    switch ( nShade )
                    {
                        // Metal colors.
                        case 4: return   4; // Light iron
                        case 5: return   5; // Iron
                        case 6: return   6; // Deep iron
                        case 7: return   7; // Dark iron
                        case 8: return  54; // Aged iron
                        case 9: return  55; // Aged deep iron
                        case 0: return  60; // "Hi-C" iron
                    }
                    break;

                case 5: // Rust
                    switch ( nShade )
                    {
                        // Metal colors.
                        case 7: return  50; // Rust
                        case 8: return  52; // Dark rust
                        case 9: return  51; // Aged rust
                        case 0: return  53; // Aged dark rust
                    }
            }//switch(nHue) - TRUE greys and multi-chromatics
    }//switch(nFamily)

    // Unrecognized code.
    return -nSelection;
}


