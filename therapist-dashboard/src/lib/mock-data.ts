import type { Therapist, Case, Session, SessionRound, ActiveSession } from "./types";

export const mockTherapist: Therapist = {
  id: "t1",
  name: "林治療師",
  institution: "○○照護機構",
  email: "lin@care.com",
};

export const mockCases: Case[] = [
  {
    id: "c1",
    name: "王伯伯",
    surname: "王",
    age: 86,
    gender: "male",
    avatarColor: "#d4e4f7",
    lastSession: "2024.12.10",
    totalSessions: 5,
    tabooTopics: ["家人離世", "戰爭細節"],
    notes: "輕度認知障礙，對日治時期記憶鮮明，喜歡談論工廠生活。",
    birthYear: "1938",
    birthPlace: "彰化縣",
    career: "紡織廠工人 30 年",
    family: "太太秀英、三個孩子",
    hobbies: "爌肉飯、唱會、演講",
    mode: "輕度模式",
    isActive: true,
  },
  {
    id: "c2",
    name: "張美麗",
    surname: "張",
    age: 74,
    gender: "female",
    avatarColor: "#f7e4d4",
    lastSession: "2024.12.08",
    totalSessions: 1,
    tabooTopics: ["喪偶"],
    notes: "情緒穩定，喜歡談論年輕時的裁縫經歷。",
    birthYear: "1950",
    birthPlace: "台南市",
    career: "裁縫師 20 年",
    family: "兩個女兒",
    hobbies: "種花、看台語劇",
    mode: "輕度模式",
  },
];

export const mockSessions: Session[] = [
  {
    id: "s1",
    caseId: "c1",
    date: "2024.12.10",
    duration: 45,
    status: "completed",
    rounds: 3,
    emotionSummary: "整體穩定，第三回合有短暫激動",
    notes: "王伯伯今日狀態良好，主動分享工廠往事。",
    mode: "輕度模式",
    score: 17,
    totalScore: 20,
    rating: "優良",
    sessionNumber: 5,
    overallEmotion: "適當",
    averageResponseTime: "42s",
    storySummary: "養育的台中紡織廠，你和阿明往廟口走去。廟埕還有人唱歌仔戲，香煙的味道還沒散......",
  },
  {
    id: "s2",
    caseId: "c1",
    date: "2024.12.03",
    duration: 38,
    status: "completed",
    rounds: 3,
    emotionSummary: "穩定",
    notes: "回憶廟口生活，情緒愉快。",
    mode: "中度模式",
    score: 14,
    totalScore: 20,
    rating: "普通",
    sessionNumber: 4,
    overallEmotion: "適當",
    averageResponseTime: "38s",
    storySummary: "廟口的夜晚，燈籠的光打在石板路上，你和老朋友們圍著攤子說笑......",
  },
  {
    id: "s3",
    caseId: "c1",
    date: "2024.11.26",
    duration: 42,
    status: "completed",
    rounds: 3,
    emotionSummary: "前半穩定，後半略顯疲倦",
    notes: "第三回合王伯伯表示疲倦，提前結束。",
    mode: "輕度模式",
    score: 15,
    totalScore: 20,
    rating: "普通",
    sessionNumber: 3,
    overallEmotion: "適當",
    averageResponseTime: "45s",
    storySummary: "紡織廠的機器聲響，梭子來回穿梭，空氣中飄著棉絮的氣味......",
  },
  {
    id: "s4",
    caseId: "c2",
    date: "2024.12.08",
    duration: 40,
    status: "completed",
    rounds: 3,
    emotionSummary: "整體愉快，主動分享裁縫往事",
    notes: "張美麗今日狀態極佳，詳細描述了年輕時為婚禮縫製旗袍的經歷。",
    mode: "輕度模式",
    score: 18,
    totalScore: 20,
    rating: "優良",
    sessionNumber: 1,
    overallEmotion: "適當",
    averageResponseTime: "35s",
    storySummary: "縫衣機嗡嗡作響，你細心地為新娘子量身，絲綢在手中滑過......",
  },
];

