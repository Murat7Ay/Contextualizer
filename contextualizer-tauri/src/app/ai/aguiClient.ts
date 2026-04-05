export interface AgUiEvent {
  type: string;
  [key: string]: unknown;
}

export interface AgUiRunInput {
  thread_id: string;
  run_id: string;
  messages: Array<{ role: string; content: string }>;
  tools?: Array<{ name: string; description: string }>;
  context?: Array<{ key: string; value: unknown }>;
}

export async function* streamAgUiEvents(
  endpoint: string,
  input: AgUiRunInput,
  signal?: AbortSignal,
): AsyncGenerator<AgUiEvent> {
  const response = await fetch(endpoint, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
    signal,
  });

  if (!response.ok) {
    throw new Error(`AG-UI request failed: ${response.status}`);
  }

  const reader = response.body?.getReader();
  if (!reader) throw new Error("No response body");

  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split("\n");
    buffer = lines.pop() ?? "";

    for (const line of lines) {
      const trimmed = line.trim();
      if (trimmed.startsWith("data:")) {
        const jsonStr = trimmed.slice(5).trim();
        if (jsonStr) {
          try {
            yield JSON.parse(jsonStr) as AgUiEvent;
          } catch {
            // skip malformed events
          }
        }
      }
    }
  }
}

export function createAgUiRunInput(
  threadId: string,
  message: string,
): AgUiRunInput {
  return {
    thread_id: threadId,
    run_id: crypto.randomUUID(),
    messages: [{ role: "user", content: message }],
    tools: [],
    context: [],
  };
}
