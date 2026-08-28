# Dialogue Evidence and Examples

These lines are copied from current project assets for calibration. They are evidence, not mandatory formulas. Do not rewrite them unless the user requests a dialogue edit.

## Good voice examples

### Timor — care expressed as concrete direction

> “Ừ. Không cần nghĩ hết mọi thứ bây giờ đâu. Đi rửa mặt trước nhé.”

Source: `Assets/_Audere/Data/Dialogue/Day1/Home/Dialogue_D1_HOME_MORNING.asset`

Why it is useful: Timor reduces cognitive load, chooses one manageable action, and softens direction without explaining Audere's psychology.

### Audere — sleepy protest without exposition

> “…Tớ đang dậy mà.”

Source: `Assets/_Audere/Data/Dialogue/Day1/Home/Dialogue_D1_HOME_MORNING.asset`

Why it is useful: short, familiar, mildly defensive, and emotionally legible without self-analysis.

### Timor — familiarity through light teasing

> “Đủ muộn để cậu đừng thử ngủ thêm năm phút nữa.”

Source: `Assets/_Audere/Data/Dialogue/Day1/Home/Dialogue_D1_HOME_MORNING.asset`

Why it is useful: shows that Timor knows Audere's likely behavior and can push her gently without sounding threatening.

### Audere — minimization

> “Một chút thôi.”

Source: `Assets/_Audere/Data/Dialogue/Day1/Home/Dialogue_D1_AFTER_BRUSHING.asset`

Why it is useful: Audere deflects Timor's positive observation rather than giving an emotional report.

### Timor — observation followed by the next task

> “Tốt mà. Trông cậu đỡ ngái ngủ hơn rồi đấy.”

> “Vậy mình làm tiếp một việc nữa nhé. Giờ cậu cần ăn sáng.”

Source: `Assets/_Audere/Data/Dialogue/Day1/Home/Dialogue_D1_AFTER_BRUSHING.asset`

Why it is useful: warm acknowledgement transitions directly into structure; `mình` frames direction as shared action.

### Retry tone

> “Không sao. Mình quay lại từ đầu và thử từng bước nhé.”

Source: serialized `UseAllPiecesTutorialGuide` text in `Assets/_Audere/Scenes/20_D1_Home_Morning.unity`

Why it is useful: removes blame and gives a concrete recovery direction.

### Teacher — mature warmth through bounded choice

> “Các em cứ chọn phần mình thích.”

> “Mỗi người một việc vừa sức là được.”

> “Không cần vội đâu.”

Source: `Assets/_Audere/Data/Dialogue/Day1/Classroom/Dialogue_D1_TEACHER_DETAILS.asset`

Why it is useful: the teacher stays authoritative and clear while removing urgency, workload
pressure, and competition. Each bubble carries one complete thought instead of packing the
whole announcement into a paragraph. Her healing quality is behavioral rather than explained aloud.

### Bianca — invitation with an exit

> “Cậu có muốn làm cùng không?”

> “Không tiện cũng không sao.”

Source: `Assets/_Audere/Data/Dialogue/Day1/Classroom/Dialogue_D1_CLASSROOM_BIANCA_INVITATION.asset`
and `Dialogue_D1_CLASSROOM_BIANCA_EXIT.asset`

Why it is useful: Bianca makes a concrete, small invitation and then gives Audere space. She
does not over-explain Audere, pressure her, or cast herself as a rescuer.

### Timor — fear crossing into restricted agency

> “Nhưng tớ sợ lắm.”

> “Mẹ cậu cũng từng tin người khác.”

> “Nhưng tớ vẫn muốn trả lời.”

> “Cậu phải nghe tớ lần này.”

> “Lần này, để tớ tự trả lời.”

> “Tớ không thể để cậu làm vậy.”

Source: `D1_HOME_NIGHT_MESSAGE` and the authored Timor night-pressure bark assets.

Status: **Design Intent**. Timor begins with a genuine admission of fear, then uses a past loss to
make a present invitation feel unsafe. Audere does not become suddenly confident; she gives one
tentative boundary and asks to make this reply herself. Timor's anger appears only after that small
resistance and becomes an explicit demand for obedience. The progression should still read as fear
and attachment curdling into control, not a villain changing personality. The mother-history claim,
combat ontology, and Timor's reliability remain unresolved outside this authored beat.

## Potentially inconsistent examples

### Legacy sample naming

> “Nhật Linh ơi, có con cừu đang ăn cỏ kìa!”

Source: `Assets/_Audere/Data/Dialogue/Samples/Dialogue_Sample.asset`

Concern: the speaker slot is Audere, but the line addresses `Nhật Linh`; current character systems use `Audere`. The sheep scene is a generic MVP sample and is not linked to the real Day 1 event. Do not use it to establish present story canon.

### Stronger-than-intended early control

> “Không được đâu, Audere. Cậu phải làm từng bước một, không được bỏ sót gì cả. Thử lại nhé.”

Source: serialized `UseAllPiecesTutorialGuide` text in `Assets/_Audere/Scenes/20_D1_Home_Morning.unity`

Concern: the hard opening and repeated obligation are stronger than the stated Day 1 intent, although the retry ending is gentle. Treat this as implemented text that may need future review, not as the default Timor voice pattern.

### Bathroom continuity

> “Đi rửa mặt trước nhé.”

compared with:

> “Xong rồi… vị bạc hà làm tớ tỉnh hẳn.”

Sources: the two D1 DialogueData assets.

Concern: the first establishes washing the face while the follow-up strongly signals brushing teeth. This is a continuity ambiguity, not a voice defect.

## Insufficient evidence

### Khoảng Lặng — current placeholder combat pressure

> “Nói gì đi.”

> “Bây giờ trả lời còn kỳ hơn.”

Source: D1 Classroom production combat DialogueData assets marked `PLACEHOLDER`.

Status: **Design Intent**, not a reusable canon voice example. The short repetition currently
serves combat pacing by crowding Audere's unanswered choice. Do not extrapolate vocabulary,
personality, ontology, or perception rules from these lines.

There are currently no reliable project examples for:

- Audere carrying a sustained conversation with classmates or unfamiliar people; her current
  Bianca response is brief and followed by silence.
- Audere making a meaningful independent choice.
- Timor under genuine anger, fear, or loss of control.
- Later-stage Timor restriction or an Audere–Timor confrontation.
- Accepted final combat voice, text-message voice, or monologue/internal narration.

Do not fabricate “representative examples” for these modes until accepted story content exists.
