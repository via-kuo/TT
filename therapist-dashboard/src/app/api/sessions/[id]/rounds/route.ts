import { NextRequest, NextResponse } from "next/server";
import sql from "@/lib/db";
import { getSession } from "@/lib/session";

export async function GET(_req: NextRequest, { params }: { params: Promise<{ id: string }> }) {
  const session = await getSession();
  if (!session) return NextResponse.json({ error: "未登入" }, { status: 401 });

  const { id } = await params;

  const rounds = await sql`
    SELECT id, session_id, round_number, response_time, emotion, generated_scene, patient_response
    FROM rounds
    WHERE session_id = ${parseInt(id)}
    ORDER BY round_number ASC
  `;

  return NextResponse.json(rounds.map(r => ({
    id: r.id.toString(),
    sessionId: id,
    roundNumber: r.round_number,
    type: "回合",
    duration: r.response_time ?? 0,
    sceneName: r.generated_scene ?? "",
    content: r.patient_response ?? "",
    emotion: r.emotion ?? "—",
    sceneImage: null,
    exchanges: [],
  })));
}
