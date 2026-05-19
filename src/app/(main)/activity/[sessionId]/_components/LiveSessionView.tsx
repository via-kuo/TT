"use client";

import { useState } from "react";
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

  const handlePause = () => setSession((s) => ({ ...s, status: "paused" }));
  const handleResume = () => setSession((s) => ({ ...s, status: "running" }));

  return (
    <div className="h-screen overflow-y-auto bg-[#f5e6d3] px-4 md:px-6 lg:px-8 pt-6 lg:pt-10 pb-4 flex flex-col gap-3 lg:gap-4">

      {/* 標題 */}
      <h1 className="text-[23px] md:text-[36px] lg:text-[50px] font-medium text-[#0a0a0a] leading-none">
        {session.caseName}
      </h1>

      {/* 回合追蹤 */}
      <div className="flex items-center gap-4 lg:gap-6">
        <h2 className="text-[18px] md:text-[24px] lg:text-[30px] font-medium text-[#0a0a0a] shrink-0">回合追蹤</h2>
        <div className="flex gap-2 lg:gap-4">
          {Array.from({ length: session.totalRounds }, (_, i) => i + 1).map((round) => (
            <button
              key={round}
              type="button"
              onClick={() => setCurrentRound(round)}
              className={`w-9 h-9 md:w-12 md:h-12 lg:w-14 lg:h-14 rounded-full text-[12px] md:text-[15px] lg:text-[17px] font-normal transition-colors ${
                round === currentRound
                  ? "bg-[#2b7fff] text-white"
                  : "bg-[#d1d5dc] text-[#0a0a0a] hover:bg-[#b8bcc4]"
              }`}
            >
              {round}
            </button>
          ))}
        </div>
      </div>

      {/* 主要內容 */}
      <div className="flex gap-4 lg:gap-6 flex-1">

        {/* 左欄 */}
        <div className="flex-1 flex flex-col gap-4 lg:gap-5">

          {/* 場景 / 回應切換 */}
          <div className="flex flex-col gap-2 lg:gap-3">
            <h2 className="text-[16px] md:text-[22px] lg:text-[28px] font-medium text-[#0a0a0a]">
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
            <div className="bg-white rounded-xl p-4 lg:p-5 h-[120px] md:h-[140px] lg:h-[160px] overflow-y-auto">
              <p className="text-[14px] md:text-[16px] lg:text-[20px] text-black leading-relaxed">
                {view === "scene" ? session.currentScene : MOCK_ELDER_RESPONSE}
              </p>
            </div>
          </div>

          {/* AI 建議 */}
          <div className="bg-[#f9fafb] rounded-xl p-4 lg:p-6 flex flex-col gap-3 lg:gap-4">
            <h3 className="text-[15px] md:text-[18px] lg:text-[20px] font-medium text-[#0a0a0a]">AI 建議追問語（參考用）</h3>
            <p className="text-[13px] md:text-[15px] lg:text-[17px] text-[#0a0a0a]">本回合可引導的方向：</p>
            <div className="flex flex-col gap-2 lg:gap-3">
              {session.aiSuggestions.map((s, i) => (
                <button
                  key={i}
                  type="button"
                  className="bg-white border border-[#e5e7eb] rounded-xl py-3 lg:py-4 px-4 lg:px-5 text-[13px] md:text-[14px] lg:text-[16px] font-medium text-[#0a0a0a] text-left hover:bg-[#f5f5f5] transition-colors"
                >
                  {s}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* 右欄 */}
        <div className="w-[220px] md:w-[290px] lg:w-[380px] flex flex-col gap-4 lg:gap-5 shrink-0 -translate-y-[2.5%]">

          {/* 即時檢測回饋 */}
          <div>
            <h2 className="text-[16px] md:text-[22px] lg:text-[28px] font-medium text-[#0a0a0a] mb-3 lg:mb-4">即時檢測回饋</h2>
            <div className="flex gap-3 lg:gap-4">
              <div className="bg-white rounded-xl p-3 lg:p-5 flex-1 flex flex-col gap-1 items-center justify-center">
                <span
                  className="text-[16px] md:text-[20px] lg:text-[24px] font-medium"
                  style={{ color: EMOTION_COLORS[session.emotionState] }}
                >
                  {session.emotionState}
                </span>
                <span className="text-[11px] md:text-[13px] lg:text-[14px] text-[#888]">情緒狀態</span>
              </div>
              <div className="bg-white rounded-xl p-3 lg:p-5 flex-1 flex flex-col gap-1 items-center justify-center">
                <span className="text-[18px] md:text-[22px] lg:text-[28px] font-medium text-[#0a0a0a]">{session.responseTime}</span>
                <span className="text-[11px] md:text-[13px] lg:text-[14px] text-[#888]">反應時間</span>
              </div>
            </div>
          </div>

          {/* 禁忌話題 */}
          <div className="bg-[#fef2f2] border-[3px] border-[#ffa2a2] rounded-2xl p-4 lg:p-6 flex flex-col gap-2 lg:gap-3">
            <h3 className="text-[14px] md:text-[17px] lg:text-[20px] font-medium text-[#0a0a0a]">禁忌話題提醒</h3>
            <div className="flex gap-3 lg:gap-4 flex-wrap">
              {session.tabooTopics.map((topic, i) => (
                <span key={i} className="text-[15px] md:text-[18px] lg:text-[22px] text-[#0a0a0a]">{topic}</span>
              ))}
            </div>
          </div>

          {/* 療程控制 */}
          <div className="flex flex-col gap-2 lg:gap-3">
            <h3 className="text-[14px] md:text-[17px] lg:text-[20px] font-medium text-[#0a0a0a]">療程控制</h3>
            <div className="grid grid-cols-2 gap-2 lg:gap-3">
              <button
                type="button"
                className="bg-white border border-[#d1d5dc] rounded-xl py-2.5 lg:py-4 text-[12px] md:text-[14px] lg:text-[18px] font-medium text-[#0a0a0a] hover:bg-[#f5f5f5] transition-colors"
              >
                重播語音
              </button>
              <button
                type="button"
                className="bg-white border border-[#d1d5dc] rounded-xl py-2.5 lg:py-4 text-[12px] md:text-[14px] lg:text-[18px] font-medium text-[#0a0a0a] hover:bg-[#f5f5f5] transition-colors"
              >
                跳過此場景
              </button>
              <button
                type="button"
                onClick={handlePause}
                disabled={session.status === "paused"}
                className="bg-white border border-[#d1d5dc] rounded-xl py-2.5 lg:py-4 text-[12px] md:text-[14px] lg:text-[18px] font-medium text-[#0a0a0a] hover:bg-[#f5f5f5] transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
              >
                暫停
              </button>
              <button
                type="button"
                onClick={handleResume}
                disabled={session.status === "running"}
                className="bg-white border border-[#d1d5dc] rounded-xl py-2.5 lg:py-4 text-[12px] md:text-[14px] lg:text-[18px] font-medium text-[#0a0a0a] hover:bg-[#f5f5f5] transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
              >
                繼續
              </button>
            </div>
            <button
              type="button"
              onClick={() => setShowConfirm(true)}
              className="bg-[#fb2c36] text-white rounded-xl py-2.5 lg:py-4 text-[14px] md:text-[16px] lg:text-[18px] font-medium text-center hover:bg-[#e0252e] transition-colors"
            >
              結束療程
            </button>
          </div>
        </div>
      </div>

      {/* 防呆確認視窗 */}
      {showConfirm && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-7 md:p-9 flex flex-col gap-5 w-[300px] md:w-[400px] shadow-xl">
            <div className="flex flex-col gap-2">
              <h2 className="text-[20px] md:text-[22px] font-bold text-[#1a1a1a]">確定要結束療程？</h2>
              <p className="text-[13px] md:text-[15px] text-[#888]">結束後將到結束量表，本次療程紀錄將會儲存。</p>
            </div>
            <div className="flex gap-3">
              <button
                type="button"
                onClick={() => setShowConfirm(false)}
                className="flex-1 border border-[#d0d0d0] text-[#1a1a1a] rounded-xl py-3 text-[14px] md:text-[16px] font-medium hover:bg-[#f5f5f5] transition-colors"
              >
                取消
              </button>
              <button
                type="button"
                onClick={() => router.push(`/activity/${session.sessionId}/end`)}
                className="flex-1 bg-[#fb2c36] text-white rounded-xl py-3 text-[14px] md:text-[16px] font-medium hover:bg-[#e0252e] transition-colors"
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
