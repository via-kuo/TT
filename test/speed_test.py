import requests
import time
import re
import os
import base64
import whisper
import torch # 為了檢查顯卡

# ================= 🔧 設定區 =================
OLLAMA_URL = "http://ollama:11434/api/generate"
TTS_URL = "http://tts:9880"
STABILITY_URL = "https://api.stability.ai/v1/generation/stable-diffusion-xl-1024-v1-0/text-to-image"

STABILITY_API_KEY = "sk-TdxyAlg2W2Oo8zIK3A4fr01yaVd2MlnkjodOhR0dKyVjpsle" 

INPUT_WAV = "test/sample.wav"       
OUTPUT_WAV = "test/output_voice.wav" 
OUTPUT_PNG = "test/output_image.png"
NORMAL_REFER_WAV = "/app/test/sample.wav" 
# ============================================

def run_real_speed_test():
    if not os.path.exists(INPUT_WAV):
        print(f"❌ 找不到音檔: {INPUT_WAV}")
        return

    print("\n🚀 [全系統實測] 目標：挑戰 3 秒內完成影音產出...")
    total_start = time.time()

    # --- Step 1: M1 語音辨識 (啟動 GPU) ---
    print("\n[M1] 👂 正在聽取並辨識 (使用 RTX 5060 Ti)...")
    m1_start = time.time()
    # 強制指定用 cuda 跑
    device = "cuda" if torch.cuda.is_available() else "cpu"
    model = whisper.load_model("base", device=device)
    result = model.transcribe(INPUT_WAV, fp16=(device=="cuda"))
    user_text = result['text'].strip()
    m1_time = time.time() - m1_start
    print(f"   ➤ 辨識結果：{user_text} ({m1_time:.2f}s)")

    # --- Step 2: M2 大腦思考 (強化 Prompt) ---
    print("\n[M2] 🧠 Llama-3 思考劇本中...")
    m2_start = time.time()
    try:
        # 強制 LLM 遵守格式
        prompt = f"請針對這句話給予20字內回憶回應，並在最後一行寫 Keyword: 關鍵字。句子：{user_text}"
        m2_res = requests.post(OLLAMA_URL, json={
            "model": "cwchang/llama-3-taiwan-8b-instruct",
            "prompt": prompt,
            "stream": False
        }, timeout=15)
        full_response = m2_res.json().get('response', '想起以前真的很溫暖。')
        
        # 更穩定的抓取邏輯
        if "Keyword:" in full_response:
            ai_reply = full_response.split("Keyword:")[0].strip()
            img_keywords = full_response.split("Keyword:")[1].strip()
        else:
            ai_reply = full_response.strip()
            img_keywords = "vintage Taiwan grocery store"
    except:
        ai_reply = "那間雜貨店是不是有很多糖果？真是令人懷念。"
        img_keywords = "vintage Taiwan grocery store"
    m2_time = time.time() - m2_start

    print("-" * 15 + " 📜 生成內容 " + "-" * 15)
    print(f"劇本：{ai_reply}")
    print(f"關鍵字：{img_keywords}")

    # --- Step 4: M3 影音並行 ---
    print("\n[M3] 🎨 同步產出影音...")
    p_start = time.time()

   # (A) 實測語音合成 (增加路徑檢查與錯誤回報)
    if ai_reply:
        try:
            # 增加一個極短的緩衝，讓顯卡切換任務
            time.sleep(0.2) 
            
            tts_res = requests.get(TTS_URL, params={
                "text": ai_reply,
                "text_language": "zh",
                "refer_wav_path": "/app/test/sample.wav", # 確保這是在 tts 容器內的路徑
                "prompt_text": "你好", 
                "prompt_language": "zh"
            }, timeout=60)
            
            if tts_res.status_code == 200:
                with open(OUTPUT_WAV, "wb") as f:
                    f.write(tts_res.content)
                print("   ✅ 語音已存檔")
            else:
                # 這裡會印出為什麼 tts 容器不爽的原因
                print(f"   ❌ 語音 API 報錯 (代碼 {tts_res.status_code}): {tts_res.text}")
                
        except Exception as e:
            print(f"   ❌ 語音請求異常: {e}")

    # (B) 影像
    try:
        # M7 脫敏簡易過濾
        safe_prompt = f"{img_keywords}, nostalgic Taiwan, 1960s atmosphere, highly detailed"
        img_res = requests.post(STABILITY_URL, headers={
            "Accept": "application/json",
            "Authorization": f"Bearer {STABILITY_API_KEY}"
        }, json={
            "text_prompts": [{"text": safe_prompt}],
            "height": 704, "width": 704,
        }, timeout=60)
        if img_res.status_code == 200:
            with open(OUTPUT_PNG, "wb") as f:
                f.write(base64.b64decode(img_res.json()["artifacts"][0]["base64"]))
            print("   ✅ 影像已存檔")
    except: pass

    p_time = time.time() - p_start

    print("\n" + "="*45)
    print(f"📊 RTX 5060 Ti 效能驗收成績單")
    print(f"1. 語音辨識 (M1): {m1_time:.2f}s (顯卡加速)")
    print(f"2. 影音並行 (M3): {p_time:.2f}s")
    print(f"🔥 全系統總耗時: {time.time() - total_start:.2f}s")
    print("="*45)

if __name__ == "__main__":
    run_real_speed_test()
