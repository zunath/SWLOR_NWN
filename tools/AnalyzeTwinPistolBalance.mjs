// Arithmetic comparison for the revised Twin Pistol proposal, not release proof.
// Run: node tools/AnalyzeTwinPistolBalance.mjs
//
// Discrete damage ranges/rounding and effective delays follow Combat arithmetic.
// Float RNG truncation excludes the upper integer endpoint (rare float rounding ignored).
// Ordinary attacks use 64 mixed-seed hit traces with startup, same-target ramp,
// focus expiry and sustain cooldowns. Fluid cadence omits native swing batching.
// Activated damage, Momentum and STM refunds use fractional expectations;
// averaging before resource thresholds cannot reproduce individual encounters.
// Each activated impact includes the rank-50 flat power bonus of seven.
// Rotations skip unfunded slots, retain 4 STM and are not optimized. Crossfire
// opens Slide -> Rush -> Grapple. Barrage selects stance by sampled auto output.
// No cast/travel interruption, target switching, avoidance/mitigation, per-hit
// enhancements, capstones, Balanced Grip, Momentum ACC, Changing Angles,
// Second Wind or Cool Under Fire is simulated. Movement is assumed legal.
// Pistol assumes sustained Rapid Shot/Reload Tempo and 30% High Noon uptime.
// Deadeye Reload is represented by a crit sweep; Quick Draw's new-target bonus
// is absent against the continuously damaged target. These are approximations.
// Current activated critical base is 5%; Gambler's generic critical stat is
// read by ordinary rolls only. Pistol activated critical chance is an input,
// defaulting to 15% to approximate its 0-15% Deadeye accumulation.
// Defaults: D22/Delay250, rank50/equal attributes/ATK/DEF, hit85%, 120s,
// MGT26, +12 max STM, +5 STM regen/30s. Standalone lines cost 69 SP including
// Armor; legal combined lines cost 119 SP and are not equal-investment controls.

import assert from 'node:assert/strict';

function mean(d,c,ratio=1,delta=0,high=false,bonus=0,penalty=100) {
 let hi=Math.max(0,(d+delta)*Math.min(3.625,Math.max(.01,ratio)));
 const minimum=d>0?1:0;
 const average=(low,upper,critical)=>{
  low=Math.max(minimum,Math.floor(low)); upper=Math.max(low,Math.floor(upper));
  if(upper>low)upper--; // (int)NextFloat(low, upper), not an inclusive integer roll.
  let total=0;
  for(let roll=low;roll<=upper;roll++) {
   let value=roll+(critical&&high?Math.trunc(roll*15/100):0);
   if(bonus!==0&&value>0)value=Math.max(1,value+Math.ceil(Math.fround(value*Math.fround(bonus/100))));
   total+=Math.floor(value*penalty/100);
  }
  return total/(upper-low+1);
 };
 return average(.7*hi,hi,false)*(1-c)+average(hi,hi*1.25,true)*c;
}
function delay(paired,haste,offReduction=30) {
 const conv=x=>Math.trunc(x/60*1000), pct=(x,p)=>x-Math.trunc(x*p/100);
 const ref=conv(250)-1750;
 const href=Math.max(584,pct(conv(250),Math.max(-50,Math.min(50,haste)))-1750);
 if(!paired)return href/1000;
 const off=pct(250,offReduction);
 return Math.max(584,Math.round((conv(105+145+Math.max(0,off-105))-1750)*href/ref))/1000;
}

function sampleRandom(sample) {
 let seed=(sample+0x9e3779b9)>>>0;
 seed=Math.imul(seed^(seed>>>16),0x21f0aaad);
 seed=Math.imul(seed^(seed>>>15),0x735a2d97);
 seed=(seed^(seed>>>15))>>>0;
 return ()=>{seed=(Math.imul(1664525,seed)+1013904223)>>>0;return seed/2**32;};
}

function restoreOnOrdinaryHit(state,time) {
 if(time<state.ready){state.hits=0;return 0;}
 if(++state.hits<3)return 0;
 state.hits=0;state.ready=time+6;return 1;
}

