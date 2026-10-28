# MarketCrate — UI/UX & Custom Controls

## 5차 사용자 연결 화면 (최신)

- 새 `Steam 실험 연동` 탭: QR 연결 / 실제 보관함·호가 / 판매·재가격 / 결과·수동 대조. 기존 WPF 템플릿/MVVM 유지, 별도의 native look/브라우저 UI 없음. 실험 탭은 전체 작업 폭을 쓰고 Mock 메뉴/합계/상태와 혼합하지 않는다.
- 위험별 체크 후 QR 시작, 공식 앱에서 직접 승인, 명시적 상태 확인, 만료·거부·취소·네트워크/접근/계약 실패, 로그아웃. 로컬 QR quiet zone과 빈 상태, 모달 Esc/탭 탐색, 자체 접근 가능한 이름을 둔다. 정책 capability가 없으면 거래 동의만으로 활성화되지 않는다.
- 게임별 보관함 수량·예약 반영 가용량, marketable/tradable, 사용자 선택 시세 갱신과 원천/시각, 최대50종 요청 경계. ListBox recycling/virtualization 유지. 중간가격 이력은 VWAP로 표시하지 않는다.
- KRW 원/USD 달러 편집→정수 minor 환산, 호가와 증분 반영 추천을 같이 표시한다. 수량·Steam/게임 fee·실수령·총합과 부족 수량을 분리하며 잘못된 가격/신선도는 제출 불가다. 최종 전체 승인 1회·미전송 중단·공식 앱 수동 확인을 유지한다.
- 판매 개별/전체 선택, 취소·새 가격 최종 검토, 보존된 재가격 대조/재개, 요청별 후보/시각/원래 가격·수량/기존 ID/판정/감사 이력. 증거 부족이면 Unknown과 차단 이유를 보여 주고 강제 성공 기능은 두지 않는다.
- 자체 렌더에서 standalone foreground와 좁은 화면/Mock 문구 혼동을 수정했다. 실제 입력·열린 Popup·모니터 DPI·스크린리더·실제 QR 스캔은 미검증이다. 합성 화면은 CONTRACT TEST 표식을 사용한다.

## 4차: 실험적 코드와 제품 UI 구분 (최신)

- 기록·안전에 비공식 어댑터 기본 비활성·실계정 미검증·제재 위험·미지원 통화/스택/재가격을 명시하는 자체 테마 카드를 추가한다. 이 화면에 계정 로그인/실거래 버튼을 노출하지 않는다. 앱은 계속 Mock이다.
- 안전 화면의 좁은 viewport에서 오른쪽 잘림을 줄이도록 가로 스크롤을 막고 여백을 확보했다. detached 렌더/카드 경계 검사를 추가한다. 실제 창·입력·팝업·DPI 검증으로 확대 해석하지 않는다.
- 향후 QR·위험별 최초 옵트인·읽기 연결 상태·실계정 통화/수수료 검토 UI는 별도 구현해야 한다. 위험별 옵트인은 세션 활성화 때만, 거래당 승인은 최종 전체 배치에서 한 번이라는 목표를 유지한다.

## 3차 검토·관리 UX (최신)

- 검토표에서 등록 단위별 구매자 단가 편집, 판매 수량, 구매자 합계, Steam/게임 수수료, 실수령, 추천/수동 근거, 조회 시각/신선도를 구분한다. 수정은 검토 합계·우측 예상 합계에 즉시 반영된다.
- 부족 잔량은 가격을 비워 둔 별도 행이다. 부족/즉시 체결 추정 불가 수량을 표시하고 입력 전에는 제출을 막는다. 직접 지정한 가격의 체결은 보장하지 않는다. 전략 변경·검토 닫기·갱신은 수동 초안을 초기화하며 화면에 안내한다.
- 판매 목록은 게시 항목 전체 선택/해제, 취소 검토, 가격 변경 검토, 확정 실패 관리 재시도. 배치 결과는 실패 선택/재시도 검토를 제공한다. 불명/승인 대기는 재시도 대상이 아니다.
- 하나의 재사용 관리 모달에서 대상·가격·수량·수수료·실수령·목적을 명시하고 최종 한 번 승인한다. 가격 변경만 입력 가능하고 취소/재시도 가격은 읽기 전용이다. 서로 다른 모달/배경 작업은 동시에 조작하지 않게 한다.
- 모달의 Tab cycle·Esc·외부 영역 닫기·자체 활성 창 내부 포커스 경로를 유지/정리했다. 테스트는 실제 포커스·입력·열린 Popup을 사용하지 않으므로 실제 상호작용은 미검증이다. ComboBox Popup은 StaysOpen=false와 커스텀 템플릿을 유지한다.
- 로딩/조회 실패에서도 기존 데이터를 보존하며 수동 갱신/대조를 안내한다. 렌더 검토에서 로딩 중단 버튼이 잘리던 패널 크기를 수정하고 범위 검사를 추가했다. 가격 오류는 합계 오류 안내와 명령 차단으로 연결한다.
- `runtime/ui-tests/stage3-*.png`는 자체 컨트롤 렌더다. 10,000개 카드 반복 스크롤 후 선택 보존도 검사한다. 모니터별 DPI·실제 키보드/팝업/스크린리더를 검증한 것으로 표현하지 않는다.

