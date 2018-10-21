CREATE TABLE [dbo].[GrowingPlants] (
    [GrowingPlantID]      INT           IDENTITY (1, 1) NOT NULL,
    [PlantID]             INT           NOT NULL,
    [RemainingTicks]      INT           CONSTRAINT [DF__GrowingPl__Remai__690797E6] DEFAULT ((0)) NOT NULL,
    [LocationAreaTag]     NVARCHAR (64) CONSTRAINT [DF__GrowingPl__Locat__69FBBC1F] DEFAULT ('') NOT NULL,
    [LocationX]           FLOAT (53)    CONSTRAINT [df_GrowingPlants_LocationX] DEFAULT ((0.0)) NOT NULL,
    [LocationY]           FLOAT (53)    CONSTRAINT [df_GrowingPlants_LocationY] DEFAULT ((0.0)) NOT NULL,
    [LocationZ]           FLOAT (53)    CONSTRAINT [df_GrowingPlants_LocationZ] DEFAULT ((0.0)) NOT NULL,
    [LocationOrientation] FLOAT (53)    CONSTRAINT [df_GrowingPlants_LocationOrientation] DEFAULT ((0.0)) NOT NULL,
    [DateCreated]         DATETIME2 (7) CONSTRAINT [DF__GrowingPl__DateC__6EC0713C] DEFAULT (getutcdate()) NOT NULL,
    [IsActive]            BIT           CONSTRAINT [DF__GrowingPl__IsAct__6FB49575] DEFAULT ((1)) NOT NULL,
    [TotalTicks]          INT           CONSTRAINT [DF__GrowingPl__Total__7869D707] DEFAULT ((0)) NOT NULL,
    [WaterStatus]         INT           CONSTRAINT [DF__GrowingPl__Water__7B4643B2] DEFAULT ((0)) NOT NULL,
    [LongevityBonus]      INT           CONSTRAINT [DF__GrowingPl__Longe__7C3A67EB] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK__GrowingP__807B119175152584] PRIMARY KEY CLUSTERED ([GrowingPlantID] ASC),
    CONSTRAINT [FK_GrowingPlants_PlantID] FOREIGN KEY ([PlantID]) REFERENCES [dbo].[Plants] ([PlantID])
);

