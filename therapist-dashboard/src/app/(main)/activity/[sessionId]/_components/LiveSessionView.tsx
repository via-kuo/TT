"use client";


import { useState, Fragment, useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import { mockActiveSession } from "@/lib/mock-data";


const EMOTION_COLORS: Record<string, string> = {
 適當: "#34c759",
 亢奮: "#f0c52c",
 焦躁: "#fb2c36",
 低落: "#888888",
};


const MOCK_ELDER_RESPONSE =
 "我都去找我那個同事阿明，他很會唱歌，我們去廟口那邊坐......";


type View = "scene" | "response";


export function LiveSessionView() {
 const [session, setSession] = useState(mockActiveSession);
 const [currentRound, setCurrentRound] = useState(session.currentRound);
 const [view, setView] = useState<View>("scene");


 const router = useRouter();
 const [showConfirm, setShowConfirm] = useState(false);
 const containerRef = useRef<HTMLDivElement>(null);


 useEffect(() => {
   const isDesktop = window.matchMedia("(pointer: fine)").matches;
   if (isDesktop) {
     document.body.classList.add("overflow-hidden");
     containerRef.current?.classList.add("h-screen", "overflow-hidden");
     return () => {
       document.body.classList.remove("overflow-hidden");
       containerRef.current?.classList.remove("h-screen", "overflow-hidden");
     };
   }
 }, []);


 const handlePause = () => setSession((s) => ({ ...s, status: "paused" }));
 const handleResume = () => setSession((s) => ({ ...s, status: "running" }));


 return (
   <div
     ref={containerRef}
     className="min-h-screen bg-[#f5e6d3] px-[clamp(16px,3vw,56px)] pt-[clamp(12px,3vh,36px)] pb-4 flex flex-col gap-[clamp(8px,1vw,20px)]"
   >

     {/* 標題 + 回合追蹤 */}
     <div className="flex items-center gap-[clamp(12px,2vw,32px)] flex-wrap">
       <h1 className="text-[clamp(23px,3.5vw,52px)] font-medium text-[#0a0a0a] leading-none shrink-0">
         {session.caseName}
       </h1>
       <div className="flex items-center gap-[clamp(8px,1.2vw,16px)]">
         <span className="text-[clamp(13px,1.6vw,22px)] font-medium text-[#0a0a0a] shrink-0">回合追蹤</span>
         <div className="flex items-center">
           {Array.from({ length: session.totalRounds }, (_, i) => i + 1).map((round, idx) => (
             <Fragment key={round}>
               {idx > 0 && (
                 <div className="w-[clamp(16px,2.5vw,40px)] h-[2px] bg-[#c08252]" />
               )}
               <button
                 type="button"
                 onClick={() => setCurrentRound(round)}
                 className={`w-[clamp(32px,3.5vw,56px)] h-[clamp(32px,3.5vw,56px)] rounded-full text-[clamp(11px,1.2vw,16px)] font-medium transition-colors ${
                   round < currentRound
                     ? "bg-[#c08252] text-white"
                     : round === currentRound
                     ? "bg-[#7a4a28] text-white"
                     : "bg-[#e8d5c0] text-[#b8a090]"
                 }`}
               >
                 {round}
               </button>
               {round === currentRound && (
                 <span className="ml-2 mr-1 text-[clamp(11px,1.2vw,16px)] text-[#0a0a0a]">進行中</span>
               )}
             </Fragment>
           ))}
         </div>
       </div>
     </div>


     {/* 主要內容 */}
     <div className="flex flex-col sm:flex-row gap-[clamp(12px,2vw,32px)] flex-1">


       {/* 左欄 */}
       <div className="flex-1 flex flex-col gap-[clamp(4px,0.8vw,14px)]">


         {/* 場景 / 回應切換 */}
         <div className="flex flex-col gap-[clamp(8px,1vw,16px)]">
           <h2 className="text-[clamp(16px,2vw,28px)] font-medium text-[#0a0a0a]">
             <button
               type="button"
               onClick={() => setView("scene")}
               className={view === "scene" ? "text-[#e09540]" : "text-[#888]"}
             >
               長者端目前顯示的場景
             </button>
             <span className="text-[#888]"> / </span>
             <button
               type="button"
               onClick={() => setView("response")}
               className={view === "response" ? "text-[#e09540]" : "text-[#888]"}
             >
               長者的回應
             </button>
           </h2>
           <div className="bg-white rounded-xl p-[clamp(12px,1.5vw,24px)] h-[clamp(90px,13vw,200px)] overflow-y-auto">
             <p className="text-[clamp(14px,1.5vw,20px)] text-black leading-relaxed">
               {view === "scene" ? session.currentScene : MOCK_ELDER_RESPONSE}
             </p>
           </div>
         </div>


         {/* AI 建議 */}
         {/* 外層容器：淺灰背景卡片，內距與間距使用 clamp 隨視窗縮放 */}
         <div className="bg-[#f9fafb] rounded-xl p-[clamp(12px,1.8vw,32px)] flex flex-col gap-[clamp(8px,1vw,20px)]">
           {/* 區塊標題 */}
           <h3 className="text-[clamp(14px,1.6vw,20px)] font-medium text-[#0a0a0a]">AI 建議追問語（參考用）</h3>
           {/* 引導說明文字 */}
           <p className="text-[clamp(13px,1.3vw,17px)] text-[#0a0a0a]">本回合可引導的方向：</p>
           {/* 建議列表：逐項渲染 aiSuggestions */}
           <div className="flex flex-col gap-[clamp(8px,1vw,16px)]">
             {session.aiSuggestions.map((s, i) => (
               // 每個建議顯示為可點擊按鈕，目前無送出行為
               <button
                 key={i}
                 type="button"
                 className="bg-white border border-[#e5e7eb] rounded-xl py-[clamp(8px,1vw,20px)] px-[clamp(12px,1.2vw,24px)] text-[clamp(13px,1.3vw,16px)] font-medium text-[#0a0a0a] text-left hover:bg-[#f5f5f5] transition-colors"
               >
                 {s}
               </button>
             ))}
           </div>
         </div>
       </div>


       {/* 右欄 */}
       <div className="w-full sm:w-[clamp(200px,28vw,460px)] flex flex-col gap-[clamp(10px,1.5vw,24px)] sm:shrink-0">


         {/* 即時檢測回饋 */}
         <div>
           <h2 className="text-[clamp(16px,2vw,28px)] font-medium text-[#0a0a0a] mb-[clamp(8px,1vw,20px)]">即時檢測回饋</h2>
           <div className="flex gap-[clamp(8px,1vw,20px)]">
             <div className="bg-white rounded-xl p-[clamp(10px,1.2vw,24px)] flex-1 flex flex-col gap-1 items-center justify-center">
               <span
                 className="text-[clamp(15px,1.8vw,24px)] font-medium"
                 style={{ color: EMOTION_COLORS[session.emotionState] }}
               >
                 {session.emotionState}
               </span>
               <span className="text-[clamp(11px,1vw,14px)] text-[#888]">情緒狀態</span>
             </div>
             <div className="bg-white rounded-xl p-[clamp(10px,1.2vw,24px)] flex-1 flex flex-col gap-1 items-center justify-center">
               <span className="text-[clamp(17px,2vw,28px)] font-medium text-[#0a0a0a]">{session.responseTime}</span>
               <span className="text-[clamp(11px,1vw,14px)] text-[#888]">反應時間</span>
             </div>
           </div>
         </div>


         {/* 禁忌話題 */}
         <div className="bg-[#fef2f2] border-[3px] border-[#ffa2a2] rounded-2xl p-[clamp(12px,1.8vw,32px)] flex flex-col gap-[clamp(6px,0.8vw,16px)]">
           <h3 className="text-[clamp(13px,1.5vw,20px)] font-medium text-[#0a0a0a]">禁忌話題提醒</h3>
           <div className="flex gap-[clamp(10px,1.5vw,20px)] flex-wrap">
             {session.tabooTopics.map((topic, i) => (
               <span key={i} className="text-[clamp(14px,1.7vw,22px)] text-[#0a0a0a]">{topic}</span>
             ))}
           </div>
         </div>


         {/* 療程控制 */}
         <div className="flex flex-col gap-[clamp(6px,0.8vw,16px)]">
           <h3 className="text-[clamp(13px,1.5vw,20px)] font-medium text-[#0a0a0a]">療程控制</h3>
           <div className="grid grid-cols-2 gap-[clamp(6px,0.8vw,16px)]">
             <button
               type="button"
               className="bg-white border border-[#d1d5dc] rounded-xl py-[clamp(10px,1vw,20px)] text-[clamp(12px,1.3vw,18px)] font-medium text-[#0a0a0a] hover:bg-[#f5f5f5] transition-colors"
             >
               重播語音
             </button>
             <button
               type="button"
               className="bg-white border border-[#d1d5dc] rounded-xl py-[clamp(10px,1vw,20px)] text-[clamp(12px,1.3vw,18px)] font-medium text-[#0a0a0a] hover:bg-[#f5f5f5] transition-colors"
             >
               跳過此場景
             </button>
             <button
               type="button"
               onClick={handlePause}
               disabled={session.status === "paused"}
               className="bg-white border border-[#d1d5dc] rounded-xl py-[clamp(10px,1vw,20px)] text-[clamp(12px,1.3vw,18px)] font-medium text-[#0a0a0a] hover:bg-[#f5f5f5] transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
             >
               暫停
             </button>
             <button
               type="button"
               onClick={handleResume}
               disabled={session.status === "running"}
               className="bg-white border border-[#d1d5dc] rounded-xl py-[clamp(10px,1vw,20px)] text-[clamp(12px,1.3vw,18px)] font-medium text-[#0a0a0a] hover:bg-[#f5f5f5] transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
             >
               繼續
             </button>
           </div>
           <button
             type="button"
             onClick={() => setShowConfirm(true)}
             className="bg-[#fb2c36] text-white rounded-xl py-[clamp(10px,1vw,20px)] text-[clamp(14px,1.4vw,18px)] font-medium text-center hover:bg-[#e0252e] transition-colors"
           >
             結束療程
           </button>
         </div>
       </div>
     </div>


     {/* 防呆確認視窗 */}
     {showConfirm && (
       <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
         <div className="bg-white rounded-2xl p-7 md:p-9 xl:p-10 flex flex-col gap-5 w-[300px] md:w-[400px] xl:w-[480px] shadow-xl">
           <div className="flex flex-col gap-2">
             <h2 className="text-[20px] md:text-[22px] font-bold text-[#1a1a1a]">確定要結束療程？</h2>
             <p className="text-[13px] md:text-[15px] text-[#888]">結束後將到結束量表，本次療程紀錄將會儲存。</p>
           </div>
           <div className="flex gap-3">
             <button
               type="button"
               onClick={() => setShowConfirm(false)}
               className="flex-1 border border-[#d0d0d0] text-[#1a1a1a] rounded-xl py-3 xl:py-4 text-[14px] md:text-[16px] font-medium hover:bg-[#f5f5f5] transition-colors"
             >
               取消
             </button>
             <button
               type="button"
               onClick={() => router.push(`/activity/${session.sessionId}/end`)}
               className="flex-1 bg-[#fb2c36] text-white rounded-xl py-3 xl:py-4 text-[14px] md:text-[16px] font-medium hover:bg-[#e0252e] transition-colors"
             >
               結束療程
             </button>
           </div>
         </div>
       </div>
     )}
   </div>
 );
}


