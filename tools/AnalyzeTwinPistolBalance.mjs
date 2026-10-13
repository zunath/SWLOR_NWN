// Formula and resource-budget analysis for the Twin Pistol feedback proposal.
// Run: node tools/AnalyzeTwinPistolBalance.mjs
//
// This is NOT the game's combat engine or a release-balance test.
// Damage range and delay arithmetic mirror Combat; expected hit/critical
// outcomes, steady-state trigger uptime and fractional resource refunds are
// approximations. No animation lockout, deflection, Guard, resistance, capstone,
// enhancement procs, movement, Balanced Grip or Changing Angles is simulated.
// Fixed rotations skip unfunded opportunities; they are not optimized.
//
// Reference: DMG 22, Delay 250, equal attributes/Attack/Defense, 85% hit,
// rank-50 skill, 120 seconds, MGT 26, +12 max STM, +5 STM regen/30s.
// Pistol: Doublehand III, Gambler, Lucky Chamber averaged to +2.5pp,
// Rapid Shot assumed active, Reload Tempo duty approximated from two rolls,
// High Noon active for 30% of the fight, ability crit swept 22/30/37% to
// represent Deadeye Reload (before Quick Draw/Fan bonuses).
// Twin: Dual Wield III, shared +25% ordinary damage, +5% Barrage focus,
// each stance +10% haste; Crossfire reduces primary-target damage by 10%.
// Barrage sustain realizes 75% of its 1 STM/6s ceiling. Crossfire's pending
// discount uses a conservative hitChance^2 realization. No damage-triggered
// Cool Under Fire refunds are assumed. New capstones are intentionally excluded.
// Full Twin hybrid costs 119 SP including Armor, versus 69 for one line;
// its row is a marginal-investment illustration, not an equal-SP comparison.

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
function budget({build="pistol",targets=1,h=.85,ext=0,might=26,gear=12,food=5,d=22,critAbility=.30,seconds=120,support=0,crossCrit=false,ratio=1,delta=0}) {
 const pistol=build==="pistol", barrage=build==="barrage", hybrid=build==="hybrid", cross=build==="crossfire";
 const b=barrage||hybrid;
 const generic=pistol||crossCrit;
 const autoCrit=.10+(pistol?.12:0)+(generic?.025:0);
 const abilityCrit=generic?critAbility:.10;
 const baseH=ext+((b||cross)?10:0)+(generic?7:0);
 const reloadUptime=generic?1-(1-h*autoCrit)**2:0;
 const interval=(1-reloadUptime)*delay(!pistol,baseH)+reloadUptime*delay(!pistol,baseH+20);
 const dmgRating=pistol?Math.ceil(d*1.6):d;
 const avgAuto=.7*mean(dmgRating,autoCrit,ratio,delta)+.3*mean(dmgRating,autoCrit,ratio,delta,generic);
 const autoMod=pistol?1:(1.25+(b?.05:0))*(cross?.90:1);
 const autoDps=(pistol?1:2)*avgAuto*h*autoMod/interval;
 const multi=targets>1;
 const abMod=(cross||hybrid)&&multi?1.20:1;
 const primaryPenalty=cross?.90:1;
 const a=(power,c=abilityCrit)=>h*(.7*mean(power,c,ratio,delta)+.3*mean(power,c,ratio,delta,generic));
 const specs=pistol?[
  {id:"burst",cd:24,cost:10,hits:2,critical:abilityCrit,damage:2*a(dmgRating+20),refund:4*(1-(1-h*abilityCrit)**2)},
  {id:"volley",cd:18,cost:12,hits:1,critical:Math.min(.95,abilityCrit+.18),damage:a(dmgRating+38,Math.min(.95,abilityCrit+.18)),refund:0},
  {id:"sweep",cd:30,cost:9,hits:Math.min(3,targets),critical:Math.min(.95,abilityCrit+.12),damage:Math.min(3,targets)*a(dmgRating+26,Math.min(.95,abilityCrit+.12)),refund:0}
 ]:b&&!multi?[
  {id:"burst",cd:24,cost:10,damage:2*a(d+34),refund:0},
  {id:"volley",cd:18,cost:12,damage:2*a(d+19),refund:0},
  {id:"sweep",cd:30,cost:9,damage:2*a(Math.floor(.75*d)+13),refund:3*(1-(1-h)**2)}
 ]:[
  {id:"burst",cd:24,cost:8,damage:multi?(Math.min(3,targets)-1+primaryPenalty)*a(d+20)*abMod:1.75*a(d+20)*primaryPenalty,refund:0},
  {id:"volley",cd:18,cost:10,damage:(multi?(1+primaryPenalty):1.75*primaryPenalty)*a(d+26)*abMod,refund:0},
  {id:"sweep",cd:30,cost:8,damage:2*(Math.min(3,targets)-1+primaryPenalty)*a(Math.floor(.75*d)+13)*abMod,refund:0}
 ];
 // Barrage retains single-target actions against groups.
 if(barrage&&multi)return budget({build,targets:1,h,ext,might,gear,food,d,critAbility,seconds,support,crossCrit,ratio,delta});
 let stm=10+Math.floor(might*3/2)+gear,max=stm,total=0,casts=0,missed=0,rem=0;
 let counts={}; let nextSpread=0,nextDiscount=0,discountUntil=-1;
 for(let t=0;t<seconds;t++){
  if(t>0&&t%6===0){let available=10+Math.floor(might/4)+food+rem;stm=Math.min(max,stm+Math.floor(available/5));rem=available%5;}
  if(t>0&&t%4===0&&support)stm=Math.min(max,stm+support);
  // New ordinary-hit sustain is capped at 1/6s; use a conservative 75% realization.
  if(b&&t>0&&t%6===0)stm=Math.min(max,stm+.75);
  for(let i=0;i<specs.length;i++){
   let s=specs[i]; if(t<i||(t-i)%s.cd!==0)continue;
   const gamblerSurcharge=pistol?2*(1-h*s.critical)**s.hits:0;
   let cost=Math.max(0,s.cost+gamblerSurcharge-((cross||hybrid)&&multi&&t<=discountUntil?2*h*h:0));
   if(stm<cost+4){missed++;continue;}
   if(t<=discountUntil)discountUntil=-1;
   stm-=cost;total+=s.damage;casts++;counts[s.id]=(counts[s.id]||0)+1;
   stm=Math.min(max,stm+s.refund);
   if(cross||(hybrid&&multi)){
    const q=1-(1-h)**2;
    if(t>=nextSpread){stm=Math.min(max,stm+q);nextSpread=t+8;}
    if(multi&&t>=nextDiscount){discountUntil=t+10;nextDiscount=t+12;}
   }
  }
 }
 return {build,targets,autoDps,abilityDps:total/seconds,dps:autoDps+total/seconds,endingSTM:stm,casts,missed,counts};
}

assert.equal(delay(false,0),2.416);
assert.equal(delay(true,0,0),4.833);
assert.equal(delay(true,0,30),3.583);
assert.equal(Math.round(mean(36,.1)/delay(false,0)*100)/100,13.04);
const summary=[];
for(const build of ["pistol","barrage","crossfire","hybrid"])
 for(const targets of [1,3])summary.push({...budget({build,targets}),targets});
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
 method:"Expected damage and finite-STM budget; no engine execution or capstones",
 coreCases:sensitivity.length,
 coreRanges:ratios(sensitivity,["barrageToPistol","crossSoloToBarrage","crossMultiToPistol"]),
 stressCases:stress.length,
 stressRanges:ratios(stress,["barrageToPistol","crossMultiToPistol"]),
 summary
},null,2));
