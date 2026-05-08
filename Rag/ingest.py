# ingest.py
# ingest.py
import os
import re
import importlib.resources
from symspellpy import SymSpell
from langchain_qdrant import QdrantVectorStore
from qdrant_client import QdrantClient
from langchain_ollama import OllamaEmbeddings
from langchain_text_splitters import RecursiveCharacterTextSplitter

class AdvancedCleaner:
    def __init__(self):
        self.sym_spell = SymSpell(max_dictionary_edit_distance=2, prefix_length=7)
        try:
            with importlib.resources.path("symspellpy", "frequency_dictionary_en_82_765.txt") as dict_path:
                self.sym_spell.load_dictionary(str(dict_path), term_index=0, count_index=1)
        except:
            pass

    def advanced_clean_text(self, text):
        """深度清洗：移除贅詞、正規化標點與雜訊"""
        redundant = ["那個", "然後", "喔", "啦", "欸", "呢", "的一聲"]
        for word in redundant:
            text = text.replace(word, "")
        text = re.sub(r'<[^>]+>|https?://\S+', '', text)
        punc = {"！": "!", "？": "?", "，": ",", "。": ".", "：": ":", "；": ";"}
        for old, new in punc.items():
            text = text.replace(old, new)
        text = re.sub(r'(.)\1{2,}', r'\1', text)
        return re.sub(r'\s+', ' ', text).strip()

def process_and_save(file_path, elder_id):
    if not os.path.exists(file_path): return
    
    cleaner = AdvancedCleaner()
    with open(file_path, "r", encoding="utf-8") as f:
        raw_text = f.read()

    cleaned_text = cleaner.advanced_clean_text(raw_text)
    
    # 輸出清洗成果
    os.makedirs("data/cleaned_output", exist_ok=True)
    with open(f"data/cleaned_output/cleaned_{os.path.basename(file_path)}", "w", encoding="utf-8") as f:
        f.write(cleaned_text)

    text_splitter = RecursiveCharacterTextSplitter(chunk_size=250, chunk_overlap=40)
    chunks = text_splitter.split_text(cleaned_text)
    
    embeddings = OllamaEmbeddings(model="bge-m3")
    
    # 存入 Qdrant 向量資料庫
    QdrantVectorStore.from_texts(
        texts=chunks,
        embedding=embeddings,
        metadatas=[{"elder_id": elder_id}] * len(chunks),
        path="./qdrant_db",
        collection_name="elderly_memories",
    )
    print(f"✅ Qdrant 向量資料庫已建立。")

if __name__ == "__main__":
    process_and_save("data/grandpa_wang.txt", "elder_001")