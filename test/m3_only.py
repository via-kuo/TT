import requests
import time

# TTS 容器在 Docker 內網的網址
TTS_URL = "http://tts:9880"
OUTPUT_FILE = "test/m3_test_voice.wav"

def test_voice_only():
    print("📢 [M3 獨立測試] 正在繞過大腦，直接測試嘴巴...")
    
    # 測試用的參數 - 讓我們先用「最安全」的設定 (暫時不傳參考音檔)
    params = {
        "text": "這是一次獨立測試，測試台灣口音是否正常。",
        "text_language": "zh",
        # "refer_wav_path": "/app/test/sample.wav", # 先註解掉，看空裝能不能跑通
        "prompt_language": "zh"
    }

    try:
        start = time.time()
        print(f"📡 正在請求 TTS 服務: {TTS_URL}...")
        
        # 設定較長的 timeout，看看它是真的掛掉還是只是慢
        response = requests.get(TTS_URL, params=params, timeout=30)
        
        if response.status_code == 200:
            with open(OUTPUT_FILE, "wb") as f:
                f.write(response.content)
            print(f"✅ 測試成功！音檔已存至: {OUTPUT_FILE}")
            print(f"⏱️ 耗時: {time.time() - start:.2f}s")
        else:
            print(f"❌ 服務有反應但出錯，代碼: {response.status_code}")
            print(f"🔍 錯誤訊息: {response.text}")
            
    except Exception as e:
        print(f"🔥 連線完全失敗！錯誤原因: {e}")

if __name__ == "__main__":
    test_voice_only()