export const mockSessionRounds: SessionRound[] = [
  // s1 rounds
  { id: "r1-0", sessionId: "s1", type: "心得", duration: 38, sceneName: "心得分享", content: "不錯有想起回憶", emotion: "適當" },
  {
    id: "r1-1", sessionId: "s1", type: "回合", roundNumber: 1, duration: 38, sceneName: "紡織廠童事場景", content: "「去廟口找阿明」", emotion: "適當",
    sceneImage: "/image/housephoto.png",
    exchanges: [
      { questionNumber: 1, question: "這個場景你認得嗎？", answer: "這是工廠附近啊，我以前住這邊" },
      { questionNumber: 2, question: "工廠生活是怎麼樣的？", answer: "很忙，機器很吵，但我們感情好" },
      { questionNumber: 3, question: "有沒有印象深刻的同事？" },
    ],
  },
  {
    id: "r1-2", sessionId: "s1", type: "回合", roundNumber: 2, duration: 51, sceneName: "廟口夜市場景", content: "「坐下來聽戲」", emotion: "焦躁",
    sceneImage: "/image/housephoto.png",
    exchanges: [
      { questionNumber: 1, question: "這個地方你熟悉嗎？", answer: "廟口嘛，我常來，買碗粿吃" },
      { questionNumber: 2, question: "平常下班都來這邊嗎？", answer: "對啊，跟阿明一起" },
      { questionNumber: 3, question: "這邊有什麼讓你印象深刻的？", answer: "戲台旁邊的滷肉飯最好吃" },
    ],
  },
  {
    id: "r1-3", sessionId: "s1", type: "回合", roundNumber: 3, duration: 34, sceneName: "宿舍生活場景", content: "「跟阿明下棋」", emotion: "亢奮",
    sceneImage: "/image/housephoto.png",
    exchanges: [
      { questionNumber: 1, question: "這個房間你住過嗎？", answer: "像我們宿舍，六個人住" },
      { questionNumber: 2, question: "宿舍生活開心嗎？" },
      { questionNumber: 3, question: "有沒有跟同事下棋的習慣？", answer: "跟阿明常下棋，他棋很高" },
    ],
  },
  // s2 rounds
  { id: "r2-0", sessionId: "s2", type: "心得", duration: 30, sceneName: "心得分享", content: "感覺很親切", emotion: "適當" },
  {
    id: "r2-1", sessionId: "s2", type: "回合", roundNumber: 1, duration: 42, sceneName: "廟口場景", content: "「買碗粿來吃」", emotion: "適當",
    sceneImage: "/image/housephoto.png",
    exchanges: [
      { questionNumber: 1, question: "這裡是哪裡呢？", answer: "廟口啊，以前常來" },
      { questionNumber: 2, question: "來這邊通常都做什麼？", answer: "買東西吃，跟朋友聊天" },
      { questionNumber: 3, question: "最喜歡吃什麼？", answer: "碗粿，那邊的最好吃" },
    ],
  },
  {
    id: "r2-2", sessionId: "s2", type: "回合", roundNumber: 2, duration: 41, sceneName: "夜市場景", content: "「和朋友聊天」", emotion: "低落",
    sceneImage: "/image/housephoto.png",
    exchanges: [
      { questionNumber: 1, question: "夜市這邊你去過嗎？", answer: "去過啊，年輕的時候" },
      { questionNumber: 2, question: "通常跟誰一起去？" },
      { questionNumber: 3, question: "夜市有什麼好吃的嗎？", answer: "肉圓很好吃，還有烤玉米" },
    ],
  },
  {
    id: "r2-3", sessionId: "s2", type: "回合", roundNumber: 3, duration: 40, sceneName: "回家路上場景", content: "「走路回宿舍」", emotion: "適當",
    sceneImage: "/image/housephoto.png",
    exchanges: [
      { questionNumber: 1, question: "這條路你走過嗎？", answer: "走過啊，下班都走這邊" },
      { questionNumber: 2, question: "走路回去要多久？", answer: "大概十幾分鐘" },
      { questionNumber: 3, question: "路上會遇到什麼人嗎？", answer: "同事啊，大家一起走" },
    ],
  },
  // s3 rounds
  { id: "r3-0", sessionId: "s3", type: "心得", duration: 33, sceneName: "心得分享", content: "有點累但還好", emotion: "適當" },
  {
    id: "r3-1", sessionId: "s3", type: "回合", roundNumber: 1, duration: 48, sceneName: "紡織廠場景", content: "「顧機器」", emotion: "低落",
    sceneImage: "/image/housephoto.png",
    exchanges: [
      { questionNumber: 1, question: "這台機器你操作過嗎？", answer: "操作過啊，我顧了幾十年" },
      { questionNumber: 2, question: "顧機器辛苦嗎？", answer: "很辛苦，站一整天" },
      { questionNumber: 3, question: "那時候薪水怎麼樣？" },
    ],
  },
  {
    id: "r3-2", sessionId: "s3", type: "回合", roundNumber: 2, duration: 50, sceneName: "午休場景", content: "「吃便當」", emotion: "適當",
    sceneImage: "/image/housephoto.png",
    exchanges: [
      { questionNumber: 1, question: "午休的時候都在哪裡休息？", answer: "就在廠裡，有個小角落" },
      { questionNumber: 2, question: "便當是自己帶的嗎？", answer: "老婆幫我準備的，很好吃" },
      { questionNumber: 3, question: "午休時間夠不夠休息？", answer: "夠啦，睡個小覺就好" },
    ],
  },
  {
    id: "r3-3", sessionId: "s3", type: "回合", roundNumber: 3, duration: 44, sceneName: "廠區場景", content: "「巡視機台」", emotion: "焦躁",
    sceneImage: "/image/housephoto.png",
    exchanges: [
      { questionNumber: 1, question: "你在廠區做什麼工作？", answer: "我負責巡視機台，看有沒有問題" },
      { questionNumber: 2, question: "有沒有發生過什麼意外？" },
      { questionNumber: 3, question: "廠裡還有其他工作嗎？", answer: "有啊，很多種，大家輪流做" },
    ],
  },
  // s4 rounds
  { id: "r4-0", sessionId: "s4", type: "心得", duration: 28, sceneName: "心得分享", content: "好像回到年輕時", emotion: "適當" },
  {
    id: "r4-1", sessionId: "s4", type: "回合", roundNumber: 1, duration: 36, sceneName: "裁縫店場景", content: "「量旗袍的尺寸」", emotion: "亢奮",
    sceneImage: "/image/housephoto.png",
    exchanges: [
      { questionNumber: 1, question: "這是裁縫店嗎？你在這裡工作過？", answer: "對啊，這是我年輕時開的店" },
      { questionNumber: 2, question: "旗袍難做嗎？", answer: "很難，要量很多地方，要很精準" },
      { questionNumber: 3, question: "一件旗袍要做多久？", answer: "要幾天，有時候客人急就熬夜做" },
    ],
  },
  {
    id: "r4-2", sessionId: "s4", type: "回合", roundNumber: 2, duration: 40, sceneName: "婚禮場景", content: "「新娘穿上了旗袍」", emotion: "適當",
    sceneImage: "/image/housephoto.png",
    exchanges: [
      { questionNumber: 1, question: "這個婚禮場景讓你想到什麼？", answer: "我以前幫很多新娘做旗袍" },
      { questionNumber: 2, question: "新娘穿上你做的旗袍是什麼感覺？", answer: "很開心，覺得自己做的東西有用" },
      { questionNumber: 3, question: "有沒有特別難忘的婚禮？" },
    ],
  },
  {
    id: "r4-3", sessionId: "s4", type: "回合", roundNumber: 3, duration: 37, sceneName: "市場場景", content: "「選布料」", emotion: "焦躁",
    sceneImage: "/image/housephoto.png",
    exchanges: [
      { questionNumber: 1, question: "你在市場買過布料嗎？", answer: "常常來，選布料是最重要的" },
      { questionNumber: 2, question: "怎麼挑選好的布料？", answer: "要摸手感，看顏色有沒有均勻" },
      { questionNumber: 3, question: "最喜歡什麼樣的布料？", answer: "絲綢最好，但也最貴" },
    ],
  },
];

export const mockActiveSession: ActiveSession = {
  sessionId: "s-live-1",
  caseId: "c1",
  caseName: "王伯伯",
  currentRound: 1,
  totalRounds: 3,
  status: "running",
  currentScene:
    "廠門口的風吹來，你和阿明沿著路走向廟口。香煙的味道還沒散，廟埕旁邊有人在唱歌仔戲——你在廟口通常都做什麼？",
  elderResponse: "",
  emotionState: "適當",
  responseTime: "4s",
  aiSuggestions: [
    "「王伯伯，你在廠裡住宿舍嗎？跟幾個人同住？」",
    "「那時候宿舍的同事，有沒有特別要好的朋友？」",
    "「你們下班之後通常都去哪裡吃東西？」",
  ],
  tabooTopics: ["家人離世", "戰爭細節"],
};
