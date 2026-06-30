# brain.py
import os
from dotenv import load_dotenv
from langchain_qdrant import QdrantVectorStore

load_dotenv()
from qdrant_client import QdrantClient
from qdrant_client.http import models
from langchain_ollama import ChatOllama, OllamaEmbeddings
from langchain_core.prompts import ChatPromptTemplate, SystemMessagePromptTemplate, HumanMessagePromptTemplate
from base_knowledge import KNOWLEDGE_BASE

class ElderlyAI:
    def __init__(self, model_name=None):
        self.model_name = model_name or os.getenv("OLLAMA_MODEL", "llama3:8b-instruct-q4_K_M")
        self.llm = ChatOllama(model=self.model_name, temperature=0.3)
        self.embeddings = OllamaEmbeddings(model=os.getenv("EMBEDDING_MODEL", "bge-m3"))
        self.turn_count = 0
        
        self.time_info = {
            1: {"name": "清晨", "sense": "微涼的空氣、遠處傳來的雞鳴聲"},
            2: {"name": "中午", "sense": "樹蔭下的蟬鳴、熱騰騰的飯菜香"},
            3: {"name": "下午", "sense": "昏黃的夕陽、收工時的汗水與充實"},
            4: {"name": "晚上", "sense": "靜謐的月光、家人的低聲細語"}
        }

        qdrant_path = os.getenv("QDRANT_PATH", "./qdrant_db")
        qdrant_collection = os.getenv("QDRANT_COLLECTION", "elderly_memories")
        if os.path.exists(qdrant_path):
            client = QdrantClient(path=qdrant_path)
            self.db = QdrantVectorStore(client=client, collection_name=qdrant_collection, embedding=self.embeddings)
        else:
            self.db = None

    def generate_response(self, elder_id, topic):
        self.turn_count = 1
        return self._execute_rag(elder_id, topic, "", "開始新的一天")

    def continue_story(self, elder_id, last_output, user_input):
        self.turn_count += 1
        return self._execute_rag(elder_id, None, last_output, user_input)

    def _execute_rag(self, elder_id, topic, last_output, user_input):
        if not self.db: return "資料庫未就緒"

        # 檢索個人記憶
        query = user_input if user_input else (topic if topic else "當年回憶")
        docs = self.db.similarity_search(
            query, k=3,
            filter=models.Filter(must=[models.FieldCondition(key="metadata.elder_id", match=models.MatchValue(value=elder_id))])
        )
        personal_context = "\n".join([d.page_content for d in docs])

        t_info = self.time_info.get(self.turn_count, self.time_info[4])
        is_final = (self.turn_count >= 4)

        # 1. 系統指令：嚴格鎖定語言
        system_template = """你是一位專業的台灣懷舊治療師。
        【絕對命令】：
        - 你必須全程使用「繁體中文」回覆。
        - 嚴禁輸出任何英文字母。
        - 視角固定為「第三人稱」。
        
        【範例格式】：
        ### 當年情境描述 ###
        那是個清晨，阿明挑著擔子走在碎石路上...
        ### 治療師的小問題 ###
        您當時在那條路上，最喜歡聽什麼聲音呢？"""

        # 2. 人類指令：注入數據
        human_template = """
        現在時間是：{time_name}，感官為：{time_sense}。
        【主角回憶】：{personal_context}
        【使用者輸入】：{user_input}
        {history_block}

        請依照範例格式，用繁體中文描述主角的故事：
        ### 當年情境描述 ###
        """

        history_block = f"\n【前情提要】：{last_output}" if last_output else ""
        
        prompt = ChatPromptTemplate.from_messages([
            SystemMessagePromptTemplate.from_template(system_template),
            HumanMessagePromptTemplate.from_template(human_template)
        ])

        chain = prompt | self.llm
        
        # 這裡強制在結尾處引導生成
        response = chain.invoke({
            "time_name": t_info['name'], 
            "time_sense": t_info['sense'],
            "personal_context": personal_context, 
            "user_input": user_input,
            "history_block": history_block
        }).content

        # 如果回應中帶有格式標題以外的英文，我們在這裡進行簡單清洗（可選）
        return response
