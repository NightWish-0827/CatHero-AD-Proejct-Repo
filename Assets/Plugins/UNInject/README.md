# UNInject

**UNInject**는 Unity 프로젝트의 객체 간 결합도를 낮추고 의존성 관리를 효율적으로 처리하기 위해 설계된 경량 의존성 주입(Dependency Injection) 프레임워크입니다.

## 개요

많은 리소스를 사용하는 무거운 DI 컨테이너 대신, Unity의 워크플로우를 존중하면서도 성능과 명확성을 동시에 확보하는 것을 목표로 제작되었습니다. 에디터 타임의 의존성 베이킹(Baking)과 런타임의 동적 해소(Resolution)를 결합하여 유연한 구조를 제공합니다.

## 주요 특징

- **계층적 주입 구조**: `Local`, `Scene`, `Global` 세 가지 범위를 통해 의존성의 생명주기와 범위를 명확하게 관리합니다.
- **에디터 기반 베이킹**: `Local` 의존성의 경우 에디터 타임에 미리 연결하여 런타임 오버헤드를 최소화합니다.
- **Attribute 기반 인터페이스**: `[Inject]`, `[GlobalInject]`, `[SceneInject]` 등 직관적인 어트리뷰트를 통해 손쉽게 의존성을 정의할 수 있습니다.
- **자동화된 관리**: `Installer` 시리즈(`Master`, `Scene`, `Object`)를 통해 중앙 집중식 또는 분산식 의존성 관리가 가능합니다.

## 권리 고지 및 소유권

본 소프트웨어 및 포함된 모든 소스 코드는 **Team. Waleuleu LAB**에 의해 설계 및 구현되었으며, 모든 창작적 권리는 작성자에게 귀속됩니다. 

- **개발자**: waleuleu LAB_황준혁
- **목적**: Unity 프로젝트 내 효율적인 아키텍처 구축 및 의존성 관리 자동화

---

*Usage documentation and detailed guides will be updated in the future.*
