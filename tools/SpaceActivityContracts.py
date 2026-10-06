"""Authoritative objectives, route cadence and ownership for space contracts."""
OPERATIONS = {
    "Starter patrol": dict(id="patrol", legs=3, kills=["starter_pirate"]*3, region="starter", reputation=5),
    "Advanced bounty": dict(id="bounty", legs=3, kills=["advanced_interceptor","electronic_raider","elite_heavy_target"], region="advanced", reputation=10),
    "Selective prospecting": dict(id="prospecting", legs=3, ore=200, reserve=300, composition={"ore_tilarium":.60,"ore_idailia":.40}, region="advanced", reputation=10),
    "Bulk expedition with escort": dict(id="bulk", legs=3, ore=600, reserve=900, composition={"ore_tilarium":.70,"ore_currian":.20,"ore_idailia":.10}, kills=["routine_interceptor"], region="advanced", reputation=15),
    "Routine salvage": dict(id="salvage", legs=3, wrecks=4, bulk=144, attempts=12, region="advanced", reputation=10),
    "Survey and anomaly": dict(id="survey", legs=3, surveys=3, region="advanced", reputation=10),
    "Resource delivery": dict(id="delivery", legs=3, freight=60, region="starter", reputation=5),
    "Sealed freight": dict(id="freight", legs=3, freight=120, kills=["routine_interceptor"], region="advanced", reputation=10),
    "Escort and rescue": dict(id="rescue", legs=3, kills=["routine_interceptor"]*3, rescue=30, region="advanced", reputation=15),
    "Boarding operation": dict(id="boarding", legs=3, kills=["routine_interceptor"]*2, boarding=True, boarding_target="elite_heavy_target", boarding_consoles=3, boarding_console_seconds=6, boarding_seconds=90, boarding_preparation=2, boarding_range=20, boarding_hull_fraction=.20, boarding_precision_per_console=11, boarding_ore_per_console=1, region="advanced", reputation=15),
    "Capital fleet objective": dict(id="fleet", legs=3, kills=["advanced_interceptor","electronic_raider","fleet_objective"], surveys=1, region="advanced", reputation=20),
}
def expand(activities):
    for row in activities:
        row.update(OPERATIONS[row["name"]])
        row.update(minimum_seconds=row["seconds"], expiry_seconds=7200, leg_interval=row["seconds"]//row["legs"], leg_radius=10, route_minimum_distance=40, load_seconds=30, unload_seconds=30, freight_deposit=60 if row.get("freight") else 0, navigation_fee=10)
        row["service"] += row["legs"]*row["navigation_fee"]
    return activities
