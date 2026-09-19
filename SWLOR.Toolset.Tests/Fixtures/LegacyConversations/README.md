# Legacy conversation test inputs

These frozen examples exercise importing Aurora DLG files and editing native conversations from external modules. They cover shared replies, old scripts and tokens, dispatcher errors, quest guards, and lossless GFF editing. They are test data only: they are never packed, regenerated, or synchronized with gameplay content.

Live conversations are authored exclusively in `SWLOR.Game.Server/ConversationData/*.conversation.json`. Do not update these fixtures when changing gameplay dialogue. Tests of live quests and conversations must read the SWLOR graphs. The one native gameplay exception, `dmfi_universal`, is read directly from `Module/dlg` rather than copied here.