function ordinaryWindow({seconds,h,intervalAt,haste,weapons,damageAtBonus,barrage,ramp,samples=64}) {
 let total=0;
 const stamina=Array(Math.ceil(seconds)+1).fill(0);
 for(let sample=0;sample<samples;sample++) {
  let stacks=0, lastHit=-Infinity, nextGrant=0, focusHits=0;
  const restore={hits:0,ready:0},random=sampleRandom(sample);
  const hit=time=>{
   if(time-lastHit>=6){stacks=0;focusHits=0;}
   if(random()>=h)return;
   const focus=barrage&&focusHits>=3?5:0;
   total+=damageAtBonus(focus);
   lastHit=time;
   focusHits=Math.min(3,focusHits+1);
   if(ramp&&time>=nextGrant){stacks=Math.min(5,stacks+1);nextGrant=time+2;}
   if(barrage)stamina[Math.ceil(time)]+=restoreOnOrdinaryHit(restore,time)/samples;
  };
  for(let time=0;time<seconds;) {
   if(time-lastHit>=6)stacks=0;
   hit(time);
   if(weapons===2&&time+.45<seconds)hit(time+.45);
   let interval=intervalAt(haste+(ramp?2*stacks:0));
   if(ramp&&stacks>0&&time+interval-lastHit>=6)interval=intervalAt(haste);
   time+=interval;
  }
 }
 return {dps:total/samples/seconds,stamina};
}

