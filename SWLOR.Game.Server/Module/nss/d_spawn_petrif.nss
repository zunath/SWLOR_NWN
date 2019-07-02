/**********************************/
/*         d_spawn_petrif
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/**********************************/
/*  Used to paralyze the faction
/*  referance objects.
/**********************************/

void main()
{
    ApplyEffectToObject (DURATION_TYPE_PERMANENT, EffectPetrify(), OBJECT_SELF);
    ApplyEffectToObject (DURATION_TYPE_PERMANENT, EffectInvisibility (INVISIBILITY_TYPE_NORMAL), OBJECT_SELF);
}
