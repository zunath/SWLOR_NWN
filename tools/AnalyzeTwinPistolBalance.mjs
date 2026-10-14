// Formula and resource-budget analysis for the Twin Pistol feedback proposal.
// Run: node tools/AnalyzeTwinPistolBalance.mjs
//
// This is NOT the game's combat engine or a release-balance test.
// Damage range and delay arithmetic mirror Combat; expected hit/critical
// outcomes, steady-state trigger uptime and fractional resource refunds are
// approximations. No animation lockout, deflection, Guard, resistance, capstone,
// enhancement procs, movement, Balanced Grip or Changing Angles is simulated.
// Fixed rotations skip unfunded opportunities; they are not optimized.
// Barrage selects stance on/off for the better ordinary-shot output, so Haste
// cap cases do not force it to pay a penalty for an unavailable speed increase.
//
// Reference: DMG 22, Delay 250, equal attributes/Attack/Defense, 85% hit,
// rank-50 skill, 120 seconds, MGT 26, +12 max STM, +5 STM regen/30s.
// Pistol: Doublehand III, Gambler, Lucky Chamber averaged to +2.5pp,
// Rapid Shot assumed active, Reload Tempo duty approximated from two rolls,
// High Noon active for 30% of the fight, ability crit swept 22/30/37% to
// represent Deadeye Reload (before Quick Draw/Fan bonuses).
// Twin: Dual Wield III, Barrage +8% Haste OR Crossfire +25% ordinary damage.
// The two handling purchases are mutually exclusive; hybrids choose one.
// Barrage stance +10% starting haste, +2% per stack to +20%, ordinary damage -15%.
// Seeded ordinary-hit samples model ramp startup, the 2s stack grant gate,
// 6s expiry, and the three-hit focus/sustain triggers against one tracked target.
// Target-switch resets and action lockout remain excluded.
// Crossfire stance
// extends Momentum to 18s / +10% haste / 10% less ordinary damage.
// Barrage sustain follows sampled landed hits, capped at 1 STM/6s. Slide/Rush
// build expected Momentum (maximum 3); Grapple consumes it for +6 power per
// charge per gun, and Follow Through refunds 1 STM per charge if any shot hits.
// All movement casts are assumed legal, using a 2s movement lockout. Geometry,
// travel time, Momentum's ordinary ACC, Changing Angles and evade charges are
// excluded. Fractional charges approximate hit uncertainty; no damage-triggered
// Cool Under Fire refunds are assumed. Capstones remain excluded.
// Maximum legal combined investment is 119 SP including optional Armor,
// versus 69 for a standalone line. Hybrid rows are not equal-SP comparisons.

import assert from 'node:assert/strict';

function mean(d,c,ratio=1,delta=0,high=false) {
 let hi=Math.max(0,(d+delta)*Math.min(3.625,Math.max(.01,ratio)));
 let n=(Math.max(1,Math.floor(.7*hi))+Math.max(1,Math.floor(hi)))/2;
 let k=(Math.max(1,Math.floor(hi))+Math.max(1,Math.floor(hi*1.25)))/2;
 return n*(1-c)+k*c*(high?1.15:1);
}
function delay(paired,haste,offReduction=30) {
 const conv=x=>Math.trunc(x/60*1000), pct=(x,p)=>x-Math.trunc(x*p/100);
 const ref=conv(250)-1750;
 const href=Math.max(584,pct(conv(250),Math.max(-50,Math.min(50,haste)))-1750);
 if(!paired)return href/1000;
 const off=pct(250,offReduction);
 return Math.max(584,Math.round((conv(105+145+Math.max(0,off-105))-1750)*href/ref))/1000;
}

function ordinaryWindow({seconds,h,intervalAt,haste,weapons,damage,damageBonus,penalty,barrage,ramp,samples=64}) {
 let total=0;
 const stamina=Array(Math.ceil(seconds)).fill(0);
 for(let sample=0;sample<samples;sample++) {
  let seed=sample+1, stacks=0, lastHit=-Infinity, nextGrant=0, focusHits=0, restoreHits=0, nextRestore=0;
  const landed=()=>{ seed=(Math.imul(1664525,seed)+1013904223)>>>0; return seed/2**32<h; };
  const hit=time=>{
   if(time-lastHit>=6){stacks=0;focusHits=0;}
   if(!landed())return;
   const focus=barrage&&focusHits>=3?5:0;
   total+=damage*(1+(damageBonus+focus)/100)*penalty;
   lastHit=time;
   focusHits++;
   if(ramp&&time>=nextGrant){stacks=Math.min(5,stacks+1);nextGrant=time+2;}
   if(barrage&&++restoreHits>=3){
    restoreHits=0;
    if(time>=nextRestore){stamina[Math.floor(time)]+=1/samples;nextRestore=time+6;}
   }
  };
  for(let time=0;time<seconds;) {
   if(time-lastHit>=6)stacks=0;
   const interval=intervalAt(haste+(ramp?2*stacks:0));
   hit(time);
   if(weapons===2&&time+.45<seconds)hit(time+.45);
   time+=interval;
  }
 }
 return {dps:total/samples/seconds,stamina};
}

