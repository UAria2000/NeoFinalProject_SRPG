# Hit Effect Type Classification

작성 기준: 2026-05-14

## 목적

`이펙트/` 폴더의 피격 이펙트 이미지와 현재 `HitEffectType`/`HitEffectRegistry` 기준으로 운영 스킬의 피격 이펙트 타입을 재분류한다.

대상 생성기:

- `Assets/Scripts/Editor/EnemyBossSkillGeneratorWindow.cs`
- `Assets/Scripts/Editor/EnemyElfSkillGeneratorWindow.cs`
- `Assets/Scripts/Editor/EnemyHumanSkillGeneratorWindow.cs`
- `Assets/Scripts/Editor/AllySkillGeneratorWindow.cs`

제외 기준:

- 위 생성기에 없는 스킬은 테스트용 또는 구형 스킬로 보고서 본문에서 제외.
- `Assets/SkillDefinition/Temp/` 제외.
- `Assets/SkillDefinition/EnemySkill/SK_*.asset`, `Sk_*.asset` 제외.
- 생성기 ID와 현재 에셋 ID가 맞지 않는 항목은 본문 제외 후 별도 메모 처리.

## 확인한 피격 이펙트 이미지

| 권장 타입 | 이미지 묶음 | 용도 |
|---|---|---|
| `ArcaneMagic` | `마법1`, `마법2`, `마법3` | 보라/청색 계열 마법, 어둠, 표식, 비전, 영혼 |
| `FireMagic` | `붉은계열 마법1`, `붉은계열 마법2`, `붉은계열 마법3` | 화염, 폭발, 유성, 화상 |
| `HolyMagic` | `신성마법1`, `신성마법2`, `신성마법3` | 신성 빛 피해, 성광 타격 |
| `Shield` | `shield_barrier` | 적군용 노란 보호막, 방어막 부여 |
| `Blessing` | `blessing_01`, `blessing_02` | 적군용 노란 회복, 버프, 정화, 자기 강화 |
| `AllyShield` | `ally_purple_barrier` | 아군용 보라 보호막, 방어막 부여 |
| `AllyBlessing` | `ally_purple_blessing` | 아군용 보라 회복, 버프, 정화, 자기 강화 |
| `Slashing` | `슬래싱1`, `슬래싱2`, `슬래싱3` | 검, 절단, 그림자 근접 |
| `SlashingBlood` | `찍는 느낌1`, `찍는 느낌2`, `찍는 느낌3` | 피가 튀는 찌르기, 갈고리, 출혈성 근접 관통 |
| `Blunt` | `타격1`, `타격2`, `타격3` | 둔기, 돌진, 투석, 짓밟기, 충격 |
| `Piercing` | `피어싱1`, `피어싱2`, `피어싱3` | 화살, 석궁, 창 투사체, 원거리 관통 |
| `None` | 없음 | 패시브, 소환, 도주, 증원 등 피격 이펙트 미적용 |

현재 프리팹 운용 타입은 `None` 제외 11개 구조다.
`Magic`은 enum에 남아 있으나 현재 공용 히트 이펙트 프리팹 등록 기준에서는 사용하지 않는다.

## 요약

| 타입 | 수량 | 비고 |
|---|---:|---|
| `ArcaneMagic` | 13 | 마법, 어둠, 표식, 비전, 영혼 |
| `Slashing` | 10 | 검, 그림자, 출혈성 절단 |
| `SlashingBlood` | 3 | 피가 튀는 찌르기, 갈고리, 출혈성 관통 |
| `Blunt` | 9 | 둔기, 돌진, 투석, 충격 |
| `Blessing` | 6 | 적군용 노란 회복, 버프, 정화 |
| `AllyBlessing` | 3 | 아군용 보라 회복, 버프, 정화 |
| `None` | 9 | 패시브, 소환, 도주, 증원 |
| `Piercing` | 7 | 사격, 창 투사체 |
| `Shield` | 3 | 적군용 노란 보호막 |
| `AllyShield` | 2 | 아군용 보라 보호막 |
| `HolyMagic` | 4 | 신성 빛 피해 |
| `FireMagic` | 3 | 화염, 유성 |
| **합계** | **72** | 생성기 기준 현재 존재하는 `SkillDefinition` 에셋 |

