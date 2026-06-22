from fastapi import APIRouter, HTTPException, Request
from pydantic import BaseModel

router = APIRouter(prefix="/session", tags=["session"])


# ════════════ 評估分數計算輔助 ════════════════════════════════════════

def _score_attention(looking_away_rate: float, eye_closed_rate: float) -> int:
    """注意力：視線離開比率越低分數越高。"""
    raw = 1.0 - looking_away_rate - eye_closed_rate * 0.5
    if raw < 0.25: return 1
    if raw < 0.50: return 2
    if raw < 0.75: return 3
    return 4


def _score_engagement(looking_away_rate: float, mouth_moved_rate: float, high_sway_rate: float) -> int:
    """參與度：干擾行為(晃動) > 不注意 > 主動說話 的優先判斷順序。"""
    if high_sway_rate > 0.50:                              return 1  # 干擾
    if looking_away_rate > 0.50:                           return 2  # 被動
    if mouth_moved_rate > 0.30 and looking_away_rate < 0.30: return 4  # 主動
    return 3                                                          # 可配合


def _score_persistence(sad_rate: float, looking_away_rate: float) -> int:
    """持續力：長時間低落/不注意 = 難以維持投入。"""
    if sad_rate > 0.50:                                    return 1
    if sad_rate > 0.30 or looking_away_rate > 0.60:        return 2
    if sad_rate > 0.10:                                    return 3
    return 4


def _score_emotion(emo: dict[str, int]) -> int:
    """情緒狀況：整場出現最多的情緒類別決定分數。"""
    dominant = max(emo, key=emo.get) if any(emo.values()) else "happy"
    return {"sad": 1, "angry": 2, "excited": 3, "happy": 4}[dominant]


def _score_interaction(response_count: int, speech_chars: int) -> int:
    """互動頻率：依平均每次回應的字數判斷主動程度。"""
    if response_count == 0:              return 1
    avg = speech_chars / response_count
    if avg < 10:                         return 2   # 僅指令回覆（嗯/好/有）
    if avg < 25:                         return 3   # 需引導互動
    return 4                                        # 主動互動（完整句子/故事）


class SessionState(BaseModel):
    user_id: str
    session_id: str
    round: int
    scene_elements: list[str]
    covered_w: list[str] = []
    skipped_w: list[str] = []
    last_question_type: str = "step1"
    last_w_asked: str = ""


class RespondRequest(BaseModel):
    elder_response: str
    state: SessionState


@router.post("/start")
async def session_start(request: Request, user_id: str, session_id: str):
    """
    啟動療程第一回合（backward-compat，等同 /session/round?round_number=1）。

    回傳包含 state 欄位，請儲存並傳給後續 /session/respond。
    """
    orchestrator = request.app.state.orchestrator
    try:
        return await orchestrator.start_round(
            user_id=user_id,
            session_id=session_id,
            round_number=1,
        )
    except ValueError as e:
        raise HTTPException(status_code=404, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"療程開場失敗: {str(e)}")


@router.post("/round")
async def session_round(request: Request, user_id: str, session_id: str, round_number: int = 1):
    """
    開始指定回合（n=1,2,3）。

    收到 end_round 後，用 next_round 呼叫此端點繼續下一回合。
    回傳包含 state 欄位，請儲存並傳給後續 /session/respond。
    """
    orchestrator = request.app.state.orchestrator
    try:
        return await orchestrator.start_round(
            user_id=user_id,
            session_id=session_id,
            round_number=round_number,
        )
    except ValueError as e:
        raise HTTPException(status_code=404, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"回合開場失敗: {str(e)}")


@router.get("/{session_id}/metrics", summary="取得即時檢測回饋（供治療師頁面 polling）")
async def session_metrics(request: Request, session_id: str):
    r = request.app.state.redis
    data: dict = await r.hgetall(f"session:{session_id}:metrics")
    return {
        "emotion": data.get("emotion", "適當"),
        "response_time": data.get("response_time", "--"),
    }


class TranscriptPayload(BaseModel):
    text: str


@router.post("/{session_id}/response", summary="記錄 STT 最終辨識結果（供互動頻率統計）")
async def session_transcript(request: Request, session_id: str, body: TranscriptPayload):
    if not body.text.strip():
        return {"ok": True, "skipped": True}
    r = request.app.state.redis
    key = f"session:{session_id}:stats"
    pipe = r.pipeline(transaction=False)
    pipe.hincrby(key, "response_count", 1)
    pipe.hincrby(key, "speech_chars",   len(body.text.strip()))
    await pipe.execute()
    await r.expire(key, 86400)
    return {"ok": True}


@router.get("/{session_id}/assessment", summary="產出療程結束五指標評估分數（1-4 分）")
async def session_assessment(request: Request, session_id: str):
    r = request.app.state.redis
    raw: dict = await r.hgetall(f"session:{session_id}:stats")
    if not raw:
        raise HTTPException(status_code=404, detail="找不到此療程的統計資料，請確認 session_id 正確且療程已進行")

    frame_count      = max(int(raw.get("frame_count",    0)), 1)
    looking_away_n   = int(raw.get("looking_away_n",  0))
    eye_closed_n     = int(raw.get("eye_closed_n",    0))
    mouth_moved_n    = int(raw.get("mouth_moved_n",   0))
    high_sway_n      = int(raw.get("high_sway_n",     0))
    response_count   = int(raw.get("response_count",  0))
    speech_chars     = int(raw.get("speech_chars",    0))
    emo = {
        "happy":   int(raw.get("emo_happy",   0)),
        "excited": int(raw.get("emo_excited", 0)),
        "angry":   int(raw.get("emo_angry",   0)),
        "sad":     int(raw.get("emo_sad",     0)),
    }

    looking_away_rate = looking_away_n / frame_count
    eye_closed_rate   = eye_closed_n   / frame_count
    mouth_moved_rate  = mouth_moved_n  / frame_count
    high_sway_rate    = high_sway_n    / frame_count
    sad_rate          = emo["sad"]     / frame_count

    return {
        "參與度": _score_engagement(looking_away_rate, mouth_moved_rate, high_sway_rate),
        "注意力": _score_attention(looking_away_rate, eye_closed_rate),
        "持續力": _score_persistence(sad_rate, looking_away_rate),
        "情緒狀況": _score_emotion(emo),
        "互動頻率": _score_interaction(response_count, speech_chars),
    }


@router.post("/respond")
async def session_respond(request: Request, body: RespondRequest):
    """
    長者說完話後呼叫此端點，取得下一步動作。

    回傳的 action：
      open_followup    → 話題豐富，繼續順著長者深入（含 scene_text + question）
      ask_supplement_w → 話題結束，切入未問的W維度（含 scene_text + question）
      end_round        → 本回合完成，用 next_round 呼叫 /session/round
      end_session      → 三回合結束，療程收尾

    前端每次收到回應後，用回傳的 state 取代本地的 state。
    """
    orchestrator = request.app.state.orchestrator
    try:
        return await orchestrator.process_response(
            elder_response=body.elder_response,
            state=body.state.model_dump(),
        )
    except ValueError as e:
        raise HTTPException(status_code=404, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"處理回應失敗: {str(e)}")
