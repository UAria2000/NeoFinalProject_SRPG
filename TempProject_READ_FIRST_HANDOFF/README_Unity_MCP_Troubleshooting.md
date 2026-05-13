# Unity MCP 연결 이슈 및 임시 해결 기록

## 개요

이 문서는 Unity AI Assistant의 Unity MCP Server와 Codex MCP 클라이언트 연결 실패 이슈 및 임시 해결 방법을 기록한다.

대상 환경:

- 프로젝트 경로: `C:\UnityProject\NeoFinalProject_SRPG`
- Unity Editor: `6000.3.2f1`
- OS: Windows
- MCP 클라이언트: Codex, VS Code OpenAI ChatGPT 확장 내부 `codex.exe`
- 관련 패키지: `com.unity.ai.assistant` `2.7.0-pre.3`

`Packages\manifest.json`에 `com.unity.ai.assistant`가 없거나 Unity 패키지 캐시가 재생성된 경우, Unity AI Assistant를 다시 설치한 뒤 아래 절차를 적용한다.

## 발생 증상

Unity Editor 내부 상태:

- `Project Settings > AI > Unity MCP Server`에서 Unity Bridge는 `Running`.
- Unity MCP Tools는 정상 발견.
- 예시: `Tools (20 of 52 enabled)`.
- Editor 로그에 `McpToolRegistry Discovered 20 MCP tools` 출력.

외부 MCP 클라이언트 상태:

- Codex MCP 서버 초기화는 성공.
- 그러나 Unity MCP 도구가 Codex 세션에 노출되지 않음.
- 새 채팅에서도 Unity MCP 도구가 나타나지 않음.

수동 테스트 로그:

```text
Successfully set up discovery for 1 debug tools + 0 Unity tools
Connecting to named pipe: \\.\pipe\unity-mcp-...
Named pipe connection error: ECONNREFUSED
Unity not available at startup
```

직접 Named Pipe 연결 테스트 결과:

```text
Access to the path is denied.
```

## 원인 판단

주요 원인은 Unity AI Assistant 패키지의 Windows Named Pipe 생성 보안 설정으로 판단.

패키지 내부 파일:

```text
Library\PackageCache\com.unity.ai.assistant@...\Modules\Unity.AI.MCP.Editor\Connection\NamedPipeListener.cs
```

해당 파일은 `CreatePipeViaPInvoke`에서 `CreateNamedPipe`를 직접 호출하며, SDDL 기반 보안 설명자를 적용한다.

기본 SDDL은 현재 사용자 SID와 System에만 권한을 부여한다.
이로 인해 Codex MCP 클라이언트가 같은 사용자 계정에서 실행되어도 Windows 무결성 수준 또는 프로세스 보안 컨텍스트 차이로 pipe 접근이 거부될 수 있다.

관리자 권한 실행과 일반 권한 실행을 모두 시도했으나 동일 증상이 발생했다.

## 적용한 임시 해결

`NamedPipeListener.cs`의 `CreatePipeViaPInvoke` 내부 SDDL 설정을 로컬 개발용으로 완화한다.

수정 대상 코드 위치:

```text
Library\PackageCache\com.unity.ai.assistant@...\Modules\Unity.AI.MCP.Editor\Connection\NamedPipeListener.cs
```

수정 내용:

```csharp
// Local workaround: allow same-machine MCP clients across integrity levels.
string sddl = "D:(A;;GA;;;WD)(A;;GA;;;SY)(A;;GA;;;BA)S:(ML;;NW;;;LW)";
```

의미:

- `WD`: Everyone 허용.
- `SY`: System 허용.
- `BA`: Built-in Administrators 허용.
- `S:(ML;;NW;;;LW)`: 낮은 무결성 수준 클라이언트까지 연결 가능하도록 완화.

주의:

- 이 수정은 로컬 개발용 임시 우회책.
- Unity 패키지 캐시를 직접 수정하는 방식이므로 Unity AI Assistant 업데이트, 재설치, PackageCache 재생성 시 사라질 수 있음.
- 보안 범위를 넓히므로 신뢰 가능한 로컬 개발 환경에서만 사용 권장.

## 백업 권장

수정 전 원본 파일을 프로젝트 밖 또는 Unity가 스캔하지 않는 임시 폴더에 백업한다.

권장 백업 위치:

```text
.codex_tmp\unity-ai-assistant-backups\NamedPipeListener.cs.codex-backup
```

주의:

- `Library\PackageCache` 내부에 `.cs` 확장자를 포함한 백업 파일을 두면 Unity가 에셋으로 스캔할 수 있음.
- 백업은 `.codex_tmp`처럼 프로젝트 작업용 임시 폴더에 보관하는 편이 안전함.

복구 시 백업 파일을 아래 위치에 덮어쓴다.

```text
Library\PackageCache\com.unity.ai.assistant@...\Modules\Unity.AI.MCP.Editor\Connection\NamedPipeListener.cs
```

## 적용 절차

1. Unity AI Assistant 설치.
2. Unity Editor 종료.
3. VS Code 및 Codex 관련 프로세스 종료.
4. `NamedPipeListener.cs` 원본 백업.
5. `NamedPipeListener.cs`의 SDDL 설정 수정.
6. Unity Editor 재실행.
7. Unity 컴파일 완료 대기.
8. `Project Settings > AI > Unity MCP Server` 확인.
9. Unity Bridge가 `Running`인지 확인.
10. Codex 새 채팅 또는 새 세션 시작.
11. Unity MCP Server 화면에서 `codex-mcp-client` 연결 확인.
12. 연결 요청이 표시되면 `Accept`.

## 검증 결과

패치 전 직접 pipe 테스트:

```text
Access to the path is denied.
```

패치 후 직접 pipe 테스트:

```text
connected=True
```

패치 후 Unity 로그:

```text
Client connected: NamedPipe-1
```

새 Codex 채팅에서 Unity MCP 연결 성공.

Unity MCP Server 화면에 다음 항목 표시:

```text
codex-mcp-client (PID ...)
Accepted
```

Unity 경고:

```text
codex-mcp-client has connected. This application is unsigned or not recognized and may be dangerous.
```

이 경고는 연결 실패가 아니라 외부 MCP 클라이언트 신뢰 여부 안내다.
Codex 실행 파일이 Unity에서 공식 인식된 앱으로 분류되지 않아 표시된다.
경로가 VS Code OpenAI ChatGPT 확장 내부 `codex.exe`이면 의도한 연결로 판단 가능.

## 재발 시 점검 순서

1. Unity MCP Server 화면에서 Bridge `Running` 확인.
2. Tools 목록이 발견되는지 확인.
3. `C:\Users\home\.unity\mcp\connections` 아래 최신 bridge JSON 확인.
4. `relay_win.exe --mcp` 실행 시 named pipe 연결이 되는지 확인.
5. `Access to the path is denied`가 재발하면 `NamedPipeListener.cs` 패치가 유지되어 있는지 확인.
6. Unity AI Assistant 업데이트 후에는 패치가 사라졌을 수 있음.
7. Codex는 새 채팅 또는 새 세션에서 다시 확인.

## 관련 참고

- Unity MCP 연결은 세션 시작 시 MCP 도구가 로드되는 방식으로 보임.
- 기존 Codex 채팅에는 Unity MCP 도구가 즉시 추가되지 않을 수 있음.
- Unity MCP 설정에서 `codex-mcp-client`가 여러 개 보이면 오래된 Codex 프로세스가 남은 상태일 수 있음.
- 사용하지 않는 VS Code/Codex 창을 닫고 Unity MCP Server 화면에서 불필요한 연결을 `Revoke`할 수 있음.

## 현재 프로젝트 적용 메모

- 현재 bridge 파일: `C:\Users\home\.unity\mcp\connections\bridge-3929f1dd-30472.json`
- 현재 pipe 경로: `\\.\pipe\unity-mcp-3929f1dd-30472`
- 현재 Unity Editor PID: `30472`
- 현재 패치 대상: `Library\PackageCache\com.unity.ai.assistant@198d71476a35\Modules\Unity.AI.MCP.Editor\Connection\NamedPipeListener.cs`
- 2026-05-13 기준, Unity AI Assistant 패키지 캐시 재생성으로 SDDL 패치가 사라져 재적용함.
- 2026-05-13 재시작 후 직접 pipe 테스트 결과: `connected=True`.
- 2026-05-13 연결 승인 기록: `Library\AI.MCP\connections-v2.asset`에 `Status: 1`, `ValidationReason: Approved by user` 확인.
