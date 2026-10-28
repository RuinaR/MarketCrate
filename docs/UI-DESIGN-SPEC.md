# MarketCrate — UI/UX & Custom Controls

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