## 그룹별 요약

| 그룹 | 수량 | 주요 타입 분포 |
|---|---:|---|
| 보스 스킬 | 11 | `None` 4, `Blunt` 2, `HolyMagic` 2, `Blessing` 1, `Shield` 1, `Piercing` 1 |
| 엘프 스킬 | 19 | `ArcaneMagic` 5, `Slashing` 3, `None` 3, `Blessing` 2, `Piercing` 2, `Blunt` 2, `FireMagic` 1, `Shield` 1 |
| 휴먼 스킬 | 17 | `Blunt` 3, `Blessing` 3, `Piercing` 3, `SlashingBlood` 2, `None` 2, `HolyMagic` 2, `ArcaneMagic` 1, `Shield` 1 |
| 아군 스킬 | 25 | `Slashing` 7, `ArcaneMagic` 7, `AllyBlessing` 3, `Blunt` 2, `AllyShield` 2, `FireMagic` 2, `SlashingBlood` 1, `Piercing` 1 |

## 보스 스킬

| 스킬 | 타입 | 권장 이미지 | 에셋 | 근거 |
|---|---|---|---|---|
| 심판의 철퇴 | `Blunt` | `타격2/3` | `EnemySkill/Boss/Human/human_boss_judge_basic.asset` | 철퇴 충격 |
| 정의로운 복수 | `Blessing` | `blessing_01/02` | `EnemySkill/Boss/Human/human_boss_judge_righteous_revenge.asset` | 자기 버프/반격 태세 |
| 순교의 심판 | `None` | 없음 | `EnemySkill/Boss/Human/human_boss_judge_enrage_when_high_priest_dies.asset` | 패시브 |
| 징벌의 빛 | `HolyMagic` | `신성마법1/2/3` | `EnemySkill/Boss/Human/human_boss_high_priest_basic.asset` | 신성 빛 피해 |
| 참회의 사슬 | `HolyMagic` | `신성마법2/3` | `EnemySkill/Boss/Human/human_boss_high_priest_chain_of_penitence.asset` | 신성 속박/상태이상 |
| 고해성사 | `Shield` | `shield_barrier` | `EnemySkill/Boss/Human/human_boss_high_priest_confession.asset` | 적군 보호막 |
| 신성한 소생 | `None` | 없음 | `EnemySkill/Boss/Human/human_boss_high_priest_revive_judge.asset` | 패시브 부활 |
| 짓밟기 | `Blunt` | `타격3` | `EnemySkill/Boss/Dragon/dragon_boss_stomp.asset` | 중량 충격 |
| 용아병 소환 | `None` | 없음 | `EnemySkill/Boss/Dragon/dragon_boss_summon_dragon_soldier.asset` | 소환 |
| 용아병 창격 | `Piercing` | `피어싱1/2/3` | `EnemySkill/Boss/Dragon/dragon_soldier_spear.asset` | 창격 |
| 용아병 숭배 | `None` | 없음 | `EnemySkill/Boss/Dragon/dragon_soldier_worship.asset` | 패시브 |

## 엘프 스킬

