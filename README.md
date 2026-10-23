# Unity Resources

Unity `6000.3` 이상에서 사용하는 UPM 패키지예요. 이 폴더의 **내용 전체(숨김 파일과 `.meta` 포함)**를 새 `unity-resources` 저장소의 루트에 복사하면 돼요. `package.json`이 저장소 루트에 있어야 해요.

## 포함된 기능

| 타입 | 네임스페이스 | 기능 |
| --- | --- | --- |
| `ObjectPooling` | `Utility` | Resources 프리팹 생성, 대여, 반납, 사전 생성과 정리를 제공해요. |
| `SpriteCacheHelper` | `Utility` | Resources에서 읽은 Sprite를 경로별로 캐시해요. |

기존 호출부와의 호환성을 위해 원본 런타임 스크립트의 내용, 네임스페이스, `.meta` GUID를 유지해요. Player, Tile, Ground 등 CrawlStars의 코드나 에셋은 포함하지 않아요.

## 구조

```text
package.json
Runtime/
  Song.Unity.Resources.asmdef
  ObjectPooling.cs
  SpriteCacheHelper.cs
Tests/Editor/
  Song.Unity.Resources.Editor.Tests.asmdef
  ObjectPoolingTests.cs
  SpriteCacheHelperTests.cs
  ResourceTestAssets.cs
```

패키지 ID는 `com.song.unity-resources`, 어셈블리 이름은 `Song.Unity.Resources`, 초기 버전은 `0.1.0`이에요.

## 의존성과 설치

```text
Unity Resources 0.1.0
  → Unity Utilities 0.1.0 (SingletonMonoBehaviour)
      → UniTask 2.5.10 (ErrorHandler)
```

ObjectPooling이 SingletonMonoBehaviour를 상속하므로 Unity Utilities가 필요해요. Unity Utilities는 ErrorHandler 때문에 UniTask에 의존해요.

Unity의 Package Manager에서 **Install package from Git URL**로 다음 순서대로 설치해요. `YOUR_ACCOUNT`는 생성한 저장소의 소유자로 바꾸고, 두 저장소에 `v0.1.0` 태그를 먼저 생성해요.

1. `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.10`
2. `https://github.com/YOUR_ACCOUNT/unity-utilities.git#v0.1.0`
3. `https://github.com/YOUR_ACCOUNT/unity-resources.git#v0.1.0`

`package.json`에는 Unity Utilities의 패키지 ID와 버전을 선언해요. UPM은 패키지 사이의 Git URL 의존성을 지원하지 않으므로, Git 배포에서는 위 패키지들을 소비 프로젝트에 각각 설치해야 해요.

로컬 개발 중에는 UniTask, Unity Utilities를 먼저 설치한 뒤 **Install package from disk**에서 이 폴더의 `package.json`을 선택하면 돼요. 두 패키지의 메타데이터에는 이 저장소의 로컬 경로가 들어 있지 않아요.

소비 코드가 자체 `.asmdef`를 사용하면 `Song.Unity.Resources` 참조를 추가해요. SingletonMonoBehaviour 같은 Utility 타입도 직접 사용하면 `Song.Unity.Utilities`도 참조해요. 기본 `Assembly-CSharp`에서는 자동 참조해요.

## 사용 예시

씬의 루트 GameObject에 ObjectPooling 컴포넌트를 추가한 뒤 Awake가 실행된 이후에 호출해요. `Resources/Effects/Hit.prefab`을 준비했다면 다음과 같이 사용해요.

```csharp
using UnityEngine;
using Utility;

GameObject effect = ObjectPooling.Instance.Get("Effects/Hit");
ObjectPooling.Instance.TryAbandon("Effects/Hit", effect);
```

대여와 반납에 같은 Resources 상대 경로를 사용하고 확장자는 생략해요. 풀링 API는 Unity 메인 스레드에서 사용해요.

`Resources/UI/Icon.png`의 Texture Type을 Sprite로 설정했다면 다음과 같이 읽을 수 있어요.

```csharp
using UnityEngine;
using Utility;

Sprite icon = SpriteCacheHelper.Get("UI/Icon");
```

`SpriteCacheHelper.Clear()`는 캐시를 비우면서 Resources.UnloadAsset도 호출해요. 캐시의 Sprite를 사용하는 UI 등의 수명주기를 고려해서 호출해요. 런타임 동작은 이번 복사에서 변경하지 않았어요.

## EditMode 테스트

Unity Test Framework `com.unity.test-framework`를 검증 프로젝트에 설치해요. 기준 버전은 `1.6.0`이에요. 테스트 프레임워크는 일반 소비 프로젝트에 강제로 설치하지 않도록 이 패키지의 런타임 의존성에 포함하지 않았어요.

Git 또는 외부 로컬 경로로 설치했다면 검증 프로젝트의 `Packages/manifest.json` 최상위에 다음 속성을 병합해요. 기존 dependencies와 다른 testables 항목은 유지해요.

```json
{
  "testables": [
    "com.song.unity-resources"
  ]
}
```

Test Runner의 EditMode에서 `Tests.EditMode.UnityResources` 테스트를 실행해요. 테스트는 기존 풀링 10개 케이스와 Sprite 캐시 6개 케이스를 포함해요.

ResourceTestAssets는 테스트 클래스마다 고유한 `Assets/UnityResourcesTests_<GUID>` 폴더 아래에 빈 프리팹과 Sprite를 생성해요. 테스트 종료 시 자신이 생성한 폴더만 정리해요. CrawlStars의 Tile 프리팹, Ground 이미지, Constants는 필요하지 않아요. 테스트를 강제 중단하면 임시 폴더가 남을 수 있어요.

커맨드라인 실행 예시는 다음과 같아요. Unity 실행 파일과 검증 프로젝트 경로를 환경에 맞게 지정해요. `-runTests`가 종료를 처리하므로 `-quit`는 넣지 않아요.

```sh
"/path/to/Unity" -batchmode -nographics \
  -projectPath "/path/to/TestProject" \
  -runTests -testPlatform EditMode \
  -testFilter Tests.EditMode.UnityResources \
  -testResults "/path/to/results.xml" \
  -logFile "/path/to/tests.log"
```

## 기존 프로젝트에 적용할 때

원본 `.cs`와 같은 GUID의 `.meta`가 이 패키지에 들어 있어요. 소비 프로젝트의 Assets에 원본을 둔 채 패키지까지 설치하면 타입과 GUID가 중복되므로, 실제 패키지 전환 시에는 원본과 패키지 중 하나만 임포트되도록 해야 해요. 현재 CrawlStars의 원본 파일은 그대로 유지했어요.

## 릴리스

`package.json` 버전과 CHANGELOG를 갱신하고 해당 커밋에 같은 버전의 태그를 생성해요. Unity Utilities의 요구 버전이 바뀌면 dependencies도 갱신하고, 소비 프로젝트의 설치 버전도 맞춰요.
