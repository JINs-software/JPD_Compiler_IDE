### \[개요\]
네트워크 코어 라이브러리(JiniNet, https://github.com/JINs-software/JiniNet)를 통한 서버 구현에 있어 메시지 송신 시 직렬화 버퍼를 할당받고, 메시지 내용을 기입한 후 라이브러리에서 제공하는 'SendPacket' 함수를 호출한다. 수신의 경우 'OnRecv' 이벤트 함수(called by lib's thread)를 재정의하여 수신된 메시지에 따른 컨텐츠 단의 로직 코드를 구현한다. 
직렬화 버퍼의 편이성과 라이브러리가 제공하는 함수 또는 이벤트를 통해 네트워크 작업을 추상화하여 컨텐츠 개발에 집중할 수 있다. 보다 더 빠르면서 안전한 개발이 가능해졌지만, 직렬화 버퍼의 사용과 수신 이벤트 함수 내에서의 링버퍼 파싱 및 메시지 타입에 따른 분류가 불가피하기에 송수신 과정이 완전한 추상화가 이루어졌다고 보기 어렵다. 이 과정들까지 라이브러리 단에 맡기며, 송수신의 모든 과정을 함수로 추상화한다. JiniNet 라이브러리가 이 RPC 개념 및 로직이 적용된 네트워크 코어 라이브러리이며, 여기에 나아가 본 프로젝트는 서버-클라이언트 간 송수신하는 패킷(메시지)를 명세할 수 있는 자동화 툴의 제작을 담았다. 

![image](https://github.com/user-attachments/assets/9d82e045-5633-48f1-a487-0918b67177f2)

<p align="center">
  <img src="https://github.com/user-attachments/assets/9d82e045-5633-48f1-a487-0918b67177f2" width="1000">
</p>
<p align="center">
  <em>source: https://nesoy.github.io/articles/2019-07/RPC</em>
</p>

---

### \[JiniNet Library's RPC\]
https://github.com/JINs-software/JiniNet

#### Proxy
라이브러리 단에서 호출되는 이벤트 함수 또는 컨텐츠 코드 내에서 클라이언트로의 메시지 송신 시, 전송할 메시지 종류에 따른 미리 정의된 프록시(proxy) 함수를 호출한다. 메시지 종류에 따른 프록시 함수에 메시지 필드에 해당하는 인수를 전달하여 호출하는 것으로 패킷 송신은 한 단계 더 추상화된다. 내부적으로 라이브러리 네트워크 모듈을 통해 마샬링되고, 송신 API를 거쳐 패킷을 송신한다.

#### Stub
수신의 경우 OnRecv 계열의 라이브러리 이벤트 함수를 재정의하여 서버-클라 간의 프로토콜에 미리 정의된 메시지 타입에 따른 메시지 역직렬화와 타입에 따른 메시지 함수를 호출한것으로 추상화된다.
컨텐츠 구현 시 가상 함수로 선언된 각 메시지 함수를 재정의하여 메시지 필드 값들을 인자로 전달받아 메시지 수신에 따른 컨텐츠 코드를 구현한다. 이 함수들의 선언을 제공하는 것이 스텁(Stub) 클래스이다.

<p align="center">
  <img src="https://github.com/user-attachments/assets/8d25a05c-c3a3-4dc2-9a32-9442ca2ad259" width="1000">
</p>

---

### \[JPD_Compiler_IDE\]
#### JPD Compile
JiniNet에 포함된 RPC 모듈을 사용하기 위한 자동화 코드 생성 및 활용의 첫 시작은 Json 파일에 서버-클라이언트 간 주고 받을 프로토콜 메시지들에 대한 명세이다. 

```json
 "JPD_Namespaces": [
        {
            "Namespace": "FightGameCrtDel",
            "ID": "0",
            "Defines": [
                {
                    "Message": "CRT_CHARACTER",
                    "ID": "0",
                    "Dir": "S2C",
                    "Param": [
                        { "Type": "UINT32", "Name": "ID", "FixedLenOfArray": "" },
                        { "Type": "BYTE", "Name": "Dir", "FixedLenOfArray": "" },
                        { "Type": "UINT16", "Name": "X", "FixedLenOfArray": "" },
                        { "Type": "UINT16", "Name": "Y", "FixedLenOfArray": "" },
                        { "Type": "BYTE", "Name": "HP", "FixedLenOfArray": "" }
                    ]
                },
              	{
                  	"Message": "...",
                  	"ID": "...",
                  	"Dir": "...",
                    "Param": [...]
                }
			]
        },
  	 	{
            "Namespace": "FightGameAttack",
            "ID": "20",
            "Defines": [
                {
                    "Message": "ATTACK1",
                    "ID": "20",
                    "Dir": "C2S",
                    "Param": [
                        { "Type": "BYTE", "Name": "Dir", "FixedLenOfArray": "" },
                        { "Type": "UINT16", "Name": "X", "FixedLenOfArray": "" },
                        { "Type": "UINT16", "Name": "Y", "FixedLenOfArray": "" }
                    ]
                },
              	{
                  	"Message": "...",
                  	"ID": "...",
                  	"Dir": "...",
                    "Param": [...]
                }
			]
        }
   ]
```

서버-클라이언트 간 송수신 메시시지들에 대한 프로토콜을 json에 명시하고, 추후 추가되거나 수정되는 사안도 모두 json 파일을 통해 약속하여 서버-클라이언트 간 독립적으로 존재하는 헤더 또는 cs 파일가 상이해질 수 있는 문제를 방지한다.

이 json 파일을 파싱하여 서버-클라이언트 프로젝트에 포함될 자동화 코드를 만드는 컴파일 기능(컴파일러라 부르기 매우 부끄럽지만)을 수행하는 실행 파일이 존재한다. 서버의 경우 JiniNet 라이브러리 RPC 모듈의 Attact 계열의 함수를 통해 RPC 기능을 수행할 수 있도록 하는 프록시 클래스와 프록시 함수(송신)과 메시지 수신 시 수신된 메시지에 따라 컨텐츠 코드를 수행할 수 있도록 하는 메시지 함수(가상 함수)가 선언된 스텁 클래스와 클래스 선언 코드가 생성된다. 
클라이언트의 경우 유니티의 C# 스크립트 형태의 RPC 컴포넌트 코드가 생성된다. 마찬가지로 프록시 함수의 경우 그 송신을 추상화한 정의까지 구현되어 있으며, 스텁의 경우 메시지 수신 시 수행 로직을 재정의하여 구현하도록 추상 클래스로 제공된다. 

<p align="center">
  <img src="https://github.com/user-attachments/assets/cade64ce-435a-4597-9eb5-af63155d8cc2" width="1000">
</p>

* JiniNet: https://github.com/JINs-software/JiniNet
* JiniNet RPC 적용 테스트 게임 서버: https://github.com/JINs-software/MMO_Fighter
* JiniNet RPC 적용 테스트 게임 클라이언트: https://github.com/JINs-software/MMO_Fighter

<B></B>

#### JPD IDE

<p align="center">
  <img src="https://github.com/user-attachments/assets/0ebddbe2-e1ec-4b23-9bdc-48c0ee28cb41" width="1000">
</p>

Json 파일을 통한 직접적인 명세만으로 프로토콜을 정의하는 것에 두 가지 불편한 점이 존재하였다. 하나는 메시지 명세가 장황해질 경우 가독성이 떨어졌다는 것이고, 또 다른 하나는 명세 과정에서 실수가 발생할 수 있다는 것이다. JPD(JiniNet Protocol Define)의 자체적인 문법에 맞는 Json을 작성하는 과정에서 문법을 어긋내 작성하는 경우와 Json 자체의 문법을 어긋내는 경우 더러 발생하였다.

JPD IDE는 메시지(및 const, enum)을 정의할 수 있는 UI 제공 툴이다. 메시지 명세에 대한 문서가 아닌 UI를 통해 명세에 대한 가독성을 높이며, Json 파일의 직접적인 작성이 아니기에 문법적 오류도 피할 수 있다(JPD 명세와 컴파일까지 수행할 수 있기에 IDE라는 이름을 붙이긴 했지만, 터무니 없는 부분이긴 하다...).

JPD IDE 개발의 또 다른 목적 중 하나는 유니티 개발 실습을 수행하는 것이고, UI 시스템에 대한 이해를 함양하기 위한 것도 있다.

---

### \[Usage (JPD 편집 툴)\]
### 1. 초기 화면

<img src="https://velog.velcdn.com/images/jinh2352/post/8d19bb5c-93f5-4c6b-9811-e286addc81d2/image.png" width="500">


* 'Load': 기존 json 명세 파일로 불러오기
* 'New': 새로운 명세 작성 

### 2. Json 파일 로드

<img src="https://velog.velcdn.com/images/jinh2352/post/852df056-eb63-47d5-8f14-4fdd721f42ee/image.png" width="500">

'Load' 버튼 클릭 - 파일 탐색기 - 프로토콜 명세가 작성된 json 파일 로드

<img src="https://velog.velcdn.com/images/jinh2352/post/e8385ff2-b181-4611-a44f-9057f60f7a3a/image.png" width="500">

파일을 로드하면 메타데이터로서 저장된 서버 측 코드 저장 경로와 클라이언트 측 코드 저장 경로가 나타난다. 'Server', 'Client' 토글로 자동화된 코드 생성에서 제외할 수 있으며, 파일 경로 또한 바꿀 수 있다. 

<img src="https://velog.velcdn.com/images/jinh2352/post/95c4bbd8-2147-4e77-aaa4-84007b999bcf/image.png" width="500">
<em>(경로 입력 창 클릭 시 디렉터리 탐색기 창 활성화)</em>

<img src="https://velog.velcdn.com/images/jinh2352/post/bc0f57ad-dc64-48c5-92b3-34445c6fe2dd/image.png" width="500">

'edit' 버튼 클릭 시 미리 정의된 json 명세는 파싱되며 \[Message\], \[Enum\], \[Const\] 별로 편집할 수 있는 추가 UI가 생성된다.

### 3. Message 편집

\[Message\] 항목은 서버-클라이언트 간 주고 받을 메시지(패킷)에 대한 정의 및 수정을 하는 곳이다.

<img src="https://velog.velcdn.com/images/jinh2352/post/ae303174-781f-4320-ae4c-cd3ed37ced7a/image.png" width="500">

* Valid Code: 서버-클라이언트 간 미리 정의된 정적인 값의 코드(메시지 헤더에 포함되며 규약되지 않은 패킷 또는 비정상적인 패킷을 식별하는 코드)

* Simple Header 토글: 기본 프로토콜 헤더는 1바이트 valid code, 2바이트의 메시지 길이, 1바이트의 임의 난수 키(인/디코딩 시 활용), 그리고 1바이트의 체크섬 필드를 가짐. 
단순히 짧은 길이의, 많은 종류의 메시지 종류를 가지지 않은 경우이거나 LAN 구간 내 통신 프로토콜이기에 메시지에 대한 Encode/Decode 작업이 필요없는 경우 'Simple Header' 토글을 선택하여 간단한 헤더를 사용할 수 있음.

* Encode/Decode 토글: Simple Header가 아닌 경우 Encode/Decode 작업이 가능한 헤더를 가짐. 해당 토글을 통해 패킷 송수신 시 Encode/Decode 수행 여부를 결정.

* Namespace List: 메시지 정의 시 네임 스페이스에 따라 분류 가능. 로그인/로비/던전 등 별개의 메시지 항목을 같는 컨텐츠 코드 작성 시 네임 스페이스 분류를 통한 메시지 관리를 수행할 수 있음.

 <img src="https://velog.velcdn.com/images/jinh2352/post/acf4d51e-bf5c-4e3b-ade8-7b20d4b208bf/image.png" width="500">

 드롭다운 UI를 적용하여 Namespace를 선택할 수 있으며, 'New' 버튼을 통해 네임 스페이스를 추가하고 새로운 네임 스페이스 메시지들을 정의. 

 각 네임 스페이스에는 ID를 지정할 수 있으며, 해당 ID를 증분의 시작 값으로 하여 각 메시지들의 ID(메시지 종류를 표현하는 정수 값)이 할당. 
 현재 'FighterGameMove' 네임 스페이스가 ID가 10이고, 'FightGameAttack'의 ID가 20인 상황에서 'FighterGameMove' 네임 스페이스에 정의된 메시지가 10개를 넘어가게 되면 다음 네임 스페이스 ID를 침범하게 되므로 컴파일 에러 발생.

 <img src="https://velog.velcdn.com/images/jinh2352/post/2649863d-05a7-4879-9d4f-0c4455d112d0/image.png" width="500">

<B></B>
<B></B>

네임 스페이스 'ID' 입력란 아래 스크롤 뷰는 네임 스페이스 내 메시지 정의 항목들이다(이하 메시지 블록). 메시지 블록은 버튼 객체이며 블록 클릭 시 우측에 메시지에 대한 편집을 수행할 수 있는 추가 UI가 확장된다.

<img src="https://velog.velcdn.com/images/jinh2352/post/76b45ae1-8b6d-408a-afd2-209a62824e2e/image.png" width="500">

* Name: 메시지 이름을 정의(패킷의 이름).

* Direction: 메시지의 방향을 정의. 'Server to Client' 토글 선택 시 서버->클라이언트 방향의 메시지이며 서버 측의 proxy 함수가 추가되며, 클라이언트 측의 스텁 함수가 추가. 
'Client to Server' 토글 선택 시 그 반대입니다.

* Parameter: 메시지의 필드이자 함수의 파라미터를 편집. Type의 경우 서버 측 언어인 C++과 클라이언트 측 언어에서 그 이름이 다를 수 있기에 매핑 메타데이터를 통해 언어에 상응되는 이름으로 치환. 'Array' 토글을 통해 고정 길이의 배열 필드(파라미터)로 생성.

메시지를 편집하면, 그 내용이 메시지 블록의 텍스트에 자동으로 반영된다. 
	
다시 네임 스페이스 메시지 블록 스크롤 뷰로 돌아와, 'Add' 버튼을 통해 메시지를 추가할 수 있다. 새로운 메시지 블록이 생성되며, 비어있는 메시지 편집창이 추가된다.

<img src="https://velog.velcdn.com/images/jinh2352/post/04c3ea6d-61b3-4910-b6e2-447d5f9984d2/image.png" width="500">

<img src="https://velog.velcdn.com/images/jinh2352/post/e3d30b71-61ef-48ba-853c-0716f06a9543/image.png" width="500">
<em>(메시지 이름 정의)</em>

<img src="https://velog.velcdn.com/images/jinh2352/post/f73f9bf5-1da1-4b6e-9fbc-8825b10087e8/image.png" width="500">
<em>(메시지 필드 및 파라미터 편집)</em>


### 4. Enum 편집

서버-클라이언트 간 주고 받을 메시지를 정의하다 보면 공통적으로 사용될 enumeration을 자주 정의하게 되었다.

<img src="https://velog.velcdn.com/images/jinh2352/post/4f2b9191-60e5-4bbb-ad4d-f157d9586050/image.png" width="500">
	
enum의 정의가 많아지면 서버-클라 중 한 쪽에서 누락이 발생한 경우가 종종 있었고, 이에 은근 골치 아프던 상황이 종종 발생하였다. 따라서 툴에 enum 정의를 할 수 있도록 기능을 추가하였다.

<img src="https://velog.velcdn.com/images/jinh2352/post/3ee71fa3-544b-4983-a7a8-c825175111f3/image.png" width="500">

아래 define으로 정의된 방향 관련 정적 데이터를 enum화 시켜보면,

<img src="https://velog.velcdn.com/images/jinh2352/post/f51de627-9741-4d93-9a36-2841816e3c3a/image.png" width="500">

공란의 'Enum' 입력창에 적절한 enum 이름을 입력하고 'Ok' 버튼을 누르면 'Enum List'에 추가된다.

<img src="https://velog.velcdn.com/images/jinh2352/post/2865d77a-0ed4-4ad9-9c21-e34bcf8c940b/image.png" width="500">

메시지 정의와 마찬가지로 'Add' 버튼을 눌러 enum 필드를 추가할 수 있다. 

<img src="https://velog.velcdn.com/images/jinh2352/post/3ad1eae6-b0be-467f-a2bc-171893a523cb/image.png" width="500">

'Save Json'을 눌러 json 파일을 저장하면 추가된 enum 항목을 확인할 수 있다.

<img src="https://velog.velcdn.com/images/jinh2352/post/5091306c-cb21-46d1-8d5a-6f7432a35c5c/image.png" width="500">

#### 5. Const 편집

Enum 편집 기능 추가와 비슷한 맥락으로 서버-클라 공통으로 사용되는 const를 추가 및 편집하는 기능을 툴에 추가하였다. 'Const' 토글 선택을 통해 진입한다.

<img src="https://velog.velcdn.com/images/jinh2352/post/b5fe4d1d-e481-48a7-b5c1-3166da5ed3a1/image.png" width="500">

아래와 같은 const 멤버를 같는 구조체를 생성할 수 있다.

<img src="https://velog.velcdn.com/images/jinh2352/post/5f6df291-6ad2-45ea-9e27-20b44e690825/image.png" width="500">

<img src="https://velog.velcdn.com/images/jinh2352/post/afef9eda-d32a-4c32-92d9-44e4c01f4e60/image.png" width="500">

<img src="https://velog.velcdn.com/images/jinh2352/post/82b50111-787c-463e-b084-8fef26f59441/image.png" width="500">
<em>(Json 파일)</em>

---

### \[Usage (자동 생성 코드 RPC 적용)\]

편집 도구를 통해 메시지 및 enum, const를 정의한 후 'Save Json' 버튼을 누르면 json 명세 파일이 저장된다. 
'Compile' 버튼을 누르면 좌측 서버 경로와 클라이언트 경로에 각 역할과 언어에 맞는 RPC 코드가 자동으로 생성된다.

<img src="https://velog.velcdn.com/images/jinh2352/post/1729c177-4fc4-4b8d-a748-35cbea5b4f8b/image.png" width="500">

<img src="https://velog.velcdn.com/images/jinh2352/post/068d55bf-2fa3-4538-bd12-22c4b4215798/image.png" width="500">
<em>(RPC 파일들 생성)</em>


### 서버 코드 적용
#### 1. 정의된 Stub 클래스를 상속받아 메시지 수신 로직 처리
메시지 송신 방향이 'Client to Server'인 경우 Stub 클래스(네임 스페이스로 분류) 가상 함수로 선언된다. 이 Stub 클래스를 상속받아 가상 함수를 재정의하여 메시지 수신 시 처리할 컨텐츠 코드를 삽입한다.
	
<img src="https://velog.velcdn.com/images/jinh2352/post/2424ad11-c12f-4d54-8560-ec1aeb79605a/image.png" width="500">
<em>(FightGameMove_C2S::Stub.h)</em>

프로젝트의 컴파일 대상 cpp 파일 중 적절한 파일에 스텁 계열의 cpp 파일을 포함(include)하여 프로젝트 빌드 시 컴파일되도록 한다.
	
<img src="https://velog.velcdn.com/images/jinh2352/post/61c6d338-d84b-484f-a25a-811c39fd18cb/image.png" width="500">
<em>(Stub.h)</em>
	
<img src="https://velog.velcdn.com/images/jinh2352/post/334b7343-abe6-4c3f-a30c-aef431bd3d4a/image.png" width="500">
<em>(Stup.cpp)</em>

#### 2. Proxy 객체 생성 및 라이브러리 코드에 부착(Attach)
프록시 함수를 사용하기 위해서 스텁과 마찬가지로 프로젝트 상 존재하는 cpp 파일에 Proxy_\[namespace\].cpp 파일 및 Comm_\[namespace\].cpp 파일을 포함하여 컴파일되도록 하여야 한다. 
	
구현한 스텁 클래스의 인스턴스와 프록시 객체를 생성하고 이를 서버 객체(JNetServer)에 부착하면 된다. 이 과정을 main 함수의 시작 시 초기화 과정으로 수행할 것이므로 main.cpp에 컴파일이 필요한 파일들을 포함한다.
여러 콘텐츠 파일에서 프록시 함수를 호출할 수 있도록 전역 변수 선언을 하였으며(스텁 객체는 컨텐츠에서 직접 접근하지 않음), 다른 파일에선 'extern' 키워드와 함께 선언하여 파일 간 중복 정의를 막는다.
	
(main.cpp, proxy 및 comm 포함)
	
	
(main.cpp, RPC 객체 attach)
	
	
(Contents.cpp, 프록시 객체 함수를 호출하는 컨텐츠 코드 파일)
	
	
(Contents.cpp, 프록시 함수 호출)
	
	
### 클라이언트 코드 적용
클라이언트 자동화 코드 생성 경로에 아래와 같이 코드가 생성된다. 


#### 1. RPC
RPC 클래스는 싱글톤으로 인스턴스화 된다. 이 싱글톤 객체의 proxy 정적 멤버를 통해 proxy 함수를 호출할 수 있다. 또한 스텁 컴포넌트를 부착(Attach)함으로써 메시지 수신 시 이에 맵핑되는 함수를 자동으로 호출되도록 해준다.
* AttachStub / DetachStub: 스텁 컴포넌트를 부착함으로써 재정의를 통해 구현한 메시지 처리 멤버 함수를 자동 호출되도록 함. RPC의 이벤트 핸들러 객체에 함수를 등록하고, RPC는 메시지 수신 시 이 핸들러에 등록된 함수를 Invoke 함. 
반대로 DetachStub은 부착되었던 스텁 컴포넌트에 대한 메시지 처리 함수 호출이 이루어지지 않도록 맵핑 관계를 끊는다. 주로 스텁 컴포넌트는 해당 스텁의 네임 스페이스 메시지를 처리하기 위한 씬 상의 객체에 부착되는데, 씬 전환 시 이 객체가 파괴되면, RPC에서는 파괴된 객체의 스텁 컴포넌트 함수를 호출할 위험이 있다.
		
		
AttachStub, DetachStub은 스텁 컴포넌트 생성 시 그 부모 스텁 클래스(이미 정의된 추상 클래스)의 Init 함수와 Clear 함수를 호출함으로써 수행된다. 부모의 Init 함수와 Clear 함수 또한 유니티 엔진의 Start, OnDestroy 함수 호출에 의존하기에 개발자는 관여할 바 없다.
(Stub.cs)
		
	
(FightGameCrtDel.cs)
		
		
		
* Update: RPC 컴포넌트가 부착된 싱글톤 객체의 Update 함수에서는 메시지 수신 및 메시지 타입에 따른 스텁 함수 호출을 수행
		
	
#### 2. Proxy
자동화로 생성된 메시지 송신 함수들이 정의된다. RPC 싱글톤 객체가 Proxy 객체를 생성해 참조하기에 메시지 송신 필요 시 RPC 전역 싱글톤 객체를 통해 proxy 멤버의 함수 호출만 하면 된다. 메시지 필드 지정은 함수 인수 전달로 추상화된다. 
	

(프록시 함수 호출 예, 플레이어가 제어를 컨트롤하는 컴포넌트에서 키보드 입력을 처리하여 이동 중지 및 공격 패킷을 송신 상황)
	
	

#### 3. Stub
스텁의 경우 서버 측 사용과 유사하게 미리 정의된 스텁 클래스를 상속받아 가상 함수를 재정의하여 동적으로 오버라이딩되도록 한다.
Stub.cs에는 정의한 메시지 네임 스페이스 별로 클래스가 정의되며, 각 추상 클래스의 자식 클래스가 하나의 파일로 생성된다. 
	
(Stub.cs)
	
(FightGameCrtDel.cs)
