# Tutorial D1 và nhịp dẫn vào trận thật

[Mục lục nguồn](../06_CombatGameplay.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
### Tutorial combat D1 Classroom

Tutorial và trận đánh thật là hai runtime tách biệt. `CombatEncounterData` tham chiếu optional
`CombatTutorialData`; D1 bật `UseGuidedLessons` với 11 bài tập có `DialogueData` riêng và điều kiện
hoàn thành bằng thao tác. Controller không kiểm tra boss ID. Nội dung mới là **Design Intent** cho
lần đầu đối diện Khoảng Lặng. Menu `Audere/Combat/Apply D1 Guided Combat Tutorial` cập nhật riêng
asset D1 và các thoại trong `Day1/Classroom/Combat/Guided`, không dựng lại scene.

```text
Tutorial runtime   → enemy tập riêng, 20 HP, 30 TIME; Frame ngoài vuông 440 × 440
Practice field     → 400 × 366; chừa 22 cho TIME và 12 khoảng cách với vùng chơi
Move               → chỉ Heart + điểm sáng; rê chuột tới điểm để qua bước
TIME               → hiện thanh TIME; xem 3 giây trôi qua trừ 3 TIME; TIME là máu
Damage             → giữ Heart tại giữa để xem một viên đạn thật trừ 3 TIME
Heal               → hiện vòng bắt; chuột trái bắt Hồi nhịp, nhận +3 TIME
Attack             → mới hiện enemy và HP; bắt Tấn công, enemy mất 1 HP
Roll               → chuột phải gieo lại; chờ viên đáp; bài tập bảo đảm ra Khiên
Shield             → bắt Khiên vừa gieo, xóa đạn gần Heart
Dodge              → né đạn chậm liên tục 4 giây
Stun Catch         → thử bắt trong vùng nhiễu: dấu X, viên không bị tiêu thụ
Stun Roll          → gieo viên ra khỏi vùng nhiễu rồi bắt ở ngoài
Finish             → chuột trái bắt đầu trận thật; trở lại board nguyên bản, 6 HP / 45 TIME
```

Các giá trị TIME/HP trên là giá trị authoring, vẫn qua difficulty settings chung: Easy hiện có
24 TIME khi học và 36 TIME khi đánh thật do global TIME multiplier 0.80. Hard còn áp dụng multiplier
riêng. Hướng dẫn nói đúng mức trừ/hồi và điều kiện thắng, không gán cứng HP trận thật trên HUD.

Timor nói một hoặc hai câu ngắn trước mỗi bài. Nhiệm vụ trong `GameplayUIRoot/CombatTutorialUI`
hiện ngay khi Timor bắt đầu nói, cùng số bước và viền focus theo target thật. Dialogue giữ
`DialoguePause`; cú click kết thúc thoại không đồng thời bắt/gieo. Panel nằm dưới board/TIME; viewport thấp chuyển panel sang
bên cạnh nếu đủ chỗ để tránh đè lên thanh TIME. Layout cập nhật theo viewport, không
chặn input. Chỉ thao tác đúng mới qua bước; sai nút, ngoài vòng hoặc viên chưa đáp đều nhắc lại.
Khi hoàn thành, HUD khôi phục nguyên văn nhiệm vụ nếu vừa hiện lời nhắc sửa thao tác, gạch ngang
toàn bộ nhiệm vụ và giữ hiển thị `2.5 s` trước bài tiếp theo. Bài mới bỏ trạng thái gạch ngang.

Với `SquareBoardSize = 400`, Frame ngoài giữ tỉ lệ vuông `440 × 440`; vùng chơi `400 × 366`
chừa rãnh TIME bên dưới, gồm thanh cao `22` và khoảng cách `12`. Quy tắc chung trong
`CombatBoardView.SyncTimerToBoard()` đặt toàn bộ track TIME theo chiều rộng bên trong Frame,
dùng khoảng đệm ngang đã author ở cả trái, phải và đáy. Track cập nhật khi board đổi kích thước
hoặc vị trí; move chỉ thu hẹp/dịch vùng chơi không làm track co theo. Runtime này áp dụng cho
mọi combat board, giữ nguyên scale actor; chỉ chiều dài phần fill thay đổi theo TIME còn lại.

TIME chỉ tự giảm trong bài TIME và né đạn, ở tốc độ thật 1 TIME/giây; các bài thao tác cho người mới
thời gian đọc/thử. Hit, Heal, Attack và Shield dùng hiệu ứng combat chung. Bullet dùng prefab thật;
các bài tập dùng viên dice đứng yên và roll có kết quả Khiên cố định, được ghi rõ là ngoại lệ trong
bài tập. Safety floor 1 TIME ngăn thất bại giữa tutorial. Vùng nhiễu chỉ chặn bắt; người chơi có thể
gieo thêm lần nữa nếu viên chưa ra ngoài. Board thay kích thước RectTransform, không đổi scale actor.

Khi hoàn thành, controller tăng session version, shutdown tutorial actor/mechanic/move, return
dice/projectile, khôi phục layout/visibility/constraint rồi khởi tạo `Enemy_KhoangLang` như attempt
mới. Tutorial đã hoàn thành được nhớ trong vòng đời controller: Retry vào thẳng trận thật; hủy khi
chưa học xong sẽ bắt đầu tutorial sạch ở lần sau. Attempt thật dùng TIME `45 s`, một phase `6 HP`.
Moveset `OrderedLoop` chạy Aimed Fan → Side Sweep → Rain liên tục trong cùng thanh máu. Enemy move
cùng dice batch được schedule ngay khi state trở lại `Playing`; không có phase marker trên HUD.

### Production narrative pacing sau tutorial

Production phase dùng cùng `CombatDialogueCue` nhưng tách presentation khỏi trigger:

```text
PhaseEnter                       → AutoCombatDialogue qua DialogueUI chuẩn (combat-local pause)
MoveStarted: Side Sweep          → AutoCombatDialogue qua DialogueUI chuẩn (combat-local pause)
CueCompleted: Side Sweep         → Heart wobble → ModalDialogue (combat-local pause)
HealthAtOrBelow: 2               → BackgroundTextField (không lấy input)
```

`AutoCombatDialogue` đọc `DialogueData` bằng chính `DialogueController`: Audere luôn ở slot trái,
Khoảng Lặng ở slot phải, dùng portrait/bubble/typewriter chuẩn nhưng không claim
`GameplayInputMode.Dialogue`. Thời lượng mỗi câu là `max(1.4 s, characters / 20 + 0.55 s)` với
gap `0.18 s`. `BackgroundTextField` dùng pool `36` TMP label phủ theo grid có jitter, font
`28–46`, alpha `0.10–0.20`; mỗi line có ghost lệch để tạo smear và dao động rotation/scale mềm.
Layer không raycast, nằm sau gameplay và được clear cùng session.

Attack hit tiếp tục dùng `Assets/_Audere/AssetGame/Vfx/scratch.aseprite`. Vì enemy actor hiện nằm
trong World Space Combat Canvas, runtime đổi `100 PPU` của SpriteRenderer sang Canvas pixel scale
và đẩy cả SpriteRenderer/SortingGroup lên trên sorting order của Canvas trước khi phát animation.

Side Sweep riêng của Khoảng Lặng dùng `ConvergingSideCorridorMove`: hai mép phát projectile đối
xứng, bỏ trống hành lang giữa co từ `46%` xuống `24%` chiều cao box nhưng không nhỏ hơn `72 px`.
Projectile đứng ở mép `0.35 s` với collision tắt trước khi bay vào. Scene 20 vẫn dùng move mẫu cũ.

Cue `audere-timor-anchor` được đánh dấu `RequiredBeforeVictory`. Nếu người chơi gây lethal damage
trước khi cue resolve, runtime chỉ clamp lần lethal đó ở `1 HP`; non-lethal damage vẫn giữ nguyên,
không tạo phase phụ và damage dư không được tích sang sau dialogue. Retry tạo runtime mới nên cue,
auto-dialogue, text field và gate đều reset.

Heart visual được tách riêng để thay art mà không đụng vào movement/collision:

```text
Assets/_Audere/Prefabs/Combat/Player/HeartVisual.prefab
```

Prefab hiện chỉ có `RectTransform + Image` và sprite vuông placeholder. Khi có art Heart chính thức, chỉ cần thay `Source Image` trên prefab này.

Bullet prefab:

```text
Assets/_Audere/Prefabs/Combat/Bullets/EnemyBullet.prefab
```

<!-- END PRESERVED SOURCE -->