function budget({build="pistol",targets=1,h=.85,ext=0,might=26,gear=12,food=5,d=22,critAbility=.15,seconds=120,support=0,crossCrit=false,ratio=1,delta=0,crossStance=true,handling="default",weaponCount=2}) {
 const pistol=build==="pistol", barrage=build==="barrage", hybrid=build==="hybrid", cross=build==="crossfire";
 const b=barrage||hybrid;
 const generic=pistol||crossCrit;
 const autoCrit=.10+(pistol?.12:0)+(generic?.025:0);
 const abilityCrit=generic?critAbility:.05;
 const reloadUptime=generic?1-(1-h*autoCrit)**2:0;
 const weapons=pistol?1:weaponCount;
 const selectedHandling=pistol?"none":handling==="default"?(b?"haste":"damage"):handling;
 assert.ok(["none","haste","damage"].includes(selectedHandling));
 const intervalAt=haste=>(1-reloadUptime)*delay(weapons===2,haste)+reloadUptime*delay(weapons===2,haste+20);
 const externalH=ext+(generic?7:0);
 const handlingHaste=selectedHandling==="haste"?8:0;
 const dmgRating=pistol?Math.ceil(d*1.6):d;
 const simulateOrdinary=(stance)=>{
  const penalty=stance?70:(cross&&crossStance?80:100);
  const damageAtBonus=focus=>{
   const bonus=(selectedHandling==="damage"?10:0)+focus;
   return .7*mean(dmgRating,autoCrit,ratio,delta,false,bonus,penalty)+.3*mean(dmgRating,autoCrit,ratio,delta,generic,bonus,penalty);
  };
  // There are only two possible same-target damage states; avoid repeating the range sums per hit.
  const damageByFocus=new Map([0,5].map(focus=>[focus,damageAtBonus(focus)]));
  return ordinaryWindow({seconds,h,intervalAt,haste:externalH+handlingHaste+(stance?10:(cross&&crossStance?10:0)),weapons,damageAtBonus:focus=>damageByFocus.get(focus),barrage:b,ramp:stance});
 };
 const ordinaryWithoutStance=simulateOrdinary(false);
 const ordinaryWithStance=b?simulateOrdinary(true):null;
 const barrageStance=b&&ordinaryWithStance.dps>ordinaryWithoutStance.dps;
 const ordinary=barrageStance?ordinaryWithStance:ordinaryWithoutStance;
 const autoDps=ordinary.dps;
 const multi=targets>1;
 const rankPower=Math.trunc(50*.15);
 const a=(power,c=abilityCrit)=>h*(.7*mean(power+rankPower,c,ratio,delta)+.3*mean(power+rankPower,c,ratio,delta,generic));
 const specs=pistol?[
  {id:"burst",cd:24,cost:10,hits:2,critical:abilityCrit,damage:2*a(dmgRating+20),refund:4*(1-(1-h*abilityCrit)**2)},
  {id:"volley",cd:18,cost:12,hits:1,critical:abilityCrit,damage:a(dmgRating+38),refund:0},
  {id:"sweep",cd:30,cost:9,hits:Math.min(3,targets),critical:abilityCrit,damage:Math.min(3,targets)*a(dmgRating+26),refund:0}
 ]:b&&!multi?[
  {id:"burst",cd:24,cost:10,damage:weapons*a(d+24),refund:0},
  {id:"volley",cd:18,cost:10,damage:weapons*a(d+16),refund:0},
  {id:"sweep",cd:30,cost:9,damage:weapons*a(Math.floor(.75*d)+13),refund:3*(1-(1-h)**weapons)}
 ]:[
  {id:"volley",cd:18,cost:10,damage:weapons*a(d+16),refund:0,movement:true,buildsMomentum:true,hits:weapons},
  {id:"sweep",cd:30,cost:10,damage:weapons*Math.min(3,targets)*a(Math.floor(.60*d)+12),refund:0,movement:true,buildsMomentum:true,hits:weapons*Math.min(3,targets)},
  {id:"burst",cd:24,cost:10,damage:weapons*a(d+14),refund:0,movement:true,spendsMomentum:true}
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
   stm-=cost;total+=s.spendsMomentum?weapons*a(d+14+4*charges):s.damage;casts++;counts[s.id]=(counts[s.id]||0)+1;
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
 stm=Math.min(max,stm+ordinary.stamina[Math.ceil(seconds)]);
 return {build,handling:selectedHandling,weapons,seconds,investmentSP:pistol?69:hybrid?119:69,targets,barrageStance,autoDps,abilityDps:total/seconds,dps:autoDps+total/seconds,endingSTM:stm,casts,missed,spentMomentum,counts};
}

assert.equal(delay(false,0),2.416);
assert.equal(delay(true,0,0),4.833);
assert.equal(delay(true,0,30),3.583);
assert.equal(Math.round(mean(36,.1)/delay(false,0)*100)/100,12.83);
assert.equal(mean(0,0),0);
assert.equal(mean(22,0,1,0,false,5,85),111/7);
const restoreCheck={hits:0,ready:0};
assert.deepEqual([0,1,2,3,4,5,6,7,8,9,10].map(time=>restoreOnOrdinaryHit(restoreCheck,time)),
 [0,0,1,0,0,0,0,0,0,0,1]);
const firstDraws=Array.from({length:64},(_,sample)=>sampleRandom(sample)());
assert.equal(new Set(firstDraws).size,64);
assert.ok(Math.min(...firstDraws)<.1&&Math.max(...firstDraws)>.9);
const refundTiming=ordinaryWindow({seconds:3,h:1,intervalAt:()=>.4,haste:0,weapons:1,
 damageAtBonus:()=>0,barrage:true,ramp:false,samples:1});
assert.equal(refundTiming.stamina[0],0);
assert.equal(refundTiming.stamina[1],1);
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
  damageHandlingRatio:1.10,
  akimboStartRatio:.70*baseline/delay(paired,externalHaste+10),
  akimboFullRatio:.70*baseline/delay(paired,externalHaste+20),
  crossfireRatio:.80*baseline/delay(paired,externalHaste+10)});
}
const sensitivity=[];
for(const h of [.65,.85,.95])for(const ext of [0,15,30,50])for(const critAbility of [.05,.15,.20]){
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
 method:"Expected activated damage/finite fractional STM and Momentum plus 64 mixed-seed ordinary-hit samples. Rank-50 activated power bonus 7 and 5% base ability critical included; Pistol ability critical is an approximate input, not Gambler's ordinary-only generic critical bonus. Fan the Hammer currently declares no extra critical adjustment in its runtime profile. Integer damage rounding; fluid cadence without native batching. Fixed rotations reserve 4 STM; Crossfire opens Slide/Rush/Grapple. No cast/travel interruption, target switches, mitigation, engine execution, per-hit enhancements or capstones. Pistol assumes sustained Rapid Shot/Reload Tempo, 30% High Noon uptime and no Quick Draw novelty bonus. Hit chance is shared by ordinary and activated attacks; this does not model the native close-range penalty. Marginal weapon tradeoffs are not complete saber/rifle builds.",
 sampling:{samples:64,firstDrawMean:firstDraws.reduce((sum,value)=>sum+value,0)/64,
  firstDrawMinimum:Math.min(...firstDraws),firstDrawMaximum:Math.max(...firstDraws)},
 coreCases:sensitivity.length,
 coreRanges:ratios(sensitivity,["barrageToPistol","crossSoloToBarrage","crossMultiToPistol"]),
 stressCases:stress.length,
 stressRanges:ratios(stress,["barrageToPistol","crossMultiToPistol"]),
 summary,
 openings,
 handlingChoices,
 ordinaryCrossSkillTradeoffs
},null,2));