## 2차 구현 / 승인된 검증 범위 (2026-10-28)

- MainView를 최상위 창 없이 생성해 보관함·검토·배치 대기/결과·판매 취소 검토/결과를 자체 비트맵으로 렌더한다. 실제 데스크톱 픽셀을 읽는 캡처가 아니다.
- 1100×700, 1280×800, 1380×820, 1600×900 DIP 범위와 96/120/144/192 DPI 비트맵 출력 밀도를 확인한다. 물리 모니터 DPI 전환, OS 배율/해상도 변경은 하지 않는다.
- Button CommonStates/FocusStates의 Normal/MouseOver/Pressed/Focused/Disabled를 VSM으로 직접 전환하여 시각 차이를 확인한다. 실제 마우스/키보드 focus 이벤트를 발생시킨 검증은 아니다. Check/Radio/Toggle의 선택 상태·Radio 원형 표시, 검색 placeholder·가격 오류·비활성 명령·Tooltip 템플릿을 검사한다.
- ScrollBar의 작은 thumb 최소 크기, NumericStepper의 범위 coercion/상한 overflow/템플릿 재적용 이벤트·managed RangeValue peer를 보완한다. 실제 휠/스크린리더의 동작 인증은 아니다.
- 카드와 목록 모두 가상화한다. 카드 높이는 고정이며 매우 긴 이름·사용자 폰트 확대에 대한 전체 접근성 검증은 남는다. 팝업은 열지 않으므로 ComboBox/ContextMenu의 실제 열림/닫힘·화면 경계 보정은 NOT VERIFIED.
- 판매목록의 개별 체크→취소 검토→한 번의 Mock 취소→개별 결과를 연결한다. 배치 게시 결과도 판매목록에 반영한다. 가격 변경/실제 취소는 계속 차단한다.
- 단계별 결과 이미지는 `runtime/ui-tests/`에 생성하며 Git에 넣지 않는다. 실제 검사 기록은 QA-CHECKLIST를 기준으로 한다.

> 2026-10-28: 아래는 목표 명세와 최초 구현의 공통 기준이다. GUI를 실행하지 않았으므로 렌더링/입력/접근성 완료 판정이 아니다.

## 최초 구현 범위
- 좌측 게임·중앙 보관함/판매 중/배치 결과/기록·안전 탭·우측 계획·인앱 검토 레이어. 기본 OS 창 프레임은 명세의 예외로 유지한다. ShowActivated=false, 외부 창 활성화 호출 없음.
- Tokens/Colors.xaml 및 Controls/Controls.xaml: Button/RepeatButton/Toggle/Check/Radio/TextBox/SearchBox/Combo/Stepper/Tab/List/Scroll/Tooltip/ContextMenu/Dialog/Toast/Skeleton 템플릿. 입력·바인딩·포커스·가상화는 WPF를 유지한다. DataGrid 대신 ListBox 행 템플릿 사용.
- normal/hover/pressed/focus/selected/disabled는 해당 컨트롤 trigger, 입력 error는 커스텀 border, loading은 명령 잠금·정적 skeleton/취소 레이어. Button의 선택적 120ms hover 전환은 축소 모션 기본 ON 및 OS 설정에서 비활성화된다. 전체 컨트롤의 전환·오류·loading 상태 완성은 후속 GUI 감사 대상이다.
- 검토: 가격 구간별 수량, 자산 식별·게임, 시세 시각·원천, 추천/편집 단가, 구매자 지불·Demo 수수료·실수령. 편집 오류는 최종 명령 차단. 시세 만료·잔량 부족은 제출 차단.
- 검토 레이어는 Tab 순환, Esc/외부 영역 클릭 닫기, 활성 앱 내부 포커스 복구를 코드로 연결했다. 실제 키보드 동작은 미검증.
- 카드 WrapPanel은 소량 Mock용. 목록은 recycling virtualization. 대량 카드 가상화/다중 범위 선택/드래그·상세 취소 검토는 미구현이다.
- 실제 스크린샷/렌더링 QA, 100/125/150/200% DPI, 긴 이름/작은 창/스크린리더/대비/팝업 동작은 NOT VERIFIED. 자동 테스트를 GUI 실검증으로 확대 해석하지 않는다.

