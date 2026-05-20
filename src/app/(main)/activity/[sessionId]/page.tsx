"use client";

import { useState, useEffect } from "react";
import { mockSessions, mockSessionRounds, mockCases } from "@/lib/mock-data";
import type { Session, SessionRound, Case } from "@/lib/types";
import { LiveSessionView } from "./_components/LiveSessionView";
import { HistorySessionView } from "./_components/HistorySessionView";

export default function ActivityPage({ params }: { params: Promise<{ sessionId: string }> }) {
  const [ready, setReady] = useState(false);
  const [completedSession, setCompletedSession] = useState<Session | null>(null);
  const [caseData, setCaseData] = useState<Case | null>(null);
  const [rounds, setRounds] = useState<SessionRound[]>([]);

  useEffect(() => {
    params.then(({ sessionId }) => {
      const session = mockSessions.find((s) => s.id === sessionId) ?? null;
      setCompletedSession(session);
      if (session) {
        setCaseData(mockCases.find((c) => c.id === session.caseId) ?? null);
        setRounds(mockSessionRounds.filter((r) => r.sessionId === sessionId));
      }
      setReady(true);
    });
  }, [params]);

  if (!ready) return null;

  if (completedSession && caseData) {
    return <HistorySessionView session={completedSession} caseData={caseData} rounds={rounds} />;
  }

  return <LiveSessionView />;
}
