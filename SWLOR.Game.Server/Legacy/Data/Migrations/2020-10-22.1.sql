DROP procedure IF EXISTS `ADM_RefundPlayerPerk`;

CREATE PROCEDURE `ADM_RefundPlayerPerk` (
	IN perkID int 
)
BEGIN
 
    CREATE TEMPORARY TABLE IF NOT EXISTS PlayerSPRefunds AS (
        SELECT 
            pcp.PlayerID,
            SUM(pl.Price) AS sum_pl_price
        FROM PCPerk pcp
        JOIN PerkLevel pl ON pl.PerkID = pcp.PerkID AND pl.Level <= pcp.PerkLevel
        WHERE pcp.PerkID = perkID
        GROUP BY pcp.PlayerID
    );
    
    UPDATE Player p
    INNER JOIN PlayerSPRefunds sp on PlayerID = sp.PlayerID
    SET p.UnallocatedSP = p.UnallocatedSP + sum_pl_price
    WHERE p.ID = sp.PlayerID;
    
    CREATE TEMPORARY TABLE IF NOT EXISTS ToRemove (
        SELECT ID
		FROM PCPerk pcp
        JOIN PlayerSPRefunds pspr ON pspr.PlayerID = pcp.PlayerID
		WHERE pcp.PerkID = perkID
    );
    
    DELETE pcp.*
    FROM PCPerk pcp
    WHERE pcp.ID IN (
		SELECT ID FROM ToRemove
    );
END