| 스킬 | 타입 | 권장 이미지 | 에셋 | 근거 |
|---|---|---|---|---|
| 마법 장난 | `ArcaneMagic` | `마법1/2/3` | `EnemySkill/Elf/elf_fairy_magic_prank_마법 장난.asset` | 마법 피해/실명 |
| 휘두르기 | `Slashing` | `슬래싱1/2/3` | `EnemySkill/Elf/elf_dryad_swing_휘두르기.asset` | 휘두르기 절단 |
| 뿌리박기 | `Blessing` | `blessing_02` | `EnemySkill/Elf/elf_dryad_root_뿌리박기.asset` | 자기 상태 부여 |
| 재생 | `None` | 없음 | `EnemySkill/Elf/elf_dryad_regeneration_재생.asset` | 패시브 |
| 이중 공격 | `Slashing` | `슬래싱1/2/3` | `EnemySkill/Elf/elf_sword_dancer_double_attack_이중 공격.asset` | 검 공격 |
| 전투 자세 | `Blessing` | `blessing_01/02` | `EnemySkill/Elf/elf_sword_dancer_battle_stance_전투 자세.asset` | 자기 버프 |
| 검무 | `Slashing` | `슬래싱1/2/3` | `EnemySkill/Elf/elf_sword_dancer_sword_dance_검무.asset` | 검무 |
| 저격 | `Piercing` | `피어싱1/2/3` | `EnemySkill/Elf/elf_hunter_snipe_저격.asset` | 사격 |
| 사냥꾼의 표식 | `ArcaneMagic` | `마법1/2` | `EnemySkill/Elf/elf_hunter_mark_사냥꾼의 표식.asset` | 디버프 표식 |
| 연발 사격 | `Piercing` | `피어싱1/2/3` | `EnemySkill/Elf/elf_hunter_rapid_shot_연발 사격.asset` | 연발 사격 |
| 들이받기 | `Blunt` | `타격1/2/3` | `EnemySkill/Elf/elf_spirit_deer_ram_들이받기.asset` | 충돌 |
| 발구르기 | `Blunt` | `타격2/3` | `EnemySkill/Elf/elf_spirit_deer_stomp_발구르기.asset` | 지면 충격 |
| 정화 | `None` | 없음 | `EnemySkill/Elf/elf_spirit_deer_purification_정화.asset` | 패시브 오라 |
| 옭아매기 | `ArcaneMagic` | `마법2/3` | `EnemySkill/Elf/elf_druid_entangle_옭아매기.asset` | 자연 마법/동상 |
| 강풍 | `ArcaneMagic` | `마법1/2` | `EnemySkill/Elf/elf_druid_gale_강풍.asset` | 바람 마법 |
| 숲의 부름 | `None` | 없음 | `EnemySkill/Elf/elf_druid_call_of_forest_숲의 부름.asset` | 소환 |
| 화염구 | `FireMagic` | `붉은계열 마법1/2/3` | `EnemySkill/Elf/elf_mage_fireball_화염구.asset` | 화염 피해 |
| 얼음 방벽 | `Shield` | `shield_barrier` | `EnemySkill/Elf/elf_mage_ice_barrier_얼음 방벽.asset` | 적군 보호막 |
| 비전 폭발 | `ArcaneMagic` | `마법3` | `EnemySkill/Elf/elf_mage_arcane_explosion_비전 폭발.asset` | 비전 폭발 |

## 휴먼 스킬

