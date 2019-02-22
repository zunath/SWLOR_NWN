

CREATE TABLE JukeboxSong(
	ID INT NOT NULL PRIMARY KEY,
	AmbientMusicID INT NOT NULL,
	FileName NVARCHAR(32) NOT NULL,
	DisplayName NVARCHAR(64) NOT NULL,
	IsActive BIT NOT NULL DEFAULT 0
)
GO

INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (1, 538, 'mus_alliance', 'DT Alliance Rebelle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (2, 345, 'mus_area_anint', 'Anchorhead', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (3, 433, 'mus_area_bespin', 'Theme, Bespin', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (4, 342, 'mus_area_cakr', 'Korriban Cave 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (5, 343, 'mus_area_cita1', 'Citadel Station 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (6, 344, 'mus_area_cita2', 'Citadel Station 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (7, 434, 'mus_area_corel', 'Theme, Corellia', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (8, 435, 'mus_area_creepy', 'Ambience, Creepy', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (9, 346, 'mus_area_daen1', 'Jedi Enclave K1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (10, 347, 'mus_area_daen2', 'Jedi Enclave K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (11, 348, 'mus_area_daex1', 'Dantooine K1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (12, 349, 'mus_area_daex2', 'Dantooine K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (13, 379, 'mus_area_dasu1', 'Jedi Enclave Sub', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (14, 436, 'mus_area_dath', 'Theme, Dathomir', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (15, 350, 'mus_area_desert', 'Dune Sea', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (16, 351, 'mus_area_dxun01', 'Dxun Ext 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (17, 352, 'mus_area_dxun02', 'Dxun Ext 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (18, 353, 'mus_area_ebonh01', 'Ebon Hawk K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (19, 354, 'mus_area_harbin', 'Harbinger Int 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (20, 355, 'mus_area_harlo', 'Harbinger Int 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (21, 356, 'mus_area_hrakert', 'Hrakert Facility', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (22, 437, 'mus_area_idtent1', 'Design Salon 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (23, 438, 'mus_area_idtent2', 'Design Salon 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (24, 439, 'mus_area_idtent3', 'Design Salon 3', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (25, 357, 'mus_area_jekt', 'Jek Jek Tar', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (26, 440, 'mus_area_jungle1', 'Jungle 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (27, 441, 'mus_area_jungle2', 'Jungle 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (28, 358, 'mus_area_kashy', 'Kashyyyk Canopy', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (29, 374, 'mus_area_koad1', 'Sith Academy K1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (30, 375, 'mus_area_koad2', 'Sith Academy K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (31, 360, 'mus_area_koex1', 'Korriban Ext K1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (32, 361, 'mus_area_koex2', 'Korriban Ext K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (33, 362, 'mus_area_kotri', 'Korriban Visions', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (34, 359, 'mus_area_ksha', 'Kashyyyk Shadow', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (35, 363, 'mus_area_leheon', 'Lehon Ext 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (36, 364, 'mus_area_manaan', 'Manaan Ext 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (37, 365, 'mus_area_mand', 'Mandalorian Camp', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (38, 366, 'mus_area_narshad', 'Nar Shaddaa Ext', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (39, 367, 'mus_area_ondiziz', 'Onderon Ext 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (40, 368, 'mus_area_ondpal', 'Onderon Palace', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (41, 369, 'mus_area_rktr1', 'Strange K1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (42, 370, 'mus_area_rktr2', 'Strange K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (43, 371, 'mus_area_ruins1', 'Rakatan Ruins', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (44, 372, 'mus_area_sandppl', 'Sand People Int 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (45, 378, 'mus_area_sf', 'Theme Starforge', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (46, 377, 'mus_area_slehey', 'Sleheyron Ext 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (47, 376, 'mus_area_sshp1', 'Theme Sith Ship', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (48, 442, 'mus_area_swmp1', 'Swamp 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (49, 443, 'mus_area_swmp2', 'Swamp 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (50, 380, 'mus_area_taap1', 'Taris Apartments', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (51, 381, 'mus_area_talo1', 'Taris Lower City', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (52, 382, 'mus_area_tase1', 'Taris Sewer', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (53, 383, 'mus_area_taup1', 'Taris Upper City', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (54, 339, 'mus_area_tav1', 'Cantina Iziz', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (55, 340, 'mus_area_tav2', 'Cantina Pazaak', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (56, 341, 'mus_area_tav3', 'Cantina Javyars', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (57, 384, 'mus_area_teacd', 'Telos Academy', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (58, 385, 'mus_area_trayus', 'Trayus Academy', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (59, 386, 'mus_area_yacht', 'Goto''s Yacht', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (60, 537, 'mus_as_love', 'DT Across the Stars', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (61, 526, 'mus_base_1', 'DT Base Imp�riale', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (62, 511, 'mus_bat_1', 'DT Bataille 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (63, 512, 'mus_bat_2', 'DT Bataille 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (64, 513, 'mus_bat_3', 'DT Bataille 3', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (65, 514, 'mus_bat_4', 'DT Bataille 4', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (66, 520, 'mus_bat_5', 'DT Bataille 5', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (67, 524, 'mus_bat_6', 'DT Bataille 6', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (68, 527, 'mus_bat_7', 'DT Bataille 7', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (69, 403, 'mus_bat_anchor', 'Anchorhead Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (71, 404, 'mus_bat_base', 'Base Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (73, 571, 'mus_bat_carkoon', 'DT Bataille de Carkoon', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (74, 515, 'mus_bat_chase', 'DT Poursuite', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (79, 191, 'mus_bat_Contact', 'd20 Batl Contact, DJohn (0:51)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (80, 201, 'mus_bat_d20_1', 'd20 Batl 1 (1:01)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (81, 202, 'mus_bat_d20_2', 'd20 Batl 2 (0:03)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (82, 203, 'mus_bat_d20_3', 'd20 Batl 3 (0:13)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (83, 204, 'mus_bat_d20_4', 'd20 Batl 4 (0:03)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (84, 205, 'mus_bat_d20_5', 'd20 Batl 5 (2:00)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (85, 299, 'mus_bat_d20_ccg1', 'd20 Batl C&C Generals (1:31)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (86, 304, 'mus_bat_d20_frnq', 'd20 Batl Fr Quarter Chase (1:27)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (87, 307, 'mus_bat_d20_hlk1', 'd20 Batl Hulk Crossroads (2:26)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (88, 308, 'mus_bat_d20_hlk2', 'd20 Batl Hulk Destruction (2:32)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (89, 309, 'mus_bat_d20_hlk3', 'd20 Batl Hulk Turning (2:32)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (90, 310, 'mus_bat_d20_ldw', 'd20 Batl Lady Death War (2:45)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (91, 311, 'mus_bat_d20_lin1', 'd20 Batl Lineage Conflict (2:06)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (92, 319, 'mus_bat_d20_swmp', 'd20 Batl Swamp (1:48)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (93, 322, 'mus_bat_d20_tlj1', 'd20 Batl TLJ Danger (1:32)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (94, 337, 'mus_bat_d20_zero', 'd20 Batl Zero Hour Theme (3:10)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (95, 405, 'mus_bat_daex1', 'Dantooine K1 Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (96, 270, 'mus_bat_dobcrusa', 'd20 Batl Crusader (1:31)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (97, 269, 'mus_bat_dob_keuf', 'd20 Batl Keufs (1:41)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (99, 525, 'mus_bat_ds_chase', 'DT Poursuite Death Star', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (100, 406, 'mus_bat_dunesea', 'Dune Sea Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (104, 407, 'mus_bat_dxun', 'Dxun Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (106, 572, 'mus_bat_endor', 'DT Bataille Endor', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (107, 408, 'mus_bat_fast01', 'Battle Fast 1 K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (108, 409, 'mus_bat_fast02', 'Battle Fast 2 K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (109, 531, 'mus_bat_fate', 'DT Dual of the fate', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (113, 211, 'mus_bat_fot_grov', 'd20 Batl FOT Groove (2:46)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (114, 206, 'mus_bat_f_rave', 'd20 Batl Oakenfold (4:48)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (115, 266, 'mus_bat_f_st_1', 'd20 Batl ST Khan (5:08)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (116, 267, 'mus_bat_f_st_2', 'd20 Batl ST Borg (3:57)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (117, 207, 'mus_bat_f_st_3', 'd20 Batl ST Kirk (1:48)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (118, 208, 'mus_bat_f_tech1', 'd20 Batl Techno 1 (0:13)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (119, 209, 'mus_bat_f_tech2', 'd20 Batl Techno 2 (0:17)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (120, 210, 'mus_bat_f_tense', 'd20 Batl FF Fight (0:25)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (121, 410, 'mus_bat_gen1', 'Battle Generic 1 K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (122, 411, 'mus_bat_gen2', 'Battle Generic 2 K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (123, 412, 'mus_bat_gen3', 'Battle Generic 3 K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (124, 532, 'mus_bat_hoth', 'DT Bataille Hoth', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (125, 413, 'mus_bat_hrakert', 'Hrakert Facility Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (126, 533, 'mus_bat_jedi_1', 'DT Bataille Jedi 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (127, 414, 'mus_bat_kashshad', 'Kashyyyk Shadow Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (128, 415, 'mus_bat_kfin', 'Theme Malak Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (129, 523, 'mus_bat_lair', 'DT Poursuite Etoile Noire', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (130, 416, 'mus_bat_large', 'Battle Large 1 K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (132, 417, 'mus_bat_manaan', 'Manaan Ext Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (133, 418, 'mus_bat_med', 'Battle Medium 1 K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (134, 271, 'mus_bat_mongoose', 'd20 Batl Mongoose (1:16)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (135, 573, 'mus_bat_nar', 'DT Bataille Nar Shaddaa', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (136, 419, 'mus_bat_narsha', 'Nar Shaddaa Ext Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (137, 421, 'mus_bat_ondi2', 'Onderon Ext Battle 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (138, 420, 'mus_bat_ondiziz', 'Onderon Ext Battle 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (139, 422, 'mus_bat_peragus', 'Peragus Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (140, 424, 'mus_bat_rakatemp', 'Lehon Int Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (141, 423, 'mus_bat_ruin1', 'Lehon Ext Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (142, 425, 'mus_bat_ruin2', 'Rakata Ruins Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (144, 426, 'mus_bat_sandppl', 'Sand People Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (145, 574, 'mus_bat_sith', 'DT Bataille Sith', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (146, 427, 'mus_bat_small', 'Battle Small 1 K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (147, 534, 'mus_bat_space_1', 'DT Bataille spatiale', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (148, 429, 'mus_bat_tarsewer', 'Taris Sewer Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (149, 428, 'mus_bat_taup1', 'Tarris Upper City Battle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (150, 430, 'mus_bat_urban01', 'Battle Urban 1 K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (151, 431, 'mus_bat_urban02', 'Battle Urban 2 K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (152, 432, 'mus_bat_urban03', 'Battle Urban 3 K2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (163, 530, 'mus_bat_yavin', 'DT Bataille Yavin', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (165, 190, 'mus_Caves', 'd20 Caves, DJohn (1:27)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (175, 272, 'mus_ctrlfreak', 'd20 Control Freak (3:32)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (176, 333, 'mus_d20_airvent', 'd20 Trapped Air Vent (3:48)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (177, 300, 'mus_d20_ccg_usa4', 'd20 C&C Generals USA 4 (3:05)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (178, 301, 'mus_d20_ccg_usa8', 'd20 C&C Generals USA 8 (3:13)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (179, 303, 'mus_d20_deserted', 'd20 Deserted TV Station (3:15)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (180, 302, 'mus_d20_destiny', 'd20 Call of Destiny (2:37)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (181, 212, 'mus_d20_drums01', 'd20 Drums 1 (0:19)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (182, 213, 'mus_d20_drums02', 'd20 Drums 2 (0:24)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (183, 214, 'mus_d20_drums03', 'd20 Drums 3 (0:28)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (184, 215, 'mus_d20_drums04', 'd20 Drums 4 (2:05)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (185, 216, 'mus_d20_hvykey01', 'd20 Hvy Keys 1 (1:53)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (186, 217, 'mus_d20_hvykey02', 'd20 Hvy Keys 2 (0:43)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (187, 218, 'mus_d20_hvykey03', 'd20 Hvy Keys 3 (0:41)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (188, 219, 'mus_d20_indust01', 'd20 Industrial 1 (0:43)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (189, 220, 'mus_d20_indust02', 'd20 Industrial 2 (0:13)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (190, 221, 'mus_d20_indust03', 'd20 Industrial 3 (0:08)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (191, 312, 'mus_d20_lineage1', 'd20 Lineage Ominous Vis  (3:00)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (192, 313, 'mus_d20_lineage2', 'd20 Lineage Trailer (3:17)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (193, 334, 'mus_d20_mellow', 'd20 Win Melodious(1:31)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (194, 314, 'mus_d20_nagasaki', 'd20 Nagasaki Dust (1:18)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (195, 298, 'mus_d20_newworld', 'd20 A New World (1:47)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (196, 222, 'mus_d20_piano01', 'd20 Piano (0:26)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (197, 305, 'mus_d20_recon1', 'd20 Ghost Recon Anthem (2:24)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (198, 306, 'mus_d20_recon2', 'd20 Ghost Recon Outro (1:10)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (199, 318, 'mus_d20_rio', 'd20 Rio (2:03)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (200, 315, 'mus_d20_rsix1', 'd20 Rainbow Six Bank (0:46)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (201, 316, 'mus_d20_rsix2', 'd20 Rainbow Six Intro (2:43)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (202, 317, 'mus_d20_rsix3', 'd20 Rainbow Six Remix (2:49)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (203, 223, 'mus_d20_sofkey01', 'd20 Soft Keys 1 (1:09)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (204, 224, 'mus_d20_sofkey02', 'd20 Soft Keys 2 (1:36)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (205, 225, 'mus_d20_sofkey03', 'd20 Soft Keys 3 (0:27)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (206, 226, 'mus_d20_sofkey04', 'd20 Soft Keys 4 (0:27)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (207, 227, 'mus_d20_sofkey05', 'd20 Soft Keys 5 (0:15)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (208, 320, 'mus_d20_temple', 'd20 Temple of the Moon (3:05)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (209, 321, 'mus_d20_tlj_alas', 'd20 TLJ Alais (1:40)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (210, 329, 'mus_d20_tlj_dolp', 'd20 TLJ Techno Dolphin (6:36)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (211, 330, 'mus_d20_tlj_drag', 'd20 TLJ Techno Dragon (5:06)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (212, 323, 'mus_d20_tlj_drum', 'd20 TLJ Keys & Drums (2:03)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (213, 331, 'mus_d20_tlj_eagl', 'd20 TLJ Techno Eagle (3:32)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (214, 324, 'mus_d20_tlj_eplg', 'd20 TLJ Epilogue (1:43)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (215, 325, 'mus_d20_tlj_orgn', 'd20 TLJ Organ (4:30)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (216, 326, 'mus_d20_tlj_pian', 'd20 TLJ Piano (0:41)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (217, 327, 'mus_d20_tlj_prlg', 'd20 TLJ Prologue (1:35)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (218, 332, 'mus_d20_tlj_shrk', 'd20 TLJ Techno Shark (7:34)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (219, 328, 'mus_d20_tlj_subw', 'd20 TLJ Subway (0:49)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (220, 335, 'mus_d20_wolfen1', 'd20 Wolfenstein Intro (2:53)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (221, 336, 'mus_d20_wolfen2', 'd20 Wolfenstein Main (1:05)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (222, 562, 'mus_dagobah', 'DT Dagobah', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (223, 552, 'mus_dark_deeds', 'DT Marche Obscure', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (224, 554, 'mus_dark_kreia', 'DT Dark Kreia', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (225, 555, 'mus_dark_maul', 'DT Dark Maul', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (226, 556, 'mus_dark_nihilus', 'DT Dark Nihilus', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (227, 557, 'mus_dark_sion', 'DT Dark Sion', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (228, 558, 'mus_dark_traya', 'DT Dark Traya', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (235, 517, 'mus_death', 'DT Mort', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (236, 273, 'mus_dob_bang', 'd20 Tense Bang (1:33)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (237, 274, 'mus_dob_bapteme', 'd20 Religious Choir (1:24)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (238, 275, 'mus_dob_btbdead', 'd20 Better Be Dead (1:35)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (239, 276, 'mus_dob_drunk', 'd20 Drunk (3:47)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (240, 277, 'mus_dob_feel_hot', 'd20 Feel Hot Nightclub (5:07)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (241, 278, 'mus_dob_higher', 'd20 Higher Mood (5:01)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (242, 279, 'mus_dob_musikbox', 'd20 Musik Box Nightclub (4:55)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (243, 518, 'mus_dyson', 'DT Drame', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (244, 280, 'mus_em_cheyenne', 'd20 Cheyenne (2:39)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (245, 551, 'mus_enter_vador', 'DT Ombre de Vador', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (248, 281, 'mus_fo1_birth', 'd20 FO1 Birth (3:24)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (249, 282, 'mus_fo1_cavern', 'd20 FO1 Cavern (3:53)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (250, 283, 'mus_fo1_children', 'd20 FO1 Children (3:17)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (251, 284, 'mus_fo1_desert', 'd20 FO1 Desert (3:20)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (252, 285, 'mus_fo1_follow', 'd20 FO1 Follow (2:59)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (253, 286, 'mus_fo1_glow', 'd20 FO1 Glow (3:57)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (254, 288, 'mus_fo1_hub', 'd20 FO1 Hub (4:03)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (255, 287, 'mus_fo1_junk', 'd20 FO1 Junk (3:24)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (256, 289, 'mus_fo1_labone', 'd20 FO1 LA Bone (3:46)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (257, 290, 'mus_fo1_master', 'd20 FO1 Masters (3:07)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (258, 291, 'mus_fo1_necro', 'd20 FO1 Necro (3:24)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (259, 292, 'mus_fo1_raider', 'd20 FO1 Raiders (3:18)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (260, 293, 'mus_fo1_shady', 'd20 FO1 Shady (4:04)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (261, 294, 'mus_fo1_vats', 'd20 FO1 Vats (3:18)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (262, 295, 'mus_fo1_vault', 'd20 FO1 Vault (4:01)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (263, 296, 'mus_fo1_wind', 'd20 FO1 Wind sound (0:05)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (264, 297, 'mus_fo1_wrldmap', 'd20 FO1 Worldmap (3:03)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (265, 542, 'mus_force', 'DT La Force', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (266, 541, 'mus_force_learn', 'DT Apprentissage Jedi', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (270, 228, 'mus_fot_moanlady', 'd20 FOT Moan Lady (2:16)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (271, 229, 'mus_fot_mystry01', 'd20 FOT Mystery 1 (1:33)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (272, 230, 'mus_fot_mystry02', 'd20 FOT Mystery 2 (0:24)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (273, 231, 'mus_fot_olcity01', 'd20 FOT Old City 1 (1:59)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (274, 232, 'mus_fot_olcity02', 'd20 FOT Old City 2 (1:12)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (275, 233, 'mus_fot_olcity03', 'd20 FOT Old City 3 (1:22)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (276, 234, 'mus_fot_olcity04', 'd20 FOT Old City 4 (1:31)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (277, 235, 'mus_fot_olcity05', 'd20 FOT Old City 5 (1:28)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (278, 236, 'mus_fot_olcity06', 'd20 FOT Old City 6 (1:22)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (279, 237, 'mus_fot_sht_bnyd', 'd20 FOT Boneyard (0:20)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (280, 238, 'mus_fot_sht_cave', 'd20 FOT Cave (0:22)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (281, 239, 'mus_fot_sht_dung', 'd20 FOT Dungeon (0:23)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (282, 240, 'mus_fot_sht_milt', 'd20 FOT Military (0:46)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (283, 241, 'mus_fot_sht_susp', 'd20 FOT Suspense (0:49)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (284, 242, 'mus_fot_susp01', 'd20 FOT Suspense 1 (2:06)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (285, 243, 'mus_fot_susp02', 'd20 FOT Suspense 2 (2:03)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (286, 244, 'mus_fot_susp03', 'd20 FOT Suspense 3 (0:24)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (287, 245, 'mus_fot_travel01', 'd20 FOT Travel 1 (0:49)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (288, 246, 'mus_fot_travel02', 'd20 FOT Travel 2 (0:49)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (289, 247, 'mus_fot_tribal01', 'd20 FOT Tribal 1 (1:39)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (290, 248, 'mus_fot_tribal02', 'd20 FOT Tribal 2 (1:37)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (291, 249, 'mus_fot_ugbase01', 'd20 FOT UG Base 1 (1:21)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (292, 250, 'mus_fot_ugbase02', 'd20 FOT UG Base 2 (1:18)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (293, 251, 'mus_fot_wastes01', 'd20 FOT Wastes 1 (1:22)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (294, 252, 'mus_fot_wastes02', 'd20 FOT Wastes 2 (1:25)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (295, 253, 'mus_fot_wastes03', 'd20 FOT Wastes 3 (1:30)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (296, 254, 'mus_fot_wastes04', 'd20 FOT Wastes 4 (1:21)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (297, 255, 'mus_fot_wastes05', 'd20 FOT Wastes 5 (1:25)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (298, 256, 'mus_fot_wastes06', 'd20 FOT Wastes 6 (1:22)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (299, 257, 'mus_f_fiddle', 'd20 FF Fiddle (2:26)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (300, 258, 'mus_f_ser_fables', 'd20 Space Fables (21:31)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (301, 259, 'mus_f_ser_muroc', 'd20 Space Muroc (6:31)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (302, 260, 'mus_f_ser_river', 'd20 Space River (10:02)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (303, 261, 'mus_f_st_doom', 'd20 ST Doom (1:36)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (304, 262, 'mus_f_st_sad', 'd20 ST Sad (1:13)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (305, 263, 'mus_f_st_susp_01', 'd20 ST Suspense 1 (4:19)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (306, 264, 'mus_f_st_susp_02', 'd20 ST Suspense 2 (0:57)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (307, 265, 'mus_f_susp_trap', 'd20 FF Suspense Trap (1:06)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (308, 268, 'mus_f_sw_trash', 'd20 SW Trash Mash (4:36)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (310, 192, 'mus_HonorBound', 'd20 Honor Bound, DJohn (2:14)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (311, 563, 'mus_hoth', 'DT Hoth', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (312, 492, 'mus_imp_march', '*Imperial March', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (313, 540, 'mus_jedi_train', 'DT Entrainement Jedi', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (314, 480, 'mus_jk_atl1', 'Atlantica 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (315, 481, 'mus_jk_atl2', 'Atlantica 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (316, 482, 'mus_jk_bit', 'Blue Ice', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (317, 561, 'mus_jungle', 'DT Jungle', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (318, 539, 'mus_lando', 'DT Lando Calrissian', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (319, 546, 'mus_leia', 'DT Leia Organa', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (320, 547, 'mus_luke', 'DT Luke Skywalker', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (321, 545, 'mus_luke_leia', 'DT Luke et Leia', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (324, 193, 'mus_moondepths', 'd20 MoonseaDepths, DJohn (1:39)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (325, 565, 'mus_mos_eisley', 'DT Mos Eisley', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (326, 564, 'mus_nar', 'DT Nar Shaddaa', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (327, 522, 'mus_nebula', 'DT Nebula', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (328, 194, 'mus_ponr', 'd20 PointOfNoReturn, DJohn (2:05)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (334, 550, 'mus_sith_teach', 'DT Apprentissage Sith', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (336, 444, 'mus_sunrise_a', 'Sunrise 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (337, 445, 'mus_sunrise_b', 'Sunrise 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (338, 446, 'mus_sunset_a', 'Sunset 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (339, 447, 'mus_sunset_b', 'Sunset 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (340, 448, 'mus_sw_area1', 'Explore 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (341, 509, 'mus_sw_bar1', 'DT Cantina 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (342, 510, 'mus_sw_bar2', 'DT Cantina 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (343, 559, 'mus_sw_bar3', 'DT Cantina 3', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (344, 560, 'mus_sw_bar4', 'DT Cantina 4', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (345, 575, 'mus_sw_bar5', 'DT Cantina 5', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (346, 576, 'mus_sw_bar6', 'DT Cantina 6', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (347, 577, 'mus_sw_bar7', 'DT Cantina 7', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (348, 449, 'mus_sw_baroq', 'Baroque Recital', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (349, 450, 'mus_sw_celeb', 'Celebration', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (350, 451, 'mus_sw_gen1', 'Generic 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (351, 452, 'mus_sw_gen2', 'Generic 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (352, 453, 'mus_sw_gen3', 'Generic 3', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (353, 454, 'mus_sw_gloom1', 'Gloom 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (354, 455, 'mus_sw_gloom2', 'Gloom 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (355, 456, 'mus_sw_humor1', 'Humor 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (356, 578, 'mus_sw_race1', 'DT Race 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (357, 579, 'mus_sw_race2', 'DT Race 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (358, 580, 'mus_sw_race3', 'DT Race 3', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (359, 581, 'mus_sw_race4', 'DT Race 4', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (360, 582, 'mus_sw_race5', 'DT Race 5', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (361, 583, 'mus_sw_race6', 'DT Race 6', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (362, 457, 'mus_sw_rebo1', 'Max Rebo 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (363, 458, 'mus_sw_rebo2', 'Max Rebo 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (364, 459, 'mus_sw_xmas1', 'Holidays 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (365, 460, 'mus_sw_xmas2', 'Holidays 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (366, 461, 'mus_sw_zombi', 'Theme, Zombie', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (367, 568, 'mus_tatooine1', 'DT Tatooine 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (368, 569, 'mus_tatooine2', 'DT Tatooine 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (369, 570, 'mus_tatooine3', 'DT Tatooine 3', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (370, 535, 'mus_tat_town', 'DT Tatooine Ville', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (385, 390, 'mus_theme_czerka', 'Theme Czerka', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (386, 462, 'mus_theme_dotf', 'Duel of the Fates', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (387, 463, 'mus_theme_emp', 'Theme, Emperor', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (388, 464, 'mus_theme_endor', 'Theme, Endor', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (389, 465, 'mus_theme_exar', 'Theme, Exar', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (390, 466, 'mus_theme_fevil', 'Theme, Force Evil', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (391, 467, 'mus_theme_fgood', 'Theme, Force Good', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (392, 468, 'mus_theme_fneut', 'Theme, Force Neutral', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (393, 469, 'mus_theme_force', 'Theme, Force', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (394, 470, 'mus_theme_griev', 'Theme, Grievous', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (395, 471, 'mus_theme_hk47', 'Theme, HK-47', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (396, 391, 'mus_theme_kreia', 'Theme Kreia', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (397, 472, 'mus_theme_lando', 'Theme, Lando', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (398, 338, 'mus_theme_main', 'NWN Main Theme (2:29)', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (399, 394, 'mus_theme_malak', 'Theme Malak', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (401, 473, 'mus_theme_mnda', 'Mando''a Chant', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (403, 474, 'mus_theme_myst', 'Mystery 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (405, 475, 'mus_theme_opra', 'Opera 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (406, 476, 'mus_theme_palp', 'Theme, Palpatine', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (407, 396, 'mus_theme_rep', 'Theme Republic', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (408, 477, 'mus_theme_sad', 'Tragic 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (409, 397, 'mus_theme_sion', 'Theme Sion', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (410, 398, 'mus_theme_sith', 'Theme Sith', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (411, 478, 'mus_theme_trail', 'Ryyatt Trail', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (412, 401, 'mus_theme_traya', 'Theme Traya', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (413, 479, 'mus_theme_trgc', 'Tragic 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (414, 402, 'mus_theme_tyrus', 'Theme Tyrus', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (415, 400, 'mus_thme_andw', 'Theme March', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (416, 387, 'mus_thme_bast', 'Theme Bastila', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (417, 388, 'mus_thme_carth', 'Theme Carth', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (418, 389, 'mus_thme_coun1', 'Theme Council', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (419, 393, 'mus_thme_kass1', 'Theme Assassins', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (420, 392, 'mus_thme_lend1', 'Theme Lightside', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (421, 395, 'mus_thme_nih', 'Theme Nihilus', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (422, 399, 'mus_thme_tele1', 'Theme Telepathy', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (423, 483, 'mus_thm_rc1', 'Mando''a 1', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (424, 484, 'mus_thm_rc2', 'Mando''a 2', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (425, 521, 'mus_title', 'DT SW Title', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (426, 536, 'mus_tore', 'DT Tore Mandalorien', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (427, 549, 'mus_troopers', 'DT Stormtroopers', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (428, 566, 'mus_tusken_at', 'DT Camp Tusken', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (429, 567, 'mus_tusken_land', 'DT Tusken', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (430, 553, 'mus_vador_rise', 'DT Dark Vador', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (431, 528, 'mus_vict_long', 'DT Fanfare C�r�monie', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (432, 529, 'mus_vict_short', 'DT Victoire', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (456, 544, 'mus_yan_and_leia', 'DT Yan Solo et Leia', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (457, 548, 'mus_yoda', 'DT Yoda', 1);
INSERT INTO dbo.JukeboxSong (ID, AmbientMusicID, FileName, DisplayName, IsActive) VALUES (458, 543, 'mus_yoda_force', 'DT Yoda et la Force', 1);





INSERT INTO dbo.BaseStructure ( ID ,
                                BaseStructureTypeID ,
                                Name ,
                                PlaceableResref ,
                                ItemResref ,
                                IsActive ,
                                Power ,
                                CPU ,
                                Durability ,
                                Storage ,
                                HasAtmosphere ,
                                ReinforcedStorage ,
                                RequiresBasePower ,
                                ResourceStorage ,
                                RetrievalRating ,
                                FuelRating ,
                                DefaultStructureModeID )
VALUES ( 5 ,    -- ID - int
         8 ,    -- BaseStructureTypeID - int
         N'Jukebox' ,  -- Name - nvarchar(64)
         N'jukebox' ,  -- PlaceableResref - nvarchar(16)
         N'furniture' ,  -- ItemResref - nvarchar(16)
         1 , -- IsActive - bit
         0.0 ,  -- Power - float
         0.0 ,  -- CPU - float
         0.0 ,  -- Durability - float
         0 ,    -- Storage - int
         0 , -- HasAtmosphere - bit
         0 ,    -- ReinforcedStorage - int
         0 , -- RequiresBasePower - bit
         0 ,    -- ResourceStorage - int
         0 ,    -- RetrievalRating - int
         0 ,    -- FuelRating - int
         0      -- DefaultStructureModeID - int
    )

INSERT INTO dbo.CraftBlueprint ( ID ,
                                 CraftCategoryID ,
                                 BaseLevel ,
                                 ItemName ,
                                 ItemResref ,
                                 Quantity ,
                                 SkillID ,
                                 CraftDeviceID ,
                                 PerkID ,
                                 RequiredPerkLevel ,
                                 IsActive ,
                                 MainComponentTypeID ,
                                 MainMinimum ,
                                 SecondaryComponentTypeID ,
                                 SecondaryMinimum ,
                                 TertiaryComponentTypeID ,
                                 TertiaryMinimum ,
                                 EnhancementSlots ,
                                 MainMaximum ,
                                 SecondaryMaximum ,
                                 TertiaryMaximum ,
                                 BaseStructureID )
VALUES ( 682 ,    -- ID - int
         0 ,    -- CraftCategoryID - int
         7 ,    -- BaseLevel - int
         N'Jukebox' ,  -- ItemName - nvarchar(64)
         N'furniture' ,  -- ItemResref - nvarchar(16)
         1 ,    -- Quantity - int
         15 ,    -- SkillID - int
         5 ,    -- CraftDeviceID - int
         2 ,    -- PerkID - int
         3 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         43 ,    -- MainComponentTypeID - int
         2 ,    -- MainMinimum - int
         0 ,    -- SecondaryComponentTypeID - int
         0 ,    -- SecondaryMinimum - int
         0 ,    -- TertiaryComponentTypeID - int
         0 ,    -- TertiaryMinimum - int
         0 ,    -- EnhancementSlots - int
         0 ,    -- MainMaximum - int
         0 ,    -- SecondaryMaximum - int
         0 ,    -- TertiaryMaximum - int
         5      -- BaseStructureID - int
    )