## 시각 콘셉트
- **자체 브랜드**: 세련된 PC 게임 인벤토리 + 트레이딩 워크스페이스. Steam 및 타 제품 화면/아이콘/브랜드를 그대로 복제하지 않음.
- 다크 베이스, 충분한 계층/간격, 숫자 정렬(Tabular numerals), 일관된 라운딩/그림자/구분선. 고가 아이템/경고 상태는 의미 있는 색으로만 강조.
- WPF 자체 플랫폼 컨트롤은 **보이지 않는 기능 엔진**으로 사용 가능. 사용자에게 표시되는 기본 Windows/WPF 스타일은 노출 금지.
- `Themes/Tokens.xaml`, `Themes/Controls/*.xaml` 등 디자인 토큰과 재사용 컴포넌트를 중앙 관리. 독립형 CustomControl은 기능이 반복되거나 Template 계약이 필요할 때만.

## 레이아웃
- 좌측 게임 사이드바/전체 목록, 상단 검색·연결 상태·새로고침/정렬, 중앙 게임별 아이템 그리드/리스트, 우측 선택 요약·전략·예상 금액.
- 별도 `현재 판매 중`, `진행 및 승인 대기`, `거래 기록`, `설정·안전` 화면.
- 검토 창: 고정 합계 헤더 + 스크롤 가능한 항목 테이블(게임/아이템/개별 asset/수량/추천가/편집가/수수료/실수령액/데이터 시각) + 위험·미확정 경고 + `전체 판매등록`.
- 일괄 결과 창: 진행률과 품목별 상태, 부분 성공/실패 및 안전한 재조회. Steam Guard는 수동 승인 안내.

## 필수 컨트롤 (공통 기본 스타일 사용하지 않음)
- Button: Primary/Secondary/Ghost/Danger, hover/pressed/focus-visible/disabled/busy, 클릭 중 중복 실행 차단.
- CheckBox/Radio/Toggle: 커스텀 표시, 체크/해제/부분선택(indeterminate), keyboard Space.
- TextBox/SearchBox: 편집·선택·placeholder·clear·validation·focus, IME/Korean 입력 대응.
- ComboBox/DropDown: 열기/닫기, keyboard navigation, Esc/외부 클릭, 현재 선택 강조, 드롭다운 최대 높이.
- NumberStepper: 증감·직접 입력·최소/최대/빈 값/휠·키 입력 처리, 아이템 수량 초과 방지.
- Tabs/NavMenu: selected/hover/focus/disabled, 현재 위치 표시, keyboard navigation.
- List/ItemCard/DataGrid: 가상화, 밀도 옵션, 선택 범위/다중 선택, 정렬, 빈 데이터/오류/로딩 상태, 행 Hover, 오른쪽 클릭 메뉴.
- ScrollBar/ScrollViewer: 커스텀 외형, 휠·트랙패드·키보드 입력, 눈에 보이는 드래그 핸들.
- Tooltip/Popover/ContextMenu: 지연 표시, 화면 경계 보정, 닫기/포커스 복구.
- Dialog/Toast/Progress/LoadingSkeleton: 진입/종료, 레이어 순서, 접근 가능한 이름, 실패·성공 메시지, 진짜 진행률 vs 불명 표시.
- Custom Window Chrome(선택): 구현 시 이동·리사이즈·스냅·최대화·최소화·DPI·접근성 유지. 기능 손상 시 기본 OS 프레임 사용 가능(창 프레임은 예외).

## 상태 계약
`Normal`, `PointerOver`, `Pressed`, `Selected`, `Focused/FocusVisible`, `Disabled`, `Loading`, `Invalid`, `Error`, `Empty`, `Pending`을 적용 가능 요소에 정의. **모든 인터랙션이 시각 상태와 로직 상태를 함께 갱신**해야 함. 호버 색만 바꿔놓은 가짜 컨트롤 금지.

## 미세 인터랙션
- 짧은 120~180ms 전환 기본값(조절 가능), 과도한 블러/루프 효과/프레임당 폴링 금지.
- 가격·수량 변동 시 미묘한 강조/숫자 전환, 구매주문 가격 단계 선택 시 해당 항목 하이라이트.
- 원치 않는 전체 창 활성화·전경 포커스 획득·전체 화면 캡처 절대 금지.
- 감소된 모션(시스템 설정/사용자 설정) 지원. 키보드 탭/Enter/Space/Esc, 스크린리더의 AutomationProperties/명도 대비 필수.

## 반응형/성능
- 1280×720~4K, 창 축소·최소 크기, 100/125/150/200% DPI, 긴 한글/영문 아이템명, 원화 숫자 정렬, 수백~수천 품목 가상화.
- GPU·CPU 부담 최소. 메이플 동시 실행 환경을 고려해 불필요한 애니메이션 자동재생/대량 이미지 동시 로딩 방지.

## UI QA 완료 기준
- 각 기본 컨트롤이 기본 OS 외형으로 새어 나오지 않는지 확인.
- 마우스 hover/pressed/focus/keyboard/disabled/selection, 빠른 더블 클릭, Esc/외부 클릭, 휠/스크롤/팝업·모달, 리사이즈/DPI 모두 확인.
- 자동 스크린샷은 MarketCrate **자체 창에 한정**하고 사용자 허용 없는 GUI 실행/캡처는 금지.
