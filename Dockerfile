FROM python:3.10-slim

# 設定容器內的工作目錄為 /app
WORKDIR /app

# 1. 因為 requirements.txt 在根目錄，直接複製進來
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# 2. 【核心優化】只複製正式的 app 資料夾內容進來！
# 這樣可以完全繞過 Unity、models、ollama_data 等巨無霸資料夾
COPY app/ .

# 3. 執行正式主程式（因為 app/ 裡的內容被倒進 /app 了，所以直接執行 main.py）
CMD ["python", "main.py"]

