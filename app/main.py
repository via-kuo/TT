from contextlib import asynccontextmanager
from fastapi import FastAPI, UploadFile, File, HTTPException
from pydantic import BaseModel
from config import settings
from services.llm import LLMService
from services.stt import STTService
from services.user_profile_client import MockUserProfileClient
from services.image import StabilityImageService
from services.rag_client import MockRAGClient
from privacy.deidentifier import Deidentifier
from orchestrator import TherapyOrchestrator


llm_service: LLMService | None = None
stt_service: STTService | None = None
user_profile_client: MockUserProfileClient | None = None
deidentifier: Deidentifier | None = None
image_service: StabilityImageService | None = None
rag_client: MockRAGClient | None = None
orchestrator: TherapyOrchestrator | None = None


@asynccontextmanager
async def lifespan(app: FastAPI):
    global llm_service, stt_service, user_profile_client, deidentifier
    global image_service, rag_client, orchestrator
    
    print("🚀 啟動服務...")
    llm_service = LLMService()
    stt_service = STTService()
    user_profile_client = MockUserProfileClient()
    deidentifier = Deidentifier()
    image_service = StabilityImageService()
    rag_client = MockRAGClient()
    
    # ⭐ 把所有 service 注入 orchestrator
    orchestrator = TherapyOrchestrator(
        llm=llm_service,
        image=image_service,
        rag=rag_client,
        user_profile=user_profile_client,
        deidentifier=deidentifier,
    )
    
    yield
    
    print("👋 關閉服務...")
    await llm_service.close()
    await stt_service.close()
    await user_profile_client.close()
    await image_service.close()


app = FastAPI(title="Rememo Backend", version="0.1.0", lifespan=lifespan)


# ════════════ 基礎端點 ════════════

@app.get("/")
async def root():
    return {
        "status": "ok",
        "service": "tku-smart-care backend",
        "message": "Rememo is alive 🌱"
    }


@app.get("/config")
async def show_config():
    return {
        "ollama_host": settings.ollama_host,
        "ollama_model": settings.ollama_model,
        "stt_host": settings.stt_host,
        "tts_host": settings.tts_host,
    }


# ════════════ 個別 service 測試端點 ════════════

@app.get("/test/llm")
async def test_llm(prompt: str = "請用繁體中文回答:你好嗎?"):
    reply = await llm_service.ask(prompt)
    return {"prompt": prompt, "reply": reply}


@app.post("/test/stt")
async def test_stt(file: UploadFile = File(...)):
    audio_bytes = await file.read()
    text = await stt_service.transcribe_bytes(audio_bytes, filename=file.filename)
    return {"filename": file.filename, "transcript": text}


@app.get("/test/user/{user_id}")
async def test_user_profile(user_id: str):
    user = await user_profile_client.get_user(user_id)
    if not user:
        raise HTTPException(status_code=404, detail=f"User {user_id} not found")
    return user


@app.get("/test/deidentify/{user_id}")
async def test_deidentify(user_id: str):
    user = await user_profile_client.get_user(user_id)
    if not user:
        raise HTTPException(status_code=404, detail=f"User {user_id} not found")
    
    example_prompt = (
        f"水彩畫風,描繪{user['name']}的回憶。"
        f"場景:{user['birth_year']} 年{user['birth_place']},"
        f"從事{user['main_occupation']}的場景。"
        f"主題:{user['today_topic']}。"
    )
    
    desensitized_prompt = deidentifier.desensitize_text(example_prompt, taboos=user["taboos"])
    desensitized_profile = deidentifier.desensitize_profile(user)
    
    return {
        "original": {"raw_profile": user, "raw_prompt": example_prompt},
        "desensitized": {
            "safe_profile_for_cloud": desensitized_profile,
            "safe_prompt_for_stability": desensitized_prompt,
        }
    }


@app.post("/test/image")
async def test_image(prompt: str = "watercolor painting of 1940s Taiwan elementary school sports day relay race"):
    path = await image_service.generate(prompt=prompt, session_id="test", round_number=1)
    return {"prompt": prompt, "saved_to": path}


# ════════════ 療程狀態 Schema ════════════

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


# ════════════ 完整療程端點(orchestrator) ════════════

@app.post("/session/start")
async def session_start(user_id: str, session_id: str):
    """
    啟動療程第一回合（backward-compat，等同 /session/round?round_number=1）。

    回傳包含 state 欄位，請儲存並傳給後續 /session/respond。
    """
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


@app.post("/session/round")
async def session_round(user_id: str, session_id: str, round_number: int = 1):
    """
    開始指定回合（n=1,2,3）。

    收到 end_round 後，用 next_round 呼叫此端點繼續下一回合。
    回傳包含 state 欄位，請儲存並傳給後續 /session/respond。
    """
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


@app.post("/session/respond")
async def session_respond(body: RespondRequest):
    """
    長者說完話後呼叫此端點，取得下一步動作。

    回傳的 action：
      open_followup    → 話題豐富，繼續順著長者深入（含 scene_text + question）
      ask_supplement_w → 話題結束，切入未問的W維度（含 scene_text + question）
      end_round        → 本回合完成，用 next_round 呼叫 /session/round
      end_session      → 三回合結束，療程收尾

    前端每次收到回應後，用回傳的 state 取代本地的 state。
    """
    try:
        return await orchestrator.process_response(
            elder_response=body.elder_response,
            state=body.state.model_dump(),
        )
    except ValueError as e:
        raise HTTPException(status_code=404, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"處理回應失敗: {str(e)}")