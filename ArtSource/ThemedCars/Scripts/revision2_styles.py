# Art-directed R02 styles. Same product IDs and silhouette selections as R01.
# paint / secondary / trim / light / ink / surface roughness and metal response.
_ROWS={
'PIRATE':('#8E243B','#352C2A','#DFAE45','#FFF0C4','#251F2B',.30,.12,.34,.62),
'ROYAL':('#663DC0','#F5E9C4','#E9B646','#FFF5CC','#30234D',.48,.20,.48,.70),
'SPACE':('#E8F1F3','#203550','#35D7E8','#F6FCFF','#172741',.55,.38,.55,.55),
'ALIEN':('#9BE546','#5E3A9B','#7662D7','#F3FFC7','#2B2447',.45,.05,.38,.12),
'BEACH':('#FFD376','#FFAF8C','#26AEBB','#FFF5D2','#17677F',.44,.02,.38,.04),
'JUNGLE':('#399451','#724F36','#C5DF54','#EDF2A4','#174D37',.28,.02,.24,.05),
'SPOOKY':('#382D4E','#EA7830','#9854CF','#D9F790','#241C39',.40,.02,.38,.05),
'CANDY':('#F5AACA','#93DDCB','#A090DB','#FFF7DB','#9C4978',.78,.02,.65,.05),
'FOOD':('#EFC36C','#CC5945','#7BAD50','#FFF0BA','#693E32',.40,.02,.30,.05),
'FIRE':('#BB3A32','#302933','#F3882F','#FFE369','#352330',.25,.05,.18,.05),
'ICE':('#E7F8FF','#8BD5E5','#5B9BCD','#FFFFFF','#32789C',.66,.24,.50,.48),
'OCEAN':('#237CBA','#2AB9B1','#EF937C','#F8E9BE','#174D77',.53,.05,.48,.10),
'CONSTRUCTION':('#F5C438','#303C48','#ED8B33','#FFF0C2','#26333D',.33,.13,.24,.20),
'RESCUE':('#F2F4EB','#E66D5D','#CF514A','#A3E6E0','#2C6171',.45,.05,.40,.08),
'WIZARD':('#313862','#987BC9','#CFAF58','#FFF0BC','#232742',.35,.08,.40,.45),
'ROBOT':('#7C929F','#303D50','#4ECAF0','#D7F7FF','#202C42',.56,.57,.52,.50),
'MUSIC':('#C5629A','#3A365F','#EAD66E','#A3EEE4','#2D274F',.57,.12,.45,.20),
'SPORT':('#53A3CC','#F1F0DC','#DE8953','#FFFFFF','#253E57',.46,.02,.40,.08),
'FARM':('#BD5D4C','#F6E5B7','#6DA35C','#FFF3D0','#704432',.30,.02,.26,.08),
'PARTY':('#A88CDC','#F2BE6C','#E88DBD','#FFF1B2','#60498E',.58,.10,.48,.18),
}
STYLES={k:dict(zip(['paint','secondary','trim','light','ink','smoothness','metallic','trimSmoothness','trimMetallic'],v)) for k,v in _ROWS.items()}
