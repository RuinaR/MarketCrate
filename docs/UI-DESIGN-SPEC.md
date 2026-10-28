# MarketCrate — UI/UX & Custom Controls

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