function budget({build="pistol",targets=1,h=.85,ext=0,might=26,gear=12,food=5,d=22,critAbility=.30,seconds=120,support=0,crossCrit=false,ratio=1,delta=0,crossStance=true,handling="default",weaponCount=2}) {
 const pistol=build==="pistol", barrage=build==="barrage", hybrid=build==="hybrid", cross=build==="crossfire";
 const b=barrage||hybrid;
 const generic=pistol||crossCrit;
 const autoCrit=.10+(pistol?.12:0)+(generic?.025:0);
 const abilityCrit=generic?critAbility:.10;
 const reloadUptime=generic?1-(1-h*autoCrit)**2:0;
 const weapons=pistol?1:weaponCount;
 const selectedHandling=pistol?"none":handling==="default"?(b?"haste":"damage"):handling;
 assert.ok(["none","haste","damage"].includes(selectedHandling));
 const intervalAt=haste=>(1-reloadUptime)*delay(weapons===2,haste)+reloadUptime*delay(weapons===2,haste+20);
 const externalH=ext+(generic?7:0);
 const handlingHaste=selectedHandling==="haste"?8:0;
 const barrageStance=b&&.85/intervalAt(externalH+handlingHaste+20)>1/intervalAt(externalH+handlingHaste);
 const dmgRating=pistol?Math.ceil(d*1.6):d;
 const avgAuto=.7*mean(dmgRating,autoCrit,ratio,delta)+.3*mean(dmgRating,autoCrit,ratio,delta,generic);
 const ordinary=ordinaryWindow({seconds,h,intervalAt,haste:externalH+handlingHaste+(barrageStance?10:(cross&&crossStance?10:0)),weapons,damage:avgAuto,damageBonus:selectedHandling==="damage"?25:0,penalty:barrageStance?.85:(cross&&crossStance?.90:1),barrage:b,ramp:barrageStance});
 const autoDps=ordinary.dps;
 const multi=targets>1;
 const a=(power,c=abilityCrit)=>h*(.7*mean(power,c,ratio,delta)+.3*mean(power,c,ratio,delta,generic));
 const specs=pistol?[
  {id:"burst",cd:24,cost:10,hits:2,critical:abilityCrit,damage:2*a(dmgRating+20),refund:4*(1-(1-h*abilityCrit)**2)},
  {id:"volley",cd:18,cost:12,hits:1,critical:Math.min(.95,abilityCrit+.18),damage:a(dmgRating+38,Math.min(.95,abilityCrit+.18)),refund:0},
  {id:"sweep",cd:30,cost:9,hits:Math.min(3,targets),critical:Math.min(.95,abilityCrit+.12),damage:Math.min(3,targets)*a(dmgRating+26,Math.min(.95,abilityCrit+.12)),refund:0}
 ]:b&&!multi?[
  {id:"burst",cd:24,cost:10,damage:weapons*a(d+34),refund:0},
  {id:"volley",cd:18,cost:10,damage:weapons*a(d+26),refund:0},
  {id:"sweep",cd:30,cost:9,damage:weapons*a(Math.floor(.75*d)+13),refund:3*(1-(1-h)**weapons)}
 ]:[
  {id:"burst",cd:24,cost:8,damage:weapons*a(d+20),refund:0,movement:true,spendsMomentum:true},
  {id:"volley",cd:18,cost:10,damage:weapons*a(d+26),refund:0,movement:true,buildsMomentum:true,hits:weapons},
  {id:"sweep",cd:30,cost:10,damage:weapons*Math.min(3,targets)*a(Math.floor(.60*d)+12),refund:0,movement:true,buildsMomentum:true,hits:weapons*Math.min(3,targets)}
 ];
 // Barrage retains single-target actions against groups.
 if(barrage&&multi)return budget({build,targets:1,h,ext,might,gear,food,d,critAbility,seconds,support,crossCrit,ratio,delta,crossStance,handling,weaponCount});
 let stm=10+Math.floor(might*3/2)+gear,max=stm,total=0,casts=0,missed=0,rem=0;
 let counts={},momentum=0,momentumUntil=-1,nextMove=0,spentMomentum=0;
 const momentumWindow=cross&&crossStance?18:12;
 for(let t=0;t<seconds;t++){
  if(t>=momentumUntil)momentum=0;
  if(t>0&&t%6===0){let available=10+Math.floor(might/4)+food+rem;stm=Math.min(max,stm+Math.floor(available/5));rem=available%5;}
  if(t>0&&t%4===0&&support)stm=Math.min(max,stm+support);
  stm=Math.min(max,stm+ordinary.stamina[t]);
  for(let i=0;i<specs.length;i++){
   let s=specs[i],offset=s.movement?i*2:i; if(t<offset||(t-offset)%s.cd!==0)continue;
   if(s.movement&&t<nextMove){missed++;continue;}
   const gamblerSurcharge=pistol?2*(1-h*s.critical)**s.hits:0;
   let cost=Math.max(0,s.cost+gamblerSurcharge);
   if(stm<cost+4){missed++;continue;}
   const charges=s.spendsMomentum?momentum:0;
   stm-=cost;total+=s.spendsMomentum?weapons*a(d+20+6*charges):s.damage;casts++;counts[s.id]=(counts[s.id]||0)+1;
   stm=Math.min(max,stm+s.refund);
   if(s.movement)nextMove=t+2;
   if(s.spendsMomentum){
    spentMomentum+=charges;
    stm=Math.min(max,stm+Math.min(cost-1,charges)*(1-(1-h)**weapons));
    momentum=0;momentumUntil=-1;
   }
   if(s.buildsMomentum){
    momentum=Math.min(3,momentum+1-(1-h)**s.hits);momentumUntil=t+momentumWindow;
   }
  }
 }
 return {build,handling:selectedHandling,weapons,seconds,investmentSP:pistol?69:hybrid?119:69,targets,barrageStance,autoDps,abilityDps:total/seconds,dps:autoDps+total/seconds,endingSTM:stm,casts,missed,spentMomentum,counts};
}

