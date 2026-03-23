Spec v1 - Sistema de Combate (Headless First)
Documento base para implementação por IA/humano com foco em Unity 6, composição, SOLID e clean code.

1) Objetivo
Construir um sistema de combate tático por turnos, testável em modo headless, com suporte a:

2 jogadores vs 4 inimigos (principal)
ranks 1..4 por lado
corrupção global por tiers
tokens, DOTs, resistências, movimentação
progressão por 3 skill trees (Fogo/Metal/Anomalia)
simulação em lote + analytics CSV (Google Sheets friendly)
integração futura com Unity (UI, animação, áudio, save) sem acoplar engine no core
2) Escopo da v1
Incluído
Núcleo completo de regras de combate (sem Unity API)
IA inimiga básica (kill-check + weighted random)
Progressão e validação de skill trees
Simulador headless em lote
Export de analytics em CSV (eventos + agregados)
Testes unitários e invariantes
Fora da v1
UI final
animações/VFX/SFX
save game final
balanceamento fino de números
multiplayer/rede
3) Princípios arquiteturais
Composição sobre herança: entidades com componentes.
Data-driven: skills, inimigos e progressão via dados.
Determinismo: RNG injetável por seed.
Camadas:
Game.Core (domínio + regras + casos de uso)
Game.Tests (unit + property + simulações)
Game.Unity (adapters futuros)
Sem dependência de Unity em Game.Core.
4) Modelo de domínio (contratos conceituais)
4.1 Entidades principais
Battle
Combatant (jogador, inimigo, cadáver)
Team (Allies, Enemies)
Skill
SkillTree
BattleEvent (analytics)
4.2 Componentes de Combatant
IdentityComponent: id, displayName, faction, tags
HealthComponent: currentHp, maxHp, isDead, isDeathblowPending
PositionComponent: side, occupiedRanks[], size
StatsComponent: speed, accuracy, critChance, damageModifiers
ResistanceComponent: burnRes, blightRes, moveRes, stunRes, deathblowRes
TokenComponent: lista de tokens com stack/turnos/consumo
DotComponent: instâncias DOT ativas
SkillLoadoutComponent: skills equipadas e cooldowns
ProgressionComponent: nível, pontos, árvores, nós desbloqueados
AIComponent: decisionPolicyId (inimigos)
ElementAffinityComponent: afinidade/elemento base (se aplicável)
5) Mecânicas e regras formais
5.1 Corrupção global
variável global corruptionValue em [0..100]
tiers:
Tier 0: < 33
Tier 1: >= 33 && < 66
Tier 2: >= 66 && < 99
Tier 3: >= 99
corrupção modifica:
dano causado pelo jogador
dano recebido pelo jogador
chance de crítico dar/tomar
v1: multiplicadores configuráveis por tabela de dados (não hardcoded no código de regra)
5.2 Triângulo elemental
elementos: Fire, Metal, Anomaly
vantagem:
Fire > Metal
Metal > Anomaly
Anomaly > Fire
multiplicadores v1:
vantagem: +50% dano e DOT (x1.5)
desvantagem: -50% efetividade (x0.5)
5.3 Tokens (v1)
Block: -50% dano recebido, consumível por hit
BlockPlus: -75% dano recebido, consumível por hit
Dodge: 50% chance de negar hit recebido, consumível no trigger
Blind: 50% chance de errar ataque efetuado
Taunt: força alvo preferencial
Stealth: não pode ser alvo direto (exceto skills permitidas)
Combo: marcador especial para efeitos extras em skills específicas
5.4 DOTs
Burn, Blight
aplicados com potency + duration
tick no início do turno do portador
aplicação respeita resistência do alvo
dano de DOT pode matar; mortes por DOT não geram cadáver (regra solicitada)
5.5 Cadáveres
inimigo morto por dano direto não crítico gera cadáver ocupando ranks
morte por crítico não gera cadáver
morte por DOT não gera cadáver
cadáver pode ser removido por skill/limpeza
5.6 Ranks e movimentação
cada lado possui ranks 1..4
tamanhos 1/2/3 ocupam slots contíguos
Push e Pull respeitam limites e ocupação
movimentação voluntária pós-skill (advance/retreat)
sistema deve resolver compactação/reorganização válida após mortes e movimentos
5.7 Turnos e iniciativa
ordem por speed + modificadores
desempate determinístico por seed
itens de combate são ação gratuita (não consome ação principal)
6) Progressão e skill trees
personagem começa no nível 0 com 3 skills base definidas por dados
nível máximo: 12
3 árvores por personagem: Fire/Metal/Anomaly
cada árvore:
3 tiers
cada tier: 3 passivas + 1 ativa
desbloqueio de tier N+1 requer 4 nós do tier N desbloqueados
distribuição livre dos 12 pontos entre árvores
7) IA inimiga (v1)
Policy padrão por turno:

listar skills utilizáveis no estado atual (rank, cooldown, alvo válido)
se existir skill com kill confirmável em algum jogador, escolher entre letais por peso/prioridade
caso contrário, escolher skill por weighted random
escolher alvo válido pela regra da skill (respeitando taunt/stealth quando aplicável)
8) Pipeline de resolução de ação (ordem oficial)
validação de executor/skill/alvos/ranks
verificação de hit/miss (blind, dodge, accuracy)
cálculo de multiplicador elemental
cálculo de crítico (inclui corrupção)
mitigação por token defensivo (block/block+)
aplicação de dano/cura/cura de corrupção
aplicação de efeitos secundários (DOT, token, stun, move, combo)
resolução de morte/deathblow/cadáver
emissão de eventos para analytics
Esse fluxo é contrato para testes.

