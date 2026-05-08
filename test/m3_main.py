import requests

print("=== M3 模組：最終 API 連通性測試 ===")

def test_full_integration():
    # 1. 測試大腦 (Ollama)
    ollama_url = "http://ollama:11434/api/tags"
    try:
        res = requests.get(ollama_url)
        print("✅ 大腦 (Ollama) 在線！")
    except:
        print("❌ 大腦連線失敗")

    # 2. 測試嘴巴 (GPT-SoVITS) - 傳送正確的 API 格式
    # 根據 breakstring/gpt-sovits 的標準 API 格式
    tts_url = "http://tts:9880"
    params = {
        "text": "你好，我是你的AI助理，環境已經架設成功了！",
        "text_language": "zh"
    }
    
    try:
        print("正在嘗試讓嘴巴說話 (這會消耗顯存)...")
        # 這裡用 GET 或 POST 帶參數，視 Image 版本而定，通常帶 params 就不會 400 了
        res = requests.get(tts_url, params=params, timeout=30)
        
        if res.status_code == 200:
            print(f"✅ 嘴巴合成成功！收到音訊資料，長度：{len(res.content)} bytes")
        else:
            print(f"⚠️ 嘴巴有回應但格式不對，狀態碼: {res.status_code}")
            print(f"後台訊息: {res.text}")
    except Exception as e:
        print(f"❌ 嘴巴通訊故障: {e}")

test_full_integration()

import time
time.sleep(86400) # 讓它睡一天，不要結束