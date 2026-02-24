# 캣 히어로 코드 구조 가이드

추후 추가되는 코드는 이 구조를 따릅니다.

---

## 1. 폴더 구조

```
Assets/Scripts/
├── Core/                 # 게임 루프, 매니저
│   └── GameManager.cs
├── Systems/              # 스폰, 웨이브, 카메라, 레지스트리
│   ├── EnemySpawner.cs
│   ├── EnemyRegistry.cs
│   └── Camera/
│       └── CameraFollow.cs
├── Units/                # 플레이어, 적, 투사체
│   ├── CatHeroPlayer.cs
│   ├── Player/
│   │   ├── PlayerStat.cs          # Root 하위. PC Stat SO 참조
│   │   ├── PlayerStatSO.cs        # ScriptableObject (PC Stat SO)
│   │   ├── PlayerMovement.cs
│   │   └── PlayerVisual.cs
│   ├── Projectile/
│   │   ├── Projectile.cs
│   │   └── ProjectileLauncher.cs
│   └── Enemy/
│       ├── Enemy Base/   # EnemyBase, IEnemy, EnemyStat, EnemyStatSO, EnemyVisual, EnemyState
│       └── NightmareMonster.cs
└── Utils/                # 풀, 이벤트 버스, 유틸
    ├── R3 Bus/GameEvents.cs
    ├── NWPool/           # PoolManager, IPoolable
    └── TweenTask Helpers/DOTweenUniTaskUtil.cs
```

---

## 2. 플레이어 계층 (UNInject 기반)

```
[Player Root]                         ← 최상위. ObjectInstaller 필수.
├── ObjectInstaller                  ← 의존성 배분. Bake Dependencies 실행.
├── CatHeroPlayer                    ← [Inject]로 Stat, Movement, Launcher, Visual 수신
├── PlayerStat                       ← PC Stat SO 참조. 스탯 제공
├── ProjectileLauncher
├── [Player Visual]                  ← PlayerVisual, SpriteRenderer
└── [Player Movement]                ← PlayerMovement (moveTarget = Player Root)
```

### PC Stat SO

- **PlayerStatSO**: ScriptableObject. CreateAssetMenu → CatHero/Player Stat
- **PlayerStat**: Root 하위 Mono. `[SerializeField] PlayerStatSO` 참조. CatHeroPlayer는 [Inject]로 PlayerStat 수신.

### 의존성 흐름

- **Root** = 최상위 오브젝트. **ObjectInstaller**가 하위 컴포넌트들의 의존성을 배분.
- **CatHeroPlayer**는 Movement, ProjectileLauncher, Visual을 **직접 참조하지 않음**.
- `[Inject]` 필드로 ObjectInstaller가 Bake한 값을 사용. (에디터에서 "Bake Dependencies" 실행)

### 적 계층 (플레이어와 동일 패턴)

**실제 프리팹 구조** ([Enemy Installer] 루트):

```
[Enemy Installer] (루트)             ← ObjectInstaller만 부착. Bake Dependencies 실행.
└── Enemy Prefab (자식)
    ├── NightmareMonster (EnemyBase)  ← [Inject]로 Stat, Visual 수신
    ├── [Enemy Visual]               ← EnemyVisual, SpriteRenderer (피격 효과)
    └── Enemy Stat                   ← EnemyStat (Enemy Stat SO 참조)
```

> EnemySpawner, PoolManager는 `GetComponentInChildren`으로 자식의 IEnemy/IPoolable을 검색.
> Despawn 시 `transform.root.gameObject`로 루트를 반환.

### Enemy Stat SO

- **EnemyStatSO**: ScriptableObject. CreateAssetMenu → CatHero/Enemy Stat
- **EnemyStat**: Root 하위 Mono. `[SerializeField] EnemyStatSO` 참조.

### UNInject 사용법

1. Player/Enemy Root에 **ObjectInstaller** 부착
2. CatHeroPlayer/EnemyBase에 `[Inject, SerializeField]` 로 의존성 필드 선언
3. ObjectInstaller Inspector에서 **🍩 Bake Dependencies** 클릭

