# Data, enemy presentation và motion reference

[Mục lục nguồn](../06_CombatGameplay.md) · [Bản đồ docs](../README.md)

> **Phạm vi:** tài liệu kỹ thuật / workflow được giữ nguyên từ các checkpoint đã ghi.
> “Hiện tại”, thông số và kết quả QA bên dưới áp dụng tại mốc của đoạn gốc; việc tách doc ngày 2026-09-20 không phải lượt xác minh runtime mới.
> Scene/DialogueData đang được tham chiếu và cấu hình runtime được ưu tiên nếu có khác biệt. Cốt truyện tổng hợp: [Story](../Story/README.md).

<!-- BEGIN PRESERVED SOURCE -->
## 6. Data và runtime code

Sample encounter:

```text
Assets/_Audere/Data/Combat/CombatEncounter_Sample.asset
```

Sample dùng `Enemy_Sample` một phase, 12 HP, TIME tối đa 40 giây, Heal `+3 s`, bullet hit
`-3 s`, 5 dice/batch, respawn delay `0.3 s`, dice speed `115–185`. Weight và hiệu ứng
Attack/Shield/Heal lấy duy nhất từ `CombatDiceConstants`.

`Timer Fill` giảm bằng cách thay `RectTransform.anchorMax.x` từ `1 → 0`, neo cố định ở mép trái. Không dùng `Image.fillAmount` làm nguồn hiển thị vì sprite-less Image của UGUI bỏ qua fill và luôn vẽ full quad.

Khi bullet gây damage, `Timer Fill` màu chính giảm ngay để người chơi đọc được lượng TIME còn lại. Phần TIME vừa mất được giữ lại bằng `Timer Damage Fill` màu trắng trong `0.12 s`, sau đó co mượt về mức mới trong `0.34 s`. Cùng lúc camera rung mạnh ở đầu rồi tắt dần trong `0.20 s`. Heal hủy trail damage đang chạy và đồng bộ cả hai fill lên mức TIME mới.

| Script | Trách nhiệm |
| --- | --- |
| `CombatSymbol.cs` / `CombatDiceConstants.cs` | Enum và tuning chung: Attack, Shield, Heal. |
| `CombatEncounterData.cs` | TIME, dice batch pacing, Heart/bullet tuning, enemy definition và optional tutorial data. |
| `CombatTutorialData.cs` | Tutorial ID, enemy một phase riêng, TIME an toàn, opening dice cố định và cue hướng dẫn. |
| `CombatEnemyDefinition.cs` | Stable enemy ID, actor prefab, phase policy và authored phases. |
| `CombatEnemyRuntime.cs` | State/phase/HP/timer/move/cue/mechanic lifecycle theo mỗi `Play()`. |
| `CombatMoveSet.cs` / `CombatMoveDefinition.cs` | Ordered/weighted selection và immutable authored move data. |
| `CompositeCombatMove.cs` | Chạy nhiều execution độc lập song song trong một phase, cancel toàn bộ theo cùng lifecycle. |
| `StunZonePressureMove.cs` / `CombatStunZoneView.cs` | Data pulse Hidden/Telegraph/Blocking/Fade và presentation/collision state của Stun Zone. |
| `CombatController.cs` | Session, TIME, input, dice batch, result và atomic phase hand-off; không chứa logic riêng của boss. |
| `CombatTutorialView.cs` | Text Scene-20-style, spotlight cutout theo target và preview trực tiếp prefab dice; unscaled fade, không nhận input. |
| `CombatBoardView.cs` | Shared Battle Box, actor mount, projectile pool theo prefab, dice/Heart/cursor/feedback. |
| `CombatCatchCursorView.cs` | Chuyển trạng thái viền cursor và phát feedback `X` khi thao tác bị Stun Zone chặn. |
| `CombatDieView.cs` | Chuyển động/bounce, reroll và capture. |

### Scene-authored enemy presentation

Mỗi production scene đặt prefab instance của enemy trực tiếp dưới `Combat Board/Enemy/Enemy Mount`
và bind instance đó vào `CombatBoardView.authoredEnemyActor`. Đây là actor thật được combat dùng,
không phải preview: runtime không clone actor, không copy transform và không chuẩn hóa scale. Vì vậy
sprite override, kích thước, offset, rotation và scale được chỉnh riêng trên scene sẽ được giữ nguyên.
Runtime chỉ bật/tắt instance và gọi lifecycle mechanic; khi cleanup actor scene-authored được shutdown
rồi ẩn, không bị destroy. `CombatEnemyDefinition.ActorPrefab` tiếp tục là nguồn để authoring tool tạo
instance ban đầu và là fallback cho scene/debug cũ chưa migrate, không phải nguồn presentation của
Scene 30/40 khi Play.
| `CombatPlayerView.cs` | Heart visual ở tâm Catch Cursor, hit flash và invulnerability. |
| `CombatBulletView.cs` | Bullet velocity, source prefab, session/phase ownership, collision reset và pooling. |

## 7. Motion reference từ GIF `ry0CXX (1).gif`

GIF có 444 frame, đa số 30 ms/frame. Các điểm đã dùng làm chuẩn:

- Dice giữ khung vuông, không quay; mỗi dice trượt theo vector riêng và bounce ở biên.
- Không có collision dice-với-dice.
- Trong phase tung, shadow chạy trên mặt board còn khung/icon tạo độ cao bằng cung parabol; shadow thu nhỏ nhẹ ở đỉnh cung để tăng cảm giác giả 3D.
- Dice chạm board 2–3 lần với biên độ giảm dần rồi mới active; trong toàn bộ phase này dice không thể catch/reroll.
- Stun Zone là dải nền chấm tím; dice đi xuyên qua mà không đổi tốc độ hay màu.
- Cursor độc lập, không hút dice; chỉ catch khi overlap và cursor không nằm trong Stun Zone.
- Khi cursor đi vào Stun Zone, viền trắng chuyển tím xỉn; thử catch phát sprite `X.aseprite` xoay và nở từ tâm rồi fade, còn chuột phải vẫn reroll bình thường.
- Catch làm dice biến mất ngay; không sinh text `ATK`, `ARM` hoặc `HEAL`.
- Một dice bị bắt không dừng các dice khác.

Attack dùng animation pixel `Assets/_Audere/AssetGame/Vfx/scratch.aseprite`: mỗi hit tạo một instance tại shared root `CombatBoard/Enemy/VFX`, root này được căn trùng tâm và kích thước với `Enemy Mount`. VFX không thuộc prefab actor và không bị thay reference khi đổi enemy; instance chạy đúng một vòng clip bằng unscaled time rồi tự hủy. Enemy rung mạnh trong phần đầu; các nhịp flash trắng được rải suốt clip và nhịp cuối kết thúc cùng scratch. Shield và Heal không tạo text hoặc projectile chữ.

<!-- END PRESERVED SOURCE -->
