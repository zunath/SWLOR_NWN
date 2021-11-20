INSERT INTO
  Perk (
    ID,
    Name,
    IsActive,
    BaseCastingTime,
    Description,
    PerkCategoryID,
    CooldownCategoryID,
    ExecutionTypeID,
    IsTargetSelfOnly,
    Enmity,
    EnmityAdjustmentRuleID,
    CastAnimationID,
    ForceBalanceTypeID
  )
VALUES
  (
    88,
    N'Speedy Crafting',
    1,
    0,
    N'Reduces the amount of time it takes to craft items.',
    4,
    NULL,
    0,
    0,
    0,
    0,
    NULL,
    0
  );
INSERT INTO
  PerkLevel (
    ID,
    PerkID,
    Level,
    Price,
    Description,
    SpecializationID
  )
VALUES
  (
    2319,
    88,
    1,
    4,
    N'+10% Crafting Speed',
    0
  );
INSERT INTO
  PerkLevel (
    ID,
    PerkID,
    Level,
    Price,
    Description,
    SpecializationID
  )
VALUES
  (
    2320,
    88,
    2,
    5,
    N'+20% Crafting Speed',
    0
  );
INSERT INTO
  PerkLevel (
    ID,
    PerkID,
    Level,
    Price,
    Description,
    SpecializationID
  )
VALUES
  (
    2321,
    88,
    3,
    6,
    N'+30% Crafting Speed',
    0
  );
INSERT INTO
  PerkLevel (
    ID,
    PerkID,
    Level,
    Price,
    Description,
    SpecializationID
  )
VALUES
  (
    2322,
    88,
    4,
    5,
    N'+40% Crafting Speed',
    0
  );
INSERT INTO
  PerkLevel (
    ID,
    PerkID,
    Level,
    Price,
    Description,
    SpecializationID
  )
VALUES
  (
    2323,
    88,
    5,
    6,
    N'+50% Crafting Speed',
    0
  );
INSERT INTO
  PerkLevel (
    ID,
    PerkID,
    Level,
    Price,
    Description,
    SpecializationID
  )
VALUES
  (
    2324,
    88,
    6,
    7,
    N'+60% Crafting Speed',
    0
  );
INSERT INTO
  PerkLevel (
    ID,
    PerkID,
    Level,
    Price,
    Description,
    SpecializationID
  )
VALUES
  (
    2325,
    88,
    7,
    8,
    N'+70% Crafting Speed',
    0
  );
INSERT INTO
  PerkLevel (
    ID,
    PerkID,
    Level,
    Price,
    Description,
    SpecializationID
  )
VALUES
  (
    2326,
    88,
    8,
    9,
    N'+80% Crafting Speed',
    0
  );
INSERT INTO
  PerkLevel (
    ID,
    PerkID,
    Level,
    Price,
    Description,
    SpecializationID
  )
VALUES
  (
    2327,
    88,
    9,
    10,
    N'+90% Crafting Speed',
    0
  );
INSERT INTO
  PerkLevel (
    ID,
    PerkID,
    Level,
    Price,
    Description,
    SpecializationID
  )
VALUES
  (
    2328,
    88,
    10,
    11,
    N'+99% Crafting Speed',
    0
  );
