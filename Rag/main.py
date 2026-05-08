# main.py
from brain import ElderlyAI

def main():
    ai = ElderlyAI()
    elder_id = "elder_001"
    
    print("懷舊之旅啟動中... AI 正在讀取資料並回到當年清晨。")
    current_response = ai.generate_response(elder_id, "年輕時辛苦工作的生活")
    
    while True:
        print("\n" + "="*60)
        print(current_response)
        print("="*60)
        
        if ai.turn_count >= 4:
            print("\n🌙 故事已到深夜，謝謝您分享這些珍貴的回憶。")
            break
            
        user_input = input("\n您可以告訴主角該怎麼做，或隨意聊聊您的回憶 (輸入 'exit' 離開)：").strip()
        
        if user_input.lower() == 'exit':
            break
        
        if not user_input:
            print("請輸入一點文字，讓故事繼續發展下去吧！")
            continue

        print("\nAI 正在傾聽並續寫當年的故事...")
        current_response = ai.continue_story(elder_id, current_response, user_input)

if __name__ == "__main__":
    main()