| 스킬 | 타입 | 권장 이미지 | 에셋 | 근거 |
|---|---|---|---|---|
| 내지르기 | `SlashingBlood` | `찍는 느낌1/2/3` | `EnemySkill/Human/human_farmer_thrust_내지르기.asset` | 피가 튀는 근접 찌르기 |
| 투석 | `Blunt` | `타격1/2/3` | `EnemySkill/Human/human_farmer_stone_throw_투석.asset` | 돌 충격 |
| 줄행랑 | `None` | 없음 | `EnemySkill/Human/human_farmer_flee_next_turn_줄행랑.asset` | 도주 |
| 찌르기 | `SlashingBlood` | `찍는 느낌1/2/3` | `EnemySkill/Human/human_guard_thrust_찌르기.asset` | 피가 튀는 근접 찌르기 |
| 방패 타격 | `Blunt` | `타격1/2/3` | `EnemySkill/Human/human_guard_shield_bash_방패 타격.asset` | 방패 충격 |
| 호각 | `None` | 없음 | `EnemySkill/Human/human_guard_horn_호각.asset` | 증원 |
| 성스러운 빛 | `HolyMagic` | `신성마법1/2/3` | `EnemySkill/Human/human_priest_holy_light_성스러운 빛.asset` | 신성 빛 피해 |
| 신실한 기도 | `Blessing` | `blessing_01/02` | `EnemySkill/Human/human_priest_prayer_신실한 기도.asset` | 회복 |
| 성서 낭독 | `Blessing` | `blessing_01/02` | `EnemySkill/Human/human_priest_scripture_성서 낭독.asset` | 적군 측 버프 |
| 사격 | `Piercing` | `피어싱1/2/3` | `EnemySkill/Human/human_crossbow_shot_사격.asset` | 석궁 사격 |
| 관통 사격 | `Piercing` | `피어싱2/3` | `EnemySkill/Human/human_crossbow_piercing_shot_관통 사격.asset` | 관통 사격 |
| 후퇴 사격 | `Piercing` | `피어싱1/2/3` | `EnemySkill/Human/human_crossbow_retreat_shot_후퇴 사격.asset` | 석궁 사격 |
| 화학 구름 | `ArcaneMagic` | `마법3` | `EnemySkill/Human/human_alchemist_chemical_cloud_화학 구름.asset` | 화학 구름 |
| 치유 물약 | `Blessing` | `blessing_01/02` | `EnemySkill/Human/human_alchemist_healing_potion_치유 물약.asset` | 회복 |
| 신성한 강타 | `HolyMagic` | `신성마법3` | `EnemySkill/Human/human_paladin_holy_smite_신성한 강타.asset` | 신성 강타 |
| 용감한 돌진 | `Blunt` | `타격2/3` | `EnemySkill/Human/human_paladin_brave_charge_용감한 돌진.asset` | 돌진 충격 |
| 수호 방패 | `Shield` | `shield_barrier` | `EnemySkill/Human/human_paladin_guardian_shield_수호 방패.asset` | 적군 보호막 |

## 아군 스킬