9) Contratos de dados (JSON v1)
9.1 skills.json (exemplo de estrutura)
{
  "id": "skill_wulfric_shield_bash",
  "name": "Shield Bash",
  "element": "Metal",
  "type": "Active",
  "allowedCasterRanks": [1,2],
  "allowedTargetRanks": [1,2,3],
  "baseDamage": { "min": 3, "max": 6 },
  "baseCritChance": 0.1,
  "accuracy": 0.9,
  "cooldown": 1,
  "selfMove": { "type": "None", "steps": 0 },
  "effectsOnHit": [
    { "type": "ApplyToken", "token": "Combo", "stacks": 1, "chance": 1.0 }
  ],
  "comboBonus": [
    { "type": "ApplyStun", "chance": 1.0 }
  ]
}
9.2 enemies.json
{
  "id": "enemy_stunner_spider",
  "name": "Aranha Stunadora",
  "size": 1,
  "baseStats": { "hp": 24, "speed": 6, "critChance": 0.1 },
  "resistances": { "burn": 0.2, "blight": 0.5, "move": 0.3, "stun": 0.4, "deathblow": 0.2 },
  "skills": ["skill_spider_bite", "skill_spider_web", "skill_spider_alt"],
  "aiPolicy": "KillThenWeighted"
}
9.3 skill_trees.json
{
  "characterId": "wulfric",
  "trees": [
    {
      "element": "Metal",
      "tiers": [
        {
          "tier": 1,
          "nodes": [
            { "id": "m_t1_p1", "type": "Passive", "cost": 1, "requires": [] },
            { "id": "m_t1_p2", "type": "Passive", "cost": 1, "requires": [] },
            { "id": "m_t1_p3", "type": "Passive", "cost": 1, "requires": [] },
            { "id": "m_t1_a1", "type": "Active",  "cost": 1, "requires": ["m_t1_p1","m_t1_p2","m_t1_p3"] }
          ]
        }
      ]
    }
  ]
}
10) Analytics (CSV schema v1)
10.1 Raw Events CSV (combat_events.csv)
Colunas mínimas:

event_id
battle_id
turn
timestamp_utc
event_type
actor_id
target_id
skill_id
element
is_hit
is_crit
damage_amount
dot_type
dot_amount
token_type
token_delta
corruption_value
corruption_tier
battle_result
10.2 Aggregates CSV (combat_aggregates.csv)
Colunas mínimas:

entity_type (enemy, player, skill)
entity_id
matches
wins
win_rate
uses
pick_rate
avg_damage_per_use
hit_rate
crit_rate
avg_damage_at_tier0
avg_damage_at_tier1
avg_damage_at_tier2
avg_damage_at_tier3
11) Testes obrigatórios (aceitação v1)
11.1 Unitários
corrupção troca de tier nos thresholds exatos
triângulo elemental x1.5/x0.5
block/block+ consomem corretamente
blind/dodge afetam hit conforme regra
DOT tica no início do turno e expira
cadáver segue regra de origem de morte
push/pull respeita bordas e ocupação
skill tree bloqueia tier seguinte sem prerequisitos
11.2 Invariantes (property/regression)
hp nunca acima de maxHp após resolver ação
ranks sempre válidos e sem colisão ilegal
tokens nunca negativos
corrupção sempre no intervalo [0..100]
seed igual => resultado igual (determinismo)
11.3 Simulação bulk
comando: simulate --battles N --seed S
smoke CI: N=50~100
local balance pass: N=1000+
12) Estrutura de projeto recomendada (início enxuto)
Game.Core
Game.Tests
Game.Unity
No futuro, se necessário:

Game.Infrastructure (export/sinks complexos)
Game.Simulations (runner separado)
13) Integração futura com Unity (contrato)
Game.Core expõe apenas DTOs/eventos neutros.
Game.Unity traduz isso para:

UI states
triggers de animação
áudio
efeitos visuais
save/load
Nunca chamar MonoBehaviour, Transform, Time, Random de Unity dentro do core.

O que fazer a seguir (roteiro prático)
Aprovar esta Spec v1

você valida regras ambíguas (deathblow, stealth exceptions, fórmula exata de corrupção)
Congelar “regras canônicas” em um arquivo

um SPEC_COMBAT_V1.md no repo (fonte única de verdade)
Pedir implementação faseada para IA

Prompt 1: “Implemente somente Game.Core conforme Spec v1”
Prompt 2: “Implemente Game.Tests + invariantes”
Prompt 3: “Implemente runner headless + CSV analytics”
Prompt 4: “Adicione CI GitHub Actions com smoke sim”
Rodar primeira bateria de 1000 sims

coletar winrate/use rate/dano médio por skill
Ajustar dados, não engine

balancear via JSONs (skills/resists/valores), mantendo motor estável
Só depois integrar Unity Presentation

UI/anim/audio consumindo eventos do core

