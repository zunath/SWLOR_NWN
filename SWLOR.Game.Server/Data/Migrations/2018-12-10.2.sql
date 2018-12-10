  UPDATE dbo.SpawnObject
  SET BehaviourScript = 'PackAggroBehaviour'
  WHERE NPCGroupID IS NOT NULL
  UPDATE dbo.SpawnObject
  SET BehaviourScript = 'SightAggroBehaviour'
  WHERE Resref = 'mynock' OR Resref = 'malsecdroid' OR Resref = 'malspiderdroid' OR Resref = 'colicoidexp'