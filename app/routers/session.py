from fastapi import APIRouter, HTTPException, Request
from pydantic import BaseModel

router = APIRouter(prefix="/session", tags=["session"])


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