---

## 3. 기본 사이클

```
GameManager.StartStage()
    → EnemySpawner.StartSpawning(player.Transform)
    → 주기적으로 PoolManager.Spawn(enemyPrefab) → IEnemy.Initialize(target)

CatHeroPlayer
    → [Inject] _stat, _movement, _projectileLauncher, _visual (Root가 배분)
    → PlayerStat: PC Stat SO에서 스탯 제공
    → ProjectileLauncher.Fire() → 투사체 발사
    → OnPlayerHit 구독 → HP 감소 → 사망 시 OnPlayerDeath

CameraFollow
    → target(플레이어 Root) 따라감. autoFindPlayer 옵션.

EnemyBase
    → [Inject] _stat, _visual (Root가 배분). EnemyStatSO에서 스탯.
    → Chase → Attack → GameEvents.OnPlayerHit.OnNext(damage)
    → TakeDamage → HP 0 시 DieSequence → PoolManager.Despawn

Projectile
    → NWPool 풀링. PoolManager.Spawn/Despawn 사용.
```

---

## 4. 이벤트 버스 (GameEvents)

| 이벤트 | 타입 | 용도 |
|--------|------|------|
| OnPlayerHit | Subject<float> | 플레이어 피격 (damage) |
| OnPlayerDeath | Subject<Unit> | 플레이어 사망 |
| OnEnemyKilled | Subject<IEnemy> | 적 처치 |
| OnWaveStarted | Subject<int> | 웨이브 시작 |
| OnWaveCleared | Subject<int> | 웨이브 클리어 |

---

## 5. 규칙

- **Root가 의존성 배분**: ObjectInstaller를 최상위에 두고, 하위는 [Inject]로 수신.
- **직접 참조 금지**: CatHeroPlayer 등은 GetComponent, SerializeField 수동 할당 대신 [Inject] 사용.
- **과한 캡슐화 지양**: 로직은 해당 Mono 클래스에 직접 구현.
- **적 추가**: `EnemyBase` 상속. `EnemyRegistry` 자동 등록.
- **플레이어 공격**: `ProjectileLauncher.Fire()` → 투사체 발사 → 타격 시 `IEnemy.TakeDamage()`.

---

## 6. 씬 구성

### Scene Referral / SceneInstaller (매니저 의존성 주입)

- **CatHeroPlayer**, **EnemySpawner**, **GameManager**: `[SceneReferral]` 부착
- **[Game Manager]** GameObject에 **ObjectInstaller** + **SceneInstaller** 부착 (둘 다 필수)
- SceneInstaller 선택 → Inspector 우클릭 → **Refresh Scene Registry** 실행
- **GameManager**: `[SceneInject]`로 CatHeroPlayer, EnemySpawner 수신 (전역 Instance 없음)
- 주입 실패 시 FindObjectOfType 폴백으로 동작 (경고 로그 출력)

### 오브젝트 배치

- **Player Root** (최상위):
  - **ObjectInstaller** (필수)
  - CatHeroPlayer, PlayerStat, ProjectileLauncher
  - PlayerStat: PlayerStatSO 에셋 참조 (Create → CatHero/Player Stat)
  - 자식 "Player Visual": PlayerVisual, SpriteRenderer
  - 자식 "Player Movement": PlayerMovement (moveTarget = Player Root)
  - ObjectInstaller → **Bake Dependencies** 실행
- **Managers** (또는 Scene Root): ObjectInstaller, SceneInstaller, GameManager
- **Main Camera** + CameraFollow
- **EnemySpawner**: enemyPrefab (ObjectInstaller + EnemyStat + EnemyVisual 포함 프리팹)
- **PoolManager**

### 적 프리팹 (NightmareMonster)

- **루트 [Enemy Installer]**: ObjectInstaller만 부착
- **자식 Enemy Prefab**: NightmareMonster, SpriteRenderer, Enemy Visual, Enemy Stat
- EnemyStat: EnemyStatSO 에셋 할당
- ObjectInstaller → **Bake Dependencies** 실행 (루트에서)
