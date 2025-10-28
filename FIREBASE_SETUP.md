# Firebase SDK 설치 가이드

## 1. Firebase SDK 다운로드

### 방법 A: Firebase Unity SDK 다운로드 (권장)
```
1. https://firebase.google.com/download/unity 방문
2. `firebase_unity_sdk.zip` 다운로드 (최신 버전)
3. ZIP 파일 압축 해제
```

### 방법 B: Package Manager에서 설치 (Unity 2020.3+)
```
1. Unity Editor 열기
2. Window → Package Manager
3. "+" 버튼 → "Add package from tarball..."
4. 다운로드한 Firebase SDK 중 필요한 패키지 선택:
   - FirebaseDatabase.unitypackage (필수)
   - FirebaseAuth.unitypackage (인증 시)
```

---

## 2. Firebase 프로젝트 설정

### Google Firebase Console 설정
```
1. https://console.firebase.google.com/ 접속
2. 프로젝트 생성 또는 기존 프로젝트 선택
3. "앱 추가" → Android/iOS 선택
4. 패키지 이름 입력 (com.company.game)
5. google-services.json (Android) 또는 GoogleService-Info.plist (iOS) 다운로드
```

### Unity 프로젝트에 설정 파일 추가
```
Android:
  Assets/Plugins/Android/google-services.json

iOS:
  Assets/Plugins/iOS/GoogleService-Info.plist
```

---

## 3. Unity Firebase 패키지 Import

### 필수 패키지
```
1. Assets → Import Package → Custom Package
2. 다운로드한 SDK에서 선택:
   - FirebaseDatabase.unitypackage (필수)
   - FirebaseAuth.unitypackage (Google Play Games 연동 시)
   - FirebaseAnalytics.unitypackage (옵션)
```

### 의존성 자동 해결
```
- Unity Editor에서 자동으로 External Dependency Manager 실행
- Android Resolver가 필요한 AAR/JAR 파일 다운로드
- iOS는 CocoaPods로 의존성 관리
```

---

## 4. Realtime Database 규칙 설정

### Firebase Console에서 Database 생성
```
1. Firebase Console → Realtime Database
2. "데이터베이스 만들기" 클릭
3. 리전 선택 (asia-northeast3 - Seoul)
4. 보안 규칙 선택:
   - 테스트 모드 (개발 중): 모든 읽기/쓰기 허용
   - 프로덕션 모드 (배포 시): 인증된 사용자만 허용
```

### 보안 규칙 예시
```json
// 테스트 모드 (개발)
{
  "rules": {
    ".read": true,
    ".write": true
  }
}

// 프로덕션 모드 (배포)
{
  "rules": {
    "users": {
      "$uid": {
        ".read": "auth != null && auth.uid == $uid",
        ".write": "auth != null && auth.uid == $uid"
      }
    }
  }
}
```

---

## 5. DatabaseManager 사용 예시

### 기본 사용법
```csharp
// DatabaseManager 초기화 대기
await UniTask.WaitUntil(() => DatabaseManager.Instance.IsInitialized);

// DatabaseReference 가져오기
var dbRef = DatabaseManager.Instance.DBReference;
var usersRef = DatabaseManager.Instance.GetReference("users");

// 데이터 쓰기
await DatabaseManager.Instance.SetDataAsync("users/123", new { name = "Player1", score = 100 });

// 데이터 읽기
var snapshot = await DatabaseManager.Instance.GetDataAsync("users/123");
if (snapshot != null && snapshot.Exists)
{
    var name = snapshot.Child("name").Value.ToString();
    Debug.Log($"Player name: {name}");
}

// 데이터 업데이트
var updates = new Dictionary<string, object>
{
    { "score", 200 },
    { "lastUpdated", DateTime.UtcNow.ToString("o") }
};
await DatabaseManager.Instance.UpdateDataAsync("users/123", updates);

// 데이터 삭제
await DatabaseManager.Instance.DeleteDataAsync("users/123");
```

### AuthManager 연동
```csharp
// 로그인 후 유저 데이터 저장
if (await AuthManager.Instance.SignInAsync())
{
    await AuthManager.Instance.SaveUserDataToDatabase();
}

// 유저 데이터 로드
await AuthManager.Instance.LoadUserDataFromDatabase();
```

---

## 6. 빌드 설정

### Android 빌드
```
1. File → Build Settings → Android
2. Player Settings → Other Settings
3. Package Name: Firebase Console과 동일하게 설정
4. Minimum API Level: 21 (Android 5.0) 이상
5. Target API Level: 최신 버전 권장
```

### iOS 빌드
```
1. File → Build Settings → iOS
2. Player Settings → Other Settings
3. Bundle Identifier: Firebase Console과 동일하게 설정
4. Xcode 프로젝트 생성 후 CocoaPods 실행:
   cd [XcodeProjectPath]
   pod install
5. .xcworkspace 파일로 프로젝트 열기
```

---

## 7. 트러블슈팅

### 문제: "Firebase dependencies could not be resolved"
```
해결:
1. Assets → External Dependency Manager → Android Resolver → Force Resolve
2. Unity 재시작
3. google-services.json 파일 경로 확인
```

### 문제: "DependencyStatus.UnavailableOther"
```
해결:
1. Firebase SDK 버전 확인 (Unity 버전과 호환 여부)
2. google-services.json에 Database URL 포함 여부 확인
3. 인터넷 연결 확인 (Firebase 초기화 시 필요)
```

### 문제: Android 빌드 실패
```
해결:
1. mainTemplate.gradle에 Firebase 플러그인 추가 확인
2. External Dependency Manager 재실행
3. Package Name 일치 여부 확인
```

---

## 8. 참고 링크

- Firebase Unity SDK: https://firebase.google.com/docs/unity/setup
- Realtime Database 가이드: https://firebase.google.com/docs/database/unity/start
- Firebase Console: https://console.firebase.google.com/
- Unity Package Manager: https://docs.unity3d.com/Manual/upm-ui.html
