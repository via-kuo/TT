"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { mockActiveSession } from "@/lib/mock-data";

// 觀察量表的每一列：名稱 + 四個等級選項（1分→4分）
const CRITERIA = [
  { name: "參與度",  options: ["干擾", "被動需提醒", "鼓勵下可配合", "主動參與"] },
  { name: "注意力",  options: ["表現差", "需不斷提醒", "經提醒可配合", "表現佳"] },
  { name: "持續力",  options: ["擅自離開", "需不斷提醒", "經提醒可配合", "表現佳"] },
  { name: "情緒狀況", options: ["低落", "焦躁", "抗奮", "適當"] },
  { name: "互動頻率", options: ["無互動", "僅指令回應", "需引導互動", "主動互動"] },
];

// 預設選擇（index 0~3 代表 1~4 分），總分 18/20
const DEFAULT_SCORES = [2, 3, 2, 3, 3];

export default function SessionEndPage() {
  const router = useRouter();
  const session = mockActiveSession;

  // 每列目前選中的分數 index（0=1分, 1=2分, 2=3分, 3=4分）
  const [scores, setScores] = useState<number[]>(DEFAULT_SCORES);
  const [notes, setNotes] = useState(""); // 治療師觀察備註
  const [isEditing, setIsEditing] = useState(false); // 是否可編輯

  // 計算總分（index+1 為分數）
  const total = scores.reduce((sum, s) => sum + (s + 1), 0);
  const maxScore = CRITERIA.length * 4;

  function handleSelect(rowIdx: number, colIdx: number) {
    if (!isEditing) return;
    setScores((prev) => prev.map((s, i) => (i === rowIdx ? colIdx : s)));
  }
  useEffect(() => {
    document.body.classList.add("overflow-hidden");
    return () => document.body.classList.remove("overflow-hidden");
  }, []);

  return (
    <div className="h-[100lvh] overflow-hidden bg-[#f5e6d3] px-[10%] pt-11 lg:pt-[77px] pb-6 flex flex-col gap-2">

      {/* 頁首：標題 + 編輯按鈕 */}
      <div className="flex items-start justify-between">
        <div className="flex flex-col gap-1">
          <h1 className="text-[32px] font-bold text-[#1a1a1a] ml-2">療程結束 填寫觀察量表</h1>
          <p className="text-[15px] text-[#888] ml-2">
            {session.caseName} 第 {6} 次療程 2024.12.17
          </p>
        </div>
        <button
          type="button"
          onClick={() => setIsEditing((v) => !v)}
          className="bg-white border border-[#d0d0d0] rounded-xl px-6 py-2.5 text-[15px] font-medium text-[#1a1a1a] hover:bg-[#f5f5f5] transition-colors mt-[1.5%]"
        >
          {isEditing ? "檢視" : "編輯"}
        </button>
      </div>

      {/* 觀察量表格 */}
      <div className="bg-white rounded-2xl overflow-hidden border border-[#e8e8e8]">
        <table className="w-full border-collapse">
          <thead>
            <tr>
              {/* 左上角空格 */}
              <th className="w-[160px] border-b border-r border-[#e8e8e8]" />
              {/* 分數欄標題 */}
              {[1, 2, 3, 4].map((score) => (
                <th key={score} className="border-b border-r border-[#e8e8e8] last:border-r-0 py-4 lg:py-7 xl:py-4">
                  <span className="bg-[#b8d0f0] text-[#1a1a1a] text-[16px] font-semibold rounded-xl px-6 py-2">
                    {score}分
                  </span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {CRITERIA.map((row, rowIdx) => (
              <tr key={row.name} className="border-b border-[#e8e8e8] last:border-b-0">
                {/* 列名 */}
                <td className="border-r border-[#e8e8e8] px-6 py-[17px] lg:py-[9px] xl:py-[17px] text-[15px] font-medium text-[#1a1a1a]">
                  {row.name}
                </td>
                {/* 各分數選項 */}
                {row.options.map((option, colIdx) => {
                  const selected = scores[rowIdx] === colIdx;
                  return (
                    <td
                      key={colIdx}
                      onClick={() => handleSelect(rowIdx, colIdx)}
                      className={`border-r border-[#e8e8e8] last:border-r-0 px-4 py-[17px] lg:py-[9px] xl:py-[17px] text-[14px] text-center transition-colors ${
                        selected
                          ? "bg-[#e6f4ee] text-[#1a1a1a] font-medium"
                          : isEditing
                          ? "text-[#555] cursor-pointer hover:bg-[#f5f5f5]"
                          : "text-[#555]"
                      }`}
                    >
                      {option}{selected ? " ✓" : ""}
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* 今日總分 */}
      <p className="text-[22px] font-semibold text-[#1a1a1a]">
        今日總分 <span className="text-[36px] font-bold">{total}</span>
        <span className="text-[#888] text-[20px]"> /{maxScore}</span>
      </p>

      {/* 治療師觀察備註 */}
      <div className="flex flex-col gap-2">
        <h2 className="text-[18px] font-semibold text-[#1a1a1a]">治療師觀察備註</h2>
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="今天對廟口場景反應特別好，主動提到阿明......"
          className="w-full h-[140px] bg-white border border-[#e0e0e0] rounded-xl px-5 py-4 text-[15px] text-[#1a1a1a] placeholder:text-[#bbb] outline-none resize-none focus:border-[#5b8ac5] transition-colors"
        />
      </div>

      {/* 底部按鈕 */}
      <div className="flex gap-3">
        <button
          type="button"
          onClick={() => router.push("/dashboard")}
          className="bg-[#2b7fff] text-white rounded-xl px-7 py-[14px] text-[14.5px] font-medium hover:bg-[#1a6eee] transition-colors"
        >
          儲存並完成
        </button>
        <button
          type="button"
          onClick={() => router.push("/dashboard")}
          className="bg-white border border-[#d0d0d0] text-[#1a1a1a] rounded-xl px-7 py-[14px] text-[14.5px] font-medium hover:bg-[#f5f5f5] transition-colors"
        >
          稍後填寫
        </button>
      </div>
    </div>
  );
}