assert.equal(delay(false,0),2.416);
assert.equal(delay(true,0,0),4.833);
assert.equal(delay(true,0,30),3.583);
assert.equal(Math.round(mean(36,.1)/delay(false,0)*100)/100,13.04);
const summary=[];
for(const build of ["pistol","barrage","crossfire","hybrid"])
 for(const targets of [1,3])summary.push({...budget({build,targets}),targets});
const openings=[];
for(const seconds of [6,12])for(const build of ["pistol","barrage","crossfire"])
 openings.push(budget({build,seconds}));
const handlingChoices=[];
for(const build of ["barrage","crossfire","hybrid"])for(const handling of ["haste","damage"])
 handlingChoices.push(budget({build,handling}));
const ordinaryCrossSkillTradeoffs=[];
for(const weapon of ["single weapon / rifle","paired melee / pistols"])for(const externalHaste of [0,15,30,50]){
 const paired=weapon.startsWith("paired");
 const baseline=delay(paired,externalHaste);
 ordinaryCrossSkillTradeoffs.push({weapon,externalHaste,
  hasteHandlingRatio:baseline/delay(paired,externalHaste+8),
  damageHandlingRatio:1.25,
  akimboStartRatio:.85*baseline/delay(paired,externalHaste+10),
  akimboFullRatio:.85*baseline/delay(paired,externalHaste+20),
  crossfireRatio:.90*baseline/delay(paired,externalHaste+10)});
}
const sensitivity=[];
for(const h of [.65,.85,.95])for(const ext of [0,15,30,50])for(const critAbility of [.22,.30,.37]){
 const p=budget({h,ext,critAbility}),b=budget({build:"barrage",h,ext,critAbility}),c=budget({build:"crossfire",h,ext,critAbility}),p3=budget({h,ext,critAbility,targets:3}),c3=budget({build:"crossfire",h,ext,critAbility,targets:3});
 sensitivity.push({h,ext,critAbility,barrageToPistol:b.dps/p.dps,crossSoloToBarrage:c.dps/b.dps,crossMultiToPistol:c3.dps/p3.dps});
}
const stress=[];
for(const h of [.65,.85,.95])for(const ratio of [.5,1,2,3.625])for(const delta of [-7,0,7])for(const resource of ["low","geared","supported"]){
 const options={h,ratio,delta,might:resource==="low"?10:26,food:resource==="low"?0:5,gear:resource==="low"?0:12,support:resource==="supported"?2:0};
 const p=budget(options),b=budget({...options,build:"barrage"}),c=budget({...options,build:"crossfire",targets:3}),p3=budget({...options,targets:3});
 stress.push({h,ratio,delta,resource,barrageToPistol:b.dps/p.dps,crossMultiToPistol:c.dps/p3.dps});
}
const range=(rows,key)=>[Math.min(...rows.map(x=>x[key])),Math.max(...rows.map(x=>x[key]))];
const ratios=(rows,keys)=>Object.fromEntries(keys.map(k=>[k,range(rows,k)]));
console.log(JSON.stringify({
 method:"Expected activated damage/finite STM plus 64 seeded ordinary-hit ramp samples. Fixed rotations, no cast/travel lockout, target-switches, engine execution, per-hit enhancements or capstones. Short openings exclude capstones. Single-weapon tradeoffs are marginal ordinary-output ratios, not complete saber/rifle builds.",
 coreCases:sensitivity.length,
 coreRanges:ratios(sensitivity,["barrageToPistol","crossSoloToBarrage","crossMultiToPistol"]),
 stressCases:stress.length,
 stressRanges:ratios(stress,["barrageToPistol","crossMultiToPistol"]),
 summary,
 openings,
 handlingChoices,
 ordinaryCrossSkillTradeoffs
},null,2));