| 스킬 | 타입 | 권장 이미지 | 에셋 | 근거 |
|---|---|---|---|---|
| 결투 | `Slashing` | `슬래싱1/2/3` | `AllySkill/Melee/ally_melee_duel.asset` | 근접 절단 |
| 파괴적인 강타 | `Blunt` | `타격2/3` | `AllySkill/Melee/ally_melee_destructive_blow.asset` | 강타/밀쳐내기 |
| 공포의 시선 | `ArcaneMagic` | `마법2/3` | `AllySkill/Melee/ally_melee_terrifying_gaze.asset` | 공포/기절 마법 |
| 고통의 갑옷 | `AllyShield` | `ally_purple_barrier` | `AllySkill/Melee/ally_melee_armor_of_agony.asset` | 아군 보호막 |
| 복수 | `AllyBlessing` | `ally_purple_blessing` | `AllySkill/Melee/ally_melee_revenge.asset` | 반격 태세 |
| 위협적인 돌진 | `Blunt` | `타격2/3` | `AllySkill/Melee/ally_melee_menacing_charge.asset` | 돌진 충격 |
| 절멸 | `Slashing` | `슬래싱1/2/3` | `AllySkill/Melee/ally_melee_extinction.asset` | 암흑 절단 |
| 저승의 갈고리 | `SlashingBlood` | `찍는 느낌1/2/3` | `AllySkill/Mid/ally_mid_hook_netherworld.asset` | 피가 튀는 갈고리 관통/출혈 |
| 재앙의 쇄도 | `Slashing` | `슬래싱1/2/3` | `AllySkill/Mid/ally_mid_surge_of_calamity.asset` | 어두운 근접 피해 |
| 그림자 도약 | `Slashing` | `슬래싱1/2/3` | `AllySkill/Mid/ally_mid_shadow_leap.asset` | 그림자 근접 피해 |
| 재의 장막 | `ArcaneMagic` | `마법2/3` | `AllySkill/Mid/ally_mid_ashen_veil.asset` | 재/실명 마법 |
| 살육 | `Slashing` | `슬래싱1/2/3` | `AllySkill/Mid/ally_mid_slaughter.asset` | 광역 출혈 절단 |
| 연쇄 처형 | `Slashing` | `슬래싱1/2/3` | `AllySkill/Mid/ally_mid_chain_execution.asset` | 처형 절단 |
| 어둠의 구속 | `ArcaneMagic` | `마법2/3` | `AllySkill/Ranged/ally_ranged_dark_binding.asset` | 어둠/동상 |
| 심연의 창 | `Piercing` | `피어싱1/2/3` | `AllySkill/Ranged/ally_ranged_abyss_spear.asset` | 창 투사체 |
| 영혼 수확 | `ArcaneMagic` | `마법1/2` | `AllySkill/Ranged/ally_ranged_soul_harvest.asset` | 영혼 흡수 |
| 연쇄 번개 | `ArcaneMagic` | `마법1/2` | `AllySkill/Ranged/ally_ranged_chain_lightning.asset` | 번개/마법 투사체 |
| 종말 | `FireMagic` | `붉은계열 마법1/2/3` | `AllySkill/Ranged/ally_ranged_apocalypse.asset` | 광역 화상 |
| 피의 축복 | `AllyBlessing` | `ally_purple_blessing` | `AllySkill/Common/ally_common_blood_blessing.asset` | 정화/흡혈 부여 |
| 불길한 결속 | `AllyShield` | `ally_purple_barrier` | `AllySkill/Common/ally_common_ominous_bond.asset` | 아군 보호막 |
| 와일드 헌트 | `AllyBlessing` | `ally_purple_blessing` | `AllySkill/Common/ally_common_wild_hunt.asset` | 아군 버프 |
| 수확 | `Slashing` | `슬래싱1/2/3` | `AllySkill/Unique/hero_harvest.asset` | 출혈 절단 |
| 붕괴의 유성 | `FireMagic` | `붉은계열 마법1/2/3` | `AllySkill/Unique/hero_collapse_meteor.asset` | 유성/화상 |
| 차원균열 | `ArcaneMagic` | `마법2/3` | `AllySkill/Unique/hero_dimensional_rift.asset` | 차원/기절 |
| 영혼 착취 | `ArcaneMagic` | `마법1/2` | `AllySkill/Unique/hero_soul_drain.asset` | 영혼 흡수 |

## 데이터 불일치 메모

- 생성기 기준 스킬 수: 74개.
- 현재 존재하며 생성기 ID와 매칭되는 에셋: 72개.
- 생성기에는 있으나 현재 같은 ID 에셋이 없는 항목:
  - `dragon_boss_claw` / 용의 발톱
  - `human_alchemist_explosive_potion` / 폭발 물약
- 현재 에셋은 있으나 생성기 ID에 없는 항목은 본문 제외:
  - `dragon_boss_breath.asset`
  - `human_alchemist_acid_potion_산성 물약.asset`
  - `ally_melee_strike.asset`
  - `Temp/*`
  - `EnemySkill/SK_*`, `EnemySkill/Sk_*`

## 현재 운용 타입

현재 `HitEffectRegistry` 등록 기준 운용 타입:

- `Slashing`
- `SlashingBlood`
- `Piercing`
- `Blunt`
- `Blessing`
- `ArcaneMagic`
- `FireMagic`
- `HolyMagic`
- `Shield`
- `AllyBlessing`
- `AllyShield`

보호막/블레싱 계열 분리 규칙:

- 적군 스킬: `Shield`, `Blessing` 사용. 노란색 계열 프리팹.
- 아군 스킬: `AllyShield`, `AllyBlessing` 사용. 보라색 계열 프리팹.
- `None`은 피격 이펙트를 출력하지 않는 스킬에만 사용.
- `Magic`은 enum에 남아 있으나 현재 분류에서는 사용하지 않음.
