type HostMessage =
  | { type: "connect"; clientSecret: string }
  | { type: "mute"; muted: boolean }
  | { type: "tool_result"; callId: string; output: string }
  | { type: "tool_interrupted"; callId: string; output: string }
  | { type: "cancel_response" }
  | { type: "disconnect" };

type RealtimeEvent = Record<string, unknown> & { type?: string };

declare global {
  interface Window {
    chrome: {
      webview: {
        addEventListener(type: "message", listener: (event: MessageEvent<HostMessage>) => void): void;
        postMessage(message: unknown): void;
      };
    };
  }
}

let peer: RTCPeerConnection | null = null;
let channel: RTCDataChannel | null = null;
let microphone: MediaStream | null = null;
let outputAudio: HTMLAudioElement | null = null;
let assistantTranscript = "";
let responseActive = false;

const post = (message: unknown): void => window.chrome.webview.postMessage(message);
const send = (event: unknown): void => {
  if (channel?.readyState !== "open") throw new Error("Realtime data channel is not open.");
  channel.send(JSON.stringify(event));
};

async function connect(clientSecret: string): Promise<void> {
  disconnect(false);
  const nextPeer = new RTCPeerConnection();
  peer = nextPeer;
  outputAudio = document.createElement("audio");
  outputAudio.autoplay = true;
  nextPeer.ontrack = event => {
    if (outputAudio) outputAudio.srcObject = event.streams[0] ?? null;
  };

  microphone = await navigator.mediaDevices.getUserMedia({ audio: { echoCancellation: true, noiseSuppression: true } });
  const track = microphone.getAudioTracks()[0];
  if (!track) throw new Error("No microphone audio track is available.");
  nextPeer.addTrack(track, microphone);

  const nextChannel = nextPeer.createDataChannel("oai-events");
  channel = nextChannel;
  nextChannel.addEventListener("open", () => post({ type: "connected" }));
  nextChannel.addEventListener("message", event => handleRealtimeEvent(JSON.parse(String(event.data)) as RealtimeEvent));
  nextChannel.addEventListener("close", () => post({ type: "disconnected" }));

  const offer = await nextPeer.createOffer();
  await nextPeer.setLocalDescription(offer);
  const response = await fetch("https://api.openai.com/v1/realtime/calls", {
    method: "POST",
    body: offer.sdp,
    headers: { Authorization: `Bearer ${clientSecret}`, "Content-Type": "application/sdp" }
  });
  if (!response.ok) throw new Error(`Realtime connection failed with HTTP ${response.status}.`);
  await nextPeer.setRemoteDescription({ type: "answer", sdp: await response.text() });
}

function handleRealtimeEvent(event: RealtimeEvent): void {
  switch (event.type) {
    case "input_audio_buffer.speech_started":
      assistantTranscript = "";
      post({ type: "speech_started" });
      break;
    case "conversation.item.input_audio_transcription.completed":
      post({ type: "transcript", role: "user", text: String(event.transcript ?? ""), final: true });
      break;
    case "response.output_audio_transcript.delta":
    case "response.audio_transcript.delta":
      assistantTranscript += String(event.delta ?? "");
      post({ type: "transcript", role: "assistant", text: assistantTranscript, final: false });
      break;
    case "response.output_audio_transcript.done":
    case "response.audio_transcript.done":
      assistantTranscript = String(event.transcript ?? assistantTranscript);
      post({ type: "transcript", role: "assistant", text: assistantTranscript, final: true });
      break;
    case "response.done":
      responseActive = false;
      emitToolCalls(event);
      break;
    case "response.created":
      responseActive = true;
      break;
    case "error":
      post({ type: "error" });
      break;
  }
}

function emitToolCalls(event: RealtimeEvent): void {
  const response = isRecord(event.response) ? event.response : null;
  const output = Array.isArray(response?.output) ? response.output : [];
  for (const item of output) {
    if (!isRecord(item) || item.type !== "function_call") continue;
    post({
      type: "tool_call",
      name: String(item.name ?? ""),
      callId: String(item.call_id ?? ""),
      arguments: String(item.arguments ?? "{}")
    });
  }
}

function sendToolResult(callId: string, output: string, createResponse: boolean): void {
  send({ type: "conversation.item.create", item: { type: "function_call_output", call_id: callId, output } });
  if (createResponse) send({ type: "response.create" });
}

function setMuted(muted: boolean): void {
  for (const track of microphone?.getAudioTracks() ?? []) track.enabled = !muted;
}

function disconnect(notify = true): void {
  for (const track of microphone?.getTracks() ?? []) track.stop();
  microphone = null;
  channel?.close();
  channel = null;
  peer?.close();
  peer = null;
  if (outputAudio) outputAudio.srcObject = null;
  outputAudio = null;
  if (notify) post({ type: "disconnected" });
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

window.chrome.webview.addEventListener("message", event => {
  const message = event.data;
  try {
    switch (message.type) {
      case "connect": void connect(message.clientSecret).catch(() => post({ type: "error" })); break;
      case "mute": setMuted(message.muted); break;
      case "tool_result": sendToolResult(message.callId, message.output, true); break;
      case "tool_interrupted": sendToolResult(message.callId, message.output, false); break;
      case "cancel_response": if (channel?.readyState === "open" && responseActive) send({ type: "response.cancel" }); break;
      case "disconnect": disconnect(); break;
    }
  } catch {
    post({ type: "error" });
  }
});

post({ type: "ready" });

export {};
