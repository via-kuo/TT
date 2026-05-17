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
    <div className="h-screen overflow-hidden bg-[#f5e6d3] px-8 pt-10 pb-4 flex flex-col gap-4">
      {/* 標題 */}
      <h1 className="text-[50px] font-medium text-[#0a0a0a] leading-none">
        {session.caseName}
      </h1>

      {/* 回合追蹤 */}
      <div className="flex flex-col gap-3">
        <h2 className="text-[30px] font-medium text-[#0a0a0a]">回合追蹤</h2>
        <div className="flex gap-4">
          {Array.from({ length: session.totalRounds }, (_, i) => i + 1).map((round) => (
            <button
              key={round}
              type="button"
              onClick={() => setCurrentRound(round)}
              className={`w-14 h-14 rounded-full text-[17px] font-normal transition-colors ${
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
      <div className="flex gap-6 flex-1">
        {/* 左欄 */}
        <div className="flex-1 flex flex-col gap-5">
          {/* 場景 / 回應切換 */}
          <div className="flex flex-col gap-3">
            <h2 className="text-[28px] font-medium text-[#0a0a0a]">
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
            <div className="bg-white rounded-xl p-5">
              <p className="text-[20px] text-black leading-relaxed">
                {view === "scene" ? session.currentScene : MOCK_ELDER_RESPONSE}
              </p>
            </div>
          </div>

          {/* AI 建議 */}
          <div className="bg-[#f9fafb] rounded-xl p-6 flex flex-col gap-4">
            <h3 className="text-[20px] font-medium text-[#0a0a0a]">AI 建議追問語（參考用）</h3>
            <p className="text-[17px] text-[#0a0a0a]">本回合可引導的方向：</p>
            <div className="flex flex-col gap-3">
              {session.aiSuggestions.map((s, i) => (
                <button
                  key={i}
                  type="button"
                  className="bg-white border border-[#e5e7eb] rounded-xl py-4 px-5 text-[16px] font-medium text-[#0a0a0a] text-left hover:bg-[#f5f5f5] transition-colors"
                >
                  {s}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* 右欄 */}
        <div className="w-[380px] flex flex-col gap-5 shrink-0 -translate-y-[11%]">
          {/* 即時檢測回饋 */}
          <div>
            <h2 className="text-[28px] font-medium text-[#0a0a0a] mb-4">即時檢測回饋</h2>
            <div className="flex gap-4">
              <div className="bg-white rounded-xl p-5 flex-1 flex flex-col gap-1 items-center justify-center">
                <span
                  className="text-[24px] font-medium"
                  style={{ color: EMOTION_COLORS[session.emotionState] }}
                >
                  {session.emotionState}
                </span>
                <span className="text-[14px] text-[#888]">情緒狀態</span>
              </div>
              <div className="bg-white rounded-xl p-5 flex-1 flex flex-col gap-1 items-center justify-center">
                <span className="text-[28px] font-medium text-[#0a0a0a]">{session.responseTime}</span>
                <span className="text-[14px] text-[#888]">反應時間</span>
              </div>
            </div>
          </div>

          {/* 禁忌話題 */}
          <div className="bg-[#fef2f2] border-[3px] border-[#ffa2a2] rounded-2xl p-6 flex flex-col gap-3">
            <h3 className="text-[20px] font-medium text-[#0a0a0a]">禁忌話題提醒</h3>
            <div className="flex gap-4 flex-wrap">
              {session.tabooTopics.map((topic, i) => (
                <span key={i} className="text-[22px] text-[#0a0a0a]">{topic}</span>
              ))}
            </div>
          </div>

          {/* 療程控制 */}
          <div className="flex flex-col gap-3">
            <h3 className="text-[20px] font-medium text-[#0a0a0a]">療程控制</h3>
            <div className="grid grid-cols-2 gap-3">
              <button
                type="button"
                className="bg-white border border-[#d1d5dc] rounded-xl py-4 text-[18px] font-medium text-[#0a0a0a] hover:bg-[#f5f5f5] transition-colors"
              >
                重播語音
              </button>
              <button
                type="button"
                className="bg-white border border-[#d1d5dc] rounded-xl py-4 text-[18px] font-medium text-[#0a0a0a] hover:bg-[#f5f5f5] transition-colors"
              >
                跳過此場景
              </button>
              <button
                type="button"
                onClick={handlePause}
                disabled={session.status === "paused"}
                className="bg-white border border-[#d1d5dc] rounded-xl py-4 text-[18px] font-medium text-[#0a0a0a] hover:bg-[#f5f5f5] transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
              >
                暫停
              </button>
              <button
                type="button"
                onClick={handleResume}
                disabled={session.status === "running"}
                className="bg-white border border-[#d1d5dc] rounded-xl py-4 text-[18px] font-medium text-[#0a0a0a] hover:bg-[#f5f5f5] transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
              >
                繼續
              </button>
            </div>
            <button
              type="button"
              onClick={() => setShowConfirm(true)}
              className="bg-[#fb2c36] text-white rounded-xl py-4 text-[18px] font-medium text-center hover:bg-[#e0252e] transition-colors"
            >
              結束療程
            </button>
          </div>
        </div>
      </div>

      {/* 防呆確認視窗 */}
      {showConfirm && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-9 flex flex-col gap-5 w-[400px] shadow-xl">
            <div className="flex flex-col gap-2">
              <h2 className="text-[22px] font-bold text-[#1a1a1a]">確定要結束療程？</h2>
              <p className="text-[15px] text-[#888] whitespace-nowrap">結束後將到結束量表，本次療程紀錄將會儲存。</p>
            </div>
            <div className="flex gap-3">
              <button
                type="button"
                onClick={() => setShowConfirm(false)}
                className="flex-1 border border-[#d0d0d0] text-[#1a1a1a] rounded-xl py-3 text-[16px] font-medium hover:bg-[#f5f5f5] transition-colors"
              >
                取消
              </button>
              <button
                type="button"
                onClick={() => router.push(`/activity/${session.sessionId}/end`)}
                className="flex-1 bg-[#fb2c36] text-white rounded-xl py-3 text-[16px] font-medium hover:bg-[#e0252e] transition-colors"
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
