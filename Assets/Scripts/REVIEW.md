# Assets/Scripts 구조 검토 보고서

검토일: 2025-02-24  
STRUCTURE.md 기준 일관성 및 구조 적합성 검토 결과입니다.

---

## 1. STRUCTURE.md 대비 준수 현황

### ✅ 잘 지키고 있는 항목

| 항목 | 상태 | 비고 |
|------|------|------|
| Root 의존성 배분 | ✅ | CatHeroPlayer, EnemyBase 모두 `[Inject]` 사용 |
| 직접 참조 금지 | ✅ | Units는 GetComponent/수동 할당 없이 [Inject] 사용 |
| 이벤트 버스 (GameEvents) | ✅ | OnPlayerHit, OnPlayerDeath, OnEnemyKilled, OnWaveStarted, OnWaveCleared 정의 |
| 적 추가 패턴 | ✅ | EnemyBase 상속, EnemyRegistry 자동 등록 |
| 플레이어 공격 흐름 | ✅ | ProjectileLauncher.Fire() → IEnemy.TakeDamage() |
| PoolManager 사용 | ✅ | EnemySpawner에서 적 스폰 시 풀링 사용 |
| Stat SO 패턴 | ✅ | PlayerStatSO/EnemyStatSO → PlayerStat/EnemyStat 동일 구조 |

### ⚠️ STRUCTURE와의 차이 (실제 프리팹 구조)

**STRUCTURE.md** 적 계층:
```
[Enemy Root]
├── ObjectInstaller, NightmareMonster, EnemyStat (동일 레벨)
└── [Enemy Visual]
```

**실제 프리팹** ([Enemy Installer] 루트):
```
[Enemy Installer] (루트 - ObjectInstaller만)
└── Enemy Prefab (자식)
    ├── NightmareMonster, SpriteRenderer
    ├── Enemy Visual
    └── Enemy Stat
```

→ 코드는 `GetComponentInChildren`, `transform.root`로 실제 구조에 맞게 수정됨.  
→ **STRUCTURE.md**를 실제 프리팹 구조에 맞게 갱신하는 것을 권장.

---

## 2. 폴더 구조 비교

| STRUCTURE | 실제 | 비고 |
|-----------|------|------|
| Core/GameManager | ✅ | 동일 |
| Systems/EnemySpawner, EnemyRegistry | ✅ | 동일 |
| Systems/Camera/CameraFollow | ✅ | 동일 |
| Units/CatHeroPlayer | Units/Player/CatHeroPlayer | Player 폴더에 배치 (구조상 자연스러움) |
| Units/Player/* | ✅ | 동일 |
| Units/Projectile/* | ✅ | 동일 |
| Units/Enemy/* | ✅ | 동일 |
| Utils/R3 Bus, NWPool, TweenTask Helpers | ✅ | 동일 |

---

## 3. 개선 권장 사항

### 3.1 Projectile 풀링 (선택)

- **현재**: `Instantiate` / `Destroy` 사용
- **권장**: 적과 동일하게 PoolManager 사용 시 메모리·성능 일관성 확보
- **우선순위**: 낮음 (투사체 수가 적으면 현재 방식도 무방)

### 3.2 EnemyBase 디버그 로그 (선택)

- **현재**: `#if UNITY_EDITOR` 내부에 `_stat`, `_visual`, `target`, `poolCts` null 경고
- **권장**: 동작 확인 후 제거 또는 `[Conditional("UNITY_EDITOR")]` 등으로 축소
- **우선순위**: 낮음 (개발 중에는 유용)

### 3.3 GameManager 참조 방식

- **현재**: `[SerializeField]` + `FindObjectOfType` 폴백
- **권장**: SceneInstaller 등으로 [SceneInject] 사용 시 DI 일관성 향상
- **우선순위**: 중간 (현 구조로도 동작에는 문제 없음)

### 3.4 FindObjectOfType 사용

- **현재**: GameManager, CameraFollow에서 사용
- **권장**: Unity 2023+에서는 `FindFirstObjectByType` 등으로 교체 검토
- **우선순위**: 낮음 (버전 호환성 확인 후 적용)

---

## 4. 코드 일관성 요약

| 영역 | 평가 | 설명 |
|------|------|------|
| UNInject 패턴 | ✅ | Player/Enemy 모두 [Inject] 기반 |
| Stat SO 패턴 | ✅ | PlayerStat/EnemyStat 동일 구조 |
| 이벤트 흐름 | ✅ | GameEvents 중심으로 결합도 낮음 |
| 풀링 | ⚠️ | 적만 풀링, 투사체는 Instantiate |
| 네이밍 | ✅ | 클래스/폴더명 규칙 일관적 |
| 주석/요약 | ✅ | XML summary 적절히 사용 |

---

## 5. 결론

- **STRUCTURE.md**의 핵심 규칙(Root 의존성 배분, [Inject] 사용, 이벤트 버스, 풀링)은 잘 반영되어 있음.
- 실제 적 프리팹 구조([Enemy Root] + 자식 Enemy Prefab)에 맞게 코드가 수정되어 있어, 현재 구조와 잘 맞음.
- 위 개선 사항은 선택적으로 적용해도 되며, 현재 상태로도 구조적으로 적절함.

**STRUCTURE.md 갱신 권장**: 6. 씬 구성 > 적 프리팹 섹션에 실제 계층 구조([Enemy Installer] 루트 + 자식 Enemy Prefab)를 반영하는 것을 권장합니다.
